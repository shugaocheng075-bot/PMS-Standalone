using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.Operations;
using PMS.Application.Models;
using PMS.Application.Models.Operations;

namespace PMS.API.Controllers.Operations;

[ApiController]
[Route("api/operations")]
public class OperationsController(
    IOperationsTaskService operationsTaskService,
    IAccessControlService accessControlService) : ControllerBase
{
    [HttpGet("tasks/summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] string? source,
        [FromQuery] string? level,
        [FromQuery] string? owner,
        [FromQuery] string? hospitalName,
        [FromQuery] string? keyword,
        CancellationToken cancellationToken = default)
    {
        var query = await BuildQueryAsync(source, level, owner, hospitalName, keyword, null, null, cancellationToken);
        var summary = await operationsTaskService.GetSummaryAsync(query, cancellationToken);
        return Ok(ApiResponse<OperationsTaskSummaryDto>.Success(summary));
    }

    [HttpGet("tasks")]
    public async Task<IActionResult> GetTasks(
        [FromQuery] string? source,
        [FromQuery] string? level,
        [FromQuery] string? owner,
        [FromQuery] string? hospitalName,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var query = await BuildQueryAsync(source, level, owner, hospitalName, keyword, page, size, cancellationToken);
        var result = await operationsTaskService.QueryAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResult<OperationsTaskItemDto>>.Success(result));
    }

    private async Task<OperationsTaskQuery> BuildQueryAsync(
        string? source,
        string? level,
        string? owner,
        string? hospitalName,
        string? keyword,
        int? page,
        int? size,
        CancellationToken cancellationToken)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);

        return new OperationsTaskQuery
        {
            Source = source,
            Level = level,
            Owner = owner,
            HospitalName = hospitalName,
            Keyword = keyword,
            ScopeType = dataScope.ScopeType,
            AccessibleHospitalNames = dataScope.AccessibleHospitalNames ?? [],
            Page = page ?? 1,
            Size = size ?? 20,
        };
    }
}
