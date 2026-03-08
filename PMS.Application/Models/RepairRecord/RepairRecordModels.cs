namespace PMS.Application.Models.RepairRecord;

public class RepairRecordItemDto
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty;
    public string IssueCategory { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string FunctionModule { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? ReportedAt { get; set; }
    public decimal? ActualWorkHours { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
    public string AttachmentImages { get; set; } = string.Empty;
    public string RegistrationStatus { get; set; } = string.Empty;
    public string WorkHoursDetail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public string AssigneeName { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RepairRecordUpsertDto
{
    public long ProjectId { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty;
    public string IssueCategory { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string FunctionModule { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? ReportedAt { get; set; }
    public decimal? ActualWorkHours { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
    public string AttachmentImages { get; set; } = string.Empty;
    public string RegistrationStatus { get; set; } = string.Empty;
    public string Status { get; set; } = "待处理";
    public string Urgency { get; set; } = "普通";
    public string AssigneeName { get; set; } = string.Empty;
}

public class RepairRecordStatusTransitionDto
{
    public string Status { get; set; } = string.Empty;
    public string? Resolution { get; set; }
}

public class RepairRecordAssignDto
{
    public string AssigneeName { get; set; } = string.Empty;
}

public class RepairRecordQuery
{
    public string? HospitalName { get; set; }
    public string? ReporterName { get; set; }
    public string? Status { get; set; }
    /// <summary>可访问的维护人员姓名列表（数据范围过滤）</summary>
    public List<string>? AccessiblePersonnelNames { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}

public class RepairRecordSummaryDto
{
    public int Total { get; set; }
    public int PendingCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int ClosedCount { get; set; }
}
