using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.Contract;
using PMS.Application.Contracts.Handover;
using PMS.Application.Contracts.Inspection;
using PMS.Application.Contracts.Personnel;
using PMS.Application.Contracts.RepairRecord;
using PMS.Application.Models.Contract;
using PMS.Application.Models.Handover;
using PMS.Application.Models.Inspection;
using PMS.Application.Models.Personnel;
using PMS.Application.Models.RepairRecord;
using PMS.Infrastructure.Services;

namespace PMS.API.Controllers.Dashboard;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(
    IContractAlertService contractAlertService,
    IHandoverService handoverService,
    IInspectionService inspectionService,
    IPersonnelService personnelService,
    IRepairRecordService repairRecordService,
    IAccessControlService accessControlService) : ControllerBase
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

        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);

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

        // 数据范围过滤：非 manager 角色只看到自己负责的医院数据
        events = HospitalScopeHelper.FilterByHospitalScope(
            dataScope, events, x => x.HospitalName).ToList();

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

    /// <summary>
    /// Dashboard v3 — 5 大 KPI 指标
    /// </summary>
    [HttpGet("v3")]
    public async Task<IActionResult> GetV3(CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);

        var projects = HospitalScopeHelper.FilterByHospitalScope(
            dataScope, InMemoryProjectDataStore.Projects, x => x.HospitalName).ToList();

        // 1. 合同状态占比
        var statusGroups = projects
            .GroupBy(x => string.IsNullOrWhiteSpace(x.ContractStatus) ? "未知" : x.ContractStatus)
            .Select(g => new { status = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToList();

        var contractStatusDistribution = new
        {
            total = projects.Count,
            items = statusGroups
        };

        // 2. 医院产品覆盖率
        var distinctHospitals = projects.Select(x => x.HospitalName).Distinct().Count();
        var distinctProducts = projects.Select(x => x.ProductName).Distinct().Count();
        var hospitalProductPairs = projects
            .Select(x => $"{x.HospitalName}||{x.ProductName}")
            .Distinct()
            .Count();

        var hospitalProductCoverage = new
        {
            hospitalCount = distinctHospitals,
            productCount = distinctProducts,
            coveragePairs = hospitalProductPairs,
            avgProductsPerHospital = distinctHospitals > 0
                ? Math.Round((double)hospitalProductPairs / distinctHospitals, 2)
                : 0
        };

        // 3. 报修处理率
        var repairResult = await repairRecordService.QueryAsync(new RepairRecordQuery
        {
            Page = 1,
            Size = 50000
        }, cancellationToken);

        var scopedRepairs = HospitalScopeHelper.FilterByHospitalScope(
            dataScope, repairResult.Items, x => x.HospitalName).ToList();
        var repairTotal = scopedRepairs.Count;
        var repairCompleted = scopedRepairs.Count(x =>
            string.Equals(x.Status, "已完成", StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.Status, "已关闭", StringComparison.OrdinalIgnoreCase));

        var repairProcessingRate = new
        {
            total = repairTotal,
            completed = repairCompleted,
            rate = repairTotal > 0
                ? Math.Round((double)repairCompleted / repairTotal * 100, 1)
                : 0.0
        };

        // 4. 人员负载率
        var personnelResult = await personnelService.QueryAsync(new PersonnelQuery
        {
            Page = 1,
            Size = 5000
        }, cancellationToken);

        var totalPersonnel = personnelResult.Items.Count;
        var workHoursSnapshot = HospitalScopeHelper.FilterByHospitalScope(
            dataScope, InMemoryWorkHoursService.GetSnapshot(), x => x.HospitalName).ToList();
        var personnelWithHours = workHoursSnapshot
            .Select(x => x.PersonnelName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var personnelLoadRate = new
        {
            totalPersonnel,
            activePersonnel = personnelWithHours,
            rate = totalPersonnel > 0
                ? Math.Round((double)personnelWithHours / totalPersonnel * 100, 1)
                : 0.0
        };

        // 5. 巡检完成率
        var inspectionResult = await inspectionService.QueryAsync(new InspectionQuery
        {
            Page = 1,
            Size = 50000
        }, cancellationToken);

        var scopedInspections = HospitalScopeHelper.FilterByHospitalScope(
            dataScope, inspectionResult.Items, x => x.HospitalName).ToList();
        var inspectionTotal = scopedInspections.Count;
        var inspectionCompleted = scopedInspections.Count(x =>
            string.Equals(x.Status, "已完成", StringComparison.OrdinalIgnoreCase));

        var inspectionCompletionRate = new
        {
            total = inspectionTotal,
            completed = inspectionCompleted,
            rate = inspectionTotal > 0
                ? Math.Round((double)inspectionCompleted / inspectionTotal * 100, 1)
                : 0.0
        };

        return Ok(ApiResponse<object>.Success(new
        {
            contractStatusDistribution,
            hospitalProductCoverage,
            repairProcessingRate,
            personnelLoadRate,
            inspectionCompletionRate
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

    /// <summary>
    /// 个人工作台 — 汇聚待办事项与个人统计
    /// </summary>
    [HttpGet("workbench")]
    public async Task<IActionResult> GetWorkbench(CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);

        var projects = HospitalScopeHelper.FilterByHospitalScope(
            dataScope, InMemoryProjectDataStore.Projects, x => x.HospitalName).ToList();

        // 临期合同：售后到期日在未来 30 天内
        var today = DateTime.Today;
        var horizon = today.AddDays(30);
        var expiringContracts = projects
            .Where(x =>
            {
                if (!DateTime.TryParse(x.AfterSalesEndDate, out var endDate)) return false;
                return endDate.Date >= today && endDate.Date <= horizon;
            })
            .Select(x => new
            {
                projectId = x.Id,
                hospitalName = x.HospitalName,
                productName = x.ProductName,
                afterSalesEndDate = x.AfterSalesEndDate,
                daysRemaining = (DateTime.Parse(x.AfterSalesEndDate).Date - today).Days
            })
            .OrderBy(x => x.daysRemaining)
            .Take(20)
            .ToList();

        // 待巡检
        var inspectionResult = await inspectionService.QueryAsync(new InspectionQuery
        {
            Page = 1,
            Size = 50000
        }, cancellationToken);

        var pendingInspections = HospitalScopeHelper.FilterByHospitalScope(
                dataScope, inspectionResult.Items, x => x.HospitalName)
            .Where(x => !string.Equals(x.Status, "已完成", StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(x.Status, "已取消", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.PlanDate)
            .Take(20)
            .Select(x => new
            {
                id = x.Id,
                hospitalName = x.HospitalName,
                productName = x.ProductName,
                inspector = x.Inspector,
                planDate = x.PlanDate.ToString("yyyy-MM-dd"),
                status = x.Status
            })
            .ToList();

        // 未处理报修
        var repairResult = await repairRecordService.QueryAsync(new RepairRecordQuery
        {
            Page = 1,
            Size = 50000
        }, cancellationToken);

        var unresolvedRepairs = HospitalScopeHelper.FilterByHospitalScope(
                dataScope, repairResult.Items, x => x.HospitalName)
            .Where(x => string.Equals(x.Status, "待处理", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Urgency == "非常紧急" ? 0 : x.Urgency == "紧急" ? 1 : 2)
            .ThenByDescending(x => x.ReportedAt)
            .Take(20)
            .Select(x => new
            {
                id = x.Id,
                hospitalName = x.HospitalName,
                productName = x.ProductName,
                description = x.Description,
                reportedAt = x.ReportedAt?.ToString("yyyy-MM-dd"),
                urgency = x.Urgency
            })
            .ToList();

        // 本月工时
        var workHoursSnapshot = HospitalScopeHelper.FilterByHospitalScope(
            dataScope, InMemoryWorkHoursService.GetSnapshot(), x => x.HospitalName).ToList();
        var monthStart = new DateTime(today.Year, today.Month, 1).ToString("yyyy-MM-dd");
        var monthEnd = today.ToString("yyyy-MM-dd");
        var thisMonthHours = workHoursSnapshot
            .Where(x => string.Compare(x.WorkDate, monthStart, StringComparison.Ordinal) >= 0
                     && string.Compare(x.WorkDate, monthEnd, StringComparison.Ordinal) <= 0)
            .Sum(x => x.Hours);

        return Ok(ApiResponse<object>.Success(new
        {
            myProjects = projects.Count,
            pendingRepairCount = unresolvedRepairs.Count,
            pendingInspectionCount = pendingInspections.Count,
            thisMonthWorkHours = thisMonthHours,
            expiringContracts,
            pendingInspections,
            unresolvedRepairs
        }));
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