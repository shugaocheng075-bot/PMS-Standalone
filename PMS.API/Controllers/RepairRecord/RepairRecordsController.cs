using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.RepairRecord;
using PMS.Application.Models;
using PMS.Application.Models.Access;
using PMS.Application.Models.RepairRecord;
using System.Text;

namespace PMS.API.Controllers.RepairRecord;

[ApiController]
[Route("api/repair-records")]
public class RepairRecordsController(
    IRepairRecordService repairRecordService,
    IProjectQueryService projectQueryService,
    IAccessControlService accessControlService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await repairRecordService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<RepairRecordSummaryDto>.Success(summary));
    }

    [HttpGet]
    public async Task<IActionResult> Query(
        [FromQuery] string? hospitalName,
        [FromQuery] string? reporterName,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        var allowedHospitals = await GetAccessibleHospitalSetAsync(dataScope, cancellationToken);
        var normalizedPage = page > 0 ? page : 1;
        var normalizedSize = size > 0 ? size : 20;

        var query = new RepairRecordQuery
        {
            HospitalName = hospitalName,
            ReporterName = reporterName,
            Status = status,
            Page = 1,
            Size = 50000,
            AccessiblePersonnelNames = dataScope.ScopeType == "all" ? null : dataScope.AccessiblePersonnelNames
        };

        var rawResult = await repairRecordService.QueryAsync(query, cancellationToken);
        var scopedItems = rawResult.Items.AsEnumerable();

        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            if (allowedHospitals.Count == 0)
            {
                scopedItems = Enumerable.Empty<RepairRecordItemDto>();
            }
            else
            {
                scopedItems = scopedItems.Where(x => allowedHospitals.Contains(x.HospitalName));
            }
        }

        var scopedList = scopedItems.ToList();
        var result = new Application.Models.PagedResult<RepairRecordItemDto>
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
        var item = await repairRecordService.GetByIdAsync(id, cancellationToken);
        if (item is null) return NotFound(ApiResponse<object>.Success(null));

        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        if (!await CanOperateInOwnProjectsAsync(dataScope, item.HospitalName, cancellationToken))
        {
            return StatusCode(403, new { code = 403, message = "无权查看该项目的报修记录" });
        }

        return Ok(ApiResponse<RepairRecordItemDto>.Success(item));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RepairRecordUpsertDto dto, CancellationToken cancellationToken)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var profile = await accessControlService.GetUserProfileAsync(personnelId);
        var reporterName = profile?.PersonnelName ?? "未知";

        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        if (!await CanOperateInOwnProjectsAsync(dataScope, dto.HospitalName, cancellationToken))
        {
            return StatusCode(403, new { code = 403, message = "无权在自己的项目外创建报修记录" });
        }

        dto.ReporterName = reporterName;

        var item = await repairRecordService.CreateAsync(reporterName, dto, cancellationToken);
        return Ok(ApiResponse<RepairRecordItemDto>.Success(item));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] RepairRecordUpsertDto dto, CancellationToken cancellationToken)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        var existing = await repairRecordService.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound(ApiResponse<object>.Success(null));
        }

        if (!await CanOperateInOwnProjectsAsync(dataScope, existing.HospitalName, cancellationToken)
            || !await CanOperateInOwnProjectsAsync(dataScope, dto.HospitalName, cancellationToken))
        {
            return StatusCode(403, new { code = 403, message = "无权修改自己的项目外报修记录" });
        }

        var item = await repairRecordService.UpdateAsync(id, dto, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<object>.Success(null));
        }

        return Ok(ApiResponse<RepairRecordItemDto>.Success(item));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        var existing = await repairRecordService.GetByIdAsync(id, cancellationToken);
        if (existing is not null && !await CanOperateInOwnProjectsAsync(dataScope, existing.HospitalName, cancellationToken))
        {
            return StatusCode(403, new { code = 403, message = "无权删除自己的项目外报修记录" });
        }

        var deleted = await repairRecordService.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound(ApiResponse<object>.Success(null));
        return Ok(ApiResponse<object>.Success(new { message = "删除成功" }));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? hospitalName,
        [FromQuery] string? reporterName,
        [FromQuery] string? status,
        CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        var allowedHospitals = await GetAccessibleHospitalSetAsync(dataScope, cancellationToken);

        var rawResult = await repairRecordService.QueryAsync(new RepairRecordQuery
        {
            HospitalName = hospitalName,
            ReporterName = reporterName,
            Status = status,
            Page = 1,
            Size = 50000,
            AccessiblePersonnelNames = dataScope.ScopeType == "all" ? null : dataScope.AccessiblePersonnelNames
        }, cancellationToken);

        var items = rawResult.Items.AsEnumerable();
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            if (allowedHospitals.Count == 0)
            {
                items = Enumerable.Empty<RepairRecordItemDto>();
            }
            else
            {
                items = items.Where(x => allowedHospitals.Contains(x.HospitalName));
            }
        }

        var headers = new[]
        {
            "ID", "项目ID", "医院", "产品", "项目名称", "问题分类", "上报人", "严重程度", "状态", "上报日期", "处理内容", "处理方案"
        };

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var item in items)
        {
            var row = new List<string>
            {
                item.Id.ToString(),
                item.ProjectId.ToString(),
                item.HospitalName ?? string.Empty,
                item.ProductName ?? string.Empty,
                item.ProjectName ?? string.Empty,
                item.IssueCategory ?? string.Empty,
                item.ReporterName ?? string.Empty,
                item.Severity ?? string.Empty,
                item.Status ?? string.Empty,
                item.ReportedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty,
                item.Content ?? string.Empty,
                item.Resolution ?? string.Empty
            };
            sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }

        var content = Encoding.UTF8.GetBytes("\uFEFF" + sb);
        return File(content, "text/csv; charset=utf-8", $"repair-records-{DateTime.Now:yyyyMMddHHmmss}.csv");
    }

    private async Task<bool> CanOperateInOwnProjectsAsync(DataScopeDto dataScope, string hospitalName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(hospitalName))
        {
            return false;
        }

        if (string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var result = await projectQueryService.QueryAsync(new ProjectQuery
        {
            HospitalName = hospitalName,
            Page = 1,
            Size = 1,
            AccessiblePersonnelNames = dataScope.AccessiblePersonnelNames
        }, cancellationToken);

        return result.Total > 0;
    }

    private async Task<HashSet<string>> GetAccessibleHospitalSetAsync(DataScopeDto dataScope, CancellationToken cancellationToken)
    {
        if (string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var result = await projectQueryService.QueryAsync(new ProjectQuery
        {
            Page = 1,
            Size = 50000,
            AccessiblePersonnelNames = dataScope.AccessiblePersonnelNames
        }, cancellationToken);

        return result.Items
            .Select(x => x.HospitalName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
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
}
