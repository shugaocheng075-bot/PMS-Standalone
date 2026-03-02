using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.Auth;
using PMS.Application.Models.Auth;
using PMS.Application.Models.Access;

namespace PMS.API.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, IAccessControlService accessControlService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (result is null)
        {
            return Unauthorized(new { code = 401, message = "账号或密码错误" });
        }

        return Ok(ApiResponse<LoginResultDto>.Success(result));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
        var token = HttpContext.GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Ok(ApiResponse<bool>.Success(true));
        }

        _ = await authService.LogoutAsync(token, cancellationToken);
        return Ok(ApiResponse<bool>.Success(true));
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var profile = await accessControlService.GetCurrentAsync(personnelId, cancellationToken);
        if (profile is null)
        {
            return NotFound(new { code = 404, message = "personnel not found" });
        }

        return Ok(ApiResponse<PersonnelAccessProfileDto>.Success(profile));
    }
}
