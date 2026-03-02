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
}