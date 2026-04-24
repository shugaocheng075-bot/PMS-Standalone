using PMS.Application.Models;
using PMS.Application.Models.AnnualReport;

namespace PMS.Application.Contracts.AnnualReport;

public interface IAnnualReportService
{
    Task<AnnualReportSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AnnualReportItemDto>> QueryAsync(AnnualReportQuery query, CancellationToken cancellationToken = default);
    Task<AnnualReportItemDto> CreateAsync(AnnualReportUpsertDto dto, CancellationToken cancellationToken = default);
    Task<AnnualReportItemDto?> UpdateAsync(long id, AnnualReportUpsertDto dto, CancellationToken cancellationToken = default);
    Task<AnnualReportItemDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<AnnualReportItemDto?> StartAsync(long id, CancellationToken cancellationToken = default);
    Task<AnnualReportItemDto?> SubmitAsync(long id, CancellationToken cancellationToken = default);
    Task<AnnualReportItemDto?> CompleteAsync(long id, string reviewer, CancellationToken cancellationToken = default);
    Task<AnnualReportItemDto?> ReopenAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
