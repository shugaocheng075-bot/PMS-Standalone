using Microsoft.AspNetCore.Mvc;
using PMS.API.Models;
using PMS.Application.Contracts.Personnel;
using PMS.Application.Models;
using PMS.Application.Models.Personnel;

namespace PMS.API.Controllers.Personnel;

[ApiController]
[Route("api/personnel")]
public class PersonnelController(IPersonnelService personnelService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var result = await personnelService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<PersonnelSummaryDto>.Success(result));
    }

    [HttpGet("workload")]
    public async Task<IActionResult> GetWorkload(CancellationToken cancellationToken = default)
    {
        var result = await personnelService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<PersonnelSummaryDto>.Success(result));
    }

    [HttpPost("sync-external")]
    public async Task<IActionResult> SyncExternal([FromQuery] bool force = false, CancellationToken cancellationToken = default)
    {
        var result = await personnelService.SyncFromExternalAsync(force, cancellationToken);
        return Ok(ApiResponse<PersonnelExternalSyncResultDto>.Success(result));
    }

    [HttpPost("import-json")]
    public async Task<IActionResult> ImportJson([FromQuery] bool clear = false, CancellationToken cancellationToken = default)
    {
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
        var result = await personnelService.QueryAsync(new PersonnelQuery
        {
            Name = name,
            Department = department,
            GroupName = groupName,
            RoleType = roleType,
            IsOnsite = isOnsite,
            Page = page,
            Size = size
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
        var result = await personnelService.DeleteAsync(id, cancellationToken);
        if (!result)
        {
            return NotFound(new { code = 404, message = "personnel not found" });
        }

        return Ok(new { code = 200, message = "success" });
    }
}
