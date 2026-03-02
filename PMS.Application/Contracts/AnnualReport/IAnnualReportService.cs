using PMS.Application.Models;
using PMS.Application.Models.AnnualReport;

namespace PMS.Application.Contracts.AnnualReport;

public interface IAnnualReportService
{
    Task<AnnualReportSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AnnualReportItemDto>> QueryAsync(AnnualReportQuery query, CancellationToken cancellationToken = default);
}
