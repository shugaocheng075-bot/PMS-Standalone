using Microsoft.AspNetCore.Mvc;
using PMS.API.Models;
using PMS.Application.Contracts.Contract;
using PMS.Application.Contracts.Handover;
using PMS.Application.Contracts.Inspection;
using PMS.Application.Models.Contract;
using PMS.Application.Models.Handover;
using PMS.Application.Models.Inspection;

namespace PMS.API.Controllers.Dashboard;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(
    IContractAlertService contractAlertService,
    IHandoverService handoverService,
    IInspectionService inspectionService) : ControllerBase
{
    [HttpGet("v2")]
    public async Task<IActionResult> GetV2(
        [FromQuery] string? source,
        [FromQuery] string? level,
        [FromQuery] string? keyword,
        [FromQuery] string? owner,
        [FromQuery] int months = 6,
        CancellationToken cancellationToken = default)
    {
        var monthSpan = Math.Clamp(months, 1, 12);

        var contractTask = contractAlertService.QueryAlertsAsync(new ContractAlertQuery
        {
            Page = 1,
            Size = 5000
        }, cancellationToken);

        var handoverTask = handoverService.QueryAsync(new HandoverQuery
        {
            Page = 1,
            Size = 5000
        }, cancellationToken);

        var inspectionTask = inspectionService.QueryAsync(new InspectionQuery
        {
            Page = 1,
            Size = 5000
        }, cancellationToken);

        await Task.WhenAll(contractTask, handoverTask, inspectionTask);

        var now = DateTime.Today;
        var events = new List<DashboardAlertEvent>();
        events.AddRange(MapContract(contractTask.Result.Items, now));
        events.AddRange(MapHandover(handoverTask.Result.Items, now));
        events.AddRange(MapInspection(inspectionTask.Result.Items, now));

        if (!string.IsNullOrWhiteSpace(source))
        {
            var normalized = source.Trim();
            events = events
                .Where(x => x.Source.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(level))
        {
            var normalized = level.Trim();
            events = events
                .Where(x => x.Level.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalized = keyword.Trim();
            events = events
                .Where(x =>
                    x.HospitalName.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                    || x.Title.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                    || x.Detail.Contains(normalized, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(owner))
        {
            var normalized = owner.Trim();
            events = events
                .Where(x => x.Owner.Contains(normalized, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var startMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-(monthSpan - 1));
        var trendSource = events
            .Where(x => x.EventDate >= startMonth)
            .ToList();

        var trend = BuildTrend(trendSource, startMonth, monthSpan);
        var sourceDistribution = BuildSourceDistribution(events);
        var ownerWorkload = BuildOwnerWorkload(events);

        var summary = new
        {
            total = events.Count,
            severe = events.Count(x => x.Level == "严重"),
            warning = events.Count(x => x.Level == "警告"),
            reminder = events.Count(x => x.Level == "提醒")
        };

        return Ok(ApiResponse<object>.Success(new
        {
            summary,
            trend,
            sourceDistribution,
            ownerWorkload
        }));
    }

    private static IReadOnlyList<DashboardAlertEvent> MapContract(IReadOnlyList<ContractAlertItemDto> items, DateTime today)
    {
        return items.Select(x =>
        {
            var level = NormalizeLevel(x.AlertLevel);
            var overdueDays = x.OverdueDays < 0 ? 0 : x.OverdueDays;
            var eventDate = today.AddDays(-overdueDays);
            return new DashboardAlertEvent
            {
                Source = "合同",
                Level = level,
                HospitalName = x.HospitalName,
                Title = $"合同{level}：{x.HospitalName}",
                Detail = $"状态：{x.ContractStatus}，超期：{x.OverdueDays} 天",
                Owner = x.SalesName,
                EventDate = eventDate
            };
        }).ToList();
    }

    private static IReadOnlyList<DashboardAlertEvent> MapHandover(IReadOnlyList<HandoverItemDto> items, DateTime today)
    {
        return items
            .Where(x => !string.Equals(x.Stage, "已交接", StringComparison.OrdinalIgnoreCase))
            .Select(x =>
            {
                var level = ResolveHandoverLevel(x.Stage);
                return new DashboardAlertEvent
                {
                    Source = "交接",
                    Level = level,
                    HospitalName = x.HospitalName,
                    Title = $"交接待办：{x.HospitalName}",
                    Detail = $"阶段：{x.Stage}，接收人：{x.ToOwner}",
                    Owner = x.ToOwner,
                    EventDate = x.EmailSentDate?.Date ?? today
                };
            })
            .ToList();
    }

    private static IReadOnlyList<DashboardAlertEvent> MapInspection(IReadOnlyList<InspectionPlanItemDto> items, DateTime today)
    {
        return items
            .Where(x => !string.Equals(x.Status, "已完成", StringComparison.OrdinalIgnoreCase))
            .Select(x =>
            {
                var overdueDays = x.PlanDate.Date < today ? (today - x.PlanDate.Date).Days : 0;
                var level = overdueDays > 7
                    ? "严重"
                    : overdueDays > 0
                        ? "警告"
                        : "提醒";

                return new DashboardAlertEvent
                {
                    Source = "巡检",
                    Level = level,
                    HospitalName = x.HospitalName,
                    Title = $"巡检待办：{x.HospitalName}",
                    Detail = $"计划日期：{x.PlanDate:yyyy-MM-dd}，状态：{x.Status}",
                    Owner = x.Inspector,
                    EventDate = x.PlanDate.Date
                };
            })
            .ToList();
    }

    private static IReadOnlyList<object> BuildTrend(IReadOnlyList<DashboardAlertEvent> source, DateTime startMonth, int months)
    {
        var result = new List<object>(months);
        for (var i = 0; i < months; i++)
        {
            var month = startMonth.AddMonths(i);
            var list = source
                .Where(x => x.EventDate.Year == month.Year && x.EventDate.Month == month.Month)
                .ToList();

            result.Add(new
            {
                month = month.ToString("yyyy-MM"),
                total = list.Count,
                severe = list.Count(x => x.Level == "严重"),
                warning = list.Count(x => x.Level == "警告"),
                reminder = list.Count(x => x.Level == "提醒"),
                contract = list.Count(x => x.Source == "合同"),
                handover = list.Count(x => x.Source == "交接"),
                inspection = list.Count(x => x.Source == "巡检")
            });
        }

        return result;
    }

    private static IReadOnlyList<object> BuildSourceDistribution(IReadOnlyList<DashboardAlertEvent> source)
    {
        var all = new[] { "合同", "交接", "巡检" };
        return all.Select(name => new
        {
            source = name,
            count = source.Count(x => x.Source == name),
            total = source.Count(x => x.Source == name),
            severe = source.Count(x => x.Source == name && x.Level == "严重"),
            warning = source.Count(x => x.Source == name && x.Level == "警告"),
            reminder = source.Count(x => x.Source == name && x.Level == "提醒")
        }).ToList();
    }

    private static IReadOnlyList<object> BuildOwnerWorkload(IReadOnlyList<DashboardAlertEvent> source)
    {
        return source
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Owner) ? "未分配" : x.Owner.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                owner = group.Key,
                total = group.Count(),
                severe = group.Count(x => x.Level == "严重"),
                warning = group.Count(x => x.Level == "警告"),
                reminder = group.Count(x => x.Level == "提醒")
            })
            .OrderByDescending(x => x.total)
            .ThenByDescending(x => x.severe)
            .ThenBy(x => x.owner, StringComparer.Ordinal)
            .Take(12)
            .Cast<object>()
            .ToList();
    }

    private static string ResolveHandoverLevel(string stage)
    {
        if (string.Equals(stage, "未发", StringComparison.OrdinalIgnoreCase))
        {
            return "严重";
        }

        if (string.Equals(stage, "已发邮件", StringComparison.OrdinalIgnoreCase))
        {
            return "警告";
        }

        return "提醒";
    }

    private static string NormalizeLevel(string value)
    {
        if (value.Contains("严重", StringComparison.OrdinalIgnoreCase))
        {
            return "严重";
        }

        if (value.Contains("警告", StringComparison.OrdinalIgnoreCase))
        {
            return "警告";
        }

        return "提醒";
    }

    private class DashboardAlertEvent
    {
        public string Source { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string HospitalName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
    }
}