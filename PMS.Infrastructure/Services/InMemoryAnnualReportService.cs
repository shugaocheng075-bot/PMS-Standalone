using PMS.Application.Contracts.AnnualReport;
using PMS.Application.Models;
using PMS.Application.Models.AnnualReport;

namespace PMS.Infrastructure.Services;

public class InMemoryAnnualReportService : IAnnualReportService
{
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

    private static List<AnnualReportItemDto> BuildSeed()
    {
        var statuses = new[] { "编写中", "已提交", "未开始", "已完成" };
        var currentYear = DateTime.Today.Year;

        return InMemoryProjectDataStore.Projects
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
    }
}
