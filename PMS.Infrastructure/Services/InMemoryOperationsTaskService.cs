using PMS.Application.Contracts.AnnualReport;
using PMS.Application.Contracts.Handover;
using PMS.Application.Contracts.Inspection;
using PMS.Application.Contracts.MonthlyReport;
using PMS.Application.Contracts.Operations;
using PMS.Application.Contracts.RepairRecord;
using PMS.Application.Models;
using PMS.Application.Models.AnnualReport;
using PMS.Application.Models.Handover;
using PMS.Application.Models.Inspection;
using PMS.Application.Models.MonthlyReport;
using PMS.Application.Models.Operations;
using PMS.Application.Models.RepairRecord;
using PMS.Domain.Entities;
using System.Globalization;

namespace PMS.Infrastructure.Services;

public class InMemoryOperationsTaskService(
    IRepairRecordService repairRecordService,
    IInspectionService inspectionService,
    IHandoverService handoverService,
    IAnnualReportService annualReportService,
    IMonthlyReportService monthlyReportService) : IOperationsTaskService
{
    public async Task<OperationsTaskSummaryDto> GetSummaryAsync(OperationsTaskQuery query, CancellationToken cancellationToken = default)
    {
        var items = await BuildTasksAsync(query, cancellationToken);
        return new OperationsTaskSummaryDto
        {
            Total = items.Count,
            Severe = items.Count(x => x.Level == "严重"),
            Warning = items.Count(x => x.Level == "警告"),
            Reminder = items.Count(x => x.Level == "提醒"),
            Overdue = items.Count(x => x.OverdueDays > 0),
        };
    }

    public async Task<PagedResult<OperationsTaskItemDto>> QueryAsync(OperationsTaskQuery query, CancellationToken cancellationToken = default)
    {
        var items = await BuildTasksAsync(query, cancellationToken);
        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.Size <= 0 ? 20 : query.Size;

        return new PagedResult<OperationsTaskItemDto>
        {
            Items = items
                .Skip((page - 1) * size)
                .Take(size)
                .ToList(),
            Total = items.Count,
            Page = page,
            Size = size,
        };
    }

    private async Task<List<OperationsTaskItemDto>> BuildTasksAsync(OperationsTaskQuery query, CancellationToken cancellationToken)
    {
        var repairTask = repairRecordService.QueryAsync(new RepairRecordQuery
        {
            Page = 1,
            Size = 5000,
        }, cancellationToken);

        var inspectionTask = inspectionService.QueryAsync(new InspectionQuery
        {
            Page = 1,
            Size = 5000,
        }, cancellationToken);

        var handoverTask = handoverService.QueryAsync(new HandoverQuery
        {
            Page = 1,
            Size = 5000,
        }, cancellationToken);

        var annualTask = annualReportService.QueryAsync(new AnnualReportQuery
        {
            Page = 1,
            Size = 5000,
        }, cancellationToken);

        var monthlyTask = monthlyReportService.QueryAsync(new MonthlyReportQuery
        {
            Page = 1,
            Size = 5000,
        }, cancellationToken);

        await Task.WhenAll(repairTask, inspectionTask, handoverTask, annualTask, monthlyTask);

        var tasks = new List<OperationsTaskItemDto>();
        tasks.AddRange(BuildContractTasks(query));
        tasks.AddRange(BuildRepairTasks(repairTask.Result.Items, query));
        tasks.AddRange(BuildInspectionTasks(inspectionTask.Result.Items, query));
        tasks.AddRange(BuildHandoverTasks(handoverTask.Result.Items, query));
        tasks.AddRange(BuildMajorDemandTasks(query));
        tasks.AddRange(BuildAnnualReportTasks(annualTask.Result.Items, query));
        tasks.AddRange(BuildMonthlyReportTasks(monthlyTask.Result.Items, query));
        tasks.AddRange(BuildDataQualityTasks(query));

        return ApplyQuery(tasks, query);
    }

    private static List<OperationsTaskItemDto> BuildContractTasks(OperationsTaskQuery query)
    {
        var today = DateTime.Today;
        var horizon = today.AddDays(30);

        return InMemoryProjectDataStore.Projects
            .Where(project => IsHospitalVisible(query, project.HospitalName))
            .Select(project =>
            {
                if (!DateTime.TryParse(project.AfterSalesEndDate, out var endDate))
                {
                    return null;
                }

                if (endDate.Date > horizon)
                {
                    return null;
                }

                var overdueDays = endDate.Date < today ? (today - endDate.Date).Days : 0;
                var remainingDays = endDate.Date >= today ? (endDate.Date - today).Days : 0;
                var level = overdueDays > 0
                    ? (overdueDays > 7 ? "严重" : "警告")
                    : (remainingDays <= 7 ? "警告" : "提醒");

                return new OperationsTaskItemDto
                {
                    Id = $"contract-{project.Id}",
                    Source = "contract",
                    Level = level,
                    Priority = ResolvePriority(level),
                    Status = project.ContractStatus,
                    Owner = ResolveOwner(project.MaintenancePersonName, project.GroupName),
                    HospitalName = project.HospitalName,
                    ProductName = project.ProductName,
                    Title = "维保合同到期风险",
                    Detail = $"维保截止 {project.AfterSalesEndDate}",
                    DueAt = endDate.ToString("yyyy-MM-dd"),
                    OverdueDays = overdueDays,
                    RelatedPath = "/project/list",
                    RelatedQuery = BuildQueryMap(
                        ("hospitalName", project.HospitalName),
                        ("productName", project.ProductName),
                        ("maintenancePersonName", project.MaintenancePersonName)),
                };
            })
            .Where(item => item is not null)
            .Cast<OperationsTaskItemDto>()
            .ToList();
    }

    private static List<OperationsTaskItemDto> BuildRepairTasks(IReadOnlyList<RepairRecordItemDto> items, OperationsTaskQuery query)
    {
        var today = DateTime.Today;
        return items
            .Where(item => IsHospitalVisible(query, item.HospitalName))
            .Where(item => !IsRepairDone(item.Status))
            .Select(item =>
            {
                var dueAt = item.SlaDueAt?.Date;
                var overdueDays = dueAt.HasValue && dueAt.Value < today ? (today - dueAt.Value).Days : 0;
                var level = overdueDays > 0 || string.Equals(item.Urgency, "非常紧急", StringComparison.OrdinalIgnoreCase)
                    ? "严重"
                    : string.Equals(item.Urgency, "紧急", StringComparison.OrdinalIgnoreCase) || overdueDays == 0 && string.Equals(item.Status, "处理中", StringComparison.OrdinalIgnoreCase)
                        ? "警告"
                        : "提醒";

                return new OperationsTaskItemDto
                {
                    Id = $"repair-{item.Id}",
                    Source = "repair",
                    Level = level,
                    Priority = ResolvePriority(level),
                    Status = item.Status,
                    Owner = ResolveOwner(item.AssigneeName, item.ReporterName),
                    HospitalName = item.HospitalName,
                    ProductName = item.ProductName,
                    Title = "报修待处理",
                    Detail = item.Description,
                    DueAt = dueAt?.ToString("yyyy-MM-dd"),
                    OverdueDays = overdueDays,
                    RelatedPath = "/repair/list",
                    RelatedQuery = BuildQueryMap(
                        ("hospitalName", item.HospitalName),
                        ("status", item.Status)),
                };
            })
            .ToList();
    }

    private static List<OperationsTaskItemDto> BuildInspectionTasks(IReadOnlyList<InspectionPlanItemDto> items, OperationsTaskQuery query)
    {
        var today = DateTime.Today;
        return items
            .Where(item => IsHospitalVisible(query, item.HospitalName))
            .Where(item => !IsInspectionDone(item.Status))
            .Select(item =>
            {
                var planDate = item.PlanDate.Date;
                var overdueDays = planDate < today ? (today - planDate).Days : 0;
                var level = overdueDays > 7 ? "严重" : overdueDays > 0 ? "警告" : "提醒";

                return new OperationsTaskItemDto
                {
                    Id = $"inspection-{item.Id}",
                    Source = "inspection",
                    Level = level,
                    Priority = ResolvePriority(level),
                    Status = item.Status,
                    Owner = ResolveOwner(item.Inspector),
                    HospitalName = item.HospitalName,
                    ProductName = item.ProductName,
                    Title = "巡检待执行",
                    Detail = $"计划日期 {item.PlanDate:yyyy-MM-dd}",
                    DueAt = item.PlanDate.ToString("yyyy-MM-dd"),
                    OverdueDays = overdueDays,
                    RelatedPath = "/inspection/plan",
                    RelatedQuery = BuildQueryMap(
                        ("hospitalName", item.HospitalName),
                        ("productName", item.ProductName),
                        ("inspector", item.Inspector),
                        ("status", item.Status)),
                };
            })
            .ToList();
    }

    private static List<OperationsTaskItemDto> BuildHandoverTasks(IReadOnlyList<HandoverItemDto> items, OperationsTaskQuery query)
    {
        return items
            .Where(item => IsHospitalVisible(query, item.HospitalName))
            .Where(item => !IsHandoverDone(item.Stage))
            .Select(item =>
            {
                var level = ResolveHandoverLevel(item.Stage);
                return new OperationsTaskItemDto
                {
                    Id = $"handover-{item.Id}",
                    Source = "handover",
                    Level = level,
                    Priority = ResolvePriority(level),
                    Status = item.Stage,
                    Owner = ResolveOwner(item.ToOwner, item.FromOwner),
                    HospitalName = item.HospitalName,
                    ProductName = item.ProductName,
                    Title = "交接待处理",
                    Detail = $"阶段: {item.Stage}",
                    DueAt = item.StartedAt?.ToString("yyyy-MM-dd") ?? item.EmailSentDate?.ToString("yyyy-MM-dd"),
                    OverdueDays = 0,
                    RelatedPath = "/handover/list",
                    RelatedQuery = BuildQueryMap(
                        ("hospitalName", item.HospitalName),
                        ("productName", item.ProductName),
                        ("toOwner", item.ToOwner),
                        ("stage", item.Stage)),
                };
            })
            .ToList();
    }

    private static List<OperationsTaskItemDto> BuildMajorDemandTasks(OperationsTaskQuery query)
    {
        var today = DateTime.Today;
        var snapshot = InMemoryMajorDemandStore.GetSnapshot();
        var rowById = snapshot.Rows
            .Where(row => row.TryGetValue("_RowId", out _))
            .ToDictionary(row => row["_RowId"], row => row, StringComparer.OrdinalIgnoreCase);

        return snapshot.WorkflowItems
            .Select(workflow =>
            {
                rowById.TryGetValue(workflow.RowId, out var row);
                row ??= [];
                var hospitalName = ResolveRowValue(row, "医院名称", "医院", "客户", "客户名称");
                if (!IsHospitalVisible(query, hospitalName))
                {
                    return null;
                }

                if (IsMajorDemandDone(workflow.Status))
                {
                    return null;
                }

                var dueDate = ParseDate(ResolveOwner(workflow.DueDate, ResolveRowValue(row, "计划完成", "截止日期", "到期日期")));
                var overdueDays = dueDate.HasValue && dueDate.Value.Date < today ? (today - dueDate.Value.Date).Days : 0;
                var owner = ResolveOwner(workflow.Owner, ResolveRowValue(row, "服务人员", "负责人", "责任人"));
                var level = overdueDays > 0
                    ? "严重"
                    : dueDate.HasValue && dueDate.Value.Date <= today.AddDays(3)
                        ? "警告"
                        : string.IsNullOrWhiteSpace(owner) || owner == "未分配"
                            ? "警告"
                            : "提醒";

                return new OperationsTaskItemDto
                {
                    Id = $"major-demand-{workflow.RowId}",
                    Source = "majorDemand",
                    Level = level,
                    Priority = ResolvePriority(level),
                    Status = workflow.Status,
                    Owner = owner,
                    HospitalName = hospitalName,
                    ProductName = ResolveRowValue(row, "产品名称", "产品", "项目产品"),
                    Title = ResolveOwner(ResolveRowValue(row, "需求标题", "标题", "需求名称"), "重大需求待跟进"),
                    Detail = $"状态: {workflow.Status}",
                    DueAt = dueDate?.ToString("yyyy-MM-dd"),
                    OverdueDays = overdueDays,
                    RelatedPath = "/major-demand/list",
                    RelatedQuery = BuildQueryMap(
                        ("action", "detail"),
                        ("rowId", workflow.RowId),
                        ("status", workflow.Status)),
                };
            })
            .Where(item => item is not null)
            .Cast<OperationsTaskItemDto>()
            .ToList();
    }

    private static List<OperationsTaskItemDto> BuildAnnualReportTasks(IReadOnlyList<AnnualReportItemDto> items, OperationsTaskQuery query)
    {
        var today = DateTime.Today;
        return items
            .Where(item => IsHospitalVisible(query, item.HospitalName))
            .Where(item => !string.Equals(item.Status, "已完成", StringComparison.OrdinalIgnoreCase))
            .Select(item =>
            {
                var dueDate = ParseMonth(item.DueMonth);
                var overdueDays = dueDate.HasValue && dueDate.Value.Date < today ? (today - dueDate.Value.Date).Days : 0;
                var level = overdueDays > 0
                    ? "严重"
                    : dueDate.HasValue && dueDate.Value.Date <= today.AddDays(7)
                        ? "警告"
                        : "提醒";

                return new OperationsTaskItemDto
                {
                    Id = $"annual-report-{item.Id}",
                    Source = "annualReport",
                    Level = level,
                    Priority = ResolvePriority(level),
                    Status = item.Status,
                    Owner = ResolveOwner(item.ServicePerson),
                    HospitalName = item.HospitalName,
                    ProductName = item.ProductName,
                    Title = "年度服务报告待收口",
                    Detail = $"状态: {item.Status}",
                    DueAt = dueDate?.ToString("yyyy-MM-dd") ?? item.DueMonth,
                    OverdueDays = overdueDays,
                    RelatedPath = "/annual-report/list",
                    RelatedQuery = BuildQueryMap(
                        ("hospitalName", item.HospitalName),
                        ("productName", item.ProductName),
                        ("status", item.Status)),
                };
            })
            .ToList();
    }

    private static List<OperationsTaskItemDto> BuildMonthlyReportTasks(IReadOnlyList<MonthlyReportItemDto> items, OperationsTaskQuery query)
    {
        return items
            .Where(item => IsHospitalVisible(query, item.HospitalName))
            .Where(item => !string.Equals(item.Status, "approved", StringComparison.OrdinalIgnoreCase))
            .Select(item =>
            {
                var level = string.Equals(item.Status, "rejected", StringComparison.OrdinalIgnoreCase)
                    ? "严重"
                    : string.Equals(item.Status, "submitted", StringComparison.OrdinalIgnoreCase)
                        ? "警告"
                        : "提醒";

                return new OperationsTaskItemDto
                {
                    Id = $"monthly-report-{item.Id}",
                    Source = "monthlyReport",
                    Level = level,
                    Priority = ResolvePriority(level),
                    Status = item.Status,
                    Owner = ResolveOwner(item.SubmittedBy),
                    HospitalName = item.HospitalName,
                    ProductName = string.Empty,
                    Title = "月报待处理",
                    Detail = ResolveOwner(item.Title, $"状态: {item.Status}"),
                    DueAt = item.ReportMonth,
                    OverdueDays = 0,
                    RelatedPath = "/monthly-report/list",
                    RelatedQuery = BuildQueryMap(
                        ("hospitalName", item.HospitalName),
                        ("status", item.Status),
                        ("reportMonth", item.ReportMonth)),
                };
            })
            .ToList();
    }

    private static List<OperationsTaskItemDto> BuildDataQualityTasks(OperationsTaskQuery query)
    {
        return InMemoryProjectDataStore.Projects
            .Where(project => IsHospitalVisible(query, project.HospitalName))
            .SelectMany(project =>
            {
                var issues = new List<OperationsTaskItemDto>();
                var owner = ResolveOwner(project.MaintenancePersonName, project.GroupName);

                if (string.IsNullOrWhiteSpace(project.MaintenancePersonName))
                {
                    issues.Add(CreateDataQualityTask(project, owner, "警告", "缺少维护负责人", "项目台账缺少维护人员"));
                }

                if (string.IsNullOrWhiteSpace(project.AfterSalesEndDate))
                {
                    issues.Add(CreateDataQualityTask(project, owner, "警告", "缺少维保截止日期", "项目台账缺少售后结束日期"));
                }

                if (string.IsNullOrWhiteSpace(project.GroupName))
                {
                    issues.Add(CreateDataQualityTask(project, owner, "严重", "缺少服务组别", "项目台账缺少服务组别，影响任务归属"));
                }

                return issues;
            })
            .ToList();
    }

    private static OperationsTaskItemDto CreateDataQualityTask(ProjectEntity project, string owner, string level, string title, string detail)
    {
        return new OperationsTaskItemDto
        {
            Id = $"data-quality-{project.Id}-{title}",
            Source = "dataQuality",
            Level = level,
            Priority = ResolvePriority(level),
            Status = "待修正",
            Owner = owner,
            HospitalName = project.HospitalName,
            ProductName = project.ProductName,
            Title = title,
            Detail = detail,
            DueAt = null,
            OverdueDays = 0,
            RelatedPath = "/maintenance/data",
            RelatedQuery = BuildQueryMap(
                ("hospitalName", project.HospitalName),
                ("productName", project.ProductName)),
        };
    }

    private static List<OperationsTaskItemDto> ApplyQuery(List<OperationsTaskItemDto> items, OperationsTaskQuery query)
    {
        IEnumerable<OperationsTaskItemDto> filtered = items;

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            filtered = filtered.Where(item => string.Equals(item.Source, query.Source.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Level))
        {
            filtered = filtered.Where(item => string.Equals(item.Level, query.Level.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Owner))
        {
            filtered = filtered.Where(item => item.Owner.Contains(query.Owner.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.HospitalName))
        {
            filtered = filtered.Where(item => item.HospitalName.Contains(query.HospitalName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            filtered = filtered.Where(item =>
                item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || item.Detail.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || item.Owner.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || item.HospitalName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || item.ProductName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        return filtered
            .OrderByDescending(item => item.Priority)
            .ThenByDescending(item => item.OverdueDays)
            .ThenBy(item => string.IsNullOrWhiteSpace(item.DueAt) ? "9999-12-31" : item.DueAt)
            .ThenBy(item => item.HospitalName)
            .ThenBy(item => item.Title)
            .ToList();
    }

    private static bool IsHospitalVisible(OperationsTaskQuery query, string hospitalName)
    {
        if (string.Equals(query.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (query.AccessibleHospitalNames.Count == 0)
        {
            return false;
        }

        return query.AccessibleHospitalNames.Contains(hospitalName, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsRepairDone(string status)
    {
        return string.Equals(status, "已完成", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "已关闭", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInspectionDone(string status)
    {
        return string.Equals(status, "已完成", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "已取消", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHandoverDone(string stage)
    {
        return string.Equals(stage, "已交接", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMajorDemandDone(string status)
    {
        return string.Equals(status, "已完成", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "已关闭", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveHandoverLevel(string stage)
    {
        if (string.Equals(stage, "未发送", StringComparison.OrdinalIgnoreCase))
        {
            return "严重";
        }

        if (string.Equals(stage, "已发邮件", StringComparison.OrdinalIgnoreCase))
        {
            return "警告";
        }

        return "提醒";
    }

    private static int ResolvePriority(string level)
    {
        return level switch
        {
            "严重" => 3,
            "警告" => 2,
            _ => 1,
        };
    }

    private static string ResolveOwner(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return "未分配";
    }

    private static Dictionary<string, string> BuildQueryMap(params (string key, string? value)[] pairs)
    {
        return pairs
            .Where(pair => !string.IsNullOrWhiteSpace(pair.value))
            .ToDictionary(pair => pair.key, pair => pair.value!.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private static string ResolveRowValue(Dictionary<string, string> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (row.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(value.Trim(), out var parsed) ? parsed : null;
    }

    private static DateTime? ParseMonth(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!DateTime.TryParseExact(value.Trim(), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return null;
        }

        return new DateTime(parsed.Year, parsed.Month, DateTime.DaysInMonth(parsed.Year, parsed.Month));
    }
}
