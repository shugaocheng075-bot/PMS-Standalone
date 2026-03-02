using PMS.Application.Models;
using PMS.Application.Models.Handover;

namespace PMS.Application.Contracts.Handover;

public interface IHandoverService
{
    Task<HandoverSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<HandoverItemDto>> QueryAsync(HandoverQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HandoverKanbanColumnDto>> GetKanbanAsync(CancellationToken cancellationToken = default);
    Task<HandoverItemDto> UpdateStageAsync(long id, HandoverStageUpdateRequest request, CancellationToken cancellationToken = default);
}
