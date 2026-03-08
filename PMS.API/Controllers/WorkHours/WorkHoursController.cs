using ClosedXML.Excel;
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

    [HttpPatch("{id:long}/submit")]
    public async Task<IActionResult> Submit(long id, CancellationToken cancellationToken)
    {
        var ok = await workHoursService.SubmitAsync(id, cancellationToken);
        if (!ok) return BadRequest(new { code = 400, message = "无法提交，记录不存在或状态不允许" });
        return Ok(ApiResponse<object>.Success(new { message = "已提交" }));
    }

    [HttpPatch("{id:long}/confirm")]
    public async Task<IActionResult> Confirm(long id, CancellationToken cancellationToken)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var profile = await accessControlService.GetUserProfileAsync(personnelId);
        var confirmerName = profile?.PersonnelName ?? "unknown";
        var ok = await workHoursService.ConfirmAsync(id, confirmerName, cancellationToken);
        if (!ok) return BadRequest(new { code = 400, message = "无法确认，记录不存在或未提交" });
        return Ok(ApiResponse<object>.Success(new { message = "已确认" }));
    }

    [HttpPatch("{id:long}/reject")]
    public async Task<IActionResult> Reject(long id, CancellationToken cancellationToken)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var profile = await accessControlService.GetUserProfileAsync(personnelId);
        var rejectorName = profile?.PersonnelName ?? "unknown";
        var ok = await workHoursService.RejectAsync(id, rejectorName, cancellationToken);
        if (!ok) return BadRequest(new { code = 400, message = "无法退回，记录不存在或未提交" });
        return Ok(ApiResponse<object>.Success(new { message = "已退回" }));
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
        var result = await workHoursService.QueryAsync(new WorkHoursQuery
        {
            PersonnelName = personnelName,
            HospitalName = hospitalName,
            WorkType = workType,
            WorkDateFrom = workDateFrom,
            WorkDateTo = workDateTo,
            Page = 1,
            Size = int.MaxValue
        }, cancellationToken);

        var rows = HospitalScopeHelper.FilterByHospitalScope(dataScope, result.Items, x => x.HospitalName).ToList();

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
}
