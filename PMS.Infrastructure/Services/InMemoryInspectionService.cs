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

    private static readonly List<InspectionResultDto> StoredResults =
        SqliteTableStore.LoadAll<InspectionResultDto>(ResultsTable, ResultsLegacyKey);
    private static readonly List<InspectionPlanItemDto> PlanOverrides =
        SqliteTableStore.LoadAll<InspectionPlanItemDto>(PlanOverridesTable, PlanOverridesLegacyKey);

    private static long _nextResultId = StoredResults.Count > 0
        ? StoredResults.Max(r => r.Id) + 1
        : 1;

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

        if (!string.IsNullOrWhiteSpace(query.Province))
            filtered = filtered.Where(x => x.Province.Equals(query.Province, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.ProductName))
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.ProductName, query.ProductName));

        if (!string.IsNullOrWhiteSpace(query.GroupName))
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.GroupName, query.GroupName));

        if (!string.IsNullOrWhiteSpace(query.Inspector))
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.Inspector, query.Inspector));

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
            var current = BuildSeed().FirstOrDefault(x => x.Id == id);
            if (current is null)
            {
                return Task.FromResult<InspectionPlanItemDto?>(null);
            }

            current.GroupName = dto.GroupName.Trim();
            current.Inspector = dto.Inspector.Trim();
            current.PlanDate = dto.PlanDate;
            current.ActualDate = dto.ActualDate;
            current.Status = dto.Status.Trim();
            current.InspectionType = dto.InspectionType.Trim();

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
            return Task.FromResult<InspectionPlanItemDto?>(current);
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

    private static void PersistResults()
    {
        SqliteTableStore.ReplaceAll(ResultsTable, StoredResults);
    }

    private static void PersistPlanOverrides()
    {
        SqliteTableStore.ReplaceAll(PlanOverridesTable, PlanOverrides);
    }

    // ─── 种子数据 ───

    private static List<InspectionPlanItemDto> BuildSeed()
    {
        var statuses = new[] { "已完成", "已计划", "执行中", "已计划", "已取消" };

        var seed = InMemoryProjectDataStore.Projects
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
                    InspectionType = index % 2 == 0 ? "远程" : "现场"
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
        }

        return seed;
    }
}
