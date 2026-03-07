namespace PMS.Domain.Entities;

public class ProjectEntity
{
    public long Id { get; set; }

    /// <summary>机会号</summary>
    public string OpportunityNumber { get; set; } = string.Empty;

    public string HospitalName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string SalesName { get; set; } = string.Empty;
    public string MaintenancePersonName { get; set; } = string.Empty;
    public string AfterSalesStartDate { get; set; } = string.Empty;
    public string AfterSalesEndDate { get; set; } = string.Empty;
    public string HospitalLevel { get; set; } = string.Empty;
    public string ContractStatus { get; set; } = string.Empty;
    public string ContractValidityStatus { get; set; } = string.Empty;
    public decimal MaintenanceAmount { get; set; }
    public int OverdueDays { get; set; }

    /// <summary>实施状态: 实施中/有偿维护/质保期内/维护超期无合同/未终验/维保期内</summary>
    public string ImplementationStatus { get; set; } = string.Empty;

    /// <summary>工时(人天)</summary>
    public decimal WorkHoursManDays { get; set; }

    /// <summary>实施人员(个数)</summary>
    public int PersonnelCount { get; set; }

    /// <summary>人员1</summary>
    public string Personnel1 { get; set; } = string.Empty;
    /// <summary>人员2</summary>
    public string Personnel2 { get; set; } = string.Empty;
    /// <summary>人员3</summary>
    public string Personnel3 { get; set; } = string.Empty;
    /// <summary>人员4</summary>
    public string Personnel4 { get; set; } = string.Empty;
    /// <summary>人员5</summary>
    public string Personnel5 { get; set; } = string.Empty;

    /// <summary>售后项目类型</summary>
    public string AfterSalesProjectType { get; set; } = string.Empty;

    /// <summary>备注</summary>
    public string Remarks { get; set; } = string.Empty;
}
