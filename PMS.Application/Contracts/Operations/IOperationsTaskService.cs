using PMS.Application.Models;
using PMS.Application.Models.Operations;

namespace PMS.Application.Contracts.Operations;

public interface IOperationsTaskService
{
    Task<OperationsTaskSummaryDto> GetSummaryAsync(OperationsTaskQuery query, CancellationToken cancellationToken = default);
    Task<PagedResult<OperationsTaskItemDto>> QueryAsync(OperationsTaskQuery query, CancellationToken cancellationToken = default);
}
