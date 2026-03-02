using Microsoft.AspNetCore.Mvc;
using PMS.API.Models;
using PMS.Infrastructure.Services;
using System.Text;

namespace PMS.API.Controllers.MajorDemand;

[ApiController]
[Route("api/major-demands")]
public class MajorDemandsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        var snapshot = InMemoryMajorDemandStore.GetSnapshot();

        return Ok(ApiResponse<object>.Success(new
        {
            columns = snapshot.Columns,
            rows = snapshot.Rows,
            workflows = snapshot.WorkflowItems,
            sourceFilePath = snapshot.SourceFilePath,
            sheetName = snapshot.SheetName,
            importedAt = snapshot.ImportedAt
        }));
    }

    [HttpPost("batch/status")]
    public IActionResult BatchUpdateStatus([FromBody] BatchStatusRequest request)
    {
        if (request.RowIds.Count == 0 || string.IsNullOrWhiteSpace(request.Status))
        {
            return BadRequest(ApiResponse<object>.Success(new { message = "rowIds 与 status 不能为空" }));
        }

        var changed = InMemoryMajorDemandStore.BatchUpdateStatus(request.RowIds, request.Status, ResolveActor());
        return Ok(ApiResponse<object>.Success(new
        {
            changed,
            message = changed ? "状态更新成功" : "未匹配到可更新数据"
        }));
    }

    [HttpPost("batch/owner")]
    public IActionResult BatchAssignOwner([FromBody] BatchOwnerRequest request)
    {
        if (request.RowIds.Count == 0)
        {
            return BadRequest(ApiResponse<object>.Success(new { message = "rowIds 不能为空" }));
        }

        var changed = InMemoryMajorDemandStore.BatchAssignOwner(request.RowIds, request.Owner, ResolveActor());
        return Ok(ApiResponse<object>.Success(new
        {
            changed,
            message = changed ? "负责人更新成功" : "未匹配到可更新数据"
        }));
    }

    [HttpPost("batch/due-date")]
    public IActionResult BatchUpdateDueDate([FromBody] BatchDueDateRequest request)
    {
        if (request.RowIds.Count == 0)
        {
            return BadRequest(ApiResponse<object>.Success(new { message = "rowIds 不能为空" }));
        }

        var changed = InMemoryMajorDemandStore.BatchUpdateDueDate(request.RowIds, request.DueDate, ResolveActor());
        return Ok(ApiResponse<object>.Success(new
        {
            changed,
            message = changed ? "截止日期更新成功" : "未匹配到可更新数据"
        }));
    }

    [HttpPost("{rowId}/comments")]
    public IActionResult AddComment(string rowId, [FromBody] AddCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(rowId) || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(ApiResponse<object>.Success(new { message = "rowId 与 content 不能为空" }));
        }

        var changed = InMemoryMajorDemandStore.AddComment(rowId, request.Content, ResolveActor());
        return Ok(ApiResponse<object>.Success(new
        {
            changed,
            message = changed ? "评论添加成功" : "未匹配到对应需求"
        }));
    }

    [HttpGet("export")]
    public IActionResult ExportCsv()
    {
        var snapshot = InMemoryMajorDemandStore.GetSnapshot();
        var workflowMap = snapshot.WorkflowItems.ToDictionary(x => x.RowId, StringComparer.OrdinalIgnoreCase);

        var headers = snapshot.Columns
            .Where(x => !string.Equals(x, "_RowId", StringComparison.OrdinalIgnoreCase))
            .ToList();
        headers.AddRange(["状态", "负责人", "截止日期", "最新评论", "最近操作时间"]);

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

        foreach (var row in snapshot.Rows)
        {
            row.TryGetValue("_RowId", out var rowId);
            workflowMap.TryGetValue(rowId ?? string.Empty, out var workflow);

            var values = snapshot.Columns
                .Where(x => !string.Equals(x, "_RowId", StringComparison.OrdinalIgnoreCase))
                .Select(column => row.TryGetValue(column, out var value) ? value ?? string.Empty : string.Empty)
                .ToList();

            values.Add(workflow?.Status ?? string.Empty);
            values.Add(workflow?.Owner ?? string.Empty);
            values.Add(workflow?.DueDate ?? string.Empty);
            values.Add(workflow?.Comments.LastOrDefault()?.Content ?? string.Empty);
            values.Add(workflow?.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty);

            sb.AppendLine(string.Join(",", values.Select(EscapeCsv)));
        }

        var content = Encoding.UTF8.GetBytes("\uFEFF" + sb);
        return File(content, "text/csv; charset=utf-8", $"major-demands-{DateTime.Now:yyyyMMddHHmmss}.csv");
    }

    private string ResolveActor()
    {
        var actor = Request.Headers["X-Operator"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(actor))
        {
            return actor.Trim();
        }

        return User?.Identity?.Name?.Trim() switch
        {
            { Length: > 0 } name => name,
            _ => "系统用户"
        };
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

    public class BatchStatusRequest
    {
        public List<string> RowIds { get; set; } = [];
        public string Status { get; set; } = string.Empty;
    }

    public class BatchOwnerRequest
    {
        public List<string> RowIds { get; set; } = [];
        public string Owner { get; set; } = string.Empty;
    }

    public class BatchDueDateRequest
    {
        public List<string> RowIds { get; set; } = [];
        public string DueDate { get; set; } = string.Empty;
    }

    public class AddCommentRequest
    {
        public string Content { get; set; } = string.Empty;
    }
}