using PMS.Application.Contracts.Inspection;
using PMS.Application.Models;
using PMS.Application.Models.Inspection;

namespace PMS.Infrastructure.Services;

public class InMemoryInspectionService : IInspectionService
{
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
            filtered = filtered.Where(x => x.Status == query.Status);

        if (!string.IsNullOrWhiteSpace(query.Province))
            filtered = filtered.Where(x => x.Province.Equals(query.Province, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.ProductName))
            filtered = filtered.Where(x => x.ProductName.Contains(query.ProductName, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.GroupName))
            filtered = filtered.Where(x => x.GroupName.Contains(query.GroupName, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.Inspector))
            filtered = filtered.Where(x => x.Inspector.Contains(query.Inspector, StringComparison.OrdinalIgnoreCase));

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

    private static List<InspectionPlanItemDto> BuildSeed()
    {
        var statuses = new[] { "已完成", "已计划", "执行中", "已计划", "已取消" };
        var inspectors = new[] { "张三", "李四", "王五", "赵六", "钱七", "孙八", "周九", "吴十" };

        return InMemoryProjectDataStore.Projects
            .OrderBy(x => x.Id)
            .Select((project, index) =>
            {
                var status = statuses[index % statuses.Length];
                var planDate = DateTime.Today.AddDays(index - 3);

                return new InspectionPlanItemDto
                {
                    Id = project.Id,
                    HospitalName = project.HospitalName,
                    ProductName = project.ProductName,
                    Province = project.Province,
                    GroupName = project.GroupName,
                    Inspector = inspectors[index % inspectors.Length],
                    PlanDate = planDate,
                    ActualDate = status == "已完成" ? planDate : null,
                    Status = status,
                    InspectionType = index % 2 == 0 ? "远程" : "现场"
                };
            })
            .ToList();
    }
}
