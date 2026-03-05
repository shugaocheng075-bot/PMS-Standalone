using PMS.Application.Models;
using PMS.Application.Models.RepairRecord;

namespace PMS.Application.Contracts.RepairRecord;

public interface IRepairRecordService
{
    Task<RepairRecordSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<RepairRecordItemDto>> QueryAsync(RepairRecordQuery query, CancellationToken cancellationToken = default);
    Task<RepairRecordItemDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<RepairRecordItemDto> CreateAsync(string reporterName, RepairRecordUpsertDto dto, CancellationToken cancellationToken = default);
    Task<RepairRecordItemDto?> UpdateAsync(long id, RepairRecordUpsertDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
