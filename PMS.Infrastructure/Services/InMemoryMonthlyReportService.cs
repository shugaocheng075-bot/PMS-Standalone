using PMS.Application.Contracts.MonthlyReport;
using PMS.Application.Models;
using PMS.Application.Models.MonthlyReport;
using PMS.Domain.Entities;

namespace PMS.Infrastructure.Services;

public class InMemoryMonthlyReportService : IMonthlyReportService
{
    private const string TableName = "MonthlyReports";
    private const string LegacyJsonKey = "monthly_reports";
    private static readonly object SyncRoot = new();
    private static readonly List<MonthlyReportEntity> Records = SqliteTableStore.LoadAll<MonthlyReportEntity>(TableName, LegacyJsonKey);
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

            if (!string.IsNullOrWhiteSpace(query.GroupName))
                filtered = filtered.Where(x => x.GroupName == query.GroupName);

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
                GroupName = (dto.GroupName ?? "").Trim(),
                SubmittedBy = submittedBy,
                Title = dto.Title.Trim(),
                Content = dto.Content.Trim(),
                TeamTotal = dto.TeamTotal ?? 0,
                TeamOnsiteJson = dto.TeamOnsiteJson ?? "",
                TeamSummaryJson = dto.TeamSummaryJson ?? "{}",
                ProjectOverviewJson = dto.ProjectOverviewJson ?? "{}",
                PerCapitaMetricsJson = dto.PerCapitaMetricsJson ?? "{}",
                HandoverItemsJson = dto.HandoverItemsJson ?? "",
                WeeklyReportRate = dto.WeeklyReportRate ?? 0m,
                MonthlyReportRate = dto.MonthlyReportRate ?? 0m,
                MajorDemandAcceptanceJson = dto.MajorDemandAcceptanceJson ?? "",
                InspectionRecordsJson = dto.InspectionRecordsJson ?? "",
                AnnualServiceReportsJson = dto.AnnualServiceReportsJson ?? "",
                IncidentsJson = dto.IncidentsJson ?? "",
                NextMonthInspectionPlanJson = dto.NextMonthInspectionPlanJson ?? "",
                NextMonthAnnualReportPlanJson = dto.NextMonthAnnualReportPlanJson ?? "",
                NextMonthOtherPlanJson = dto.NextMonthOtherPlanJson ?? "",
                Attachments = dto.Attachments ?? [],
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "draft" : dto.Status.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            Records.Add(entity);
            SqliteTableStore.Insert(TableName, entity);

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
            entity.GroupName = (dto.GroupName ?? entity.GroupName).Trim();
            entity.Title = dto.Title.Trim();
            entity.Content = dto.Content.Trim();
            if (dto.TeamTotal.HasValue) entity.TeamTotal = dto.TeamTotal.Value;
            if (dto.TeamOnsiteJson != null) entity.TeamOnsiteJson = dto.TeamOnsiteJson;
            if (dto.TeamSummaryJson != null) entity.TeamSummaryJson = dto.TeamSummaryJson;
            if (dto.ProjectOverviewJson != null) entity.ProjectOverviewJson = dto.ProjectOverviewJson;
            if (dto.PerCapitaMetricsJson != null) entity.PerCapitaMetricsJson = dto.PerCapitaMetricsJson;
            if (dto.HandoverItemsJson != null) entity.HandoverItemsJson = dto.HandoverItemsJson;
            if (dto.WeeklyReportRate.HasValue) entity.WeeklyReportRate = dto.WeeklyReportRate.Value;
            if (dto.MonthlyReportRate.HasValue) entity.MonthlyReportRate = dto.MonthlyReportRate.Value;
            if (dto.MajorDemandAcceptanceJson != null) entity.MajorDemandAcceptanceJson = dto.MajorDemandAcceptanceJson;
            if (dto.InspectionRecordsJson != null) entity.InspectionRecordsJson = dto.InspectionRecordsJson;
            if (dto.AnnualServiceReportsJson != null) entity.AnnualServiceReportsJson = dto.AnnualServiceReportsJson;
            if (dto.IncidentsJson != null) entity.IncidentsJson = dto.IncidentsJson;
            if (dto.NextMonthInspectionPlanJson != null) entity.NextMonthInspectionPlanJson = dto.NextMonthInspectionPlanJson;
            if (dto.NextMonthAnnualReportPlanJson != null) entity.NextMonthAnnualReportPlanJson = dto.NextMonthAnnualReportPlanJson;
            if (dto.NextMonthOtherPlanJson != null) entity.NextMonthOtherPlanJson = dto.NextMonthOtherPlanJson;
            entity.Attachments = dto.Attachments ?? entity.Attachments;
            entity.Status = string.IsNullOrWhiteSpace(dto.Status) ? entity.Status : dto.Status.Trim();
            entity.UpdatedAt = DateTime.UtcNow;

