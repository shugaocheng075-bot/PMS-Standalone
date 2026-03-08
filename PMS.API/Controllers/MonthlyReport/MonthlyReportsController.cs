using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.MonthlyReport;
using PMS.Application.Contracts.Notification;
using PMS.Application.Models;
using PMS.Application.Models.MonthlyReport;

namespace PMS.API.Controllers.MonthlyReport;

[ApiController]
[Route("api/monthly-reports")]
public class MonthlyReportsController(
    IMonthlyReportService monthlyReportService,
    IAccessControlService accessControlService,
    INotificationService notificationService) : ControllerBase
{
    /// <summary>
    /// 查询月报列表（带医院范围过滤）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Query(
        [FromQuery] MonthlyReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        var normalizedPage = query.Page > 0 ? query.Page : 1;
        var normalizedSize = query.Size > 0 ? query.Size : 20;
        query.Page = 1;
        query.Size = 50000;

        var rawResult = await monthlyReportService.QueryAsync(query, cancellationToken);
        var scopedItems = rawResult.Items.AsEnumerable();

        // 医院范围过滤（先过滤后分页）
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase)
            && dataScope.AccessibleHospitalNames is { Count: > 0 })
        {
            scopedItems = HospitalScopeHelper.FilterByHospitalScope(
                dataScope, scopedItems, x => x.HospitalName);
        }

        var scopedList = scopedItems.ToList();
        var result = new PagedResult<MonthlyReportItemDto>
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

    /// <summary>
    /// 获取单条月报
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken = default)
    {
        var item = await monthlyReportService.GetByIdAsync(id, cancellationToken);
        if (item is null) return NotFound(ApiResponse<object>.Success(null));

        // 医院范围验证
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        if (!HospitalScopeHelper.IsHospitalAccessible(dataScope, item.HospitalName))
        {
            return StatusCode(403, new { code = 403, message = "无权查看该医院的月报" });
        }

        return Ok(ApiResponse<MonthlyReportItemDto>.Success(item));
    }

    /// <summary>
    /// 创建月报
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] MonthlyReportUpsertDto dto,
        CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var profile = await accessControlService.GetUserProfileAsync(personnelId);
        var personnelName = profile?.PersonnelName ?? "未知";

        // 医院范围验证
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        if (!HospitalScopeHelper.IsHospitalAccessible(dataScope, dto.HospitalName))
        {
            return StatusCode(403, new { code = 403, message = "无权在该医院下创建月报" });
        }

        var item = await monthlyReportService.CreateAsync(personnelName, dto, cancellationToken);

        _ = notificationService.BroadcastToManagersAsync(
            "monthly_report_new",
            $"新月报：{dto.HospitalName}",
            $"{personnelName} 提交了 {dto.HospitalName} 的月报",
            "/monthly-report/list",
            cancellationToken);

        return Ok(ApiResponse<MonthlyReportItemDto>.Success(item));
    }

    /// <summary>
    /// 更新月报
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] MonthlyReportUpsertDto dto,
        CancellationToken cancellationToken = default)
    {
        // 医院范围验证
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        if (!HospitalScopeHelper.IsHospitalAccessible(dataScope, dto.HospitalName))
        {
            return StatusCode(403, new { code = 403, message = "无权修改该医院的月报" });
        }

        var item = await monthlyReportService.UpdateAsync(id, dto, cancellationToken);
        if (item is null) return NotFound(ApiResponse<object>.Success(null));
        return Ok(ApiResponse<MonthlyReportItemDto>.Success(item));
    }

    /// <summary>
    /// 提交月报（draft/rejected → submitted）
    /// </summary>
    [HttpPatch("{id:long}/submit")]
    public async Task<IActionResult> Submit(long id, CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        var existing = await monthlyReportService.GetByIdAsync(id, cancellationToken);
        if (existing is null) return NotFound(ApiResponse<object>.Success(null));

        if (!HospitalScopeHelper.IsHospitalAccessible(dataScope, existing.HospitalName))
        {
            return StatusCode(403, new { code = 403, message = "无权操作该月报" });
        }

        try
        {
            var item = await monthlyReportService.SubmitAsync(id, cancellationToken);
            if (item is not null)
            {
                _ = notificationService.BroadcastToManagersAsync(
                    "monthly_report_submitted",
                    $"月报待审批：{existing.HospitalName}",
                    $"{existing.SubmittedBy} 提交了 {existing.HospitalName} {existing.ReportMonth} 月报，请审批",
                    "/monthly-report/list",
                    cancellationToken);
            }
            return item is null
                ? NotFound(ApiResponse<object>.Success(null))
                : Ok(ApiResponse<MonthlyReportItemDto>.Success(item));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { code = 400, message = ex.Message });
        }
    }

    /// <summary>
    /// 审批通过月报（仅管理员）
    /// </summary>
    [HttpPatch("{id:long}/approve")]
    public async Task<IActionResult> Approve(long id, CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, new { code = 403, message = "仅管理员可审批月报" });
        }

        var profile = await accessControlService.GetUserProfileAsync(personnelId);
        try
        {
            var item = await monthlyReportService.ApproveAsync(id, profile?.PersonnelName ?? "管理员", cancellationToken);
            return item is null
                ? NotFound(ApiResponse<object>.Success(null))
                : Ok(ApiResponse<MonthlyReportItemDto>.Success(item));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { code = 400, message = ex.Message });
        }
    }

    /// <summary>
    /// 驳回月报（仅管理员）
    /// </summary>
    [HttpPatch("{id:long}/reject")]
    public async Task<IActionResult> Reject(long id, [FromBody] MonthlyReportApprovalDto dto, CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, new { code = 403, message = "仅管理员可驳回月报" });
        }

        var profile = await accessControlService.GetUserProfileAsync(personnelId);
        try
        {
            var item = await monthlyReportService.RejectAsync(id, profile?.PersonnelName ?? "管理员", dto.RejectionReason, cancellationToken);
            return item is null
                ? NotFound(ApiResponse<object>.Success(null))
                : Ok(ApiResponse<MonthlyReportItemDto>.Success(item));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { code = 400, message = ex.Message });
        }
    }

    /// <summary>
    /// 删除月报
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken = default)
    {
        // 先获取记录检查医院权限
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        var existing = await monthlyReportService.GetByIdAsync(id, cancellationToken);
        if (existing is not null && !HospitalScopeHelper.IsHospitalAccessible(dataScope, existing.HospitalName))
        {
            return StatusCode(403, new { code = 403, message = "无权删除该医院的月报" });
        }

        var deleted = await monthlyReportService.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound(ApiResponse<object>.Success(null));
        return Ok(ApiResponse<object>.Success(new { message = "删除成功" }));
    }
}
