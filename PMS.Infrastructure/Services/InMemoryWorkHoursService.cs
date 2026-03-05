using PMS.Application.Contracts.WorkHours;
using PMS.Application.Models;
using PMS.Application.Models.WorkHours;
using PMS.Domain.Entities;

namespace PMS.Infrastructure.Services;

public class InMemoryWorkHoursService : IWorkHoursService
{
    private const string StateKey = "work_hours";
    private static readonly object SyncRoot = new();
    private static readonly List<WorkHoursEntity> Records = SqliteJsonStore.LoadOrSeed(StateKey, () => new List<WorkHoursEntity>());
    private static long _nextId = Records.Count > 0 ? Records.Max(x => x.Id) + 1 : 1;

    public static IReadOnlyList<WorkHoursEntity> GetSnapshot()
    {
        lock (SyncRoot)
        {
            return Records
                .Select(x => new WorkHoursEntity
                {
                    Id = x.Id,
                    ProjectId = x.ProjectId,
                    PersonnelName = x.PersonnelName,
                    HospitalName = x.HospitalName,
                    WorkDate = x.WorkDate,
                    Hours = x.Hours,
                    WorkType = x.WorkType,
                    Description = x.Description,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToList();
        }
    }

    public Task<WorkHoursSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            return Task.FromResult(new WorkHoursSummaryDto
            {
                Total = Records.Count,
                TotalHours = Records.Sum(x => x.Hours),
                OnsiteCount = Records.Count(x => x.WorkType == "驻场"),
                RemoteCount = Records.Count(x => x.WorkType == "远程"),
                TravelCount = Records.Count(x => x.WorkType == "出差")
            });
        }
    }

    public Task<PagedResult<WorkHoursItemDto>> QueryAsync(WorkHoursQuery query, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            IEnumerable<WorkHoursEntity> filtered = Records;

            // 数据范围过滤
            if (query.AccessiblePersonnelNames is { Count: > 0 })
            {
                var nameSet = new HashSet<string>(query.AccessiblePersonnelNames, StringComparer.Ordinal);
                filtered = filtered.Where(x => nameSet.Contains(x.PersonnelName));
            }

            if (!string.IsNullOrWhiteSpace(query.PersonnelName))
                filtered = filtered.Where(x => x.PersonnelName.Contains(query.PersonnelName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(query.HospitalName))
                filtered = filtered.Where(x => x.HospitalName.Contains(query.HospitalName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(query.WorkType))
                filtered = filtered.Where(x => x.WorkType == query.WorkType);

            if (!string.IsNullOrWhiteSpace(query.WorkDateFrom))
                filtered = filtered.Where(x => string.Compare(x.WorkDate, query.WorkDateFrom, StringComparison.Ordinal) >= 0);

            if (!string.IsNullOrWhiteSpace(query.WorkDateTo))
                filtered = filtered.Where(x => string.Compare(x.WorkDate, query.WorkDateTo, StringComparison.Ordinal) <= 0);

            var total = filtered.Count();
            var page = query.Page < 1 ? 1 : query.Page;
            var size = query.Size <= 0 ? 20 : query.Size;

            var items = filtered
                .OrderByDescending(x => x.WorkDate)
                .ThenByDescending(x => x.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(MapToDto)
                .ToList();

            return Task.FromResult(new PagedResult<WorkHoursItemDto>
            {
                Items = items,
                Total = total,
                Page = page,
                Size = size
            });
        }
    }

    public Task<WorkHoursItemDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var entity = Records.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(entity is null ? null : MapToDto(entity));
        }
    }

    public Task<WorkHoursItemDto> CreateAsync(string personnelName, WorkHoursUpsertDto dto, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var entity = new WorkHoursEntity
            {
                Id = _nextId++,
                ProjectId = dto.ProjectId,
                PersonnelName = personnelName,
                HospitalName = dto.HospitalName.Trim(),
                WorkDate = dto.WorkDate.Trim(),
                Hours = dto.Hours,
                WorkType = string.IsNullOrWhiteSpace(dto.WorkType) ? "驻场" : dto.WorkType.Trim(),
                Description = dto.Description.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            Records.Add(entity);
            Persist();

            return Task.FromResult(MapToDto(entity));
        }
    }

    public Task<WorkHoursItemDto?> UpdateAsync(long id, WorkHoursUpsertDto dto, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var entity = Records.FirstOrDefault(x => x.Id == id);
            if (entity is null) return Task.FromResult<WorkHoursItemDto?>(null);

            entity.ProjectId = dto.ProjectId;
            entity.HospitalName = dto.HospitalName.Trim();
            entity.WorkDate = dto.WorkDate.Trim();
            entity.Hours = dto.Hours;
            entity.WorkType = string.IsNullOrWhiteSpace(dto.WorkType) ? entity.WorkType : dto.WorkType.Trim();
            entity.Description = dto.Description.Trim();
            entity.UpdatedAt = DateTime.UtcNow;

            Persist();
            return Task.FromResult<WorkHoursItemDto?>(MapToDto(entity));
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

    private static WorkHoursItemDto MapToDto(WorkHoursEntity entity)
    {
        return new WorkHoursItemDto
        {
            Id = entity.Id,
            ProjectId = entity.ProjectId,
            PersonnelName = entity.PersonnelName,
            HospitalName = entity.HospitalName,
            WorkDate = entity.WorkDate,
            Hours = entity.Hours,
            WorkType = entity.WorkType,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static void Persist()
    {
        SqliteJsonStore.Save(StateKey, Records);
    }
}
