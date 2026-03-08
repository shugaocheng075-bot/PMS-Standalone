using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.Notification;
using PMS.Application.Models;
using PMS.Application.Models.Notification;
using PMS.Domain.Entities;

namespace PMS.Infrastructure.Services;

public class InMemoryNotificationService(IAccessControlService accessControlService) : INotificationService
{
    private const string TableName = "Notifications";
    private static readonly object SyncRoot = new();

    private static readonly List<NotificationEntity> Store =
        SqliteTableStore.LoadAll<NotificationEntity>(TableName);

    private static long _nextId = Store.Count > 0 ? Store.Max(x => x.Id) + 1 : 1;

    public Task<NotificationSummaryDto> GetSummaryAsync(long recipientId, CancellationToken cancellationToken = default)
    {
        List<NotificationEntity> items;
        lock (SyncRoot)
        {
            items = Store.Where(x => x.RecipientId == recipientId).ToList();
        }

        return Task.FromResult(new NotificationSummaryDto
        {
            Total = items.Count,
            UnreadCount = items.Count(x => !x.IsRead)
        });
    }

    public Task<PagedResult<NotificationItemDto>> QueryAsync(long recipientId, NotificationQuery query, CancellationToken cancellationToken = default)
    {
        List<NotificationEntity> items;
        lock (SyncRoot)
        {
            items = Store.Where(x => x.RecipientId == recipientId).ToList();
        }

        if (query.IsRead.HasValue)
        {
            items = items.Where(x => x.IsRead == query.IsRead.Value).ToList();
        }

        var total = items.Count;
        var paged = items
            .OrderByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.Size)
            .Take(query.Size)
            .Select(MapToDto)
            .ToList();

        return Task.FromResult(new PagedResult<NotificationItemDto>
        {
            Items = paged,
            Total = total,
            Page = query.Page,
            Size = query.Size
        });
    }

    public Task MarkAsReadAsync(long recipientId, long notificationId, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var entity = Store.FirstOrDefault(x => x.Id == notificationId && x.RecipientId == recipientId);
            if (entity is not null && !entity.IsRead)
            {
                entity.IsRead = true;
                SqliteTableStore.Update(TableName, entity, entity.Id);
            }
        }

        return Task.CompletedTask;
    }

    public Task MarkAllAsReadAsync(long recipientId, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var unread = Store.Where(x => x.RecipientId == recipientId && !x.IsRead).ToList();
            foreach (var entity in unread)
            {
                entity.IsRead = true;
                SqliteTableStore.Update(TableName, entity, entity.Id);
            }
        }

        return Task.CompletedTask;
    }

    public Task CreateAsync(long recipientId, string type, string title, string content, string relatedPath = "", CancellationToken cancellationToken = default)
    {
        var entity = new NotificationEntity
        {
            Id = _nextId++,
            RecipientId = recipientId,
            Type = type,
            Title = title,
            Content = content,
            RelatedPath = relatedPath,
            IsRead = false,
            CreatedAt = DateTime.Now
        };

        lock (SyncRoot)
        {
            SqliteTableStore.Insert(TableName, entity);
            Store.Add(entity);
        }

        return Task.CompletedTask;
    }

    private static NotificationItemDto MapToDto(NotificationEntity e) => new()
    {
        Id = e.Id,
        Type = e.Type,
        Title = e.Title,
        Content = e.Content,
        RelatedPath = e.RelatedPath,
        IsRead = e.IsRead,
        CreatedAt = e.CreatedAt
    };

    public async Task BroadcastToManagersAsync(string type, string title, string content, string relatedPath = "", CancellationToken cancellationToken = default)
    {
        var actors = await accessControlService.GetActorsAsync(cancellationToken);
        var managerIds = actors
            .Where(a => string.Equals(a.SystemRole, "manager", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(a.SystemRole, "regional_manager", StringComparison.OrdinalIgnoreCase))
            .Select(a => (long)a.PersonnelId)
            .ToList();

        foreach (var recipientId in managerIds)
        {
            await CreateAsync(recipientId, type, title, content, relatedPath, cancellationToken);
        }
    }
}
