using PMS.Application.Contracts.Inspection;
using PMS.Application.Models;
using PMS.Application.Models.Inspection;

namespace PMS.Infrastructure.Services;

public class InMemoryInspectionService : IInspectionService
{
    private static readonly object SyncRoot = new();
    private const string ResultsTable = "InspectionResults";
    private const string ResultsLegacyKey = "inspection_results";
    private const string PlanOverridesTable = "InspectionPlanOverrides";
    private const string PlanOverridesLegacyKey = "inspection_plan_overrides";
    private const string PlanCustomRowsTable = "InspectionPlanCustomRows";
    private const string PlanCustomRowsLegacyKey = "inspection_plan_custom_rows";
    private const string PlanDeletedIdsTable = "InspectionPlanDeletedIds";
    private const string PlanDeletedIdsLegacyKey = "inspection_plan_deleted_ids";

    private static readonly List<InspectionResultDto> StoredResults =
        SqliteTableStore.LoadAll<InspectionResultDto>(ResultsTable, ResultsLegacyKey);
    private static readonly List<InspectionPlanItemDto> PlanOverrides =
        SqliteTableStore.LoadAll<InspectionPlanItemDto>(PlanOverridesTable, PlanOverridesLegacyKey);
    private static readonly List<InspectionPlanItemDto> PlanCustomRows =
        SqliteTableStore.LoadAll<InspectionPlanItemDto>(PlanCustomRowsTable, PlanCustomRowsLegacyKey);
    private static readonly List<DeletedInspectionPlanId> DeletedPlanIds =
        SqliteTableStore.LoadAll<DeletedInspectionPlanId>(PlanDeletedIdsTable, PlanDeletedIdsLegacyKey);

    private static long _nextResultId = StoredResults.Count > 0
        ? StoredResults.Max(r => r.Id) + 1
        : 1;
    private static long _nextPlanId = Math.Max(
        InMemoryProjectDataStore.Projects.Count > 0 ? InMemoryProjectDataStore.Projects.Max(p => p.Id) + 1 : 1,
        PlanCustomRows.Count > 0 ? PlanCustomRows.Max(x => x.Id) + 1 : 1);

    // ─── 巡检计划（种子数据） ───

    public Task<InspectionSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var seed = BuildSeed();
        var now = DateTime.Today;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var summary = new InspectionSummaryDto
        {
            PlannedCount = seed.Count(x => x.Status == "已计划"),
            InProgressCount = seed.Count(x => x.Status == "执行中"),
            CompletedCount = seed.Count(x => x.Status == "已完成"),
            CancelledCount = seed.Count(x => x.Status == "已取消"),
            ThisMonthCount = seed.Count(x => x.PlanDate >= monthStart && x.PlanDate < monthEnd),
            Total = seed.Count
        };

