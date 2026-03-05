using PMS.Application.Contracts.MonthlyReport;
using PMS.Application.Models;
using PMS.Application.Models.MonthlyReport;
using PMS.Domain.Entities;

namespace PMS.Infrastructure.Services;

public class InMemoryMonthlyReportService : IMonthlyReportService
{
    private const string StateKey = "monthly_reports";
    private static readonly object SyncRoot = new();
    private static readonly List<MonthlyReportEntity> Records = SqliteJsonStore.LoadOrSeed(StateKey, () => new List<MonthlyReportEntity>());
    private static long _nextId = Records.Count > 0 ? Records.Max(x => x.Id) + 1 : 1;

    public Task<PagedResult<MonthlyReportItemDto>> QueryAsync(MonthlyReportQuery query, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            IEnumerable<MonthlyReportEntity> filtered = Records;

            if (!string.IsNullOrWhiteSpace(query.HospitalName))
                filtered = filtered.Where(x => x.HospitalName.Contains(query.HospitalName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(query.ReportMonth))
                filtered = filtered.Where(x => x.ReportMonth == query.ReportMonth);

            if (!string.IsNullOrWhiteSpace(query.SubmittedBy))
                filtered = filtered.Where(x => x.SubmittedBy.Contains(query.SubmittedBy, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(query.Status))
                filtered = filtered.Where(x => string.Equals(x.Status, query.Status, StringComparison.OrdinalIgnoreCase));

            var total = filtered.Count();
            var page = query.Page < 1 ? 1 : query.Page;
            var size = query.Size <= 0 ? 20 : query.Size;

            var items = filtered
                .OrderByDescending(x => x.ReportMonth)
                .ThenByDescending(x => x.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(MapToDto)
                .ToList();

            return Task.FromResult(new PagedResult<MonthlyReportItemDto>
            {
                Items = items,
                Total = total,
                Page = page,
                Size = size
            });
        }
    }

    public Task<MonthlyReportItemDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var entity = Records.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(entity is null ? null : MapToDto(entity));
        }
    }

    public Task<MonthlyReportItemDto> CreateAsync(string submittedBy, MonthlyReportUpsertDto dto, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var entity = new MonthlyReportEntity
            {
                Id = _nextId++,
                HospitalName = dto.HospitalName.Trim(),
                ReportMonth = dto.ReportMonth.Trim(),
                SubmittedBy = submittedBy,
                Title = dto.Title.Trim(),
                Content = dto.Content.Trim(),
                Attachments = dto.Attachments ?? [],
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "draft" : dto.Status.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            Records.Add(entity);
            Persist();

            return Task.FromResult(MapToDto(entity));
        }
    }

    public Task<MonthlyReportItemDto?> UpdateAsync(long id, MonthlyReportUpsertDto dto, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var entity = Records.FirstOrDefault(x => x.Id == id);
            if (entity is null) return Task.FromResult<MonthlyReportItemDto?>(null);

            entity.HospitalName = dto.HospitalName.Trim();
            entity.ReportMonth = dto.ReportMonth.Trim();
            entity.Title = dto.Title.Trim();
            entity.Content = dto.Content.Trim();
            entity.Attachments = dto.Attachments ?? entity.Attachments;
            entity.Status = string.IsNullOrWhiteSpace(dto.Status) ? entity.Status : dto.Status.Trim();
            entity.UpdatedAt = DateTime.UtcNow;

            Persist();
            return Task.FromResult<MonthlyReportItemDto?>(MapToDto(entity));
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

    private static MonthlyReportItemDto MapToDto(MonthlyReportEntity entity)
    {
        return new MonthlyReportItemDto
        {
            Id = entity.Id,
            HospitalName = entity.HospitalName,
            ReportMonth = entity.ReportMonth,
            SubmittedBy = entity.SubmittedBy,
            Title = entity.Title,
            Content = entity.Content,
            Attachments = entity.Attachments,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static void Persist()
    {
        SqliteJsonStore.Save(StateKey, Records);
    }
}
