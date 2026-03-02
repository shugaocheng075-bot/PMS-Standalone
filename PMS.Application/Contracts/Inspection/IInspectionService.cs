using PMS.Application.Models;
using PMS.Application.Models.Inspection;

namespace PMS.Application.Contracts.Inspection;

public interface IInspectionService
{
    Task<InspectionSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<InspectionPlanItemDto>> QueryAsync(InspectionQuery query, CancellationToken cancellationToken = default);
}
