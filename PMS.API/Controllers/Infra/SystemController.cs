using Microsoft.AspNetCore.Mvc;
using PMS.API.Models;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PMS.API.Controllers;

[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        var asm = Assembly.GetExecutingAssembly();
        var version = asm.GetName().Version?.ToString() ?? "1.0.0";

        return Ok(ApiResponse<object>.Success(new
        {
            appName = "PMS 项目管理平台",
            version,
            dotnetVersion = RuntimeInformation.FrameworkDescription,
            os = RuntimeInformation.OSDescription,
            serverTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            startTime = System.Diagnostics.Process.GetCurrentProcess().StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
        }));
    }
}
