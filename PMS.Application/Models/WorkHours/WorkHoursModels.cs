namespace PMS.Application.Models.WorkHours;

public class WorkHoursItemDto
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string OpportunityNumber { get; set; } = string.Empty;
    public string PersonnelName { get; set; } = string.Empty;
    public string HospitalName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string WorkDate { get; set; } = string.Empty;
    public decimal Hours { get; set; }
    public string WorkType { get; set; } = string.Empty;
    public string ImplementationStatus { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>状态：draft/submitted/confirmed/rejected</summary>
    public string Status { get; set; } = "draft";
    public string? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}

public class WorkHoursUpsertDto
{
    public long ProjectId { get; set; }
    public string OpportunityNumber { get; set; } = string.Empty;
    public string HospitalName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string WorkDate { get; set; } = string.Empty;
    public decimal Hours { get; set; }
    public string WorkType { get; set; } = string.Empty;
    public string ImplementationStatus { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class WorkHoursQuery
{
    public string? PersonnelName { get; set; }
    public string? HospitalName { get; set; }
    public string? ProductName { get; set; }
    public string? ImplementationStatus { get; set; }
    public string? WorkDateFrom { get; set; }
    public string? WorkDateTo { get; set; }
    public string? WorkType { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;

    /// <summary>
    /// 数据范围过滤 - 可访问的运维人员姓名列表。null 表示不限制（全部可见）。
    /// </summary>
    public List<string>? AccessiblePersonnelNames { get; set; }
}

public class WorkHoursSummaryDto
{
    public int Total { get; set; }
    public decimal TotalHours { get; set; }
    public int OnsiteCount { get; set; }
    public int RemoteCount { get; set; }
    public int TravelCount { get; set; }
}

/// <summary>
/// 工时报表行 — 对应工时 Excel 的每行数据
/// </summary>
public class WorkHoursReportRowDto
{
    /// <summary>行ID</summary>
    public long Id { get; set; }

    /// <summary>机会号</summary>
    public string OpportunityNumber { get; set; } = string.Empty;
    /// <summary>客户名称</summary>
    public string HospitalName { get; set; } = string.Empty;
    /// <summary>产品名称</summary>
    public string ProductName { get; set; } = string.Empty;
    /// <summary>实施状态</summary>
    public string ImplementationStatus { get; set; } = string.Empty;
    /// <summary>工时（人天）</summary>
    public decimal WorkHoursManDays { get; set; }
    /// <summary>实施人员（个数）</summary>
    public int PersonnelCount { get; set; }
    public string Personnel1 { get; set; } = string.Empty;
    public string Personnel2 { get; set; } = string.Empty;
    public string Personnel3 { get; set; } = string.Empty;
    public string Personnel4 { get; set; } = string.Empty;
    public string Personnel5 { get; set; } = string.Empty;
    /// <summary>维护开始时间</summary>
    public string MaintenanceStartDate { get; set; } = string.Empty;
    /// <summary>维护结束时间</summary>
    public string MaintenanceEndDate { get; set; } = string.Empty;
    /// <summary>售后项目类型</summary>
    public string AfterSalesProjectType { get; set; } = string.Empty;
    /// <summary>备注</summary>
    public string Remarks { get; set; } = string.Empty;
}

public class WorkHoursReportRowUpdateDto
{
    public string OpportunityNumber { get; set; } = string.Empty;
    public string HospitalName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ImplementationStatus { get; set; } = string.Empty;
    public decimal WorkHoursManDays { get; set; }
    public int PersonnelCount { get; set; }
    public string Personnel1 { get; set; } = string.Empty;
    public string Personnel2 { get; set; } = string.Empty;
    public string Personnel3 { get; set; } = string.Empty;
    public string Personnel4 { get; set; } = string.Empty;
    public string Personnel5 { get; set; } = string.Empty;
    public string MaintenanceStartDate { get; set; } = string.Empty;
    public string MaintenanceEndDate { get; set; } = string.Empty;
    public string AfterSalesProjectType { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
}
