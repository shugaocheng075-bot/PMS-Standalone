namespace PMS.Application.Models.AnnualReport;

public class AnnualReportItemDto
{
    public long Id { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string ServicePerson { get; set; } = string.Empty;
    public int ReportYear { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SubmitDate { get; set; }
}

public class AnnualReportUpsertDto
{
    public string GroupName { get; set; } = string.Empty;
    public string ServicePerson { get; set; } = string.Empty;
    public int ReportYear { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SubmitDate { get; set; }
}
