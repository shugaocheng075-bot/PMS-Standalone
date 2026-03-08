namespace PMS.Domain.Entities;

public class AuditLogEntity
{
    public long Id { get; set; }

    /// <summary>操作人姓名</summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>操作人ID</summary>
    public long OperatorId { get; set; }

    /// <summary>操作类型：create / update / delete / export / import / login / batch-delete</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>所属模块：project / product / hospital / repair / workhours 等</summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>操作目标描述</summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>变更详情</summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>客户端IP地址</summary>
    public string IpAddress { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
