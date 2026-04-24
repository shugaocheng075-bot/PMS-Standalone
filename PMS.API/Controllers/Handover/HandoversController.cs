using System.Text;
using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.AuditLog;
using PMS.Application.Contracts.Handover;
using PMS.Application.Models;
using PMS.Application.Models.Handover;

namespace PMS.API.Controllers.Handover;

[ApiController]
[Route("api/handovers")]
public class HandoversController(
    IHandoverService handoverService,
    IAccessControlService accessControlService,
    IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase)
            && dataScope.AccessibleHospitalNames is { Count: > 0 })
        {
            var allResult = await handoverService.QueryAsync(
                new HandoverQuery { Page = 1, Size = int.MaxValue }, cancellationToken);
            var scopedItems = HospitalScopeHelper.FilterByHospitalScope(
                dataScope, allResult.Items, x => x.HospitalName).ToList();

            var scopedSummary = new HandoverSummaryDto
            {
                PendingCount = scopedItems.Count(x => x.Stage == "未发"),
                EmailSentCount = scopedItems.Count(x => x.Stage == "已发邮件"),
                InProgressCount = scopedItems.Count(x => x.Stage == "交接中"),
                CompletedCount = scopedItems.Count(x => x.Stage == "已交接"),
                Total = scopedItems.Count
            };
            return Ok(ApiResponse<HandoverSummaryDto>.Success(scopedSummary));
        }

        var summary = await handoverService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<HandoverSummaryDto>.Success(summary));
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? stage,
        [FromQuery] string? batch,
        [FromQuery] string? type,
        [FromQuery] string? fromGroup,
        [FromQuery] string? toOwner,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        var needsHospitalScope =
            !string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase)
            && dataScope.AccessibleHospitalNames is { Count: > 0 };

        if (needsHospitalScope)
        {
            var allResult = await handoverService.QueryAsync(new HandoverQuery
            {
                Stage = stage,
                Batch = batch,
                Type = type,
                FromGroup = fromGroup,
                ToOwner = toOwner,
                Page = 1,
                Size = int.MaxValue
            }, cancellationToken);

            var scopedItems = HospitalScopeHelper.FilterByHospitalScope(
                dataScope, allResult.Items, x => x.HospitalName).ToList();

            var totalScoped = scopedItems.Count;
            var effectivePage = page < 1 ? 1 : page;
            var effectiveSize = size <= 0 ? 20 : size;
            var pagedItems = scopedItems
                .Skip((effectivePage - 1) * effectiveSize)
                .Take(effectiveSize)
                .ToList();

            return Ok(ApiResponse<PagedResult<HandoverItemDto>>.Success(
                new PagedResult<HandoverItemDto>
                {
                    Items = pagedItems,
                    Total = totalScoped,
                    Page = effectivePage,
                    Size = effectiveSize
                }));
        }

        var result = await handoverService.QueryAsync(new HandoverQuery
        {
            Stage = stage,
            Batch = batch,
            Type = type,
            FromGroup = fromGroup,
            ToOwner = toOwner,
            Page = page,
            Size = size
        }, cancellationToken);

        return Ok(ApiResponse<PagedResult<HandoverItemDto>>.Success(result));
    }

    [HttpGet("kanban")]
    public async Task<IActionResult> GetKanban(CancellationToken cancellationToken = default)
    {
        var result = await handoverService.GetKanbanAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<HandoverKanbanColumnDto>>.Success(result));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken = default)
    {
        var item = await GetScopedHandoverAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound(new { code = 404, message = "交接记录不存在" });
        }

        return Ok(ApiResponse<HandoverItemDto>.Success(item));
    }

    [HttpPatch("{id:long}/send-email")]
    public Task<IActionResult> SendEmail(long id, CancellationToken cancellationToken = default)
        => ExecuteWorkflowAsync(id, "发送交接邮件", handoverService.SendEmailAsync, cancellationToken);

    [HttpPatch("{id:long}/start")]
    public Task<IActionResult> Start(long id, CancellationToken cancellationToken = default)
        => ExecuteWorkflowAsync(id, "开始交接", handoverService.StartAsync, cancellationToken);

    [HttpPatch("{id:long}/complete")]
    public Task<IActionResult> Complete(long id, CancellationToken cancellationToken = default)
        => ExecuteWorkflowAsync(id, "完成交接", handoverService.CompleteAsync, cancellationToken);

    [HttpPatch("{id:long}/rollback")]
    public Task<IActionResult> Rollback(long id, CancellationToken cancellationToken = default)
        => ExecuteWorkflowAsync(id, "回退交接阶段", handoverService.RollbackAsync, cancellationToken);

    [HttpPut("{id:long}/stage")]
    public async Task<IActionResult> UpdateStage(
        [FromRoute] long id,
        [FromBody] HandoverStageUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await GetScopedHandoverAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound(new { code = 404, message = "交接记录不存在" });
            }

            var updated = await handoverService.UpdateStageAsync(id, request, cancellationToken);
            return Ok(ApiResponse<HandoverItemDto>.Success(updated));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { code = 404, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { code = 400, message = ex.Message });
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? stage,
        [FromQuery] string? batch,
        [FromQuery] string? type,
        [FromQuery] string? fromGroup,
        [FromQuery] string? toOwner,
        CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);

        var allResult = await handoverService.QueryAsync(new HandoverQuery
        {
            Stage = stage,
            Batch = batch,
            Type = type,
            FromGroup = fromGroup,
            ToOwner = toOwner,
            Page = 1,
            Size = 50000
        }, cancellationToken);

        var items = HospitalScopeHelper.FilterByHospitalScope(
            dataScope, allResult.Items, x => x.HospitalName).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("\uFEFFID,交接编号,医院名称,产品名称,原组别,原负责人,新负责人,批次,阶段,类型,邮件发送日期");
        foreach (var item in items)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(item.Id.ToString()),
                EscapeCsv(item.HandoverNo),
                EscapeCsv(item.HospitalName),
                EscapeCsv(item.ProductName),
                EscapeCsv(item.FromGroup),
                EscapeCsv(item.FromOwner),
                EscapeCsv(item.ToOwner),
                EscapeCsv(item.Batch),
                EscapeCsv(item.Stage),
                EscapeCsv(item.Type),
                EscapeCsv(item.EmailSentDate?.ToString("yyyy-MM-dd") ?? "")));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"交接管理-{DateTime.Now:yyyyMMddHHmmss}.csv");
    }

    private static string EscapeCsv(string value)
    {
        var text = value ?? string.Empty;
        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
        {
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }
        return text;
    }

    private async Task<IActionResult> ExecuteWorkflowAsync(
        long id,
        string action,
        Func<long, CancellationToken, Task<HandoverItemDto>> operation,
        CancellationToken cancellationToken)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var existing = await GetScopedHandoverAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound(new { code = 404, message = "交接记录不存在" });
        }

        var operatorName = await GetOperatorNameAsync(personnelId, cancellationToken);

        try
        {
            var updated = await operation(id, cancellationToken);
            await auditLogService.LogAsync(operatorName, personnelId, action, "handover", $"#{updated.Id}", $"{existing.Stage} -> {updated.Stage}", GetRequestIp(), cancellationToken);
            return Ok(ApiResponse<HandoverItemDto>.Success(updated));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { code = 400, message = ex.Message });
        }
    }

    private async Task<HandoverItemDto?> GetScopedHandoverAsync(long id, CancellationToken cancellationToken)
    {
        var item = await handoverService.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        if (!HospitalScopeHelper.IsHospitalAccessible(dataScope, item.HospitalName))
        {
            return null;
        }

        return item;
    }

    private async Task<string> GetOperatorNameAsync(int personnelId, CancellationToken cancellationToken)
    {
        var profile = await accessControlService.GetUserProfileAsync(personnelId, cancellationToken);
        return string.IsNullOrWhiteSpace(profile?.PersonnelName) ? "unknown" : profile!.PersonnelName;
    }

    private string GetRequestIp()
        => HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
}
