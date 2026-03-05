using Microsoft.AspNetCore.Mvc;
using PMS.API.Models;
using PMS.Application.Contracts.Contract;
using PMS.Application.Models;
using PMS.Application.Models.Contract;

namespace PMS.API.Controllers.Contract;

[ApiController]
[Route("api/contracts/alerts")]
public class AlertsController(IContractAlertService alertService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var summary = await alertService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<ContractAlertSummaryDto>.Success(summary));
    }

    [HttpGet]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] string? alertLevel,
        [FromQuery] string? contractType,
        [FromQuery] string? contractValidityStatus,
        [FromQuery] string? province,
        [FromQuery] string? groupName,
        [FromQuery] string? salesName,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await alertService.QueryAlertsAsync(new ContractAlertQuery
        {
            AlertLevel = alertLevel,
            ContractType = contractType,
            ContractValidityStatus = contractValidityStatus,
            Province = province,
            GroupName = groupName,
            SalesName = salesName,
            Page = page,
            Size = size
        }, cancellationToken);

        return Ok(ApiResponse<PagedResult<ContractAlertItemDto>>.Success(result));
    }
}
