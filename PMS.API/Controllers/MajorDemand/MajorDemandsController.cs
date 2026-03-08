using ClosedXML.Excel;
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

    [HttpPut("{rowId}/cell")]
    public async Task<IActionResult> UpdateCell(string rowId, [FromBody] UpdateCellRequest request)
    {
        if (string.IsNullOrWhiteSpace(rowId) || string.IsNullOrWhiteSpace(request.Column))
        {
            return BadRequest(ApiResponse<object>.Success(new { message = "参数不完整" }));
        }

        var allowedIds = await FilterRowIdsByHospitalScopeAsync([rowId]);
        if (allowedIds.Count == 0)
        {
            return StatusCode(403, new { code = 403, message = "无权操作该行数据" });
        }

        var changed = InMemoryMajorDemandStore.UpdateCell(rowId, request.Column, request.Value);
        return Ok(ApiResponse<object>.Success(new
        {
            changed,
            message = changed ? "更新成功" : "未找到对应行"
        }));
    }

    [HttpPost("rows")]
    public IActionResult AddRow()
    {
        var rowId = InMemoryMajorDemandStore.AddEmptyRow();
        return Ok(ApiResponse<object>.Success(new { rowId, message = "新增成功" }));
    }

    [HttpPost("rows/delete")]
    public async Task<IActionResult> DeleteRows([FromBody] DeleteRowsRequest request)
    {
        if (request.RowIds.Count == 0)
        {
            return BadRequest(ApiResponse<object>.Success(new { message = "rowIds 不能为空" }));
        }

        var allowedIds = await FilterRowIdsByHospitalScopeAsync(request.RowIds);
        if (allowedIds.Count == 0)
        {
            return StatusCode(403, new { code = 403, message = "无权操作所选行" });
        }

        var removed = InMemoryMajorDemandStore.DeleteRows(allowedIds);
        return Ok(ApiResponse<object>.Success(new
        {
            removed,
            message = removed > 0 ? "删除成功" : "未找到对应行"
        }));
    }

    [HttpGet("export-excel")]
    public async Task<IActionResult> ExportExcel()
    {
        var snapshot = InMemoryMajorDemandStore.GetSnapshot();

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

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("重大需求");

        for (var i = 0; i < headers.Count; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        var headerRange = ws.Range(1, 1, 1, headers.Count);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        for (var r = 0; r < exportRows.Count; r++)
        {
            var row = exportRows[r];
            row.TryGetValue("_RowId", out var rowId);
            workflowMap.TryGetValue(rowId ?? string.Empty, out var workflow);

            var colIdx = 1;
            foreach (var col in snapshot.Columns.Where(x =>
                         !string.Equals(x, "_RowId", StringComparison.OrdinalIgnoreCase)))
            {
                ws.Cell(r + 2, colIdx++).Value = row.TryGetValue(col, out var value) ? value ?? string.Empty : string.Empty;
            }

            ws.Cell(r + 2, colIdx++).Value = workflow?.Status ?? string.Empty;
            ws.Cell(r + 2, colIdx++).Value = workflow?.Owner ?? string.Empty;
            ws.Cell(r + 2, colIdx++).Value = workflow?.DueDate ?? string.Empty;
            ws.Cell(r + 2, colIdx++).Value = workflow?.Comments.LastOrDefault()?.Content ?? string.Empty;
            ws.Cell(r + 2, colIdx).Value = workflow?.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
        }

        ws.Columns().AdjustToContents(1, 50);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;
        return File(
            ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"major-demands-{DateTime.Now:yyyyMMddHHmmss}.xlsx");
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

    public class UpdateCellRequest
    {
        public string Column { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class DeleteRowsRequest
    {
        public List<string> RowIds { get; set; } = [];
    }
}