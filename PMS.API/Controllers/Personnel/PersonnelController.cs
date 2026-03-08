using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.Personnel;
using PMS.Application.Models;
using PMS.Application.Models.Personnel;

namespace PMS.API.Controllers.Personnel;

[ApiController]
[Route("api/personnel")]
public class PersonnelController(
    IPersonnelService personnelService,
    IAccessControlService accessControlService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        if (string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            var result = await personnelService.GetSummaryAsync(cancellationToken);
            return Ok(ApiResponse<PersonnelSummaryDto>.Success(result));
        }
        // Non-manager: compute summary from filtered list
        var all = await personnelService.QueryAsync(new PersonnelQuery { Page = 1, Size = 50000 }, cancellationToken);
        var scoped = FilterPersonnelByScope(dataScope, all.Items);
        var summary = new PersonnelSummaryDto
        {
            Total = scoped.Count,
            ServiceCount = scoped.Count(x => (x.RoleType ?? "").Contains("服务")),
            ImplementationCount = scoped.Count(x => (x.RoleType ?? "").Contains("实施")),
            OnsiteCount = scoped.Count(x => x.IsOnsite)
        };
        return Ok(ApiResponse<PersonnelSummaryDto>.Success(summary));
    }

    [HttpGet("workload")]
    public async Task<IActionResult> GetWorkload(CancellationToken cancellationToken = default)
    {
        return await GetSummary(cancellationToken);
    }

    [HttpPost("sync-external")]
    public async Task<IActionResult> SyncExternal([FromQuery] bool force = false, CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, new { code = 403, message = "仅经理角色可执行人员同步" });
        }

        var result = await personnelService.SyncFromExternalAsync(force, cancellationToken);
        return Ok(ApiResponse<PersonnelExternalSyncResultDto>.Success(result));
    }

    [HttpPost("import-json")]
    public async Task<IActionResult> ImportJson([FromQuery] bool clear = false, CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, new { code = 403, message = "仅经理角色可执行人员导入" });
        }

        using var reader = new StreamReader(Request.Body);
        var jsonData = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(jsonData))
        {
            return BadRequest(new { code = 400, message = "请求体为空，请提供 JSON 数组数据" });
        }

        var result = await personnelService.ImportJsonAsync(jsonData, clear, cancellationToken);
        return Ok(ApiResponse<PersonnelExternalSyncResultDto>.Success(result));
    }

    [HttpGet]
    public async Task<IActionResult> Query(
        [FromQuery] string? name,
        [FromQuery] string? department,
        [FromQuery] string? groupName,
        [FromQuery] string? roleType,
        [FromQuery] bool? isOnsite,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        var needsScope = !string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase)
                         && dataScope.AccessiblePersonnelNames is { Count: > 0 };

        if (needsScope)
        {
            var all = await personnelService.QueryAsync(new PersonnelQuery
            {
                Name = name, Department = department, GroupName = groupName,
                RoleType = roleType, IsOnsite = isOnsite, Page = 1, Size = 50000
            }, cancellationToken);
            var scoped = FilterPersonnelByScope(dataScope, all.Items);
            var paged = scoped.Skip((page - 1) * size).Take(size).ToList();
            return Ok(ApiResponse<PagedResult<PersonnelItemDto>>.Success(
                new PagedResult<PersonnelItemDto> { Items = paged, Total = scoped.Count }));
        }

        var result = await personnelService.QueryAsync(new PersonnelQuery
        {
            Name = name, Department = department, GroupName = groupName,
            RoleType = roleType, IsOnsite = isOnsite, Page = page, Size = size
        }, cancellationToken);
        return Ok(ApiResponse<PagedResult<PersonnelItemDto>>.Success(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var result = await personnelService.GetByIdAsync(id, cancellationToken);
        if (result is null)
        {
            return NotFound(new { code = 404, message = "personnel not found" });
        }

        return Ok(ApiResponse<PersonnelItemDto>.Success(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PersonnelUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, new { code = 403, message = "仅经理角色可创建人员" });
        }

        var result = await personnelService.CreateAsync(dto, cancellationToken);
        return Ok(ApiResponse<PersonnelItemDto>.Success(result));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PersonnelUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var result = await personnelService.UpdateAsync(id, dto, cancellationToken);
        if (result is null)
        {
            return NotFound(new { code = 404, message = "personnel not found" });
        }

        return Ok(ApiResponse<PersonnelItemDto>.Success(result));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, new { code = 403, message = "仅经理角色可删除人员" });
        }

        var result = await personnelService.DeleteAsync(id, cancellationToken);
        if (!result)
        {
            return NotFound(new { code = 404, message = "personnel not found" });
        }

        return Ok(new { code = 200, message = "success" });
    }

    private static List<PersonnelItemDto> FilterPersonnelByScope(
        Application.Models.Access.DataScopeDto dataScope,
        IReadOnlyList<PersonnelItemDto> items)
    {
        if (string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
            return items.ToList();
        if (dataScope.AccessiblePersonnelNames is not { Count: > 0 })
            return [];
        var allowed = new HashSet<string>(dataScope.AccessiblePersonnelNames, StringComparer.OrdinalIgnoreCase);
        return items.Where(x => allowed.Contains(x.Name)).ToList();
    }
}
