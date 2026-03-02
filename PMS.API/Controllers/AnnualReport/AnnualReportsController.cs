using Microsoft.AspNetCore.Mvc;
using PMS.API.Models;
using PMS.Application.Contracts.AnnualReport;
using PMS.Application.Models;
using PMS.Application.Models.AnnualReport;

namespace PMS.API.Controllers.AnnualReport;

[ApiController]
[Route("api/annual-reports")]
public class AnnualReportsController(IAnnualReportService annualReportService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var summary = await annualReportService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<AnnualReportSummaryDto>.Success(summary));
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? status,
        [FromQuery] int? reportYear,
        [FromQuery] string? groupName,
        [FromQuery] string? servicePerson,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await annualReportService.QueryAsync(new AnnualReportQuery
        {
            Status = status,
            ReportYear = reportYear,
            GroupName = groupName,
            ServicePerson = servicePerson,
            Page = page,
            Size = size
        }, cancellationToken);

        return Ok(ApiResponse<PagedResult<AnnualReportItemDto>>.Success(result));
    }
}
