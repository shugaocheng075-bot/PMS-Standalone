using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.Contract;
using PMS.Application.Models;
using PMS.Application.Models.Contract;

namespace PMS.API.Controllers.Contract;

[ApiController]
[Route("api/contracts/alerts")]
public class AlertsController(
    IContractAlertService alertService,
    IAccessControlService accessControlService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase)
            && dataScope.AccessibleHospitalNames is { Count: > 0 })
        {
            var allResult = await alertService.QueryAlertsAsync(
                new ContractAlertQuery { Page = 1, Size = int.MaxValue }, cancellationToken);
            var scopedItems = HospitalScopeHelper.FilterByHospitalScope(
                dataScope, allResult.Items, x => x.HospitalName).ToList();

            var scopedSummary = new ContractAlertSummaryDto
            {
                ReminderCount = scopedItems.Count(x => x.AlertLevel == "提醒"),
                WarningCount = scopedItems.Count(x => x.AlertLevel == "警告"),
                CriticalCount = scopedItems.Count(x => x.AlertLevel == "严重"),
                Total = scopedItems.Count
            };
            return Ok(ApiResponse<ContractAlertSummaryDto>.Success(scopedSummary));
        }

        var summary = await alertService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<ContractAlertSummaryDto>.Success(summary));
    }

    [HttpGet]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] string? alertLevel,
        [FromQuery] string? contractType,
        [FromQuery] string? contractValidityStatus,
        [FromQuery] string? province,
        [FromQuery] string? groupName,
        [FromQuery] string? salesName,
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
            var allResult = await alertService.QueryAlertsAsync(new ContractAlertQuery
            {
                AlertLevel = alertLevel,
                ContractType = contractType,
                ContractValidityStatus = contractValidityStatus,
                Province = province,
                GroupName = groupName,
                SalesName = salesName,
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

            return Ok(ApiResponse<PagedResult<ContractAlertItemDto>>.Success(
                new PagedResult<ContractAlertItemDto>
                {
                    Items = pagedItems,
                    Total = totalScoped,
                    Page = effectivePage,
                    Size = effectiveSize
                }));
        }

        var result = await alertService.QueryAlertsAsync(new ContractAlertQuery
        {
            AlertLevel = alertLevel,
            ContractType = contractType,
            ContractValidityStatus = contractValidityStatus,
            Province = province,
            GroupName = groupName,
            SalesName = salesName,
            Page = page,
            Size = size
        }, cancellationToken);

        return Ok(ApiResponse<PagedResult<ContractAlertItemDto>>.Success(result));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? alertLevel,
        [FromQuery] string? province,
        [FromQuery] string? groupName,
        [FromQuery] string? salesName,
        CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId);
        var allResult = await alertService.QueryAlertsAsync(new ContractAlertQuery
        {
            AlertLevel = alertLevel,
            Province = province,
            GroupName = groupName,
            SalesName = salesName,
            Page = 1,
            Size = int.MaxValue
        }, cancellationToken);

        var rows = HospitalScopeHelper.FilterByHospitalScope(dataScope, allResult.Items, x => x.HospitalName).ToList();

        string[] headers = ["医院", "省份", "组别", "销售", "合同类型", "合同状态", "有效性", "维护金额(万)", "超期天数", "预警级别"];
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("合同预警");
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
            ws.Cell(n, 1).Value = row.HospitalName;
            ws.Cell(n, 2).Value = row.Province;
            ws.Cell(n, 3).Value = row.GroupName;
            ws.Cell(n, 4).Value = row.SalesName;
            ws.Cell(n, 5).Value = row.ContractType;
            ws.Cell(n, 6).Value = row.ContractStatus;
            ws.Cell(n, 7).Value = row.ContractValidityStatus;
            ws.Cell(n, 8).Value = (double)row.MaintenanceAmount;
            ws.Cell(n, 9).Value = row.OverdueDays;
            ws.Cell(n, 10).Value = row.AlertLevel;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var fileName = $"合同预警_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
