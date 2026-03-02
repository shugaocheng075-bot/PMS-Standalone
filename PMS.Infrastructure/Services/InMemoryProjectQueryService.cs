using PMS.Application.Contracts;
using PMS.Application.Models;
using PMS.Domain.Entities;

namespace PMS.Infrastructure.Services;

public class InMemoryProjectQueryService : IProjectQueryService
{
    public Task<PagedResult<ProjectEntity>> QueryAsync(ProjectQuery query, CancellationToken cancellationToken = default)
    {
        IEnumerable<ProjectEntity> filtered = InMemoryProjectDataStore.Projects;

        if (!string.IsNullOrWhiteSpace(query.HospitalName))
        {
            filtered = filtered.Where(x => x.HospitalName.Contains(query.HospitalName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.ProductName))
        {
            filtered = filtered.Where(x => x.ProductName.Contains(query.ProductName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Province))
        {
            filtered = filtered.Where(x => x.Province.Equals(query.Province, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.GroupName))
        {
            filtered = filtered.Where(x => x.GroupName.Contains(query.GroupName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.HospitalLevel))
        {
            filtered = filtered.Where(x => x.HospitalLevel.Equals(query.HospitalLevel, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.ContractStatus))
        {
            filtered = filtered.Where(x => x.ContractStatus.Equals(query.ContractStatus, StringComparison.OrdinalIgnoreCase));
        }

        var total = filtered.Count();
        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.Size <= 0 ? 20 : query.Size;

        var items = filtered
            .OrderByDescending(x => x.OverdueDays)
            .ThenBy(x => x.HospitalName)
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return Task.FromResult(new PagedResult<ProjectEntity>
        {
            Items = items,
            Total = total,
            Page = page,
            Size = size
        });
    }
}
