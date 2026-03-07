namespace PMS.Domain.Entities;

public class WorkHoursEntity
{
    public long Id { get; set; }
    public long ProjectId { get; set; }

    /// <summary>机会号</summary>
    public string OpportunityNumber { get; set; } = string.Empty;

    public string PersonnelName { get; set; } = string.Empty;
    public string HospitalName { get; set; } = string.Empty;

    /// <summary>产品名称</summary>
    public string ProductName { get; set; } = string.Empty;

    public string WorkDate { get; set; } = string.Empty; // yyyy-MM-dd

    /// <summary>工时(人天)</summary>
    public decimal Hours { get; set; }

    public string WorkType { get; set; } = string.Empty; // 驻场/远程/出差

    /// <summary>实施状态</summary>
    public string ImplementationStatus { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
