using PMS.Application.Contracts.AuditLog;
using PMS.Application.Models;
using PMS.Application.Models.AuditLog;
using PMS.Domain.Entities;

namespace PMS.Infrastructure.Services;

public class InMemoryAuditLogService : IAuditLogService
{
    private const string TableName = "AuditLogs";
    private static readonly object SyncRoot = new();

    private static readonly List<AuditLogEntity> Store =
        SqliteTableStore.LoadAll<AuditLogEntity>(TableName);

    private static long _nextId = Store.Count > 0 ? Store.Max(x => x.Id) + 1 : 1;

    public Task<PagedResult<AuditLogItemDto>> QueryAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        List<AuditLogEntity> items;
        lock (SyncRoot)
        {
            items = Store.ToList();
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
            items = items.Where(x => x.Action == query.Action).ToList();

        if (!string.IsNullOrWhiteSpace(query.Module))
            items = items.Where(x => x.Module == query.Module).ToList();

        if (!string.IsNullOrWhiteSpace(query.Operator))
            items = items.Where(x => x.Operator.Contains(query.Operator, StringComparison.OrdinalIgnoreCase)).ToList();

        if (query.StartDate.HasValue)
            items = items.Where(x => x.CreatedAt >= query.StartDate.Value).ToList();

        if (query.EndDate.HasValue)
            items = items.Where(x => x.CreatedAt < query.EndDate.Value.AddDays(1)).ToList();

        var total = items.Count;
        var paged = items
            .OrderByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.Size)
            .Take(query.Size)
            .Select(MapToDto)
            .ToList();

        return Task.FromResult(new PagedResult<AuditLogItemDto>
        {
            Items = paged,
            Total = total,
            Page = query.Page,
            Size = query.Size
        });
    }

    public Task<AuditLogSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        List<AuditLogEntity> items;
        lock (SyncRoot)
        {
            items = Store.ToList();
        }

        var today = DateTime.Today;
        var summary = new AuditLogSummaryDto
        {
            Total = items.Count,
            TodayCount = items.Count(x => x.CreatedAt >= today),
            ActionCounts = items.GroupBy(x => x.Action)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return Task.FromResult(summary);
    }

    public Task LogAsync(string operatorName, long operatorId, string action, string module, string target, string detail = "", string ipAddress = "", CancellationToken cancellationToken = default)
    {
        var entity = new AuditLogEntity
        {
            Id = _nextId++,
            Operator = operatorName,
            OperatorId = operatorId,
            Action = action,
            Module = module,
            Target = target,
            Detail = detail,
            IpAddress = ipAddress,
            CreatedAt = DateTime.Now
        };

        lock (SyncRoot)
        {
            SqliteTableStore.Insert(TableName, entity);
            Store.Add(entity);
        }

        return Task.CompletedTask;
    }

    private static AuditLogItemDto MapToDto(AuditLogEntity e) => new()
    {
        Id = e.Id,
        Operator = e.Operator,
        OperatorId = e.OperatorId,
        Action = e.Action,
        Module = e.Module,
        Target = e.Target,
        Detail = e.Detail,
        IpAddress = e.IpAddress,
        CreatedAt = e.CreatedAt
    };
}
