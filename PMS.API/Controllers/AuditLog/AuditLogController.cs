using Microsoft.AspNetCore.Mvc;
using PMS.API.Models;
using PMS.Application.Contracts.AuditLog;
using PMS.Application.Models.AuditLog;

namespace PMS.API.Controllers.AuditLog;

[ApiController]
[Route("api/audit-logs")]
public class AuditLogController(IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Query(
        [FromQuery] string? action,
        [FromQuery] string? module,
        [FromQuery] string? operatorName,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new AuditLogQuery
        {
            Action = action,
            Module = module,
            Operator = operatorName,
            StartDate = startDate,
            EndDate = endDate,
            Page = page > 0 ? page : 1,
            Size = size > 0 ? size : 20
        };

        var result = await auditLogService.QueryAsync(query, cancellationToken);
        return Ok(ApiResponse<object>.Success(result));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await auditLogService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<AuditLogSummaryDto>.Success(summary));
    }
}
