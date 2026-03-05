using PMS.Application.Models;
using PMS.Application.Models.MonthlyReport;

namespace PMS.Application.Contracts.MonthlyReport;

public interface IMonthlyReportService
{
    Task<PagedResult<MonthlyReportItemDto>> QueryAsync(MonthlyReportQuery query, CancellationToken cancellationToken = default);
    Task<MonthlyReportItemDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<MonthlyReportItemDto> CreateAsync(string submittedBy, MonthlyReportUpsertDto dto, CancellationToken cancellationToken = default);
    Task<MonthlyReportItemDto?> UpdateAsync(long id, MonthlyReportUpsertDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
