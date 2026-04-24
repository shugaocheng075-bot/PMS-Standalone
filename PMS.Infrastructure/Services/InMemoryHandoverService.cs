using PMS.Application.Contracts.Handover;
using PMS.Application.Models;
using PMS.Application.Models.Handover;

namespace PMS.Infrastructure.Services;

public class InMemoryHandoverService : IHandoverService
{
    private static readonly object SyncRoot = new();
    private const string StageStateKey = "handover_stage_overrides";
    private const string EmailDateStateKey = "handover_email_date_overrides";
    private const string StartedAtStateKey = "handover_started_at_overrides";
    private const string CompletedAtStateKey = "handover_completed_at_overrides";
    private static readonly Dictionary<long, string> StageOverrides =
        SqliteJsonStore.LoadOrSeed(StageStateKey, () => new Dictionary<long, string>());
    private static readonly Dictionary<long, DateTime?> EmailSentDateOverrides =
        SqliteJsonStore.LoadOrSeed(EmailDateStateKey, () => new Dictionary<long, DateTime?>());
    private static readonly Dictionary<long, DateTime?> StartedAtOverrides =
        SqliteJsonStore.LoadOrSeed(StartedAtStateKey, () => new Dictionary<long, DateTime?>());
    private static readonly Dictionary<long, DateTime?> CompletedAtOverrides =
        SqliteJsonStore.LoadOrSeed(CompletedAtStateKey, () => new Dictionary<long, DateTime?>());

    private static readonly Dictionary<string, string[]> StageTransitions = new()
    {
        ["未发"] = ["已发邮件"],
        ["已发邮件"] = ["交接中"],
        ["交接中"] = ["已交接"],
        ["已交接"] = []
    };

    private static readonly Dictionary<string, string> PreviousStageMap = new()
    {
        ["已发邮件"] = "未发",
        ["交接中"] = "已发邮件",
        ["已交接"] = "交接中"
    };

    public Task<HandoverSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var seed = BuildItems();
        var summary = new HandoverSummaryDto
        {
            PendingCount = seed.Count(x => x.Stage == "未发"),
            EmailSentCount = seed.Count(x => x.Stage == "已发邮件"),
            InProgressCount = seed.Count(x => x.Stage == "交接中"),
            CompletedCount = seed.Count(x => x.Stage == "已交接"),
            Total = seed.Count
        };

