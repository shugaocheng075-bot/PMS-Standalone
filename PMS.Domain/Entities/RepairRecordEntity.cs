namespace PMS.Domain.Entities;

public class RepairRecordEntity
{
    public long Id { get; set; }

    /// <summary>关联项目ID</summary>
    public long ProjectId { get; set; }

    /// <summary>医院名称（冗余字段，便于查询）</summary>
    public string HospitalName { get; set; } = string.Empty;

    /// <summary>产品名称</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>项目名称（对齐快速登记：项目名称）</summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>产品类别（对齐快速登记：产品类别）</summary>
    public string ProductCategory { get; set; } = string.Empty;

    /// <summary>问题类别（对齐快速登记：问题类别）</summary>
    public string IssueCategory { get; set; } = string.Empty;

    /// <summary>报修人（维护人员姓名）</summary>
    public string ReporterName { get; set; } = string.Empty;

    /// <summary>故障描述</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>功能模块（对齐快速登记：功能模块）</summary>
    public string FunctionModule { get; set; } = string.Empty;

    /// <summary>报修日期（对齐快速登记：报修日期）</summary>
    public DateTime? ReportedAt { get; set; }

    /// <summary>实际工时（小时，对齐快速登记：实际工时）</summary>
    public decimal? ActualWorkHours { get; set; }

    /// <summary>内容（对齐快速登记：内容）</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>处理措施</summary>
    public string Resolution { get; set; } = string.Empty;

    /// <summary>状态: 待处理/处理中/已完成</summary>
    public string Status { get; set; } = "待处理";

    /// <summary>紧急程度: 普通/紧急/非常紧急</summary>
    public string Urgency { get; set; } = "普通";

    /// <summary>严重程度（对齐快速登记：严重程度）</summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>附件图片路径（分号分隔，对齐快速登记：附件图片）</summary>
    public string AttachmentImages { get; set; } = string.Empty;

    /// <summary>登记状态（对齐快速登记：登记状态）</summary>
    public string RegistrationStatus { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
