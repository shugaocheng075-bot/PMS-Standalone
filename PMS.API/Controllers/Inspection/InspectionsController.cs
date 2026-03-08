using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.Inspection;
using PMS.Application.Models;
using PMS.Application.Models.Inspection;

namespace PMS.API.Controllers.Inspection;

[ApiController]
[Route("api/inspections")]
public class InspectionsController(
    IInspectionService inspectionService,
    IAccessControlService accessControlService) : ControllerBase
{
    // ─── 巡检计划 ───

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase)
            && dataScope.AccessibleHospitalNames is { Count: > 0 })
        {
            var allResult = await inspectionService.QueryAsync(
                new InspectionQuery { Page = 1, Size = int.MaxValue }, cancellationToken);
            var scopedItems = HospitalScopeHelper.FilterByHospitalScope(
                dataScope, allResult.Items, x => x.HospitalName).ToList();

            var now = DateTime.Today;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var scopedSummary = new InspectionSummaryDto
            {
                PlannedCount = scopedItems.Count(x => x.Status == "已计划"),
                InProgressCount = scopedItems.Count(x => x.Status == "执行中"),
                CompletedCount = scopedItems.Count(x => x.Status == "已完成"),
                CancelledCount = scopedItems.Count(x => x.Status == "已取消"),
                ThisMonthCount = scopedItems.Count(x => x.PlanDate >= monthStart && x.PlanDate < monthEnd),
                Total = scopedItems.Count
            };
            return Ok(ApiResponse<InspectionSummaryDto>.Success(scopedSummary));
        }

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
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        var needsHospitalScope =
            !string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase)
            && dataScope.AccessibleHospitalNames is { Count: > 0 };

        if (needsHospitalScope)
        {
            var allResult = await inspectionService.QueryAsync(new InspectionQuery
            {
                Status = status,
                Province = province,
                ProductName = productName,
                GroupName = groupName,
                Inspector = inspector,
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

            return Ok(ApiResponse<PagedResult<InspectionPlanItemDto>>.Success(
                new PagedResult<InspectionPlanItemDto>
                {
                    Items = pagedItems,
                    Total = totalScoped,
                    Page = effectivePage,
                    Size = effectiveSize
                }));
        }

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

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] InspectionPlanUpsertDto dto,
        CancellationToken cancellationToken = default)
    {
        var updated = await inspectionService.UpdateAsync(id, dto, cancellationToken);
        if (updated is null)
        {
            return NotFound(new ApiResponse<object> { Code = 404, Message = "未找到对应巡检计划" });
        }

        return Ok(ApiResponse<InspectionPlanItemDto>.Success(updated));
    }

    // ─── SystemAuditTool 巡检结果推送 ───

    /// <summary>
    /// 接收单条巡检结果。
    /// POST /api/inspections/results
    /// </summary>
    [HttpPost("results")]
    public async Task<IActionResult> SubmitResult(
        [FromBody] InspectionResultDto result,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(result.HospitalName) || string.IsNullOrWhiteSpace(result.ProductName))
        {
            return BadRequest(new ApiResponse<object> { Code = 400, Message = "hospitalName 和 productName 不能为空" });
        }

        // 医院范围验证
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        if (!HospitalScopeHelper.IsHospitalAccessible(dataScope, result.HospitalName))
        {
            return StatusCode(403, new { code = 403, message = "无权在该医院下提交巡检结果" });
        }

        await inspectionService.SubmitResultAsync(result, cancellationToken);
        return Ok(ApiResponse<object>.Success(new { message = "巡检结果已接收", id = result.Id }));
    }

    /// <summary>
    /// 批量接收巡检结果（一次巡检多个产品）。
    /// POST /api/inspections/results/batch
    /// </summary>
    [HttpPost("results/batch")]
    public async Task<IActionResult> SubmitResults(
        [FromBody] List<InspectionResultDto> results,
        CancellationToken cancellationToken = default)
    {
        if (results == null || results.Count == 0)
        {
            return BadRequest(new ApiResponse<object> { Code = 400, Message = "结果列表不能为空" });
        }

        // 医院范围验证 - 过滤掉无权操作的医院
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        var allowed = results
            .Where(r => HospitalScopeHelper.IsHospitalAccessible(dataScope, r.HospitalName))
            .ToList();
        if (allowed.Count == 0)
        {
            return StatusCode(403, new { code = 403, message = "无权提交所选医院的巡检结果" });
        }

        await inspectionService.SubmitResultsAsync(allowed, cancellationToken);
        return Ok(ApiResponse<object>.Success(new { message = $"已接收 {allowed.Count} 条巡检结果" }));
    }

    /// <summary>
    /// 查询已提交的巡检结果。
    /// GET /api/inspections/results
    /// </summary>
    [HttpGet("results")]
    public async Task<IActionResult> GetResults(
        [FromQuery] string? hospitalName,
        [FromQuery] string? productName,
        [FromQuery] string? inspector,
        [FromQuery] string? healthLevel,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
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
            var allResult = await inspectionService.QueryResultsAsync(new InspectionResultQuery
            {
                HospitalName = hospitalName,
                ProductName = productName,
                Inspector = inspector,
                HealthLevel = healthLevel,
                From = from,
                To = to,
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

            return Ok(ApiResponse<PagedResult<InspectionResultDto>>.Success(
                new PagedResult<InspectionResultDto>
                {
                    Items = pagedItems,
                    Total = totalScoped,
                    Page = effectivePage,
                    Size = effectiveSize
                }));
        }

        var result = await inspectionService.QueryResultsAsync(new InspectionResultQuery
        {
            HospitalName = hospitalName,
            ProductName = productName,
            Inspector = inspector,
            HealthLevel = healthLevel,
            From = from,
            To = to,
            Page = page,
            Size = size
        }, cancellationToken);

        return Ok(ApiResponse<PagedResult<InspectionResultDto>>.Success(result));
    }

    /// <summary>
    /// 获取指定医院+产品的最新巡检结果。
    /// GET /api/inspections/results/latest?hospitalName=xxx&productName=xxx
    /// </summary>
    [HttpGet("results/latest")]
    public async Task<IActionResult> GetLatestResult(
        [FromQuery] string hospitalName,
        [FromQuery] string productName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hospitalName) || string.IsNullOrWhiteSpace(productName))
        {
            return BadRequest(new ApiResponse<object> { Code = 400, Message = "hospitalName 和 productName 不能为空" });
        }

        var latest = await inspectionService.GetLatestResultAsync(hospitalName, productName, cancellationToken);
        if (latest == null)
        {
            return NotFound(new ApiResponse<object> { Code = 404, Message = "未找到该医院产品的巡检结果" });
        }

        return Ok(ApiResponse<InspectionResultDto>.Success(latest));
    }
}
