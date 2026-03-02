using Microsoft.AspNetCore.Mvc;
using PMS.API.Models;
using PMS.Application.Contracts;
using PMS.Application.Models;
using PMS.Domain.Entities;
using PMS.Infrastructure.Services;
using System.Text;

namespace PMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController(IProjectQueryService projectQueryService) : ControllerBase
{
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
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
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
            Page = page,
            Size = size
        }, cancellationToken);

        return Ok(ApiResponse<PagedResult<ProjectEntity>>.Success(result));
    }

    [HttpPost("batch-update")]
    public IActionResult BatchUpdate([FromBody] ProjectBatchUpdateRequest request)
    {
        if (request.ProjectIds.Count == 0)
        {
            return BadRequest(ApiResponse<object>.Success(new { message = "projectIds 不能为空" }));
        }

        var affected = InMemoryProjectDataStore.BatchUpdateProjects(
            request.ProjectIds,
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
        CancellationToken cancellationToken = default)
    {
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
            Page = 1,
            Size = 50000
        }, cancellationToken);

        var headers = new[]
        {
            "ID", "医院名称", "产品", "省份", "组别", "销售", "维护人员", "售后开始", "售后结束", "级别", "合同状态", "维护金额(万)", "超期天数"
        };

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var item in result.Items)
        {
            var row = new[]
            {
                item.Id.ToString(),
                item.HospitalName,
                item.ProductName,
                item.Province,
                item.GroupName,
                item.SalesName,
                item.MaintenancePersonName,
                item.AfterSalesStartDate,
                item.AfterSalesEndDate,
                item.HospitalLevel,
                item.ContractStatus,
                item.MaintenanceAmount.ToString("0.##"),
                item.OverdueDays.ToString()
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
