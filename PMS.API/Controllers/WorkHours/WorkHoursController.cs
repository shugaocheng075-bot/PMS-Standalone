using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.WorkHours;
using PMS.Application.Models.Access;
using PMS.Application.Models.WorkHours;

namespace PMS.API.Controllers.WorkHours;

[ApiController]
[Route("api/workhours")]
public class WorkHoursController(
    IWorkHoursService workHoursService,
    IAccessControlService accessControlService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] string? personnelName,
        [FromQuery] string? hospitalName,
        [FromQuery] string? workDateFrom,
        [FromQuery] string? workDateTo,
        [FromQuery] string? workType,
        CancellationToken cancellationToken)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        var summary = await workHoursService.GetSummaryAsync(
            BuildScopedQuery(dataScope, personnelName, hospitalName, workDateFrom, workDateTo, workType),
            cancellationToken);
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
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        var normalizedPage = page > 0 ? page : 1;
        var normalizedSize = size > 0 ? size : 20;

        var result = await workHoursService.QueryAsync(
            BuildScopedQuery(dataScope, personnelName, hospitalName, workDateFrom, workDateTo, workType, normalizedPage, normalizedSize),
            cancellationToken);

        return Ok(ApiResponse<object>.Success(result));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var item = await workHoursService.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<object?>.Success(null));
        }

        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        if (!HospitalScopeHelper.IsHospitalAccessible(dataScope, item.HospitalName))
        {
            return StatusCode(403, new { code = 403, message = "无权查看该医院下的工时记录" });
        }

        return Ok(ApiResponse<WorkHoursItemDto>.Success(item));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WorkHoursUpsertDto dto, CancellationToken cancellationToken)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var profile = await accessControlService.GetUserProfileAsync(personnelId, cancellationToken);
        var personnelName = profile?.PersonnelName ?? "未知";

        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
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
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        if (!HospitalScopeHelper.IsHospitalAccessible(dataScope, dto.HospitalName))
        {
            return StatusCode(403, new { code = 403, message = "无权在该医院下修改工时记录" });
        }

        try
        {
            var item = await workHoursService.UpdateAsync(id, dto, cancellationToken);
            if (item is null)
            {
                return NotFound(ApiResponse<object?>.Success(null));
            }

            return Ok(ApiResponse<WorkHoursItemDto>.Success(item));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { code = 400, message = ex.Message });
        }
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        var existing = await workHoursService.GetByIdAsync(id, cancellationToken);
        if (existing is not null && !HospitalScopeHelper.IsHospitalAccessible(dataScope, existing.HospitalName))
        {
            return StatusCode(403, new { code = 403, message = "无权删除该医院下的工时记录" });
        }

        try
        {
            var deleted = await workHoursService.DeleteAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound(ApiResponse<object?>.Success(null));
            }

            return Ok(ApiResponse<object>.Success(new { message = "删除成功" }));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { code = 400, message = ex.Message });
        }
    }

    [HttpPatch("{id:long}/submit")]
    public async Task<IActionResult> Submit(long id, CancellationToken cancellationToken)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        var existing = await workHoursService.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound(ApiResponse<object?>.Success(null));
        }

        if (!HospitalScopeHelper.IsHospitalAccessible(dataScope, existing.HospitalName))
        {
            return StatusCode(403, new { code = 403, message = "无权操作该医院下的工时记录" });
        }

        var ok = await workHoursService.SubmitAsync(id, cancellationToken);
        if (!ok)
        {
            return BadRequest(new { code = 400, message = "无法提交，记录不存在或状态不允许" });
        }

        var item = await workHoursService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<WorkHoursItemDto>.Success(item!));
    }

    [HttpPatch("{id:long}/confirm")]
    public async Task<IActionResult> Confirm(long id, CancellationToken cancellationToken)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        var existing = await workHoursService.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound(ApiResponse<object?>.Success(null));
        }

        if (!HospitalScopeHelper.IsHospitalAccessible(dataScope, existing.HospitalName))
        {
            return StatusCode(403, new { code = 403, message = "无权操作该医院下的工时记录" });
        }

        var profile = await accessControlService.GetUserProfileAsync(personnelId, cancellationToken);
        var confirmerName = profile?.PersonnelName ?? "unknown";
        var ok = await workHoursService.ConfirmAsync(id, confirmerName, cancellationToken);
        if (!ok)
        {
            return BadRequest(new { code = 400, message = "无法确认，记录不存在或尚未提交" });
        }

        var item = await workHoursService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<WorkHoursItemDto>.Success(item!));
    }

    [HttpPatch("{id:long}/reject")]
    public async Task<IActionResult> Reject(long id, CancellationToken cancellationToken)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        var existing = await workHoursService.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound(ApiResponse<object?>.Success(null));
        }

        if (!HospitalScopeHelper.IsHospitalAccessible(dataScope, existing.HospitalName))
        {
            return StatusCode(403, new { code = 403, message = "无权操作该医院下的工时记录" });
        }

        var profile = await accessControlService.GetUserProfileAsync(personnelId, cancellationToken);
        var rejectorName = profile?.PersonnelName ?? "unknown";
        var ok = await workHoursService.RejectAsync(id, rejectorName, cancellationToken);
        if (!ok)
        {
            return BadRequest(new { code = 400, message = "无法退回，记录不存在或尚未提交" });
        }

        var item = await workHoursService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<WorkHoursItemDto>.Success(item!));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? personnelName,
        [FromQuery] string? hospitalName,
        [FromQuery] string? workType,
        [FromQuery] string? workDateFrom,
        [FromQuery] string? workDateTo,
        CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        var result = await workHoursService.QueryAsync(
            BuildScopedQuery(dataScope, personnelName, hospitalName, workDateFrom, workDateTo, workType, 1, int.MaxValue),
            cancellationToken);

        var rows = result.Items.ToList();
        string[] headers = ["人员", "机会号", "医院名称", "产品", "工作日期", "工时(h)", "工作类型", "实施状态", "描述", "状态", "确认人", "确认时间"];
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("工时明细");
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var n = r + 2;
            ws.Cell(n, 1).Value = row.PersonnelName;
            ws.Cell(n, 2).Value = row.OpportunityNumber;
            ws.Cell(n, 3).Value = row.HospitalName;
            ws.Cell(n, 4).Value = row.ProductName;
            ws.Cell(n, 5).Value = row.WorkDate;
            ws.Cell(n, 6).Value = (double)row.Hours;
            ws.Cell(n, 7).Value = row.WorkType;
            ws.Cell(n, 8).Value = row.ImplementationStatus;
            ws.Cell(n, 9).Value = row.Description;
            ws.Cell(n, 10).Value = row.Status;
            ws.Cell(n, 11).Value = row.ConfirmedBy;
            ws.Cell(n, 12).Value = row.ConfirmedAt;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var fileName = $"工时明细_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private static WorkHoursQuery BuildScopedQuery(
        DataScopeDto dataScope,
        string? personnelName,
        string? hospitalName,
        string? workDateFrom,
        string? workDateTo,
        string? workType,
        int page = 1,
        int size = 20)
    {
        return new WorkHoursQuery
        {
            PersonnelName = personnelName,
            HospitalName = hospitalName,
            WorkDateFrom = workDateFrom,
            WorkDateTo = workDateTo,
            WorkType = workType,
            Page = page,
            Size = size,
            AccessiblePersonnelNames = dataScope.ScopeType == "all" ? null : dataScope.AccessiblePersonnelNames,
            AccessibleHospitalNames = dataScope.ScopeType == "all" ? null : dataScope.AccessibleHospitalNames
        };
    }
}
