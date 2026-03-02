using PMS.Domain.Entities;
using PMS.Application.Models;

namespace PMS.Application.Contracts;

public interface IProjectQueryService
{
    Task<PagedResult<ProjectEntity>> QueryAsync(ProjectQuery query, CancellationToken cancellationToken = default);
}
