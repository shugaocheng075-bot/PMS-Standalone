namespace PMS.Application.Models.AnnualReport;

public class AnnualReportItemDto
{
    public long Id { get; set; }
    public string OpportunityNumber { get; set; } = string.Empty;
    public string HospitalName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string ServicePerson { get; set; } = string.Empty;
    public string ImplementationStatus { get; set; } = string.Empty;
    public string MaintenanceStartDate { get; set; } = string.Empty;
    public string MaintenanceEndDate { get; set; } = string.Empty;
    /// <summary>年报到期月份，格式 YYYY-MM，由维护结束日期推导</summary>
    public string DueMonth { get; set; } = string.Empty;
    public int ReportYear { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime? SubmitDate { get; set; }
    public string Reviewer { get; set; } = string.Empty;
    public DateTime? ReviewDate { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

public class AnnualReportUpsertDto
{
    public string? OpportunityNumber { get; set; }
    public string? HospitalName { get; set; }
    public string? ProductName { get; set; }
    public string? Province { get; set; }
    public string? GroupName { get; set; }
    public string? ServicePerson { get; set; }
    public string? ImplementationStatus { get; set; }
    public string? MaintenanceStartDate { get; set; }
    public string? MaintenanceEndDate { get; set; }
    public int? ReportYear { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public DateTime? SubmitDate { get; set; }
    public string? Reviewer { get; set; }
    public DateTime? ReviewDate { get; set; }
    public string? Remarks { get; set; }
}
