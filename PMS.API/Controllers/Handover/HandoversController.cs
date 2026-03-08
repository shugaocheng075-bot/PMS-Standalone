using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.Handover;
using PMS.Application.Models;
using PMS.Application.Models.Handover;

namespace PMS.API.Controllers.Handover;

[ApiController]
[Route("api/handovers")]
public class HandoversController(
    IHandoverService handoverService,
    IAccessControlService accessControlService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase)
            && dataScope.AccessibleHospitalNames is { Count: > 0 })
        {
            var allResult = await handoverService.QueryAsync(
                new HandoverQuery { Page = 1, Size = int.MaxValue }, cancellationToken);
            var scopedItems = HospitalScopeHelper.FilterByHospitalScope(
                dataScope, allResult.Items, x => x.HospitalName).ToList();

            var scopedSummary = new HandoverSummaryDto
            {
                PendingCount = scopedItems.Count(x => x.Stage == "未发"),
                EmailSentCount = scopedItems.Count(x => x.Stage == "已发邮件"),
                InProgressCount = scopedItems.Count(x => x.Stage == "交接中"),
                CompletedCount = scopedItems.Count(x => x.Stage == "已交接"),
                Total = scopedItems.Count
            };
            return Ok(ApiResponse<HandoverSummaryDto>.Success(scopedSummary));
        }

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
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        var needsHospitalScope =
            !string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase)
            && dataScope.AccessibleHospitalNames is { Count: > 0 };

        if (needsHospitalScope)
        {
            var allResult = await handoverService.QueryAsync(new HandoverQuery
            {
                Stage = stage,
                Batch = batch,
                Type = type,
                FromGroup = fromGroup,
                ToOwner = toOwner,
                Page = 1,
                Size = int.MaxValue
            }, cancellationToken);

            var scopedItems = HospitalScopeHelper.FilterByHospitalScope(
                dataScope, allResult.Items, x => x.HospitalName).ToList();

            var totalScoped = scopedItems.Count;
            var effectivePage = page < 1 ? 1 : page;
            var effectiveSize = size <= 0 ? 20 : size;
            var pagedItems = scopedItems
                .Skip((effectivePage - 1) * effectiveSize)
                .Take(effectiveSize)
                .ToList();

            return Ok(ApiResponse<PagedResult<HandoverItemDto>>.Success(
                new PagedResult<HandoverItemDto>
                {
                    Items = pagedItems,
                    Total = totalScoped,
                    Page = effectivePage,
                    Size = effectiveSize
                }));
        }

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
