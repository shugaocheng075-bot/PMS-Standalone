using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts;
using PMS.Application.Contracts.Access;
using PMS.Application.Models;
using PMS.Application.Models.Access;
using PMS.Domain.Entities;
using PMS.Infrastructure.Services;
using System.Text;

namespace PMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController(
    IProjectQueryService projectQueryService,
    IAccessControlService accessControlService) : ControllerBase
{
    private static readonly string[] EditableProjectFields =
    [
        nameof(ProjectBatchUpdateRequest.ContractStatus),
        nameof(ProjectBatchUpdateRequest.GroupName),
        nameof(ProjectBatchUpdateRequest.SalesName),
        nameof(ProjectBatchUpdateRequest.MaintenancePersonName),
        nameof(ProjectBatchUpdateRequest.HospitalLevel)
    ];

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? hospitalName,
        [FromQuery] string? productName,
        [FromQuery] string? province,
        [FromQuery] string? groupName,
        [FromQuery] string? salesName,
        [FromQuery] string? maintenancePersonName,
        [FromQuery] string? afterSalesEndDateFrom,
        [FromQuery] string? afterSalesEndDateTo,
        [FromQuery] string? hospitalLevel,
        [FromQuery] string? contractStatus,
        [FromQuery] string? contractValidityStatus,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);

        var result = await projectQueryService.QueryAsync(new ProjectQuery
        {
            HospitalName = hospitalName,
            ProductName = productName,
            Province = province,
            GroupName = groupName,
            SalesName = salesName,
            MaintenancePersonName = maintenancePersonName,
            AfterSalesEndDateFrom = afterSalesEndDateFrom,
            AfterSalesEndDateTo = afterSalesEndDateTo,
            HospitalLevel = hospitalLevel,
            ContractStatus = contractStatus,
            ContractValidityStatus = contractValidityStatus,
            Page = page,
            Size = size,
            AccessiblePersonnelNames = dataScope.ScopeType == "all" ? null : dataScope.AccessiblePersonnelNames
        }, cancellationToken);

        return Ok(ApiResponse<PagedResult<ProjectEntity>>.Success(result));
    }

    [HttpPost("batch-update")]
    public async Task<IActionResult> BatchUpdate([FromBody] ProjectBatchUpdateRequest request)
    {
        if (request.ProjectIds.Count == 0)
        {
            return BadRequest(ApiResponse<object>.Success(new { message = "projectIds 不能为空" }));
        }

        var personnelId = HttpContext.GetCurrentPersonnelId();
        var profile = await accessControlService.GetUserProfileAsync(personnelId);
        if (!CanManageProjects(profile))
        {
            return StatusCode(403, new { code = 403, message = "普通运维人员不能编辑项目信息" });
        }

        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        var requestedIds = request.ProjectIds.Distinct().ToList();
        var allowedIds = requestedIds
            .Where(id =>
            {
                var project = InMemoryProjectDataStore.GetById(id);
                return project is not null && CanAccessProject(project, dataScope);
            })
            .ToList();

        if (allowedIds.Count == 0)
        {
            return StatusCode(403, new { code = 403, message = "无权编辑所选项目" });
        }

        var affected = InMemoryProjectDataStore.BatchUpdateProjects(
            allowedIds,
            request.ContractStatus,
            request.GroupName,
            request.SalesName,
            request.MaintenancePersonName,
            request.HospitalLevel);

        return Ok(ApiResponse<object>.Success(new
        {
            affected,
            message = affected > 0 ? $"批量更新成功，共 {affected} 条" : "未匹配到可更新数据"
        }));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] ProjectBatchUpdateRequest request)
    {
        if (!HasAnyEditableField(request))
        {
            return BadRequest(ApiResponse<object>.Success(new { message = "请至少填写一个需要修改的字段" }));
        }

        var personnelId = HttpContext.GetCurrentPersonnelId();
        var profile = await accessControlService.GetUserProfileAsync(personnelId);
        if (!CanManageProjects(profile))
        {
            return StatusCode(403, new { code = 403, message = "普通运维人员不能编辑项目信息" });
        }

        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);

        var project = InMemoryProjectDataStore.GetById(id);
        if (project is null)
        {
            return NotFound(ApiResponse<object>.Success(new { message = "项目不存在" }));
        }

        if (!CanAccessProject(project, dataScope))
        {
            return Forbid();
        }

        var affected = InMemoryProjectDataStore.BatchUpdateProjects(
            [id],
            request.ContractStatus,
            request.GroupName,
            request.SalesName,
            request.MaintenancePersonName,
            request.HospitalLevel);

        return Ok(ApiResponse<object>.Success(new
        {
            affected,
            message = affected > 0 ? "更新成功" : "未匹配到可更新数据"
        }));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var profile = await accessControlService.GetUserProfileAsync(personnelId);
        if (!CanManageProjects(profile))
        {
            return StatusCode(403, new { code = 403, message = "普通运维人员不能编辑项目信息" });
        }

        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);

        var project = InMemoryProjectDataStore.GetById(id);
        if (project is null)
        {
            return NotFound(ApiResponse<object>.Success(new { message = "项目不存在" }));
        }

        if (!CanAccessProject(project, dataScope))
        {
            return Forbid();
        }

        var affected = InMemoryProjectDataStore.DeleteProjects([id]);
        return Ok(ApiResponse<object>.Success(new
        {
            affected,
            message = affected > 0 ? "删除成功" : "未匹配到可删除数据"
        }));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? hospitalName,
        [FromQuery] string? productName,
        [FromQuery] string? province,
        [FromQuery] string? groupName,
        [FromQuery] string? salesName,
        [FromQuery] string? maintenancePersonName,
        [FromQuery] string? afterSalesEndDateFrom,
        [FromQuery] string? afterSalesEndDateTo,
        [FromQuery] string? hospitalLevel,
        [FromQuery] string? contractStatus,
        [FromQuery] string? contractValidityStatus,
        CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);

        var result = await projectQueryService.QueryAsync(new ProjectQuery
        {
            HospitalName = hospitalName,
            ProductName = productName,
            Province = province,
            GroupName = groupName,
            SalesName = salesName,
            MaintenancePersonName = maintenancePersonName,
            AfterSalesEndDateFrom = afterSalesEndDateFrom,
            AfterSalesEndDateTo = afterSalesEndDateTo,
            HospitalLevel = hospitalLevel,
            ContractStatus = contractStatus,
            ContractValidityStatus = contractValidityStatus,
            Page = 1,
            Size = 50000,
            AccessiblePersonnelNames = dataScope.ScopeType == "all" ? null : dataScope.AccessiblePersonnelNames
        }, cancellationToken);

        var headers = new[]
        {
            "序号", "服务区域", "省", "市", "销售", "服务组别", "服务人员", "医院等级", "最终用户", "维护产品", "点位", "销售合同额", "维护合同额", "年度产值", "合同状态", "维护开始日期", "维护结束日期", "是否超期", "超期天数", "机会号", "驻地", "是否驻场", "驻场人数", "备注", "验收日期"
        };

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var item in result.Items)
        {
            var row = new[]
            {
                item.SerialNumber,
                item.ServiceArea,
                item.Province,
                item.City,
                item.SalesName,
                item.GroupName,
                item.MaintenancePersonName,
                item.HospitalLevel,
                item.HospitalName,
                item.ProductName,
                item.Points,
                item.SalesAmount.ToString("0.##"),
                item.MaintenanceAmount.ToString("0.##"),
                item.AnnualOutput.ToString("0.##"),
                item.ContractStatus,
                item.AfterSalesStartDate,
                item.AfterSalesEndDate,
                item.OverdueDays > 0 ? "是" : "否",
                item.OverdueDays.ToString(),
                item.OpportunityNumber,
                item.StationLocation,
                item.IsStationedOnsite,
                item.StationedCount,
                item.Remarks,
                item.AcceptanceDate
            };
            sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }

        var content = Encoding.UTF8.GetBytes("\uFEFF" + sb);
        return File(content, "text/csv; charset=utf-8", $"projects-{DateTime.Now:yyyyMMddHHmmss}.csv");
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

    private static bool HasAnyEditableField(ProjectBatchUpdateRequest request)
    {
        foreach (var field in EditableProjectFields)
        {
            var value = request.GetType().GetProperty(field)?.GetValue(request) as string;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool CanAccessProject(ProjectEntity project, DataScopeDto scope)
    {
        if (scope.ScopeType == "all")
        {
            return true;
        }

        var accessibleNames = scope.AccessiblePersonnelNames ?? [];
        return accessibleNames.Contains(project.MaintenancePersonName);
    }

    private static bool CanManageProjects(PersonnelAccessProfileDto? profile)
    {
        if (profile is null)
        {
            return false;
        }

        if (profile.IsAdmin)
        {
            return true;
        }

        return string.Equals(profile.SystemRole, InMemoryAccessControlService.RoleManager, StringComparison.OrdinalIgnoreCase)
            || string.Equals(profile.SystemRole, InMemoryAccessControlService.RoleSupervisor, StringComparison.OrdinalIgnoreCase);
    }

    public class ProjectBatchUpdateRequest
    {
        public List<long> ProjectIds { get; set; } = [];
        public string? ContractStatus { get; set; }
        public string? GroupName { get; set; }
        public string? SalesName { get; set; }
        public string? MaintenancePersonName { get; set; }
        public string? HospitalLevel { get; set; }
    }
}
