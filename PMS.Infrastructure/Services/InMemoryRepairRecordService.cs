using PMS.Application.Contracts.RepairRecord;
using PMS.Application.Models;
using PMS.Application.Models.RepairRecord;
using PMS.Domain.Entities;
using System.Text;

namespace PMS.Infrastructure.Services;

public class InMemoryRepairRecordService : IRepairRecordService
{
    private const string TableName = "RepairRecords";
    private const string LegacyJsonKey = "repair_records";
    private static readonly object SyncRoot = new();
    private static readonly List<RepairRecordEntity> Records = SqliteTableStore.LoadAll<RepairRecordEntity>(TableName, LegacyJsonKey);
    private static long _nextId = Records.Count > 0 ? Records.Max(x => x.Id) + 1 : 1;

    private static DateTime CalculateSlaDueAt(RepairRecordEntity entity, DateTime referenceTimeUtc)
    {
        return referenceTimeUtc.AddHours(ResolveSlaHours(entity));
    }

    private static int ResolveSlaHours(RepairRecordEntity entity)
    {
        var urgency = (entity.Urgency ?? string.Empty).Trim();
        var severity = (entity.Severity ?? string.Empty).Trim();

        if (string.Equals(urgency, "非常紧急", StringComparison.OrdinalIgnoreCase)
            || severity.Contains("严重", StringComparison.OrdinalIgnoreCase)
            || severity.Contains("高", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (string.Equals(urgency, "紧急", StringComparison.OrdinalIgnoreCase)
            || severity.Contains("中", StringComparison.OrdinalIgnoreCase))
        {
            return 12;
        }

        return 24;
    }

    public Task<RepairRecordSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            return Task.FromResult(new RepairRecordSummaryDto
            {
                Total = Records.Count,
                PendingCount = Records.Count(x => x.Status == "待处理"),
                InProgressCount = Records.Count(x => x.Status == "处理中"),
                CompletedCount = Records.Count(x => x.Status == "已完成"),
                ClosedCount = Records.Count(x => x.Status == "已关闭")
            });
        }
    }

