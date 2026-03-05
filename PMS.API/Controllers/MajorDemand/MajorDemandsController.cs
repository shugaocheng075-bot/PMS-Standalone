using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts;
using PMS.Application.Contracts.Access;
using PMS.Application.Models;
using PMS.Application.Models.Access;
using PMS.Infrastructure.Services;
using System.Text;

namespace PMS.API.Controllers.MajorDemand;

[ApiController]
[Route("api/major-demands")]
public class MajorDemandsController(
    IAccessControlService accessControlService,
    IProjectQueryService projectQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var snapshot = InMemoryMajorDemandStore.GetSnapshot();

        // 医院范围过滤
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        var allowedHospitals = await GetAccessibleHospitalSetAsync(dataScope);
        var rows = snapshot.Rows;
        var workflows = snapshot.WorkflowItems;

        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            if (allowedHospitals.Count == 0)
            {
                rows = [];
                workflows = [];
            }
            else
            {
                var allowedSet = allowedHospitals;
            var filteredRows = rows.Where(row =>
            {
                var hospitalName = row.TryGetValue("医院名称", out var v) ? v : null;
                return !string.IsNullOrWhiteSpace(hospitalName) && allowedSet.Contains(hospitalName);
            }).ToList();

            var filteredRowIds = filteredRows
                .Select(r => r.TryGetValue("_RowId", out var id) ? id : null)
                .Where(id => id is not null)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            rows = filteredRows;
            workflows = workflows.Where(w => filteredRowIds.Contains(w.RowId)).ToList();
            }
        }

        return Ok(ApiResponse<object>.Success(new
        {
            columns = snapshot.Columns,
            rows,
            workflows,
            sourceFilePath = snapshot.SourceFilePath,
            sheetName = snapshot.SheetName,
            importedAt = snapshot.ImportedAt
        }));
    }

    [HttpPost("batch/status")]
    public async Task<IActionResult> BatchUpdateStatus([FromBody] BatchStatusRequest request)
    {
        if (request.RowIds.Count == 0 || string.IsNullOrWhiteSpace(request.Status))
        {
            return BadRequest(ApiResponse<object>.Success(new { message = "rowIds 与 status 不能为空" }));
        }

        var allowedIds = await FilterRowIdsByHospitalScopeAsync(request.RowIds);
        if (allowedIds.Count == 0)
        {
            return StatusCode(403, new { code = 403, message = "无权操作所选医院的需求" });
        }

        var changed = InMemoryMajorDemandStore.BatchUpdateStatus(allowedIds, request.Status, ResolveActor());
        return Ok(ApiResponse<object>.Success(new
        {
            changed,
            message = changed ? "状态更新成功" : "未匹配到可更新数据"
        }));
    }

    [HttpPost("batch/owner")]
    public async Task<IActionResult> BatchAssignOwner([FromBody] BatchOwnerRequest request)
    {
        if (request.RowIds.Count == 0)
        {
            return BadRequest(ApiResponse<object>.Success(new { message = "rowIds 不能为空" }));
        }

        var allowedIds = await FilterRowIdsByHospitalScopeAsync(request.RowIds);
        if (allowedIds.Count == 0)
        {
            return StatusCode(403, new { code = 403, message = "无权操作所选医院的需求" });
        }

        var changed = InMemoryMajorDemandStore.BatchAssignOwner(allowedIds, request.Owner, ResolveActor());
        return Ok(ApiResponse<object>.Success(new
        {
            changed,
            message = changed ? "负责人更新成功" : "未匹配到可更新数据"
        }));
    }

    [HttpPost("batch/due-date")]
    public async Task<IActionResult> BatchUpdateDueDate([FromBody] BatchDueDateRequest request)
    {
        if (request.RowIds.Count == 0)
        {
            return BadRequest(ApiResponse<object>.Success(new { message = "rowIds 不能为空" }));
        }

        var allowedIds = await FilterRowIdsByHospitalScopeAsync(request.RowIds);
        if (allowedIds.Count == 0)
        {
            return StatusCode(403, new { code = 403, message = "无权操作所选医院的需求" });
        }

        var changed = InMemoryMajorDemandStore.BatchUpdateDueDate(allowedIds, request.DueDate, ResolveActor());
        return Ok(ApiResponse<object>.Success(new
        {
            changed,
            message = changed ? "截止日期更新成功" : "未匹配到可更新数据"
        }));
    }

    [HttpPost("{rowId}/comments")]
    public async Task<IActionResult> AddComment(string rowId, [FromBody] AddCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(rowId) || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(ApiResponse<object>.Success(new { message = "rowId 与 content 不能为空" }));
        }

        var allowedIds = await FilterRowIdsByHospitalScopeAsync([rowId]);
        if (allowedIds.Count == 0)
        {
            return StatusCode(403, new { code = 403, message = "无权操作该医院的需求" });
        }

        var changed = InMemoryMajorDemandStore.AddComment(rowId, request.Content, ResolveActor());
        return Ok(ApiResponse<object>.Success(new
        {
            changed,
            message = changed ? "评论添加成功" : "未匹配到对应需求"
        }));
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportCsv()
    {
        var snapshot = InMemoryMajorDemandStore.GetSnapshot();

        // 医院范围过滤
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        var allowedHospitals = await GetAccessibleHospitalSetAsync(dataScope);
        var exportRows = snapshot.Rows;
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            if (allowedHospitals.Count == 0)
            {
                exportRows = [];
            }
            else
            {
                exportRows = exportRows.Where(row =>
                {
                    var h = row.TryGetValue("医院名称", out var v) ? v : null;
                    return !string.IsNullOrWhiteSpace(h) && allowedHospitals.Contains(h);
                }).ToList();
            }
        }

        var workflowMap = snapshot.WorkflowItems.ToDictionary(x => x.RowId, StringComparer.OrdinalIgnoreCase);

        var headers = snapshot.Columns
            .Where(x => !string.Equals(x, "_RowId", StringComparison.OrdinalIgnoreCase))
            .ToList();
        headers.AddRange(["状态", "负责人", "截止日期", "最新评论", "最近操作时间"]);

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

        foreach (var row in exportRows)
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

    /// <summary>
    /// 根据医院范围过滤 RowId 列表，仅保留当前用户可访问医院的行
    /// </summary>
    private async Task<List<string>> FilterRowIdsByHospitalScopeAsync(List<string> rowIds)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        if (string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            return rowIds; // 管理员/经理不受限制
        }

        var allowedHospitals = await GetAccessibleHospitalSetAsync(dataScope);
        if (allowedHospitals.Count == 0)
        {
            return []; // 无医院权限
        }

        var allowedSet = allowedHospitals;
        var snapshot = InMemoryMajorDemandStore.GetSnapshot();
        var requestedSet = new HashSet<string>(rowIds, StringComparer.OrdinalIgnoreCase);

        return snapshot.Rows
            .Where(row =>
            {
                var id = row.TryGetValue("_RowId", out var v) ? v : null;
                if (id is null || !requestedSet.Contains(id)) return false;
                var hospital = row.TryGetValue("医院名称", out var h) ? h : null;
                return !string.IsNullOrWhiteSpace(hospital) && allowedSet.Contains(hospital);
            })
            .Select(row => row["_RowId"])
            .ToList();
    }

    private async Task<HashSet<string>> GetAccessibleHospitalSetAsync(DataScopeDto dataScope)
    {
        if (string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var projectResult = await projectQueryService.QueryAsync(new ProjectQuery
        {
            Page = 1,
            Size = 50000,
            AccessiblePersonnelNames = dataScope.AccessiblePersonnelNames
        });

        return projectResult.Items
            .Select(x => x.HospitalName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
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