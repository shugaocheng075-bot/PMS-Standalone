using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.WorkHours;
using PMS.Application.Models.WorkHours;

namespace PMS.API.Controllers.WorkHours;

[ApiController]
[Route("api/workhours")]
public class WorkHoursController(
    IWorkHoursService workHoursService,
    IAccessControlService accessControlService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await workHoursService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<WorkHoursSummaryDto>.Success(summary));
    }

    [HttpGet]
    public async Task<IActionResult> Query(
        [FromQuery] string? personnelName,
        [FromQuery] string? hospitalName,
        [FromQuery] string? workDateFrom,
        [FromQuery] string? workDateTo,
        [FromQuery] string? workType,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        var normalizedPage = page > 0 ? page : 1;
        var normalizedSize = size > 0 ? size : 20;

        var query = new WorkHoursQuery
        {
            PersonnelName = personnelName,
            HospitalName = hospitalName,
            WorkDateFrom = workDateFrom,
            WorkDateTo = workDateTo,
            WorkType = workType,
            Page = 1,
            Size = 50000,
            AccessiblePersonnelNames = dataScope.ScopeType == "all" ? null : dataScope.AccessiblePersonnelNames
        };

        var rawResult = await workHoursService.QueryAsync(query, cancellationToken);
        var scopedItems = rawResult.Items.AsEnumerable();

        // 医院范围过滤（先过滤后分页）
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase)
            && dataScope.AccessibleHospitalNames is { Count: > 0 })
        {
            scopedItems = HospitalScopeHelper.FilterByHospitalScope(
                dataScope, scopedItems, x => x.HospitalName);
        }

        var scopedList = scopedItems.ToList();
        var result = new Application.Models.PagedResult<WorkHoursItemDto>
        {
            Items = scopedList
                .Skip((normalizedPage - 1) * normalizedSize)
                .Take(normalizedSize)
                .ToList(),
            Total = scopedList.Count,
            Page = normalizedPage,
            Size = normalizedSize
        };

        return Ok(ApiResponse<object>.Success(result));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var item = await workHoursService.GetByIdAsync(id, cancellationToken);
        if (item is null) return NotFound(ApiResponse<object>.Success(null));
        return Ok(ApiResponse<WorkHoursItemDto>.Success(item));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WorkHoursUpsertDto dto, CancellationToken cancellationToken)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var profile = await accessControlService.GetUserProfileAsync(personnelId);
        var personnelName = profile?.PersonnelName ?? "未知";

        // 医院范围验证
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        if (!HospitalScopeHelper.IsHospitalAccessible(dataScope, dto.HospitalName))
        {
            return StatusCode(403, new { code = 403, message = "无权在该医院下创建工时记录" });
        }

        var item = await workHoursService.CreateAsync(personnelName, dto, cancellationToken);
        return Ok(ApiResponse<WorkHoursItemDto>.Success(item));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] WorkHoursUpsertDto dto, CancellationToken cancellationToken)
    {
        // 医院范围验证
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        if (!HospitalScopeHelper.IsHospitalAccessible(dataScope, dto.HospitalName))
        {
            return StatusCode(403, new { code = 403, message = "无权在该医院下修改工时记录" });
        }

        var item = await workHoursService.UpdateAsync(id, dto, cancellationToken);
        if (item is null) return NotFound(ApiResponse<object>.Success(null));
        return Ok(ApiResponse<WorkHoursItemDto>.Success(item));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        // 医院范围验证
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        var existing = await workHoursService.GetByIdAsync(id, cancellationToken);
        if (existing is not null && !HospitalScopeHelper.IsHospitalAccessible(dataScope, existing.HospitalName))
        {
            return StatusCode(403, new { code = 403, message = "无权删除该医院下的工时记录" });
        }

        var deleted = await workHoursService.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound(ApiResponse<object>.Success(null));
        return Ok(ApiResponse<object>.Success(new { message = "删除成功" }));
    }
}