    public Task<PagedResult<RepairRecordItemDto>> QueryAsync(RepairRecordQuery query, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            IEnumerable<RepairRecordEntity> filtered = Records;

            // 数据范围过滤
            if (query.AccessiblePersonnelNames is { Count: > 0 })
            {
                var nameSet = new HashSet<string>(query.AccessiblePersonnelNames, StringComparer.Ordinal);
                filtered = filtered.Where(x => nameSet.Contains(x.ReporterName));
            }

            if (!string.IsNullOrWhiteSpace(query.HospitalName))
                filtered = filtered.Where(x => x.HospitalName.Contains(query.HospitalName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(query.ReporterName))
                filtered = filtered.Where(x => x.ReporterName.Contains(query.ReporterName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(query.Status))
                filtered = filtered.Where(x => x.Status == query.Status);

            var total = filtered.Count();
            var page = query.Page < 1 ? 1 : query.Page;
            var size = query.Size <= 0 ? 20 : query.Size;
            var workHours = InMemoryWorkHoursService.GetSnapshot();

            var items = filtered
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(x => MapToDto(x, workHours))
                .ToList();

            return Task.FromResult(new PagedResult<RepairRecordItemDto>
            {
                Items = items,
                Total = total,
                Page = page,
                Size = size
            });
        }
    }

    public Task<RepairRecordItemDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var entity = Records.FirstOrDefault(x => x.Id == id);
            if (entity is null)
            {
                return Task.FromResult<RepairRecordItemDto?>(null);
            }

            var workHours = InMemoryWorkHoursService.GetSnapshot();
            return Task.FromResult<RepairRecordItemDto?>(MapToDto(entity, workHours));
        }
    }

    public Task<RepairRecordItemDto> CreateAsync(string reporterName, RepairRecordUpsertDto dto, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var now = DateTime.UtcNow;
            var entity = new RepairRecordEntity
            {
                Id = _nextId++,
                ProjectId = dto.ProjectId,
                HospitalName = dto.HospitalName.Trim(),
                ProductName = dto.ProductName.Trim(),
                ProjectName = dto.ProjectName.Trim(),
                ProductCategory = dto.ProductCategory.Trim(),
                IssueCategory = dto.IssueCategory.Trim(),
                ReporterName = string.IsNullOrWhiteSpace(dto.ReporterName) ? reporterName : dto.ReporterName.Trim(),
                Severity = dto.Severity.Trim(),
                FunctionModule = dto.FunctionModule.Trim(),
                Description = dto.Description.Trim(),
                ReportedAt = dto.ReportedAt,
                ActualWorkHours = dto.ActualWorkHours,
                Content = dto.Content.Trim(),
                Resolution = dto.Resolution.Trim(),
                AttachmentImages = dto.AttachmentImages.Trim(),
                RegistrationStatus = dto.RegistrationStatus.Trim(),
                AssigneeName = dto.AssigneeName?.Trim() ?? string.Empty,
                Status = "待处理",
                Urgency = string.IsNullOrWhiteSpace(dto.Urgency) ? "普通" : dto.Urgency.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            entity.SlaDueAt = CalculateSlaDueAt(entity, now);

            Records.Add(entity);
            SqliteTableStore.Insert(TableName, entity);

            return Task.FromResult(MapToDto(entity));
        }
    }

    public Task<RepairRecordItemDto?> UpdateAsync(long id, RepairRecordUpsertDto dto, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var entity = Records.FirstOrDefault(x => x.Id == id);
            if (entity is null) return Task.FromResult<RepairRecordItemDto?>(null);

            entity.ProjectId = dto.ProjectId;
            entity.HospitalName = dto.HospitalName.Trim();
            entity.ProductName = dto.ProductName.Trim();
            entity.ProjectName = dto.ProjectName.Trim();
            entity.ProductCategory = dto.ProductCategory.Trim();
            entity.IssueCategory = dto.IssueCategory.Trim();
            if (!string.IsNullOrWhiteSpace(dto.ReporterName))
            {
                entity.ReporterName = dto.ReporterName.Trim();
            }
            entity.Severity = dto.Severity.Trim();
            entity.FunctionModule = dto.FunctionModule.Trim();
            entity.Description = dto.Description.Trim();
            entity.ReportedAt = dto.ReportedAt;
            entity.ActualWorkHours = dto.ActualWorkHours;
            entity.Content = dto.Content.Trim();
            entity.Resolution = dto.Resolution.Trim();
            entity.AttachmentImages = dto.AttachmentImages.Trim();
            entity.RegistrationStatus = dto.RegistrationStatus.Trim();
            if (!string.IsNullOrWhiteSpace(dto.AssigneeName))
            {
                entity.AssigneeName = dto.AssigneeName.Trim();
            }
            entity.Status = string.IsNullOrWhiteSpace(dto.Status) ? entity.Status : dto.Status.Trim();
            entity.Urgency = string.IsNullOrWhiteSpace(dto.Urgency) ? entity.Urgency : dto.Urgency.Trim();
            var now = DateTime.UtcNow;
            if (entity.Status == "处理中")
            {
                entity.AcceptedAt ??= now;
                entity.CompletedAt = null;
                entity.SlaDueAt = CalculateSlaDueAt(entity, entity.AcceptedAt.Value);
            }
            else if (entity.Status == "已完成" || entity.Status == "已关闭")
            {
                entity.AcceptedAt ??= now;
                entity.CompletedAt ??= now;
            }
            else
            {
                entity.AcceptedAt = null;
                entity.CompletedAt = null;
                entity.SlaDueAt = CalculateSlaDueAt(entity, now);
            }
            entity.UpdatedAt = now;

            SqliteTableStore.Update(TableName, entity, entity.Id);
            var workHours = InMemoryWorkHoursService.GetSnapshot();
            return Task.FromResult<RepairRecordItemDto?>(MapToDto(entity, workHours));
        }
    }

    private static readonly Dictionary<string, HashSet<string>> ValidTransitions = new()
    {
        ["待处理"] = ["处理中", "已完成", "已关闭"],
        ["处理中"] = ["已完成", "已关闭"],
        ["已完成"] = ["已关闭", "待处理"],
        ["已关闭"] = ["待处理"]
    };

    public Task<RepairRecordItemDto?> AcceptAsync(long id, string assigneeName, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var entity = Records.FirstOrDefault(x => x.Id == id);
            if (entity is null) return Task.FromResult<RepairRecordItemDto?>(null);

            if (entity.Status == "已完成" || entity.Status == "已关闭")
            {
                throw new InvalidOperationException("已完成或已关闭的报修不能直接签收");
            }

            var now = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(assigneeName))
            {
                entity.AssigneeName = assigneeName.Trim();
            }

            entity.Status = "处理中";
            entity.AcceptedAt ??= now;
            entity.CompletedAt = null;
            entity.SlaDueAt = CalculateSlaDueAt(entity, entity.AcceptedAt.Value);
            entity.UpdatedAt = now;

            SqliteTableStore.Update(TableName, entity, entity.Id);
            var workHours = InMemoryWorkHoursService.GetSnapshot();
            return Task.FromResult<RepairRecordItemDto?>(MapToDto(entity, workHours));
        }
    }

    public Task<RepairRecordItemDto?> ResolveAsync(long id, string resolution, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var entity = Records.FirstOrDefault(x => x.Id == id);
            if (entity is null) return Task.FromResult<RepairRecordItemDto?>(null);

            if (entity.Status == "已完成" || entity.Status == "已关闭")
            {
                throw new InvalidOperationException("当前报修已完成或已关闭，无需重复完成");
            }

            if (string.IsNullOrWhiteSpace(resolution))
            {
                throw new InvalidOperationException("请填写处理结果");
            }

            var now = DateTime.UtcNow;
            entity.AcceptedAt ??= now;
            entity.Status = "已完成";
            entity.Resolution = resolution.Trim();
            entity.CompletedAt = now;
            entity.SlaDueAt ??= CalculateSlaDueAt(entity, entity.AcceptedAt.Value);
            entity.UpdatedAt = now;

            SqliteTableStore.Update(TableName, entity, entity.Id);
            var workHours = InMemoryWorkHoursService.GetSnapshot();
            return Task.FromResult<RepairRecordItemDto?>(MapToDto(entity, workHours));
        }
    }

    public Task<RepairRecordItemDto?> ReopenAsync(long id, string? reason, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var entity = Records.FirstOrDefault(x => x.Id == id);
            if (entity is null) return Task.FromResult<RepairRecordItemDto?>(null);

            if (entity.Status != "已完成" && entity.Status != "已关闭")
            {
                throw new InvalidOperationException("仅已完成或已关闭的报修可重开");
            }

            var now = DateTime.UtcNow;
            entity.Status = "待处理";
            entity.AcceptedAt = null;
            entity.CompletedAt = null;
            entity.SlaDueAt = CalculateSlaDueAt(entity, now);
            entity.UpdatedAt = now;

            if (!string.IsNullOrWhiteSpace(reason))
            {
                entity.Content = string.IsNullOrWhiteSpace(entity.Content)
                    ? $"重开原因：{reason.Trim()}"
                    : $"{entity.Content}\n重开原因：{reason.Trim()}";
            }

            SqliteTableStore.Update(TableName, entity, entity.Id);
            var workHours = InMemoryWorkHoursService.GetSnapshot();
            return Task.FromResult<RepairRecordItemDto?>(MapToDto(entity, workHours));
        }
    }

    public Task<RepairRecordItemDto?> TransitionStatusAsync(long id, string newStatus, string? resolution, CancellationToken cancellationToken = default)
    {
        if (newStatus == "处理中")
        {
            return AcceptAsync(id, string.Empty, cancellationToken);
        }

        if (newStatus == "已完成")
        {
            return ResolveAsync(id, resolution ?? string.Empty, cancellationToken);
        }

        if (newStatus == "待处理")
        {
            return ReopenAsync(id, resolution, cancellationToken);
        }

        lock (SyncRoot)
        {
            var entity = Records.FirstOrDefault(x => x.Id == id);
            if (entity is null) return Task.FromResult<RepairRecordItemDto?>(null);

            if (!ValidTransitions.TryGetValue(entity.Status, out var allowed) || !allowed.Contains(newStatus))
            {
                throw new InvalidOperationException($"不允许从 '{entity.Status}' 转换到 '{newStatus}'");
            }

            entity.Status = newStatus;
            if (!string.IsNullOrWhiteSpace(resolution))
            {
                entity.Resolution = resolution.Trim();
            }
            var now = DateTime.UtcNow;
            if (newStatus == "已关闭")
            {
                entity.AcceptedAt ??= now;
                entity.CompletedAt ??= now;
            }
            entity.UpdatedAt = now;

            SqliteTableStore.Update(TableName, entity, entity.Id);
            var workHours = InMemoryWorkHoursService.GetSnapshot();
            return Task.FromResult<RepairRecordItemDto?>(MapToDto(entity, workHours));
        }
    }

    public Task<RepairRecordItemDto?> AssignAsync(long id, string assigneeName, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var entity = Records.FirstOrDefault(x => x.Id == id);
            if (entity is null) return Task.FromResult<RepairRecordItemDto?>(null);

            entity.AssigneeName = assigneeName.Trim();
            var now = DateTime.UtcNow;
            if (entity.Status == "待处理")
            {
                entity.Status = "处理中";
                entity.AcceptedAt ??= now;
                entity.SlaDueAt = CalculateSlaDueAt(entity, entity.AcceptedAt.Value);
            }
            entity.UpdatedAt = now;

            SqliteTableStore.Update(TableName, entity, entity.Id);
            var workHours = InMemoryWorkHoursService.GetSnapshot();
            return Task.FromResult<RepairRecordItemDto?>(MapToDto(entity, workHours));
        }
    }

    public Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var removed = Records.RemoveAll(x => x.Id == id);
            if (removed > 0) SqliteTableStore.Delete(TableName, id);
            return Task.FromResult(removed > 0);
        }
    }

    private static RepairRecordItemDto MapToDto(RepairRecordEntity entity, IReadOnlyList<WorkHoursEntity>? workHours = null)
    {
        var workHoursDetail = BuildWorkHoursDetail(entity, workHours ?? InMemoryWorkHoursService.GetSnapshot());
        var effectiveSlaDueAt = entity.SlaDueAt;
        if (effectiveSlaDueAt is null && entity.Status != "已完成" && entity.Status != "已关闭")
        {
            effectiveSlaDueAt = CalculateSlaDueAt(entity, entity.AcceptedAt ?? entity.CreatedAt);
        }

        return new RepairRecordItemDto
        {
            Id = entity.Id,
            ProjectId = entity.ProjectId,
            HospitalName = entity.HospitalName,
            ProductName = entity.ProductName,
            ProjectName = entity.ProjectName,
            ProductCategory = entity.ProductCategory,
            IssueCategory = entity.IssueCategory,
            ReporterName = entity.ReporterName,
            Severity = entity.Severity,
            FunctionModule = entity.FunctionModule,
            Description = entity.Description,
            ReportedAt = entity.ReportedAt,
            ActualWorkHours = entity.ActualWorkHours,
            Content = entity.Content,
            Resolution = entity.Resolution,
            AttachmentImages = entity.AttachmentImages,
            RegistrationStatus = entity.RegistrationStatus,
            WorkHoursDetail = workHoursDetail,
            AssigneeName = entity.AssigneeName,
            AcceptedAt = entity.AcceptedAt,
            SlaDueAt = effectiveSlaDueAt,
            CompletedAt = entity.CompletedAt,
            Status = entity.Status,
            Urgency = entity.Urgency,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static string BuildWorkHoursDetail(RepairRecordEntity entity, IReadOnlyList<WorkHoursEntity> allWorkHours)
    {
        var related = allWorkHours
            .Where(x => IsRelatedWorkHour(entity, x))
            .OrderByDescending(x => x.WorkDate)
            .ThenByDescending(x => x.CreatedAt)
            .Take(5)
            .ToList();

        if (related.Count == 0)
        {
            return string.Empty;
        }

        var totalHours = related.Sum(x => x.Hours);
        var lines = related.Select(x =>
            $"{x.WorkDate} {x.PersonnelName} {x.Hours:0.##}h {x.WorkType}");

        var sb = new StringBuilder();
        sb.Append($"合计{totalHours:0.##}h；");
        sb.Append(string.Join("；", lines));
        return sb.ToString();
    }

    private static bool IsRelatedWorkHour(RepairRecordEntity repair, WorkHoursEntity work)
    {
        if (repair.ProjectId > 0 && work.ProjectId > 0)
        {
            return repair.ProjectId == work.ProjectId;
        }

        if (!string.IsNullOrWhiteSpace(repair.HospitalName)
            && !string.Equals(repair.HospitalName, work.HospitalName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(repair.ReporterName)
            && !string.Equals(repair.ReporterName, work.PersonnelName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    internal static void PersistAll()
    {
        SqliteTableStore.ReplaceAll(TableName, Records);
    }
}
