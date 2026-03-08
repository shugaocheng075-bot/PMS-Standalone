namespace PMS.Application.Models.Notification;

public class NotificationItemDto
{
    public long Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string RelatedPath { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationQuery
{
    public bool? IsRead { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}

public class NotificationSummaryDto
{
    public int Total { get; set; }
    public int UnreadCount { get; set; }
}
