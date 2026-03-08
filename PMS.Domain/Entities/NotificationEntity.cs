namespace PMS.Domain.Entities;

public class NotificationEntity
{
    public long Id { get; set; }

    /// <summary>接收人 personnelId</summary>
    public long RecipientId { get; set; }

    /// <summary>通知类型：contract_expiring / inspection_pending / repair_new / system</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>通知标题</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>通知正文</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>关联路由路径，如 /project/list</summary>
    public string RelatedPath { get; set; } = string.Empty;

    /// <summary>是否已读</summary>
    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}
