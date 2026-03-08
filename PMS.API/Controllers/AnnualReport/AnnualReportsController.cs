using System.Text;
using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.AnnualReport;
using PMS.Application.Models;
using PMS.Application.Models.AnnualReport;

namespace PMS.API.Controllers.AnnualReport;

[ApiController]
[Route("api/annual-reports")]
public class AnnualReportsController(
    IAnnualReportService annualReportService,
    IAccessControlService accessControlService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        // 检查是否需要按医院范围过滤
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase)
            && dataScope.AccessibleHospitalNames is { Count: > 0 })
        {
            // 非管理员：获取全量数据后按医院权限过滤再统计
            var allResult = await annualReportService.QueryAsync(
                new AnnualReportQuery { Page = 1, Size = int.MaxValue }, cancellationToken);
            var scopedItems = HospitalScopeHelper.FilterByHospitalScope(
                dataScope, allResult.Items, x => x.HospitalName).ToList();

            var thisYear = DateTime.Today.Year;
            var currentMonth = DateTime.Today.ToString("yyyy-MM");
            var scopedSummary = new AnnualReportSummaryDto
            {
                NotStartedCount = scopedItems.Count(x => x.Status == "未开始"),
                WritingCount = scopedItems.Count(x => x.Status == "编写中"),
                SubmittedCount = scopedItems.Count(x => x.Status == "已提交"),
                CompletedCount = scopedItems.Count(x => x.Status == "已完成"),
                ThisYearCount = scopedItems.Count(x => x.ReportYear == thisYear),
                DueThisMonthCount = scopedItems.Count(x =>
                    string.Equals(x.DueMonth, currentMonth, StringComparison.OrdinalIgnoreCase)),
                OverdueCount = scopedItems.Count(x =>
                    string.Compare(x.DueMonth, currentMonth, StringComparison.Ordinal) <= 0
                    && x.Status != "已完成"),
                Total = scopedItems.Count
            };
            return Ok(ApiResponse<AnnualReportSummaryDto>.Success(scopedSummary));
        }

        var summary = await annualReportService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<AnnualReportSummaryDto>.Success(summary));
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? status,
        [FromQuery] int? reportYear,
        [FromQuery] string? dueMonth,
        [FromQuery] string? groupName,
        [FromQuery] string? servicePerson,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        // 医院范围过滤
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        var needsHospitalScope =
            !string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase)
            && dataScope.AccessibleHospitalNames is { Count: > 0 };

        if (needsHospitalScope)
        {
            // 先不分页地获取全量数据，按医院权限过滤后再分页
            var allResult = await annualReportService.QueryAsync(new AnnualReportQuery
            {
                Status = status,
                ReportYear = reportYear,
                DueMonth = dueMonth,
                GroupName = groupName,
                ServicePerson = servicePerson,
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

            return Ok(ApiResponse<PagedResult<AnnualReportItemDto>>.Success(
                new PagedResult<AnnualReportItemDto>
                {
                    Items = pagedItems,
                    Total = totalScoped,
                    Page = effectivePage,
                    Size = effectiveSize
                }));
        }

        var result = await annualReportService.QueryAsync(new AnnualReportQuery
        {
            Status = status,
            ReportYear = reportYear,
            DueMonth = dueMonth,
            GroupName = groupName,
            ServicePerson = servicePerson,
            Page = page,
            Size = size
        }, cancellationToken);

        return Ok(ApiResponse<PagedResult<AnnualReportItemDto>>.Success(result));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] AnnualReportUpsertDto dto,
        CancellationToken cancellationToken = default)
    {
        var updated = await annualReportService.UpdateAsync(id, dto, cancellationToken);
        if (updated is null)
        {
            return NotFound(new ApiResponse<object> { Code = 404, Message = "未找到对应年度报告" });
        }

        return Ok(ApiResponse<AnnualReportItemDto>.Success(updated));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? status,
        [FromQuery] int? reportYear,
        [FromQuery] string? dueMonth,
        [FromQuery] string? groupName,
        [FromQuery] string? servicePerson,
        CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);

        var allResult = await annualReportService.QueryAsync(new AnnualReportQuery
        {
            Status = status,
            ReportYear = reportYear,
            DueMonth = dueMonth,
            GroupName = groupName,
            ServicePerson = servicePerson,
            Page = 1,
            Size = 50000
        }, cancellationToken);

        var items = HospitalScopeHelper.FilterByHospitalScope(
            dataScope, allResult.Items, x => x.HospitalName).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("\uFEFFID,商机编号,医院名称,产品名称,省份,组别,服务人员,实施状态,维护开始日期,维护结束日期,到期月份,报告年度,状态,提交日期,备注");
        foreach (var item in items)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(item.Id.ToString()),
                EscapeCsv(item.OpportunityNumber),
                EscapeCsv(item.HospitalName),
                EscapeCsv(item.ProductName),
                EscapeCsv(item.Province),
                EscapeCsv(item.GroupName),
                EscapeCsv(item.ServicePerson),
                EscapeCsv(item.ImplementationStatus),
                EscapeCsv(item.MaintenanceStartDate),
                EscapeCsv(item.MaintenanceEndDate),
                EscapeCsv(item.DueMonth),
                EscapeCsv(item.ReportYear.ToString()),
                EscapeCsv(item.Status),
                EscapeCsv(item.SubmitDate?.ToString("yyyy-MM-dd") ?? ""),
                EscapeCsv(item.Remarks)));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"年度报告-{DateTime.Now:yyyyMMddHHmmss}.csv");
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
