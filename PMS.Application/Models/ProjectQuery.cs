namespace PMS.Application.Models;

public class ProjectQuery
{
    public string? HospitalName { get; set; }
    public string? ProductName { get; set; }
    public string? Province { get; set; }
    public string? GroupName { get; set; }
    public string? SalesName { get; set; }
    public string? MaintenancePersonName { get; set; }
    public string? AfterSalesEndDateFrom { get; set; }
    public string? AfterSalesEndDateTo { get; set; }
    public string? HospitalLevel { get; set; }
    public string? ContractStatus { get; set; }
    public string? ContractValidityStatus { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;

    /// <summary>
    /// 数据范围过滤 - 可访问的运维人员姓名列表。null 表示不限制（全部可见）。
    /// </summary>
    public List<string>? AccessiblePersonnelNames { get; set; }
}
