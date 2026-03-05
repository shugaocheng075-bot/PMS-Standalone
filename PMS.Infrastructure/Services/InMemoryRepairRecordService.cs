using PMS.Application.Contracts.RepairRecord;
using PMS.Application.Models;
using PMS.Application.Models.RepairRecord;
using PMS.Domain.Entities;
using System.Text;

namespace PMS.Infrastructure.Services;

public class InMemoryRepairRecordService : IRepairRecordService
{
    private const string StateKey = "repair_records";
    private static readonly object SyncRoot = new();
    private static readonly List<RepairRecordEntity> Records = SqliteJsonStore.LoadOrSeed(StateKey, () => new List<RepairRecordEntity>());
    private static long _nextId = Records.Count > 0 ? Records.Max(x => x.Id) + 1 : 1;

    public Task<RepairRecordSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            return Task.FromResult(new RepairRecordSummaryDto
            {
                Total = Records.Count,
                PendingCount = Records.Count(x => x.Status == "待处理"),
                InProgressCount = Records.Count(x => x.Status == "处理中"),
                CompletedCount = Records.Count(x => x.Status == "已完成")
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
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "待处理" : dto.Status.Trim(),
                Urgency = string.IsNullOrWhiteSpace(dto.Urgency) ? "普通" : dto.Urgency.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            Records.Add(entity);
            Persist();

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
            entity.Status = string.IsNullOrWhiteSpace(dto.Status) ? entity.Status : dto.Status.Trim();
            entity.Urgency = string.IsNullOrWhiteSpace(dto.Urgency) ? entity.Urgency : dto.Urgency.Trim();
            entity.UpdatedAt = DateTime.UtcNow;

            Persist();
            var workHours = InMemoryWorkHoursService.GetSnapshot();
            return Task.FromResult<RepairRecordItemDto?>(MapToDto(entity, workHours));
        }
    }

    public Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var removed = Records.RemoveAll(x => x.Id == id);
            if (removed > 0) Persist();
            return Task.FromResult(removed > 0);
        }
    }

    private static RepairRecordItemDto MapToDto(RepairRecordEntity entity, IReadOnlyList<WorkHoursEntity>? workHours = null)
    {
        var workHoursDetail = BuildWorkHoursDetail(entity, workHours ?? InMemoryWorkHoursService.GetSnapshot());

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

    private static void Persist()
    {
        SqliteJsonStore.Save(StateKey, Records);
    }
}
