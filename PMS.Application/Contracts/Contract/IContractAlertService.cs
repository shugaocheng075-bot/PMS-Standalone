using PMS.Application.Models;
using PMS.Application.Models.Contract;

namespace PMS.Application.Contracts.Contract;

public interface IContractAlertService
{
    Task<ContractAlertSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<ContractAlertItemDto>> QueryAlertsAsync(ContractAlertQuery query, CancellationToken cancellationToken = default);
}