        return Task.FromResult(summary);
    }

    public Task<PagedResult<HandoverItemDto>> QueryAsync(HandoverQuery query, CancellationToken cancellationToken = default)
    {
        IEnumerable<HandoverItemDto> filtered = BuildItems();

        if (!string.IsNullOrWhiteSpace(query.Stage))
            filtered = filtered.Where(x => SmartTextMatcher.MatchExact(x.Stage, query.Stage));

        if (!string.IsNullOrWhiteSpace(query.Batch))
            filtered = filtered.Where(x => SmartTextMatcher.MatchExact(x.Batch, query.Batch));

        if (!string.IsNullOrWhiteSpace(query.Type))
            filtered = filtered.Where(x => SmartTextMatcher.MatchExact(x.Type, query.Type));

        if (!string.IsNullOrWhiteSpace(query.FromGroup))
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.FromGroup, query.FromGroup));

        if (!string.IsNullOrWhiteSpace(query.ToOwner))
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.ToOwner, query.ToOwner));

        var total = filtered.Count();
        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.Size <= 0 ? 20 : query.Size;

        var items = filtered
            .OrderByDescending(x => x.EmailSentDate ?? DateTime.MinValue)
            .ThenBy(x => x.HandoverNo)
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return Task.FromResult(new PagedResult<HandoverItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            Size = size
        });
    }

    public Task<HandoverItemDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            return Task.FromResult(BuildItems().FirstOrDefault(x => x.Id == id));
        }
    }

    public Task<IReadOnlyList<HandoverKanbanColumnDto>> GetKanbanAsync(CancellationToken cancellationToken = default)
    {
        var stages = new[] { "未发", "已发邮件", "交接中", "已交接" };
        var seed = BuildItems();

        var result = stages
            .Select(stage =>
            {
                var items = seed
                    .Where(x => x.Stage == stage)
                    .OrderByDescending(x => x.EmailSentDate ?? DateTime.MinValue)
                    .ThenBy(x => x.HandoverNo)
                    .ToList();

                return new HandoverKanbanColumnDto
                {
                    Stage = stage,
                    Count = items.Count,
                    Items = items
                };
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<HandoverKanbanColumnDto>>(result);
    }

    public Task<HandoverItemDto> SendEmailAsync(long id, CancellationToken cancellationToken = default)
        => TransitionAsync(id, "已发邮件");

    public Task<HandoverItemDto> StartAsync(long id, CancellationToken cancellationToken = default)
        => TransitionAsync(id, "交接中");

    public Task<HandoverItemDto> CompleteAsync(long id, CancellationToken cancellationToken = default)
        => TransitionAsync(id, "已交接");

    public Task<HandoverItemDto> RollbackAsync(long id, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var item = BuildItems().FirstOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("交接记录不存在");
            if (!PreviousStageMap.TryGetValue(item.Stage, out var previousStage))
            {
                throw new InvalidOperationException($"当前阶段 {item.Stage} 不支持回退");
            }

            return TransitionInternal(item, previousStage);
        }
    }

    public Task<HandoverItemDto> UpdateStageAsync(long id, HandoverStageUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TargetStage))
        {
            throw new InvalidOperationException("目标阶段不能为空");
        }

        lock (SyncRoot)
        {
            var item = BuildItems().FirstOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("交接记录不存在");
            return TransitionInternal(item, request.TargetStage);
        }
    }

    private static Task<HandoverItemDto> TransitionAsync(long id, string targetStage)
    {
        lock (SyncRoot)
        {
            var item = BuildItems().FirstOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("交接记录不存在");
            return TransitionInternal(item, targetStage);
        }
    }

    private static Task<HandoverItemDto> TransitionInternal(HandoverItemDto item, string targetStage)
    {
        if (!CanTransition(item.Stage, targetStage))
        {
            throw new InvalidOperationException($"不允许从 {item.Stage} 流转到 {targetStage}");
        }

        StageOverrides[item.Id] = targetStage;

        switch (targetStage)
        {
            case "未发":
                EmailSentDateOverrides[item.Id] = null;
                StartedAtOverrides[item.Id] = null;
                CompletedAtOverrides[item.Id] = null;
                break;
            case "已发邮件":
                EmailSentDateOverrides[item.Id] = item.EmailSentDate ?? DateTime.Today;
                StartedAtOverrides[item.Id] = null;
                CompletedAtOverrides[item.Id] = null;
                break;
            case "交接中":
                if (item.EmailSentDate is null)
                {
                    EmailSentDateOverrides[item.Id] = DateTime.Today;
                }

                if (item.Stage != "交接中")
                {
                    StartedAtOverrides[item.Id] = DateTime.UtcNow;
                }

                CompletedAtOverrides[item.Id] = null;
                break;
            case "已交接":
                if (item.EmailSentDate is null)
                {
                    EmailSentDateOverrides[item.Id] = DateTime.Today;
                }

                if (item.StartedAt is null)
                {
                    StartedAtOverrides[item.Id] = DateTime.UtcNow;
                }

                CompletedAtOverrides[item.Id] = DateTime.UtcNow;
                break;
        }

        PersistOverrides();

        var refreshed = BuildItems().FirstOrDefault(x => x.Id == item.Id);
        if (refreshed is not null)
        {
            return Task.FromResult(refreshed);
        }

        return Task.FromResult(new HandoverItemDto
        {
            Id = item.Id,
            HandoverNo = item.HandoverNo,
            HospitalName = item.HospitalName,
            ProductName = item.ProductName,
            FromGroup = item.FromGroup,
            FromOwner = item.FromOwner,
            ToOwner = item.ToOwner,
            Batch = item.Batch,
            Stage = targetStage,
            Type = item.Type,
            EmailSentDate = EmailSentDateOverrides.TryGetValue(item.Id, out var emailDate) ? emailDate : item.EmailSentDate,
            StartedAt = StartedAtOverrides.TryGetValue(item.Id, out var startedAt) ? startedAt : item.StartedAt,
            CompletedAt = CompletedAtOverrides.TryGetValue(item.Id, out var completedAt) ? completedAt : item.CompletedAt,
        });
    }

    private static bool CanTransition(string currentStage, string targetStage)
    {
        if (StageTransitions.TryGetValue(currentStage, out var nextStages) && nextStages.Contains(targetStage))
        {
            return true;
        }

        return PreviousStageMap.TryGetValue(currentStage, out var previousStage)
            && string.Equals(previousStage, targetStage, StringComparison.OrdinalIgnoreCase);
    }

    private static void PersistOverrides()
    {
        SqliteJsonStore.Save(StageStateKey, StageOverrides);
        SqliteJsonStore.Save(EmailDateStateKey, EmailSentDateOverrides);
        SqliteJsonStore.Save(StartedAtStateKey, StartedAtOverrides);
        SqliteJsonStore.Save(CompletedAtStateKey, CompletedAtOverrides);
    }

    private static List<HandoverItemDto> BuildItems()
    {
        if (StageOverrides.Count == 0)
        {
            return [];
        }

        return StageOverrides
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Value))
            .Select(entry =>
            {
                var project = InMemoryProjectDataStore.Projects.FirstOrDefault(x => x.Id == entry.Key);
                if (project is null)
                {
                    return null;
                }

                var id = project.Id;
                var stage = entry.Value;
                var emailDate = EmailSentDateOverrides.TryGetValue(id, out var overrideDate) ? overrideDate : null;
                var startedAt = StartedAtOverrides.TryGetValue(id, out var startedAtOverride) ? startedAtOverride : null;
                var completedAt = CompletedAtOverrides.TryGetValue(id, out var completedAtOverride) ? completedAtOverride : null;
                var fromOwner = string.IsNullOrWhiteSpace(project.SalesName)
                    ? "未分配"
                    : project.SalesName.Trim();
                var toOwner = string.IsNullOrWhiteSpace(project.MaintenancePersonName)
                    ? "未分配"
                    : project.MaintenancePersonName.Trim();

                return new HandoverItemDto
                {
                    Id = id,
                    HandoverNo = $"HO-{DateTime.Today.Year}-{id:000}",
                    HospitalName = project.HospitalName,
                    ProductName = project.ProductName,
                    FromGroup = project.GroupName,
                    FromOwner = fromOwner,
                    ToOwner = toOwner,
                    Batch = "当前批次",
                    Stage = stage,
                    Type = "实施→运维",
                    EmailSentDate = emailDate,
                    StartedAt = startedAt,
                    CompletedAt = completedAt
                };
            })
            .Where(item => item is not null)
            .Cast<HandoverItemDto>()
            .OrderBy(x => x.Id)
            .ToList();
    }
}
