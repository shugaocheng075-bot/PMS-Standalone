using Microsoft.AspNetCore.Mvc;
using PMS.API.Models;
using PMS.Application.Contracts.Inspection;
using PMS.Application.Models;
using PMS.Application.Models.Inspection;

namespace PMS.API.Controllers.Inspection;

[ApiController]
[Route("api/inspections")]
public class InspectionsController(IInspectionService inspectionService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var summary = await inspectionService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<InspectionSummaryDto>.Success(summary));
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? status,
        [FromQuery] string? province,
        [FromQuery] string? productName,
        [FromQuery] string? groupName,
        [FromQuery] string? inspector,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await inspectionService.QueryAsync(new InspectionQuery
        {
            Status = status,
            Province = province,
            ProductName = productName,
            GroupName = groupName,
            Inspector = inspector,
            Page = page,
            Size = size
        }, cancellationToken);

        return Ok(ApiResponse<PagedResult<InspectionPlanItemDto>>.Success(result));
    }
}
