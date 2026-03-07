using Microsoft.AspNetCore.Mvc;
using PMS.API.Models;
using PMS.Application.Contracts.Contract;
using PMS.Application.Contracts.Handover;
using PMS.Application.Contracts.Inspection;
using PMS.Application.Models.Contract;
using PMS.Application.Models.Handover;
using PMS.Application.Models.Inspection;

namespace PMS.API.Controllers.Alert;

[ApiController]
[Route("api/alerts/center")]
public class AlertCenterController(
    IContractAlertService contractAlertService,
    IHandoverService handoverService,
    IInspectionService inspectionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? source,
        [FromQuery] string? level,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
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

        var alerts = new List<AlertCenterItemDto>();
        alerts.AddRange(MapContractAlerts(contractTask.Result.Items));
        alerts.AddRange(MapHandoverAlerts(handoverTask.Result.Items));
        alerts.AddRange(MapInspectionAlerts(inspectionTask.Result.Items));

        if (!string.IsNullOrWhiteSpace(source))
        {
            var normalizedSource = source.Trim();
            alerts = alerts.Where(x => x.Source.Equals(normalizedSource, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(level))
        {
            var normalizedLevel = level.Trim();
            alerts = alerts.Where(x => x.Level.Equals(normalizedLevel, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            alerts = alerts.Where(x =>
                    x.Title.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase)
                    || x.Detail.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase)
                    || x.Owner.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase)
                    || x.HospitalName.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        alerts = alerts
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.OverdueDays)
            .ThenBy(x => x.HospitalName)
            .ToList();

        var normalizedPage = page < 1 ? 1 : page;
        var normalizedSize = size <= 0 ? 20 : size;
        var total = alerts.Count;
        var items = alerts
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .ToList();

        var summary = new
        {
            total,
            severe = alerts.Count(x => x.Level == "严重"),
            warning = alerts.Count(x => x.Level == "警告"),
            reminder = alerts.Count(x => x.Level == "提醒"),
            contract = alerts.Count(x => x.Source == "合同"),
            handover = alerts.Count(x => x.Source == "交接"),
            inspection = alerts.Count(x => x.Source == "巡检")
        };

        return Ok(ApiResponse<object>.Success(new
        {
            items,
            total,
            page = normalizedPage,
            size = normalizedSize,
            summary
        }));
    }

    private static IReadOnlyList<AlertCenterItemDto> MapContractAlerts(IReadOnlyList<ContractAlertItemDto> items)
    {
        return items.Select(x => new AlertCenterItemDto
        {
            Id = $"contract-{x.ProjectId}",
            Source = "合同",
            Level = NormalizeLevel(x.AlertLevel),
            Priority = LevelPriority(NormalizeLevel(x.AlertLevel)),
            HospitalName = x.HospitalName,
            Title = $"合同{NormalizeLevel(x.AlertLevel)}：{x.HospitalName}",
            Detail = $"状态：{x.ContractStatus}，超期：{x.OverdueDays} 天",
            Owner = x.SalesName,
            OverdueDays = x.OverdueDays,
            RelatedPath = "/contract/alerts",
            RelatedQuery = new Dictionary<string, string>
            {
                ["alertLevel"] = x.AlertLevel,
                ["hospitalName"] = x.HospitalName,
                ["groupName"] = x.GroupName,
                ["salesName"] = x.SalesName,
                ["contractStatus"] = x.ContractStatus
            }
        }).ToList();
    }

    private static IReadOnlyList<AlertCenterItemDto> MapHandoverAlerts(IReadOnlyList<HandoverItemDto> items)
    {
        return items
            .Where(x => !string.Equals(x.Stage, "已交接", StringComparison.OrdinalIgnoreCase))
            .Select(x =>
            {
                var level = ResolveHandoverLevel(x.Stage);
                return new AlertCenterItemDto
                {
                    Id = $"handover-{x.Id}",
                    Source = "交接",
                    Level = level,
                    Priority = LevelPriority(level),
                    HospitalName = x.HospitalName,
                    Title = $"交接待办：{x.HospitalName}",
                    Detail = $"阶段：{x.Stage}，接收人：{x.ToOwner}",
                    Owner = x.ToOwner,
                    OverdueDays = 0,
                    RelatedPath = "/handover/list",
                    RelatedQuery = new Dictionary<string, string>
                    {
                        ["stage"] = x.Stage,
                        ["fromGroup"] = x.FromGroup,
                        ["toOwner"] = x.ToOwner,
                        ["hospitalName"] = x.HospitalName,
                        ["productName"] = x.ProductName,
                        ["action"] = "detail",
                        ["id"] = x.Id.ToString()
                    }
                };
            })
            .ToList();
    }

    private static IReadOnlyList<AlertCenterItemDto> MapInspectionAlerts(IReadOnlyList<InspectionPlanItemDto> items)
    {
        var today = DateTime.Today;
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

                return new AlertCenterItemDto
                {
                    Id = $"inspection-{x.Id}",
                    Source = "巡检",
                    Level = level,
                    Priority = LevelPriority(level),
                    HospitalName = x.HospitalName,
                    Title = $"巡检待办：{x.HospitalName}",
                    Detail = $"计划日期：{x.PlanDate:yyyy-MM-dd}，状态：{x.Status}",
                    Owner = x.Inspector,
                    OverdueDays = overdueDays,
                    RelatedPath = "/inspection/plan",
                    RelatedQuery = new Dictionary<string, string>
                    {
                        ["status"] = x.Status,
                        ["groupName"] = x.GroupName,
                        ["inspector"] = x.Inspector,
                        ["hospitalName"] = x.HospitalName,
                        ["productName"] = x.ProductName,
                        ["action"] = "detail",
                        ["id"] = x.Id.ToString()
                    }
                };
            })
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

    private static int LevelPriority(string level)
    {
        return level switch
        {
            "严重" => 3,
            "警告" => 2,
            _ => 1
        };
    }

    public class AlertCenterItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string HospitalName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public int OverdueDays { get; set; }
        public string RelatedPath { get; set; } = string.Empty;
        public Dictionary<string, string> RelatedQuery { get; set; } = [];
    }
}
