using Microsoft.AspNetCore.Mvc;
using PMS.API.Models;
using PMS.Application.Contracts.Handover;
using PMS.Application.Models;
using PMS.Application.Models.Handover;

namespace PMS.API.Controllers.Handover;

[ApiController]
[Route("api/handovers")]
public class HandoversController(IHandoverService handoverService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var summary = await handoverService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<HandoverSummaryDto>.Success(summary));
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? stage,
        [FromQuery] string? batch,
        [FromQuery] string? type,
        [FromQuery] string? fromGroup,
        [FromQuery] string? toOwner,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await handoverService.QueryAsync(new HandoverQuery
        {
            Stage = stage,
            Batch = batch,
            Type = type,
            FromGroup = fromGroup,
            ToOwner = toOwner,
            Page = page,
            Size = size
        }, cancellationToken);

        return Ok(ApiResponse<PagedResult<HandoverItemDto>>.Success(result));
    }

    [HttpGet("kanban")]
    public async Task<IActionResult> GetKanban(CancellationToken cancellationToken = default)
    {
        var result = await handoverService.GetKanbanAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<HandoverKanbanColumnDto>>.Success(result));
    }

    [HttpPut("{id:long}/stage")]
    public async Task<IActionResult> UpdateStage(
        [FromRoute] long id,
        [FromBody] HandoverStageUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updated = await handoverService.UpdateStageAsync(id, request, cancellationToken);
            return Ok(ApiResponse<HandoverItemDto>.Success(updated));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { code = 404, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { code = 400, message = ex.Message });
        }
    }
}
