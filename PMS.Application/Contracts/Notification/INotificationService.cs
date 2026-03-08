using PMS.Application.Models;
using PMS.Application.Models.Notification;

namespace PMS.Application.Contracts.Notification;

public interface INotificationService
{
    Task<NotificationSummaryDto> GetSummaryAsync(long recipientId, CancellationToken cancellationToken = default);
    Task<PagedResult<NotificationItemDto>> QueryAsync(long recipientId, NotificationQuery query, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(long recipientId, long notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(long recipientId, CancellationToken cancellationToken = default);
    Task CreateAsync(long recipientId, string type, string title, string content, string relatedPath = "", CancellationToken cancellationToken = default);
    Task BroadcastToManagersAsync(string type, string title, string content, string relatedPath = "", CancellationToken cancellationToken = default);
}
