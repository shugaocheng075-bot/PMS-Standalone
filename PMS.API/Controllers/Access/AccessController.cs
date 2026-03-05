using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts.Access;
using PMS.Application.Models;
using PMS.Application.Models.Access;

namespace PMS.API.Controllers.Access;

[ApiController]
[Route("api/access")]
public class AccessController(IAccessControlService accessControlService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var profile = await accessControlService.GetCurrentAsync(personnelId, cancellationToken);
        if (profile is null)
        {
            return NotFound(new { code = 404, message = "personnel not found" });
        }

        return Ok(ApiResponse<PersonnelAccessProfileDto>.Success(profile));
    }

    [HttpGet("actors")]
    public async Task<IActionResult> GetActors(CancellationToken cancellationToken = default)
    {
        var actors = await accessControlService.GetActorsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PersonnelActorDto>>.Success(actors));
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissionCatalog(CancellationToken cancellationToken = default)
    {
        var catalog = await accessControlService.GetPermissionCatalogAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PermissionDefinitionDto>>.Success(catalog));
    }

    [HttpGet("users")]
    public async Task<IActionResult> QueryUsers(
        [FromQuery] string? name,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await accessControlService.QueryUsersAsync(new PersonnelAccessQuery
        {
            Name = name,
            Page = page,
            Size = size
        }, cancellationToken);

        return Ok(ApiResponse<PagedResult<PersonnelAccessItemDto>>.Success(result));
    }

    [HttpGet("users/{personnelId:int}")]
    public async Task<IActionResult> GetUserProfile(int personnelId, CancellationToken cancellationToken = default)
    {
        var profile = await accessControlService.GetUserProfileAsync(personnelId, cancellationToken);
        if (profile is null)
        {
            return NotFound(new { code = 404, message = "personnel not found" });
        }

        return Ok(ApiResponse<PersonnelAccessProfileDto>.Success(profile));
    }

    [HttpPut("users/{personnelId:int}/permissions")]
    public async Task<IActionResult> SavePermissions(
        int personnelId,
        [FromBody] UpdateUserPermissionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await IsManagerAsync(cancellationToken))
        {
            return Forbid();
        }

        var updated = await accessControlService.SaveUserPermissionsAsync(
            personnelId,
            request.PermissionKeys ?? [],
            request.IsAdmin,
            cancellationToken);

        if (updated is null)
        {
            return NotFound(new { code = 404, message = "personnel not found" });
        }

        return Ok(ApiResponse<PersonnelAccessProfileDto>.Success(updated));
    }

    public class UpdateUserPermissionRequest
    {
        public List<string>? PermissionKeys { get; set; }
        public bool? IsAdmin { get; set; }
    }

    // ── 新增: 设置系统角色 ──

    [HttpPut("users/{personnelId:int}/role")]
    public async Task<IActionResult> SetSystemRole(
        int personnelId,
        [FromBody] SetSystemRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await IsManagerAsync(cancellationToken))
        {
            return Forbid();
        }

        var updated = await accessControlService.SetSystemRoleAsync(
            personnelId,
            request.SystemRole,
            cancellationToken);

        if (updated is null)
        {
            return NotFound(new { code = 404, message = "personnel not found" });
        }

        return Ok(ApiResponse<PersonnelAccessProfileDto>.Success(updated));
    }

    // ── 新增: 设置上级主管 ──

    [HttpPut("users/{personnelId:int}/supervisor")]
    public async Task<IActionResult> SetSupervisor(
        int personnelId,
        [FromBody] SetSupervisorRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await IsManagerAsync(cancellationToken))
        {
            return Forbid();
        }

        var updated = await accessControlService.SetSupervisorAsync(
            personnelId,
            request.SupervisorId,
            cancellationToken);

        if (updated is null)
        {
            return NotFound(new { code = 404, message = "personnel not found or invalid supervisor" });
        }

        return Ok(ApiResponse<PersonnelAccessProfileDto>.Success(updated));
    }

    // ── 新增: 获取当前用户数据范围 ──

    [HttpGet("data-scope")]
    public async Task<IActionResult> GetDataScope(CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var scope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        return Ok(ApiResponse<DataScopeDto>.Success(scope));
    }

    // ── 新增: 医院范围分配 ──

    [HttpGet("users/{personnelId:int}/hospital-scope")]
    public async Task<IActionResult> GetHospitalScope(int personnelId, CancellationToken cancellationToken = default)
    {
        var hospitals = await accessControlService.GetHospitalScopeAsync(personnelId, cancellationToken);
        return Ok(ApiResponse<List<string>>.Success(hospitals));
    }

    [HttpPut("users/{personnelId:int}/hospital-scope")]
    public async Task<IActionResult> SetHospitalScope(
        int personnelId,
        [FromBody] SetHospitalScopeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await IsManagerAsync(cancellationToken))
        {
            return Forbid();
        }

        var updated = await accessControlService.SetHospitalScopeAsync(
            personnelId,
            request.HospitalNames,
            cancellationToken);

        if (updated is null)
        {
            return NotFound(new { code = 404, message = "personnel not found" });
        }

        return Ok(ApiResponse<PersonnelAccessProfileDto>.Success(updated));
    }

    private async Task<bool> IsManagerAsync(CancellationToken cancellationToken)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var profile = await accessControlService.GetUserProfileAsync(personnelId, cancellationToken);
        if (profile is null)
        {
            return false;
        }

        return profile.IsAdmin || string.Equals(profile.SystemRole, "manager", StringComparison.OrdinalIgnoreCase);
    }
}