        return Task.FromResult(summary);
    }

    public Task<PagedResult<InspectionPlanItemDto>> QueryAsync(InspectionQuery query, CancellationToken cancellationToken = default)
    {
        IEnumerable<InspectionPlanItemDto> filtered = BuildSeed();

        if (!string.IsNullOrWhiteSpace(query.Status))
            filtered = filtered.Where(x => SmartTextMatcher.MatchExact(x.Status, query.Status));

        if (!string.IsNullOrWhiteSpace(query.HospitalName))
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.HospitalName, query.HospitalName));

        if (!string.IsNullOrWhiteSpace(query.Province))
            filtered = filtered.Where(x => x.Province.Equals(query.Province, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.ProductName))
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.ProductName, query.ProductName));

        if (!string.IsNullOrWhiteSpace(query.GroupName))
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.GroupName, query.GroupName));

        if (!string.IsNullOrWhiteSpace(query.Inspector))
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.Inspector, query.Inspector));

        if (!string.IsNullOrWhiteSpace(query.InspectionType))
            filtered = filtered.Where(x => SmartTextMatcher.MatchExact(x.InspectionType, query.InspectionType));

        if (!string.IsNullOrWhiteSpace(query.Priority))
            filtered = filtered.Where(x => SmartTextMatcher.MatchExact(x.Priority, query.Priority));

        var total = filtered.Count();
        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.Size <= 0 ? 20 : query.Size;

        var items = filtered
            .OrderBy(x => x.Status == "已完成" ? 1 : 0)
            .ThenBy(x => x.PlanDate)
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return Task.FromResult(new PagedResult<InspectionPlanItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            Size = size
        });
    }

    public Task<InspectionPlanItemDto?> UpdateAsync(long id, InspectionPlanUpsertDto dto, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var custom = PlanCustomRows.FirstOrDefault(x => x.Id == id);
            if (custom is not null)
            {
                ApplyPlanUpsert(custom, dto);
                PersistCustomRows();
                return Task.FromResult<InspectionPlanItemDto?>(custom);
            }

            var current = BuildSeed().FirstOrDefault(x => x.Id == id);
            if (current is null)
            {
                return Task.FromResult<InspectionPlanItemDto?>(null);
            }

            ApplyPlanUpsert(current, dto);

            var existingIndex = PlanOverrides.FindIndex(x => x.Id == id);
            if (existingIndex >= 0)
            {
                PlanOverrides[existingIndex] = current;
            }
            else
            {
                PlanOverrides.Add(current);
            }

            PersistPlanOverrides();

            SyncBackToProject(current);
            return Task.FromResult<InspectionPlanItemDto?>(current);
        }
    }

    public Task<InspectionPlanItemDto> CreateAsync(InspectionPlanUpsertDto dto, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var item = new InspectionPlanItemDto
            {
                Id = _nextPlanId++,
                HospitalName = (dto.HospitalName ?? "未填写医院").Trim(),
                ProductName = (dto.ProductName ?? "未填写产品").Trim(),
                Province = (dto.Province ?? string.Empty).Trim(),
                HospitalLevel = (dto.HospitalLevel ?? string.Empty).Trim(),
                GroupName = (dto.GroupName ?? string.Empty).Trim(),
                Inspector = (dto.Inspector ?? "未分配").Trim(),
                PlanDate = dto.PlanDate ?? DateTime.Today,
                ActualDate = dto.ActualDate,
                Status = (dto.Status ?? "已计划").Trim(),
                InspectionType = (dto.InspectionType ?? "远程").Trim(),
                Priority = (dto.Priority ?? "中").Trim(),
                Remarks = (dto.Remarks ?? string.Empty).Trim(),
            };

            PlanCustomRows.Add(item);
            PersistCustomRows();
            return Task.FromResult(item);
        }
    }

    public Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var customRemoved = PlanCustomRows.RemoveAll(x => x.Id == id) > 0;
            if (customRemoved)
            {
                PersistCustomRows();
                return Task.FromResult(true);
            }

            var hasProjectSeed = InMemoryProjectDataStore.Projects.Any(p => p.Id == id);
            if (!hasProjectSeed)
            {
                return Task.FromResult(false);
            }

            if (DeletedPlanIds.All(x => x.Id != id))
            {
                DeletedPlanIds.Add(new DeletedInspectionPlanId { Id = id });
                PersistDeletedIds();
            }

            PlanOverrides.RemoveAll(x => x.Id == id);
            PersistPlanOverrides();
            return Task.FromResult(true);
        }
    }

    // ─── SystemAuditTool 巡检结果 ───

    public Task SubmitResultAsync(InspectionResultDto result, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            result.Id = _nextResultId++;
            StoredResults.Add(result);
            PersistResults();
        }

        return Task.CompletedTask;
    }

    public Task SubmitResultsAsync(IReadOnlyList<InspectionResultDto> results, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            foreach (var result in results)
            {
                result.Id = _nextResultId++;
                StoredResults.Add(result);
            }

            PersistResults();
        }

        return Task.CompletedTask;
    }

    public Task<PagedResult<InspectionResultDto>> QueryResultsAsync(InspectionResultQuery query, CancellationToken cancellationToken = default)
    {
        List<InspectionResultDto> snapshot;
        lock (SyncRoot)
        {
            snapshot = [.. StoredResults];
        }

        IEnumerable<InspectionResultDto> filtered = snapshot;

        if (!string.IsNullOrWhiteSpace(query.HospitalName))
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.HospitalName, query.HospitalName));

        if (!string.IsNullOrWhiteSpace(query.ProductName))
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.ProductName, query.ProductName));

        if (!string.IsNullOrWhiteSpace(query.Inspector))
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.Inspector, query.Inspector));

        if (!string.IsNullOrWhiteSpace(query.HealthLevel))
            filtered = filtered.Where(x => x.HealthLevel.Equals(query.HealthLevel, StringComparison.OrdinalIgnoreCase));

        if (query.From.HasValue)
            filtered = filtered.Where(x => x.InspectedAt >= query.From.Value);

        if (query.To.HasValue)
            filtered = filtered.Where(x => x.InspectedAt <= query.To.Value);

        var total = filtered.Count();
        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.Size <= 0 ? 20 : query.Size;

        var items = filtered
            .OrderByDescending(x => x.InspectedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return Task.FromResult(new PagedResult<InspectionResultDto>
        {
            Items = items,
            Total = total,
            Page = page,
            Size = size
        });
    }

    public Task<InspectionResultDto?> GetLatestResultAsync(string hospitalName, string productName, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var latest = StoredResults
                .Where(x => x.HospitalName.Equals(hospitalName, StringComparison.OrdinalIgnoreCase)
                         && x.ProductName.Equals(productName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.InspectedAt)
                .FirstOrDefault();

            return Task.FromResult(latest);
        }
    }

    // ─── 持久化 ───

    public Task<bool> ReviewResultAsync(long resultId, string reviewStatus, string reviewedBy, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var result = StoredResults.FirstOrDefault(x => x.Id == resultId);
            if (result is null) return Task.FromResult(false);
            result.ReviewStatus = reviewStatus;
            result.ReviewedBy = reviewedBy;
            result.ReviewedAt = DateTime.UtcNow;
            PersistResults();
            return Task.FromResult(true);
        }
    }

    private static void PersistResults()
    {
        SqliteTableStore.ReplaceAll(ResultsTable, StoredResults);
    }

    private static void PersistPlanOverrides()
    {
        SqliteTableStore.ReplaceAll(PlanOverridesTable, PlanOverrides);
    }

    private static void PersistCustomRows()
    {
        SqliteTableStore.ReplaceAll(PlanCustomRowsTable, PlanCustomRows);
    }

    private static void PersistDeletedIds()
    {
        SqliteTableStore.ReplaceAll(PlanDeletedIdsTable, DeletedPlanIds);
    }

    private static void ApplyPlanUpsert(InspectionPlanItemDto current, InspectionPlanUpsertDto dto)
    {
        if (dto.HospitalName is not null) current.HospitalName = dto.HospitalName.Trim();
        if (dto.ProductName is not null) current.ProductName = dto.ProductName.Trim();
        if (dto.Province is not null) current.Province = dto.Province.Trim();
        if (dto.HospitalLevel is not null) current.HospitalLevel = dto.HospitalLevel.Trim();
        if (dto.GroupName is not null) current.GroupName = dto.GroupName.Trim();
        if (dto.Inspector is not null) current.Inspector = dto.Inspector.Trim();
        if (dto.PlanDate.HasValue) current.PlanDate = dto.PlanDate.Value;
        current.ActualDate = dto.ActualDate;
        if (dto.Status is not null) current.Status = dto.Status.Trim();
        if (dto.InspectionType is not null) current.InspectionType = dto.InspectionType.Trim();
        if (dto.Priority is not null) current.Priority = dto.Priority.Trim();
        if (dto.Remarks is not null) current.Remarks = dto.Remarks.Trim();
    }

    private static void SyncBackToProject(InspectionPlanItemDto item)
    {
        InMemoryProjectDataStore.UpdateSingleProject(item.Id, project =>
        {
            if (!string.IsNullOrWhiteSpace(item.HospitalName)) project.HospitalName = item.HospitalName;
            if (!string.IsNullOrWhiteSpace(item.ProductName)) project.ProductName = item.ProductName;
            if (!string.IsNullOrWhiteSpace(item.Province)) project.Province = item.Province;
            if (!string.IsNullOrWhiteSpace(item.HospitalLevel)) project.HospitalLevel = item.HospitalLevel;
            if (!string.IsNullOrWhiteSpace(item.GroupName)) project.GroupName = item.GroupName;
            if (!string.IsNullOrWhiteSpace(item.Inspector)) project.MaintenancePersonName = item.Inspector;
        });
    }

    // ─── 种子数据 ───

    private static List<InspectionPlanItemDto> BuildSeed()
    {
        var statuses = new[] { "已完成", "已计划", "执行中", "已计划", "已取消" };
        var deletedIds = DeletedPlanIds.Select(x => x.Id).ToHashSet();

        var seed = InMemoryProjectDataStore.Projects
            .Where(project => !deletedIds.Contains(project.Id))
            .OrderBy(x => x.Id)
            .Select((project, index) =>
            {
                var status = statuses[index % statuses.Length];
                var planDate = DateTime.Today.AddDays(index - 3);
                var inspector = string.IsNullOrWhiteSpace(project.MaintenancePersonName)
                    ? "未分配"
                    : project.MaintenancePersonName.Trim();

                return new InspectionPlanItemDto
                {
                    Id = project.Id,
                    HospitalName = project.HospitalName,
                    ProductName = project.ProductName,
                    Province = project.Province,
                    GroupName = project.GroupName,
                    Inspector = inspector,
                    PlanDate = planDate,
                    ActualDate = status == "已完成" ? planDate : null,
                    Status = status,
                    InspectionType = index % 2 == 0 ? "远程" : "现场",
                    Priority = project.OverdueDays > 90 ? "高" : project.OverdueDays > 0 ? "中" : "低",
                    Remarks = string.Empty,
                };
            })
            .ToList();

        if (PlanOverrides.Count == 0)
        {
            return seed;
        }

        foreach (var item in seed)
        {
            var overrideItem = PlanOverrides.FirstOrDefault(x => x.Id == item.Id);
            if (overrideItem is null)
            {
                continue;
            }

            item.GroupName = overrideItem.GroupName;
            item.Inspector = overrideItem.Inspector;
            item.PlanDate = overrideItem.PlanDate;
            item.ActualDate = overrideItem.ActualDate;
            item.Status = overrideItem.Status;
            item.InspectionType = overrideItem.InspectionType;
            item.Priority = overrideItem.Priority;
            item.Remarks = overrideItem.Remarks;
        }

        if (PlanCustomRows.Count > 0)
        {
            seed.AddRange(PlanCustomRows.Select(x => new InspectionPlanItemDto
            {
                Id = x.Id,
                HospitalName = x.HospitalName,
                ProductName = x.ProductName,
                Province = x.Province,
                HospitalLevel = x.HospitalLevel,
                GroupName = x.GroupName,
                Inspector = x.Inspector,
                PlanDate = x.PlanDate,
                ActualDate = x.ActualDate,
                Status = x.Status,
                InspectionType = x.InspectionType,
                Priority = x.Priority,
                Remarks = x.Remarks,
            }));
        }

        return seed;
    }

    private class DeletedInspectionPlanId
    {
        public long Id { get; set; }
    }
}
