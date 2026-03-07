using PMS.Application.Contracts.AnnualReport;
using PMS.Application.Models;
using PMS.Application.Models.AnnualReport;

namespace PMS.Infrastructure.Services;

public class InMemoryAnnualReportService : IAnnualReportService
{
    private static readonly object SyncRoot = new();
    private const string OverridesStateKey = "annual_report_overrides";
    private static readonly List<AnnualReportItemDto> Overrides =
        SqliteJsonStore.LoadOrSeed(OverridesStateKey, () => new List<AnnualReportItemDto>());

    public Task<AnnualReportSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var seed = BuildSeed();
        var thisYear = DateTime.Today.Year;
        var summary = new AnnualReportSummaryDto
        {
            NotStartedCount = seed.Count(x => x.Status == "未开始"),
            WritingCount = seed.Count(x => x.Status == "编写中"),
            SubmittedCount = seed.Count(x => x.Status == "已提交"),
            CompletedCount = seed.Count(x => x.Status == "已完成"),
            ThisYearCount = seed.Count(x => x.ReportYear == thisYear),
            Total = seed.Count
        };

        return Task.FromResult(summary);
    }

    public Task<PagedResult<AnnualReportItemDto>> QueryAsync(AnnualReportQuery query, CancellationToken cancellationToken = default)
    {
        IEnumerable<AnnualReportItemDto> filtered = BuildSeed();

        if (!string.IsNullOrWhiteSpace(query.Status))
            filtered = filtered.Where(x => SmartTextMatcher.MatchExact(x.Status, query.Status));

        if (query.ReportYear.HasValue)
            filtered = filtered.Where(x => x.ReportYear == query.ReportYear.Value);

        if (!string.IsNullOrWhiteSpace(query.GroupName))
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.GroupName, query.GroupName));

        if (!string.IsNullOrWhiteSpace(query.ServicePerson))
            filtered = filtered.Where(x => SmartTextMatcher.Match(x.ServicePerson, query.ServicePerson));

        var total = filtered.Count();
        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.Size <= 0 ? 20 : query.Size;

        var items = filtered
            .OrderByDescending(x => x.ReportYear)
            .ThenBy(x => x.HospitalName)
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return Task.FromResult(new PagedResult<AnnualReportItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            Size = size
        });
    }

    public Task<AnnualReportItemDto?> UpdateAsync(long id, AnnualReportUpsertDto dto, CancellationToken cancellationToken = default)
    {
        lock (SyncRoot)
        {
            var current = BuildSeed().FirstOrDefault(x => x.Id == id);
            if (current is null)
            {
                return Task.FromResult<AnnualReportItemDto?>(null);
            }

            current.GroupName = dto.GroupName.Trim();
            current.ServicePerson = dto.ServicePerson.Trim();
            current.ReportYear = dto.ReportYear;
            current.Status = dto.Status.Trim();
            current.SubmitDate = dto.SubmitDate;

            var existingIndex = Overrides.FindIndex(x => x.Id == id);
            if (existingIndex >= 0)
            {
                Overrides[existingIndex] = current;
            }
            else
            {
                Overrides.Add(current);
            }

            PersistOverrides();
            return Task.FromResult<AnnualReportItemDto?>(current);
        }
    }

    private static void PersistOverrides()
    {
        SqliteJsonStore.Save(OverridesStateKey, Overrides);
    }

    private static List<AnnualReportItemDto> BuildSeed()
    {
        var statuses = new[] { "编写中", "已提交", "未开始", "已完成" };
        var currentYear = DateTime.Today.Year;

        var seed = InMemoryProjectDataStore.Projects
            .OrderBy(x => x.Id)
            .Select((project, index) =>
            {
                var status = statuses[index % statuses.Length];
                var reportYear = index % 3 == 0 ? currentYear - 1 : currentYear;
                var servicePerson = string.IsNullOrWhiteSpace(project.MaintenancePersonName)
                    ? "未分配"
                    : project.MaintenancePersonName.Trim();

                return new AnnualReportItemDto
                {
                    Id = project.Id,
                    HospitalName = project.HospitalName,
                    Province = project.Province,
                    GroupName = project.GroupName,
                    ServicePerson = servicePerson,
                    ReportYear = reportYear,
                    Status = status,
                    SubmitDate = status is "已提交" or "已完成" ? DateTime.Today.AddDays(-(index + 1) * 3) : null
                };
            })
            .ToList();

        if (Overrides.Count == 0)
        {
            return seed;
        }

        foreach (var item in seed)
        {
            var overrideItem = Overrides.FirstOrDefault(x => x.Id == item.Id);
            if (overrideItem is null)
            {
                continue;
            }

            item.GroupName = overrideItem.GroupName;
            item.ServicePerson = overrideItem.ServicePerson;
            item.ReportYear = overrideItem.ReportYear;
            item.Status = overrideItem.Status;
            item.SubmitDate = overrideItem.SubmitDate;
        }

        return seed;
    }
}