            SqliteTableStore.Update(TableName, entity, entity.Id);
            return Task.FromResult<MonthlyReportItemDto?>(MapToDto(entity));
        }
    }

    public Task<MonthlyReportItemDto?> SubmitAsync(long id, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var entity = Records.FirstOrDefault(x => x.Id == id);
            if (entity is null) return Task.FromResult<MonthlyReportItemDto?>(null);
            if (entity.Status != "draft" && entity.Status != "rejected")
                throw new InvalidOperationException($"仅草稿或已驳回的月报可以提交，当前状态：{entity.Status}");

            entity.Status = "submitted";
            entity.RejectionReason = string.Empty;
            entity.UpdatedAt = DateTime.UtcNow;
            SqliteTableStore.Update(TableName, entity, entity.Id);
            return Task.FromResult<MonthlyReportItemDto?>(MapToDto(entity));
        }
    }

    public Task<MonthlyReportItemDto?> ApproveAsync(long id, string approvedBy, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var entity = Records.FirstOrDefault(x => x.Id == id);
            if (entity is null) return Task.FromResult<MonthlyReportItemDto?>(null);
            if (entity.Status != "submitted")
                throw new InvalidOperationException($"仅已提交的月报可以审批，当前状态：{entity.Status}");

            entity.Status = "approved";
            entity.ApprovedBy = approvedBy;
            entity.ApprovedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            SqliteTableStore.Update(TableName, entity, entity.Id);
            return Task.FromResult<MonthlyReportItemDto?>(MapToDto(entity));
        }
    }

    public Task<MonthlyReportItemDto?> RejectAsync(long id, string rejectedBy, string? reason, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var entity = Records.FirstOrDefault(x => x.Id == id);
            if (entity is null) return Task.FromResult<MonthlyReportItemDto?>(null);
            if (entity.Status != "submitted")
                throw new InvalidOperationException($"仅已提交的月报可以驳回，当前状态：{entity.Status}");

            entity.Status = "rejected";
            entity.ApprovedBy = rejectedBy;
            entity.RejectionReason = reason?.Trim() ?? string.Empty;
            entity.UpdatedAt = DateTime.UtcNow;
            SqliteTableStore.Update(TableName, entity, entity.Id);
            return Task.FromResult<MonthlyReportItemDto?>(MapToDto(entity));
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

    private static MonthlyReportItemDto MapToDto(MonthlyReportEntity entity)
    {
        return new MonthlyReportItemDto
        {
            Id = entity.Id,
            HospitalName = entity.HospitalName,
            ReportMonth = entity.ReportMonth,
            GroupName = entity.GroupName,
            SubmittedBy = entity.SubmittedBy,
            Title = entity.Title,
            Content = entity.Content,
            TeamTotal = entity.TeamTotal,
            TeamOnsiteJson = entity.TeamOnsiteJson,
            TeamSummaryJson = entity.TeamSummaryJson,
            ProjectOverviewJson = entity.ProjectOverviewJson,
            PerCapitaMetricsJson = entity.PerCapitaMetricsJson,
            HandoverItemsJson = entity.HandoverItemsJson,
            WeeklyReportRate = entity.WeeklyReportRate,
            MonthlyReportRate = entity.MonthlyReportRate,
            MajorDemandAcceptanceJson = entity.MajorDemandAcceptanceJson,
            InspectionRecordsJson = entity.InspectionRecordsJson,
            AnnualServiceReportsJson = entity.AnnualServiceReportsJson,
            IncidentsJson = entity.IncidentsJson,
            NextMonthInspectionPlanJson = entity.NextMonthInspectionPlanJson,
            NextMonthAnnualReportPlanJson = entity.NextMonthAnnualReportPlanJson,
            NextMonthOtherPlanJson = entity.NextMonthOtherPlanJson,
            Attachments = entity.Attachments,
            Status = entity.Status,
            ApprovedBy = entity.ApprovedBy,
            ApprovedAt = entity.ApprovedAt,
            RejectionReason = entity.RejectionReason,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static void PersistAll()
    {
        SqliteTableStore.ReplaceAll(TableName, Records);
    }
}
