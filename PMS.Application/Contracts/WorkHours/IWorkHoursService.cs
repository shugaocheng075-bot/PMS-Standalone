using PMS.Application.Models;
using PMS.Application.Models.WorkHours;

namespace PMS.Application.Contracts.WorkHours;

public interface IWorkHoursService
{
    Task<WorkHoursSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<WorkHoursItemDto>> QueryAsync(WorkHoursQuery query, CancellationToken cancellationToken = default);
    Task<WorkHoursItemDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<WorkHoursItemDto> CreateAsync(string personnelName, WorkHoursUpsertDto dto, CancellationToken cancellationToken = default);
    Task<WorkHoursItemDto?> UpdateAsync(long id, WorkHoursUpsertDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
