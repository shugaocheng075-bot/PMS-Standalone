using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.AnnualReport;
using PMS.Application.Models;
using PMS.Application.Models.AnnualReport;

namespace PMS.API.Controllers.AnnualReport;

[ApiController]
[Route("api/annual-reports")]
public class AnnualReportsController(
    IAnnualReportService annualReportService,
    IAccessControlService accessControlService) : ControllerBase
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

        // 医院范围过滤
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase)
            && dataScope.AccessibleHospitalNames is { Count: > 0 })
        {
            var filtered = HospitalScopeHelper.FilterByHospitalScope(
                dataScope, result.Items, x => x.HospitalName).ToList();
            result = new PagedResult<AnnualReportItemDto>
            {
                Items = filtered,
                Total = filtered.Count,
                Page = result.Page,
                Size = result.Size
            };
        }

        return Ok(ApiResponse<PagedResult<AnnualReportItemDto>>.Success(result));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] AnnualReportUpsertDto dto,
        CancellationToken cancellationToken = default)
    {
        var updated = await annualReportService.UpdateAsync(id, dto, cancellationToken);
        if (updated is null)
        {
            return NotFound(new ApiResponse<object> { Code = 404, Message = "未找到对应年度报告" });
        }

        return Ok(ApiResponse<AnnualReportItemDto>.Success(updated));
    }
}
