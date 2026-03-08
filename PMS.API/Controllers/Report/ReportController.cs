using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.Handover;
using PMS.Application.Contracts.Inspection;
using PMS.Application.Contracts.MonthlyReport;
using PMS.Application.Contracts.Personnel;
using PMS.Application.Contracts.RepairRecord;
using PMS.Application.Models.Access;
using PMS.Application.Models.Handover;
using PMS.Application.Models.Inspection;
using PMS.Application.Models.MonthlyReport;
using PMS.Application.Models.Personnel;
using PMS.Application.Models.RepairRecord;
using PMS.Application.Models.WorkHours;
using PMS.Infrastructure.Services;
using System.Text.Json;
using System.Text;
using ClosedXML.Excel;
using System.Globalization;
using System.Security.Cryptography;

namespace PMS.API.Controllers.Report;

[ApiController]
[Route("api/reports")]
public class ReportController(
    IMonthlyReportService monthlyReportService,
    IAccessControlService accessControlService,
    IPersonnelService personnelService,
    IHandoverService handoverService,
    IInspectionService inspectionService,
    IRepairRecordService repairRecordService) : ControllerBase
{
    private static readonly char[] PersonnelSeparators = [',', '，', '、', ';', '；', '/', '\\', '|', '+', '&', '\n', '\r', '\t'];

    /// <summary>
    /// 工时报表 — 从导入的工时报表数据读取
    /// </summary>
    [HttpGet("workhours")]
    public async Task<IActionResult> GetWorkHoursReport(
        [FromQuery] string? reportMonth,
        [FromQuery] string? implementationStatus,
        CancellationToken cancellationToken = default)
    {
        var resolvedMonth = ResolveReportMonth(reportMonth);
        var allRows = InMemoryWorkHoursReportStore.GetOrCreateMonthRows(
            resolvedMonth,
            () => BuildAutoWorkHoursRows(resolvedMonth));

        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        var scopedRows = PMS.API.Middleware.HospitalScopeHelper
            .FilterByHospitalScope(dataScope, allRows, x => x.HospitalName).ToList();

        IEnumerable<WorkHoursReportRowDto> filtered = scopedRows;

        if (!string.IsNullOrWhiteSpace(implementationStatus))
        {
            var s = implementationStatus.Trim();
            filtered = filtered.Where(x => string.Equals((x.ImplementationStatus ?? "").Trim(), s, StringComparison.OrdinalIgnoreCase));
        }

        var rows = filtered.ToList();
        return Ok(ApiResponse<object>.Success(new
        {
            total = rows.Count,
            rows
        }));
    }

    /// <summary>
    /// 更新工时报表单行
    /// </summary>
    [HttpPut("workhours/{id:long}")]
    public IActionResult UpdateWorkHoursReportRow(
        [FromRoute] long id,
        [FromQuery] string? reportMonth,
        [FromBody] WorkHoursReportRowUpdateDto request)
    {
        if (id <= 0)
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = "无效的行ID",
                Data = null
            });
        }

        var resolvedMonth = ResolveReportMonth(reportMonth);
        var updated = InMemoryWorkHoursReportStore.Update(resolvedMonth, id, row =>
        {
            row.OpportunityNumber = (request.OpportunityNumber ?? string.Empty).Trim();
            row.HospitalName = (request.HospitalName ?? string.Empty).Trim();
            row.ProductName = (request.ProductName ?? string.Empty).Trim();
            row.ImplementationStatus = (request.ImplementationStatus ?? string.Empty).Trim();
            row.WorkHoursManDays = Math.Round(request.WorkHoursManDays, 0, MidpointRounding.AwayFromZero);
            row.PersonnelCount = request.PersonnelCount;
            row.Personnel1 = (request.Personnel1 ?? string.Empty).Trim();
            row.Personnel2 = (request.Personnel2 ?? string.Empty).Trim();
            row.Personnel3 = (request.Personnel3 ?? string.Empty).Trim();
            row.Personnel4 = (request.Personnel4 ?? string.Empty).Trim();
            row.Personnel5 = (request.Personnel5 ?? string.Empty).Trim();
            row.MaintenanceStartDate = (request.MaintenanceStartDate ?? string.Empty).Trim();
            row.MaintenanceEndDate = (request.MaintenanceEndDate ?? string.Empty).Trim();
            row.AfterSalesProjectType = (request.AfterSalesProjectType ?? string.Empty).Trim();
            row.Remarks = (request.Remarks ?? string.Empty).Trim();
        });

        if (updated is null)
        {
            return NotFound(new ApiResponse<object>
            {
                Code = 404,
                Message = "未找到对应的工时报表行",
                Data = null
            });
        }

        return Ok(ApiResponse<WorkHoursReportRowDto>.Success(updated));
    }

    /// <summary>
    /// 导出工时报表（Excel .xlsx）
    /// </summary>
    [HttpGet("workhours/export")]
    public async Task<IActionResult> ExportWorkHoursReport(
        [FromQuery] string? reportMonth,
        [FromQuery] string? implementationStatus,
        CancellationToken cancellationToken = default)
    {
        var resolvedMonth = ResolveReportMonth(reportMonth);
        var allRows = InMemoryWorkHoursReportStore.GetOrCreateMonthRows(
            resolvedMonth,
            () => BuildAutoWorkHoursRows(resolvedMonth));

        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        var scopedRows = PMS.API.Middleware.HospitalScopeHelper
            .FilterByHospitalScope(dataScope, allRows, x => x.HospitalName).ToList();

        IEnumerable<WorkHoursReportRowDto> filtered = scopedRows;

        if (!string.IsNullOrWhiteSpace(implementationStatus))
        {
            var s = implementationStatus.Trim();
            filtered = filtered.Where(x => string.Equals((x.ImplementationStatus ?? "").Trim(), s, StringComparison.OrdinalIgnoreCase));
        }

        var rows = filtered.ToList();
        var bytes = BuildWorkHoursExcel(rows);
        var fileName = $"工时报表_{resolvedMonth}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// 重新计算某个月工时报表
    /// </summary>
    [HttpPost("workhours/regenerate")]
    public IActionResult RegenerateWorkHoursReport([FromQuery] string? reportMonth)
    {
        var resolvedMonth = ResolveReportMonth(reportMonth);
        var importedRows = InMemoryWorkHoursReportStore.GetMonthRows(resolvedMonth);
        var rows = BuildAutoWorkHoursRows(resolvedMonth, importedRows);
        InMemoryWorkHoursReportStore.ReplaceMonthRows(resolvedMonth, rows);

        return Ok(ApiResponse<object>.Success(new
        {
            reportMonth = resolvedMonth,
            total = rows.Count
        }));
    }

    /// <summary>
    /// 导入 Excel 文件到工时报表（清空指定月份后导入）
    /// </summary>
    [HttpPost("workhours/import")]
    public IActionResult ImportWorkHoursReport(
        IFormFile file,
        [FromQuery] string? reportMonth,
        [FromQuery] bool autoCalculate = true)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ApiResponse<object> { Code = 400, Message = "请上传 Excel 文件", Data = null });
        }

        var resolvedMonth = ResolveReportMonth(reportMonth);

        List<WorkHoursReportRowDto> importedRows;
        try
        {
            using var stream = file.OpenReadStream();
            importedRows = ParseWorkHoursExcel(stream);
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<object> { Code = 400, Message = $"Excel 解析失败: {ex.Message}", Data = null });
        }

        if (autoCalculate)
        {
            // 先存入原始行，再基于导入行重新计算工时
            InMemoryWorkHoursReportStore.ReplaceMonthRows(resolvedMonth, importedRows);
            var recalculated = BuildAutoWorkHoursRows(resolvedMonth, importedRows);
            InMemoryWorkHoursReportStore.ReplaceMonthRows(resolvedMonth, recalculated);
            importedRows = recalculated;
        }
        else
        {
            InMemoryWorkHoursReportStore.ReplaceMonthRows(resolvedMonth, importedRows);
        }

        return Ok(ApiResponse<object>.Success(new
        {
            reportMonth = resolvedMonth,
            total = importedRows.Count
        }));
    }

    /// <summary>
    /// 删除单行工时报表
    /// </summary>
    [HttpDelete("workhours/{id:long}")]
    public IActionResult DeleteWorkHoursReportRow(
        [FromRoute] long id,
        [FromQuery] string? reportMonth)
    {
        var resolvedMonth = ResolveReportMonth(reportMonth);
        var rows = InMemoryWorkHoursReportStore.GetMonthRows(resolvedMonth).ToList();
        var removed = rows.RemoveAll(r => r.Id == id);
        if (removed == 0)
        {
            return NotFound(new ApiResponse<object> { Code = 404, Message = "未找到对应的工时报表行", Data = null });
        }

        InMemoryWorkHoursReportStore.ReplaceMonthRows(resolvedMonth, rows);
        return Ok(ApiResponse<object>.Success(new { message = "删除成功" }));
    }

    /// <summary>
    /// 新增单行工时报表
    /// </summary>
    [HttpPost("workhours/row")]
    public IActionResult AddWorkHoursReportRow(
        [FromQuery] string? reportMonth,
        [FromBody] WorkHoursReportRowUpdateDto dto)
    {
        var resolvedMonth = ResolveReportMonth(reportMonth);
        var rows = InMemoryWorkHoursReportStore.GetMonthRows(resolvedMonth).ToList();
        var newRow = new WorkHoursReportRowDto
        {
            OpportunityNumber = (dto.OpportunityNumber ?? string.Empty).Trim(),
            HospitalName = (dto.HospitalName ?? string.Empty).Trim(),
            ProductName = (dto.ProductName ?? string.Empty).Trim(),
            ImplementationStatus = (dto.ImplementationStatus ?? string.Empty).Trim(),
            WorkHoursManDays = Math.Round(dto.WorkHoursManDays, 0, MidpointRounding.AwayFromZero),
            PersonnelCount = dto.PersonnelCount,
            Personnel1 = (dto.Personnel1 ?? string.Empty).Trim(),
            Personnel2 = (dto.Personnel2 ?? string.Empty).Trim(),
            Personnel3 = (dto.Personnel3 ?? string.Empty).Trim(),
            Personnel4 = (dto.Personnel4 ?? string.Empty).Trim(),
            Personnel5 = (dto.Personnel5 ?? string.Empty).Trim(),
            MaintenanceStartDate = (dto.MaintenanceStartDate ?? string.Empty).Trim(),
            MaintenanceEndDate = (dto.MaintenanceEndDate ?? string.Empty).Trim(),
            AfterSalesProjectType = (dto.AfterSalesProjectType ?? string.Empty).Trim(),
            Remarks = (dto.Remarks ?? string.Empty).Trim()
        };
        rows.Add(newRow);
        InMemoryWorkHoursReportStore.ReplaceMonthRows(resolvedMonth, rows);
        // Return the row with assigned ID
        var savedRows = InMemoryWorkHoursReportStore.GetMonthRows(resolvedMonth);
        var saved = savedRows.LastOrDefault();
        return Ok(ApiResponse<WorkHoursReportRowDto>.Success(saved!));
    }

    private static List<WorkHoursReportRowDto> ParseWorkHoursExcel(Stream stream)
    {
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();

        // Find header row — scan first 5 rows for "机会号" column
        var headerRow = 0;
        var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var r = 1; r <= Math.Min(5, ws.LastRowUsed()?.RowNumber() ?? 1); r++)
        {
            for (var c = 1; c <= ws.LastColumnUsed()?.ColumnNumber(); c++)
            {
                var val = ws.Cell(r, c).GetString().Trim();
                if (string.Equals(val, "机会号", StringComparison.OrdinalIgnoreCase))
                {
                    headerRow = r;
                    break;
                }
            }
            if (headerRow > 0) break;
        }

        if (headerRow == 0)
        {
            throw new InvalidOperationException("未找到表头行（需包含'机会号'列）");
        }

        // Build column map
        string[] expectedHeaders = ["机会号", "客户名称", "产品名称", "实施状态", "工时（人天）", "实施人员（个数）",
            "人员1", "人员2", "人员3", "人员4", "人员5", "维护开始时间", "维护结束时间", "售后项目类型", "备注"];

        for (var c = 1; c <= ws.LastColumnUsed()?.ColumnNumber(); c++)
        {
            var headerText = ws.Cell(headerRow, c).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(headerText))
            {
                colMap[headerText] = c;
            }
        }

        int Col(string name) => colMap.TryGetValue(name, out var idx) ? idx : 0;
        string CellStr(IXLRow row, string colName) {
            var c = Col(colName);
            return c > 0 ? row.Cell(c).GetString().Trim() : string.Empty;
        }

        var rows = new List<WorkHoursReportRowDto>();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;

        for (var r = headerRow + 1; r <= lastRow; r++)
        {
            var xlRow = ws.Row(r);
            var opportunityNumber = CellStr(xlRow, "机会号");
            var hospitalName = CellStr(xlRow, "客户名称");

            // Skip empty rows
            if (string.IsNullOrWhiteSpace(opportunityNumber) && string.IsNullOrWhiteSpace(hospitalName))
            {
                continue;
            }

            // Skip summary/total rows
            if (hospitalName.Contains("合计") || hospitalName.Contains("总计"))
            {
                continue;
            }

            var manDaysCol = Col("工时（人天）");
            decimal manDays = 0;
            if (manDaysCol > 0)
            {
                var cellValue = xlRow.Cell(manDaysCol).GetString().Trim();
                decimal.TryParse(cellValue, NumberStyles.Any, CultureInfo.InvariantCulture, out manDays);
            }

            var countCol = Col("实施人员（个数）");
            int personnelCount = 0;
            if (countCol > 0)
            {
                var cellValue = xlRow.Cell(countCol).GetString().Trim();
                int.TryParse(cellValue, out personnelCount);
            }

            rows.Add(new WorkHoursReportRowDto
            {
                OpportunityNumber = opportunityNumber,
                HospitalName = hospitalName,
                ProductName = CellStr(xlRow, "产品名称"),
                ImplementationStatus = CellStr(xlRow, "实施状态"),
                WorkHoursManDays = Math.Round(manDays, 0, MidpointRounding.AwayFromZero),
                PersonnelCount = personnelCount,
                Personnel1 = CellStr(xlRow, "人员1"),
                Personnel2 = CellStr(xlRow, "人员2"),
                Personnel3 = CellStr(xlRow, "人员3"),
                Personnel4 = CellStr(xlRow, "人员4"),
                Personnel5 = CellStr(xlRow, "人员5"),
                MaintenanceStartDate = CellStr(xlRow, "维护开始时间"),
                MaintenanceEndDate = CellStr(xlRow, "维护结束时间"),
                AfterSalesProjectType = CellStr(xlRow, "售后项目类型"),
                Remarks = CellStr(xlRow, "备注")
            });
        }

        return rows;
    }

    private static string ResolveReportMonth(string? reportMonth)
    {
        if (!string.IsNullOrWhiteSpace(reportMonth)
            && DateTime.TryParseExact(reportMonth.Trim(), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed.ToString("yyyy-MM");
        }

        return DateTime.Today.ToString("yyyy-MM");
    }

    private sealed class AutoWorkHoursProjectRef
    {
        public string Key { get; set; } = string.Empty;
        public Domain.Entities.ProjectEntity Project { get; set; } = new();
    }

    private sealed class AutoWorkHoursRowRef
    {
        public string Key { get; set; } = string.Empty;
        public WorkHoursReportRowDto Row { get; set; } = new();
    }

    private static List<WorkHoursReportRowDto> BuildAutoWorkHoursRows(string reportMonth, IReadOnlyList<WorkHoursReportRowDto>? importedRows = null)
    {
        var monthDate = DateTime.ParseExact(reportMonth, "yyyy-MM", CultureInfo.InvariantCulture);
        var cycleStart = new DateTime(monthDate.Year, monthDate.Month, 1).AddMonths(-1).AddDays(25);
        var cycleEnd = new DateTime(monthDate.Year, monthDate.Month, 25);
        var totalManDaysPerPerson = CountWorkdaysExcludingWeekends(cycleStart, cycleEnd);

        if (importedRows is not null && importedRows.Count > 0)
        {
            return BuildAutoWorkHoursRowsFromImportedRows(reportMonth, totalManDaysPerPerson, importedRows);
        }

        var projects = InMemoryProjectDataStore.Projects;
        if (projects.Count == 0)
        {
            // 项目主数据为空时，回退到工时报表中最新月份作为固定项目模板进行当月重算。
            var latestTemplateRows = InMemoryWorkHoursReportStore.GetLatestMonthRows();
            if (latestTemplateRows.Count > 0)
            {
                return BuildAutoWorkHoursRowsFromImportedRows(reportMonth, totalManDaysPerPerson, latestTemplateRows);
            }
        }

        var projectByKey = new Dictionary<string, AutoWorkHoursProjectRef>(StringComparer.OrdinalIgnoreCase);
        var assignedPeopleByProject = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var projectsByPerson = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var project in projects)
        {
            var key = $"{project.OpportunityNumber}||{project.HospitalName}||{project.ProductName}";
            if (!projectByKey.ContainsKey(key))
            {
                projectByKey[key] = new AutoWorkHoursProjectRef { Key = key, Project = project };
            }

            var names = CollectPersonnelNames(project);
            if (!assignedPeopleByProject.TryGetValue(key, out var personSet))
            {
                personSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                assignedPeopleByProject[key] = personSet;
            }

            foreach (var name in names)
            {
                personSet.Add(name);
                if (!projectsByPerson.TryGetValue(name, out var personProjects))
                {
                    personProjects = new List<string>();
                    projectsByPerson[name] = personProjects;
                }

                if (!personProjects.Contains(key, StringComparer.OrdinalIgnoreCase))
                {
                    personProjects.Add(key);
                }
            }
        }

        var workHoursByProject = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in projectsByPerson)
        {
            var personName = pair.Key;
            var projectKeys = pair.Value;
            if (projectKeys.Count == 0)
            {
                continue;
            }

            var allocations = AllocateManDays(totalManDaysPerPerson, projectKeys.Count, reportMonth, personName);
            for (var i = 0; i < projectKeys.Count; i++)
            {
                var projectKey = projectKeys[i];
                var value = allocations[i];
                if (!workHoursByProject.TryGetValue(projectKey, out var existing))
                {
                    existing = 0m;
                }

                workHoursByProject[projectKey] = existing + value;
            }
        }

        var rows = new List<WorkHoursReportRowDto>();
        foreach (var pair in projectByKey)
        {
            var key = pair.Key;
            var projectRef = pair.Value;
            var people = assignedPeopleByProject.TryGetValue(key, out var personSet)
                ? personSet.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList()
                : new List<string>();

            rows.Add(new WorkHoursReportRowDto
            {
                OpportunityNumber = projectRef.Project.OpportunityNumber,
                HospitalName = projectRef.Project.HospitalName,
                ProductName = projectRef.Project.ProductName,
                ImplementationStatus = projectRef.Project.ImplementationStatus,
                WorkHoursManDays = Math.Round(workHoursByProject.TryGetValue(key, out var manDays) ? manDays : 0m, 0),
                PersonnelCount = people.Count,
                Personnel1 = people.ElementAtOrDefault(0) ?? string.Empty,
                Personnel2 = people.ElementAtOrDefault(1) ?? string.Empty,
                Personnel3 = people.ElementAtOrDefault(2) ?? string.Empty,
                Personnel4 = people.ElementAtOrDefault(3) ?? string.Empty,
                Personnel5 = people.ElementAtOrDefault(4) ?? string.Empty,
                MaintenanceStartDate = projectRef.Project.AfterSalesStartDate,
                MaintenanceEndDate = projectRef.Project.AfterSalesEndDate,
                AfterSalesProjectType = projectRef.Project.AfterSalesProjectType,
                Remarks = projectRef.Project.Remarks
            });
        }

        return rows
            .OrderBy(x => x.HospitalName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ProductName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<WorkHoursReportRowDto> BuildAutoWorkHoursRowsFromImportedRows(
        string reportMonth,
        int totalManDaysPerPerson,
        IReadOnlyList<WorkHoursReportRowDto> importedRows)
    {
        var rowByKey = new Dictionary<string, AutoWorkHoursRowRef>(StringComparer.OrdinalIgnoreCase);
        var assignedPeopleByProject = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var projectsByPerson = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var sourceRow in importedRows)
        {
            var key = $"{sourceRow.OpportunityNumber}||{sourceRow.HospitalName}||{sourceRow.ProductName}";
            if (!rowByKey.ContainsKey(key))
            {
                rowByKey[key] = new AutoWorkHoursRowRef
                {
                    Key = key,
                    Row = sourceRow
                };
            }

            var names = CollectPersonnelNames(sourceRow);
            if (!assignedPeopleByProject.TryGetValue(key, out var personSet))
            {
                personSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                assignedPeopleByProject[key] = personSet;
            }

            foreach (var name in names)
            {
                personSet.Add(name);
                if (!projectsByPerson.TryGetValue(name, out var personProjects))
                {
                    personProjects = new List<string>();
                    projectsByPerson[name] = personProjects;
                }

                if (!personProjects.Contains(key, StringComparer.OrdinalIgnoreCase))
                {
                    personProjects.Add(key);
                }
            }
        }

        var workHoursByProject = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in projectsByPerson)
        {
            var personName = pair.Key;
            var projectKeys = pair.Value;
            if (projectKeys.Count == 0)
            {
                continue;
            }

            var allocations = AllocateManDays(totalManDaysPerPerson, projectKeys.Count, reportMonth, personName);
            for (var i = 0; i < projectKeys.Count; i++)
            {
                var projectKey = projectKeys[i];
                var value = allocations[i];
                if (!workHoursByProject.TryGetValue(projectKey, out var existing))
                {
                    existing = 0m;
                }

                workHoursByProject[projectKey] = existing + value;
            }
        }

        var rows = new List<WorkHoursReportRowDto>();
        foreach (var pair in rowByKey)
        {
            var key = pair.Key;
            var rowRef = pair.Value;
            var source = rowRef.Row;
            var people = assignedPeopleByProject.TryGetValue(key, out var personSet)
                ? personSet.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList()
                : new List<string>();

            rows.Add(new WorkHoursReportRowDto
            {
                Id = source.Id,
                OpportunityNumber = source.OpportunityNumber,
                HospitalName = source.HospitalName,
                ProductName = source.ProductName,
                ImplementationStatus = source.ImplementationStatus,
                WorkHoursManDays = Math.Round(workHoursByProject.TryGetValue(key, out var manDays) ? manDays : 0m, 0),
                PersonnelCount = people.Count,
                Personnel1 = people.ElementAtOrDefault(0) ?? string.Empty,
                Personnel2 = people.ElementAtOrDefault(1) ?? string.Empty,
                Personnel3 = people.ElementAtOrDefault(2) ?? string.Empty,
                Personnel4 = people.ElementAtOrDefault(3) ?? string.Empty,
                Personnel5 = people.ElementAtOrDefault(4) ?? string.Empty,
                MaintenanceStartDate = source.MaintenanceStartDate,
                MaintenanceEndDate = source.MaintenanceEndDate,
                AfterSalesProjectType = source.AfterSalesProjectType,
                Remarks = source.Remarks
            });
        }

        return rows
            .OrderBy(x => x.HospitalName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ProductName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int CountWorkdaysExcludingWeekends(DateTime startInclusive, DateTime endInclusive)
    {
        var count = 0;
        var cursor = startInclusive.Date;
        var end = endInclusive.Date;
        while (cursor <= end)
        {
            if (cursor.DayOfWeek != DayOfWeek.Saturday && cursor.DayOfWeek != DayOfWeek.Sunday)
            {
                count++;
            }

            cursor = cursor.AddDays(1);
        }

        return Math.Max(0, count);
    }

    private static List<decimal> AllocateManDays(int totalManDays, int projectCount, string reportMonth, string personName)
    {
        var total = Math.Max(0, totalManDays);
        if (projectCount <= 1)
        {
            return [total];
        }

        var random = CreateDeterministicRandom($"{reportMonth}|{personName}");
        var weights = new List<double>(projectCount);
        for (var i = 0; i < projectCount; i++)
        {
            weights.Add(0.2 + random.NextDouble());
        }

        var weightSum = weights.Sum();
        var floorAllocations = new int[projectCount];
        var fractions = new List<(int index, double fraction)>(projectCount);
        var allocated = 0;

        for (var i = 0; i < projectCount; i++)
        {
            var raw = (weights[i] / weightSum) * total;
            var floorValue = (int)Math.Floor(raw);
            floorAllocations[i] = floorValue;
            allocated += floorValue;
            fractions.Add((i, raw - floorValue));
        }

        var remainder = total - allocated;
        if (remainder > 0)
        {
            foreach (var item in fractions
                .OrderByDescending(x => x.fraction)
                .ThenBy(x => x.index))
            {
                if (remainder <= 0)
                {
                    break;
                }

                floorAllocations[item.index] += 1;
                remainder--;
            }
        }

        return floorAllocations.Select(x => (decimal)x).ToList();
    }

    private static Random CreateDeterministicRandom(string seed)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        var intSeed = BitConverter.ToInt32(bytes, 0);
        return new Random(intSeed);
    }

    private static List<string> CollectPersonnelNames(Domain.Entities.ProjectEntity project)
    {
        var sourceValues = new[]
        {
            project.MaintenancePersonName,
            project.Personnel1,
            project.Personnel2,
            project.Personnel3,
            project.Personnel4,
            project.Personnel5
        };

        var result = new List<string>();
        foreach (var source in sourceValues)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                continue;
            }

            var tokens = source
                .Split(PersonnelSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x));

            foreach (var token in tokens)
            {
                if (!result.Contains(token, StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(token);
                }
            }
        }

        return result;
    }

    private static List<string> CollectPersonnelNames(WorkHoursReportRowDto row)
    {
        var sourceValues = new[]
        {
            row.Personnel1,
            row.Personnel2,
            row.Personnel3,
            row.Personnel4,
            row.Personnel5
        };

        var result = new List<string>();
        foreach (var source in sourceValues)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                continue;
            }

            var tokens = source
                .Split(PersonnelSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x));

            foreach (var token in tokens)
            {
                if (!result.Contains(token, StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(token);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 月报自动生成 — 从各模块聚合指定月份数据生成月报
    /// </summary>
    [HttpPost("monthly/generate")]
    public async Task<IActionResult> GenerateMonthlyReport(
        [FromBody] MonthlyReportGenerateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseReportMonth(request.ReportMonth, out var monthStart))
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = "reportMonth 格式错误，应为 yyyy-MM",
                Data = null
            });
        }

        var computed = await BuildMonthlyReportSourceAsync(
            request.ReportMonth,
            request.GroupName,
            request.SupervisorName,
            monthStart,
            request.TeamDataSource,
            cancellationToken);

        var groupLabel = !string.IsNullOrWhiteSpace(request.SupervisorName)
            ? $"{request.SupervisorName.Trim()}组"
            : request.GroupName;
        var created = await monthlyReportService.CreateAsync(
            NormalizeSubmittedBy(request.SubmittedBy, groupLabel),
            computed.UpsertDto,
            cancellationToken);

        return Ok(ApiResponse<MonthlyReportItemDto>.Success(created));
    }

    /// <summary>
    /// 月报数据来源预览 — 按月份与组别（或主管）预览人员、项目、驻场扣减和主管归属。
    /// 优先使用 supervisorName；若未提供则回退到 groupName。
    /// </summary>
    [HttpGet("monthly/source-preview")]
    public async Task<IActionResult> GetMonthlyReportSourcePreview(
        [FromQuery] string? reportMonth,
        [FromQuery] string? groupName,
        [FromQuery] string? supervisorName,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseReportMonth(reportMonth ?? string.Empty, out var monthStart))
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = "reportMonth 格式错误，应为 yyyy-MM",
                Data = null
            });
        }

        if (string.IsNullOrWhiteSpace(supervisorName) && string.IsNullOrWhiteSpace(groupName))
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = "supervisorName 或 groupName 至少提供一个",
                Data = null
            });
        }

        var computed = await BuildMonthlyReportSourceAsync(
            reportMonth ?? string.Empty,
            groupName,
            supervisorName,
            monthStart,
            null,
            cancellationToken);

        return Ok(ApiResponse<MonthlyReportSourcePreviewDto>.Success(computed.PreviewDto));
    }

    /// <summary>
    /// 导出月报（CSV）。若当月当组无数据且提供 submittedBy，则自动生成后导出。
    /// </summary>
    [HttpGet("monthly/export")]
    public async Task<IActionResult> ExportMonthlyReport(
        [FromQuery] string? reportMonth,
        [FromQuery] string? groupName,
        [FromQuery] string? supervisorName,
        [FromQuery] string? submittedBy,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseReportMonth(reportMonth ?? string.Empty, out var monthStart))
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = "reportMonth 格式错误，应为 yyyy-MM",
                Data = null
            });
        }

        if (string.IsNullOrWhiteSpace(supervisorName) && string.IsNullOrWhiteSpace(groupName))
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = "supervisorName 或 groupName 至少提供一个",
                Data = null
            });
        }

        var groupLabel = !string.IsNullOrWhiteSpace(supervisorName)
            ? $"{supervisorName.Trim()}组"
            : groupName ?? string.Empty;

        var reportRows = await monthlyReportService.QueryAsync(new MonthlyReportQuery
        {
            ReportMonth = reportMonth ?? string.Empty,
            GroupName = groupLabel,
            Page = 1,
            Size = 5000
        }, cancellationToken);

        var items = reportRows.Items.ToList();
        if (items.Count == 0)
        {
            var computed = await BuildMonthlyReportSourceAsync(
                reportMonth ?? string.Empty,
                groupName,
                supervisorName,
                monthStart,
                null,
                cancellationToken);
            var created = await monthlyReportService.CreateAsync(
                NormalizeSubmittedBy(submittedBy, groupLabel),
                computed.UpsertDto,
                cancellationToken);
            items.Add(created);
        }

        var csv = BuildMonthlyReportCsv(items);
        var fileName = $"monthly_report_{reportMonth}_{groupLabel}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", fileName);
    }

    private static byte[] BuildWorkHoursExcel(IReadOnlyList<WorkHoursReportRowDto> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("工时报表");

        // Headers matching Excel exactly
        string[] headers = ["机会号", "客户名称", "产品名称", "实施状态", "工时（人天）", "实施人员（个数）",
            "人员1", "人员2", "人员3", "人员4", "人员5", "维护开始时间", "维护结束时间", "售后项目类型", "备注"];

        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
        }

        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var rowNum = r + 2;
            ws.Cell(rowNum, 1).Value = row.OpportunityNumber;
            ws.Cell(rowNum, 2).Value = row.HospitalName;
            ws.Cell(rowNum, 3).Value = row.ProductName;
            ws.Cell(rowNum, 4).Value = row.ImplementationStatus;
            ws.Cell(rowNum, 5).Value = (double)row.WorkHoursManDays;
            ws.Cell(rowNum, 6).Value = row.PersonnelCount;
            ws.Cell(rowNum, 7).Value = row.Personnel1;
            ws.Cell(rowNum, 8).Value = row.Personnel2;
            ws.Cell(rowNum, 9).Value = row.Personnel3;
            ws.Cell(rowNum, 10).Value = row.Personnel4;
            ws.Cell(rowNum, 11).Value = row.Personnel5;
            ws.Cell(rowNum, 12).Value = NormalizeDateForExport(row.MaintenanceStartDate);
            ws.Cell(rowNum, 13).Value = NormalizeDateForExport(row.MaintenanceEndDate);
            ws.Cell(rowNum, 14).Value = row.AfterSalesProjectType;
            ws.Cell(rowNum, 15).Value = row.Remarks;
        }

        // "合计" summary row
        var totalManDays = rows.Sum(r => r.WorkHoursManDays);
        var summaryRow1 = rows.Count + 2;
        ws.Cell(summaryRow1, 1).Value = "合计";
        ws.Cell(summaryRow1, 1).Style.Font.Bold = true;
        ws.Cell(summaryRow1, 5).Value = (double)totalManDays;
        ws.Cell(summaryRow1, 5).Style.Font.Bold = true;

        // "事业部实施人员考勤工时总计" summary row
        var summaryRow2 = summaryRow1 + 1;
        ws.Cell(summaryRow2, 1).Value = "事业部实施人员考勤工时总计";
        ws.Cell(summaryRow2, 1).Style.Font.Bold = true;
        ws.Cell(summaryRow2, 5).Value = (double)totalManDays;
        ws.Cell(summaryRow2, 5).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
        // Enforce a reasonable minimum width so narrow columns remain readable
        foreach (var col in ws.ColumnsUsed())
        {
            if (col.Width < 10)
                col.Width = 10;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static string NormalizeDateForExport(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var text = input.Trim();
        if (DateTime.TryParse(text, out var parsed))
        {
            return parsed.ToString("yyyy-MM-dd");
        }

        return text;
    }

    private async Task<MonthlyReportSourceComputationResult> BuildMonthlyReportSourceAsync(
        string reportMonth,
        string? groupName,
        string? supervisorNameParam,
        DateTime monthStart,
        MonthlyReportTeamDataSourceInput? sourceInput,
        CancellationToken cancellationToken)
    {
        var monthEnd = monthStart.AddMonths(1);
        var nextMonthStart = monthEnd;
        var nextMonthEnd = nextMonthStart.AddMonths(1);

        // 获取全部权限用户（supervisor 模式和 groupName 模式都需要）
        var accessResult = await accessControlService.QueryUsersAsync(new PersonnelAccessQuery
        {
            Page = 1,
            Size = 5000
        }, cancellationToken);
        var accessById = accessResult.Items.ToDictionary(x => x.PersonnelId);

        IReadOnlyList<PersonnelItemDto> personnelItems;
        string supervisorName;
        string groupLabel;

        if (!string.IsNullOrWhiteSpace(supervisorNameParam))
        {
            // ── 按主管姓名筛选 ──
            var normalizedSupervisor = supervisorNameParam.Trim();

            // 找到主管对应的 personnelId
            var supervisorAccess = accessResult.Items.FirstOrDefault(a =>
                string.Equals(a.PersonnelName, normalizedSupervisor, StringComparison.OrdinalIgnoreCase)
                && (string.Equals(a.SystemRole, "supervisor", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(a.SystemRole, "regional_manager", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(a.SystemRole, "manager", StringComparison.OrdinalIgnoreCase)));

            HashSet<int> subordinateIds;
            if (supervisorAccess != null)
            {
                subordinateIds = accessResult.Items
                    .Where(a => a.SupervisorId == supervisorAccess.PersonnelId)
                    .Select(a => a.PersonnelId)
                    .ToHashSet();
                subordinateIds.Add(supervisorAccess.PersonnelId);
            }
            else
            {
                // Fallback: 按 supervisorName 字段匹配
                subordinateIds = accessResult.Items
                    .Where(a => string.Equals(a.SupervisorName, normalizedSupervisor, StringComparison.OrdinalIgnoreCase))
                    .Select(a => a.PersonnelId)
                    .ToHashSet();
            }

            var allPersonnel = await personnelService.QueryAsync(new PersonnelQuery
            {
                Page = 1,
                Size = 5000
            }, cancellationToken);
            personnelItems = allPersonnel.Items.Where(p => subordinateIds.Contains(p.Id)).ToList();
            supervisorName = normalizedSupervisor;
            groupLabel = $"{normalizedSupervisor}组";
        }
        else
        {
            // ── 按 groupName 筛选（兼容旧逻辑）──
            var personnelResult = await personnelService.QueryAsync(new PersonnelQuery
            {
                GroupName = groupName,
                Page = 1,
                Size = 5000
            }, cancellationToken);
            personnelItems = personnelResult.Items;
            supervisorName = ResolveGroupSupervisorName(personnelItems, accessById);
            groupLabel = (groupName ?? string.Empty).Trim();
        }

        // 项目匹配：supervisor 模式按人员姓名匹配，groupName 模式按组名匹配
        var personnelNameSetForProjectMatch = personnelItems
            .Select(x => x.Name?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var projectRows = InMemoryProjectDataStore.Projects;
        List<Domain.Entities.ProjectEntity> groupProjects;

        if (!string.IsNullOrWhiteSpace(supervisorNameParam))
        {
            // supervisor 模式：按人员姓名在项目的运维负责人/人员1-5中匹配
            groupProjects = projectRows
                .Where(p => personnelNameSetForProjectMatch
                    .Any(name => IsPersonnelAssigned(p, name!)))
                .ToList();
        }
        else
        {
            // groupName 模式：按组名匹配（兼容旧逻辑）
            var relevantGroupNames = personnelItems
                .Select(p => (p.GroupName ?? string.Empty).Trim())
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            groupProjects = projectRows
                .Where(p => relevantGroupNames.Count > 0
                    ? relevantGroupNames.Contains((p.GroupName ?? string.Empty).Trim())
                    : true)
                .ToList();
        }

        var groupProjectSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in groupProjects)
        {
            var key = $"{project.HospitalName}||{project.ProductName}";
            groupProjectSet.Add(key);
        }

        var personnelScopes = personnelItems
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(person =>
            {
                accessById.TryGetValue(person.Id, out var accessItem);
                var assignedProjects = GetAssignedProjectsForPersonnel(groupProjects, person.Name);
                var hospitalNames = assignedProjects
                    .Select(x => x.HospitalName?.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Cast<string>()
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var productNames = assignedProjects
                    .Select(x => FormatProductScope(x.HospitalName, x.ProductName))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var resolvedSupervisorName = ResolvePersonnelSupervisorName(person, accessItem, supervisorName);

                return new
                {
                    Person = person,
                    PreviewItem = new MonthlyReportSourcePersonnelItemDto
                    {
                        PersonnelId = person.Id,
                        Name = person.Name,
                        Department = person.Department,
                        GroupName = person.GroupName,
                        RoleType = person.RoleType,
                        SystemRole = accessItem?.SystemRole ?? string.Empty,
                        ServiceMode = person.IsOnsite ? "驻场" : "远程/标服",
                        IsOnsite = person.IsOnsite,
                        SupervisorName = resolvedSupervisorName,
                        ResponsibilityHospitalCount = hospitalNames.Count,
                        ResponsibilityProductCount = productNames.Count,
                        HospitalNames = hospitalNames,
                        ProductNames = productNames,
                        MatchingStatus = hospitalNames.Count > 0 || productNames.Count > 0 ? "已匹配" : "未匹配项目"
                    }
                };
            })
            .ToList();

        var teamTotal = personnelItems.Count;
        var onsitePersonnelCount = personnelItems.Count(x => x.IsOnsite);
        var onsiteList = personnelItems
            .Where(x => x.IsOnsite)
            .GroupBy(x => ResolveRegionLabel(x.Department))
            .Select(g => new { region = g.Key, count = g.Count() })
            .ToList();

        var centralStdAuto = personnelItems.Count(x =>
            ContainsText(x.Department, "中")
            && !x.IsOnsite);
        var centralOnsiteAuto = personnelItems.Count(x =>
            ContainsText(x.Department, "中")
            && x.IsOnsite);
        var northwestStdAuto = personnelItems.Count(x =>
            (ContainsText(x.Department, "西北") || ContainsText(x.Department, "新疆"))
            && !x.IsOnsite);
        var northwestOnsiteAuto = personnelItems.Count(x =>
            (ContainsText(x.Department, "西北") || ContainsText(x.Department, "新疆"))
            && x.IsOnsite);

        var authorizedHeadcount = PositiveOrDefault(sourceInput?.AuthorizedHeadcount, teamTotal);
        var centralStandardServiceCount = PositiveOrDefault(sourceInput?.CentralStandardServiceCount, centralStdAuto);
        var centralOnsiteCount = PositiveOrDefault(sourceInput?.CentralOnsiteCount, centralOnsiteAuto);
        var northwestStandardServiceCount = PositiveOrDefault(sourceInput?.NorthwestStandardServiceCount, northwestStdAuto);
        var northwestOnsiteCount = PositiveOrDefault(sourceInput?.NorthwestOnsiteCount, northwestOnsiteAuto);

        var personnelNameSet = personnelNameSetForProjectMatch;

        var specialWorkHours = InMemoryWorkHoursService.GetSnapshot()
            .Where(x => personnelNameSet.Contains(x.PersonnelName))
            .Where(x => DateTime.TryParse(x.WorkDate, out var workDate)
                     && workDate.Date >= monthStart
                     && workDate.Date < monthEnd)
            .ToList();

        var autoSickLeaveCount = specialWorkHours
            .Where(x => string.Equals(x.WorkType, "病假", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.PersonnelName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var autoPersonalLeaveCount = specialWorkHours
            .Where(x => string.Equals(x.WorkType, "事假", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.PersonnelName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var autoOtherSpecialCount = specialWorkHours
            .Where(x => string.Equals(x.WorkType, "其他特殊", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.PersonnelName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var sickLeaveCount = PositiveOrDefault(sourceInput?.SickLeaveCount, autoSickLeaveCount);
        var personalLeaveCount = PositiveOrDefault(sourceInput?.PersonalLeaveCount, autoPersonalLeaveCount);
        var otherSpecialCount = PositiveOrDefault(sourceInput?.OtherSpecialCount, autoOtherSpecialCount);
        var otherSpecialRemark = sourceInput?.OtherSpecialRemark?.Trim() ?? string.Empty;

        var handoverResult = await handoverService.QueryAsync(new HandoverQuery
        {
            FromGroup = !string.IsNullOrWhiteSpace(supervisorNameParam) ? null : groupName,
            Page = 1,
            Size = 5000
        }, cancellationToken);

        var handoverItems = handoverResult.Items
            .Where(h => h.EmailSentDate.HasValue
                     && h.EmailSentDate.Value.Date >= monthStart
                     && h.EmailSentDate.Value.Date < monthEnd)
            .Where(h => string.IsNullOrWhiteSpace(supervisorNameParam)
                     || personnelNameSet.Contains((h.FromOwner ?? string.Empty).Trim()))
            .Select(h => new
            {
                hospitalName = h.HospitalName,
                productName = h.ProductName,
                fromPerson = h.FromOwner,
                toPerson = h.ToOwner,
                stage = h.Stage
            })
            .ToList();

        var inspectionResult = await inspectionService.QueryAsync(new InspectionQuery
        {
            GroupName = !string.IsNullOrWhiteSpace(supervisorNameParam) ? null : groupName,
            Page = 1,
            Size = 5000
        }, cancellationToken);

        // 在 supervisor 模式下，巡检未按组筛查，需按本组项目集合过滤
        var filteredInspectionItems = !string.IsNullOrWhiteSpace(supervisorNameParam)
            ? inspectionResult.Items.Where(i => groupProjectSet.Contains($"{i.HospitalName}||{i.ProductName}"))
            : inspectionResult.Items.AsEnumerable();

        var inspectionInMonth = filteredInspectionItems
            .Where(i => i.PlanDate.Date >= monthStart && i.PlanDate.Date < monthEnd)
            .ToList();

        var inspectionRecords = inspectionInMonth
            .Select(i => new
            {
                hospitalName = i.HospitalName,
                productName = i.ProductName,
                plannedDate = i.PlanDate.ToString("yyyy-MM-dd"),
                status = i.Status
            })
            .ToList();

        var inspectionTotal = inspectionRecords.Count;
        var inspectionCompleted = inspectionRecords.Count(x =>
            string.Equals(x.status, "已完成", StringComparison.OrdinalIgnoreCase));

        var repairResult = await repairRecordService.QueryAsync(new RepairRecordQuery
        {
            Page = 1,
            Size = 5000
        }, cancellationToken);

        var repairInMonthAndGroup = repairResult.Items
            .Where(r => r.ReportedAt.HasValue
                     && r.ReportedAt.Value.Date >= monthStart
                     && r.ReportedAt.Value.Date < monthEnd
                     && groupProjectSet.Contains($"{r.HospitalName}||{r.ProductName}"))
            .ToList();

        var incidents = repairInMonthAndGroup
            .Where(r => string.Equals(r.Severity, "紧急", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(r.Severity, "严重", StringComparison.OrdinalIgnoreCase))
            .Select(r => new
            {
                hospitalName = r.HospitalName,
                description = r.Description,
                date = r.ReportedAt?.ToString("yyyy-MM-dd") ?? string.Empty,
                resolution = r.Resolution
            })
            .ToList();

        var repairTotal = repairInMonthAndGroup.Count;
        var repairResolved = repairInMonthAndGroup.Count(r =>
            string.Equals(r.Status, "已完成", StringComparison.OrdinalIgnoreCase)
            || string.Equals(r.Status, "已关闭", StringComparison.OrdinalIgnoreCase));

        var nextMonthInspectionPlan = filteredInspectionItems
            .Where(i => i.PlanDate.Date >= nextMonthStart && i.PlanDate.Date < nextMonthEnd)
            .Where(i => !string.Equals(i.Status, "已完成", StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(i.Status, "已取消", StringComparison.OrdinalIgnoreCase))
            .Select(i => new
            {
                hospitalName = i.HospitalName,
                productName = i.ProductName,
                plannedDate = i.PlanDate.ToString("yyyy-MM-dd")
            })
            .ToList();

        Func<object, string> json = obj => System.Text.Json.JsonSerializer.Serialize(obj);

        var weeklyRate = inspectionTotal > 0 ? (decimal)inspectionCompleted / inspectionTotal : 0m;
        var monthlyRate = repairTotal > 0 ? (decimal)repairResolved / repairTotal : 0m;

        var totalCustomers = groupProjects
            .Select(x => x.HospitalName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var totalProducts = groupProjects
            .Select(x => $"{x.HospitalName}||{x.ProductName}")
            .Where(x => !string.IsNullOrWhiteSpace(x) && !string.Equals(x, "||", StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var centralProjects = groupProjects.Where(x => ContainsText(x.Province, "北京") || ContainsText(x.Province, "天津") || ContainsText(x.Province, "河北") || ContainsText(x.GroupName, "中")).ToList();
        var northwestProjects = groupProjects.Where(x => ContainsText(x.Province, "陕西") || ContainsText(x.Province, "甘肃") || ContainsText(x.Province, "青海") || ContainsText(x.Province, "宁夏") || ContainsText(x.Province, "新疆") || ContainsText(x.GroupName, "西北") || ContainsText(x.GroupName, "新疆")).ToList();

        var centralCustomerCount = centralProjects
            .Select(x => x.HospitalName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var centralProductCount = centralProjects
            .Select(x => $"{x.HospitalName}||{x.ProductName}")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var northwestCustomerCount = northwestProjects
            .Select(x => x.HospitalName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var northwestProductCount = northwestProjects
            .Select(x => $"{x.HospitalName}||{x.ProductName}")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var onsiteDeductionItems = personnelScopes
            .Where(x => x.Person.IsOnsite)
            .Select(x => new MonthlyReportSourceOnsiteDeductionDto
            {
                PersonnelId = x.PreviewItem.PersonnelId,
                Name = x.PreviewItem.Name,
                DeductedHospitalCount = x.PreviewItem.ResponsibilityHospitalCount,
                DeductedProductCount = x.PreviewItem.ResponsibilityProductCount,
                HospitalNames = x.PreviewItem.HospitalNames,
                ProductNames = x.PreviewItem.ProductNames
            })
            .ToList();

        var excludedCustomerCount = onsiteDeductionItems
            .SelectMany(x => x.HospitalNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var excludedProjectCount = onsiteDeductionItems
            .SelectMany(x => x.ProductNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var specialTotal = sickLeaveCount + personalLeaveCount + otherSpecialCount;
        var onsiteTotal = onsitePersonnelCount;

        var headcountExcludeOnsite = Math.Max(1, authorizedHeadcount - onsiteTotal - specialTotal);
        var headcountIncludeOnsite = Math.Max(1, authorizedHeadcount - specialTotal);
        var customersExcludeOnsite = Math.Max(0, totalCustomers - excludedCustomerCount);
        var productsExcludeOnsite = Math.Max(0, totalProducts - excludedProjectCount);
        var unmatchedPersonnelCount = personnelScopes.Count(x => x.PreviewItem.ResponsibilityHospitalCount == 0 && x.PreviewItem.ResponsibilityProductCount == 0);

        var teamSummary = new
        {
            supervisorName,
            authorizedHeadcount,
            totalHeadcount = teamTotal,
            onsiteCount = onsitePersonnelCount,
            remoteCount = Math.Max(0, teamTotal - onsitePersonnelCount),
            centralStandardServiceCount,
            centralOnsiteCount,
            northwestStandardServiceCount,
            northwestOnsiteCount,
            unmatchedPersonnelCount,
            special = new
            {
                sickLeaveCount,
                personalLeaveCount,
                otherSpecialCount,
                otherSpecialRemark
            }
        };

        var projectOverview = new
        {
            totalCustomers,
            totalProducts,
            central = new
            {
                customerCount = centralCustomerCount,
                productCount = centralProductCount
            },
            northwest = new
            {
                customerCount = northwestCustomerCount,
                productCount = northwestProductCount
            },
            onsiteDeduction = new
            {
                customerCount = excludedCustomerCount,
                productCount = excludedProjectCount,
                personnelCount = onsiteDeductionItems.Count
            }
        };

        var perCapitaMetrics = new
        {
            allPersonnelAverage = new
            {
                headcount = headcountIncludeOnsite,
                customerCount = totalCustomers,
                productCount = totalProducts,
                customerPerPerson = Math.Round((double)totalCustomers / headcountIncludeOnsite, 1),
                productPerPerson = Math.Round((double)totalProducts / headcountIncludeOnsite, 1)
            },
            excludeOnsiteAverage = new
            {
                headcount = headcountExcludeOnsite,
                customerCount = customersExcludeOnsite,
                productCount = productsExcludeOnsite,
                customerPerPerson = Math.Round((double)customersExcludeOnsite / headcountExcludeOnsite, 1),
                productPerPerson = Math.Round((double)productsExcludeOnsite / headcountExcludeOnsite, 1),
                deductedHeadcount = onsiteTotal,
                deductedCustomerCount = excludedCustomerCount,
                deductedProductCount = excludedProjectCount
            }
        };

        var previewDto = new MonthlyReportSourcePreviewDto
        {
            ReportMonth = reportMonth ?? string.Empty,
            GroupName = groupName ?? string.Empty,
            SupervisorName = supervisorName ?? string.Empty,
            TeamSummary = new MonthlyReportSourceTeamSummaryDto
            {
                AuthorizedHeadcount = authorizedHeadcount,
                TotalHeadcount = teamTotal,
                OnsiteCount = onsitePersonnelCount,
                RemoteCount = Math.Max(0, teamTotal - onsitePersonnelCount),
                CentralStandardServiceCount = centralStandardServiceCount,
                CentralOnsiteCount = centralOnsiteCount,
                NorthwestStandardServiceCount = northwestStandardServiceCount,
                NorthwestOnsiteCount = northwestOnsiteCount,
                UnmatchedPersonnelCount = unmatchedPersonnelCount,
                SickLeaveCount = sickLeaveCount,
                PersonalLeaveCount = personalLeaveCount,
                OtherSpecialCount = otherSpecialCount,
                OtherSpecialRemark = otherSpecialRemark
            },
            ProjectSummary = new MonthlyReportSourceProjectSummaryDto
            {
                TotalCustomerCount = totalCustomers,
                TotalProductCount = totalProducts,
                CentralCustomerCount = centralCustomerCount,
                CentralProductCount = centralProductCount,
                NorthwestCustomerCount = northwestCustomerCount,
                NorthwestProductCount = northwestProductCount,
                OnsiteDeductedCustomerCount = excludedCustomerCount,
                OnsiteDeductedProductCount = excludedProjectCount
            },
            PerCapitaMetrics = new MonthlyReportSourcePerCapitaDto
            {
                AllPersonnelAverage = new MonthlyReportSourceMetricBlockDto
                {
                    Headcount = headcountIncludeOnsite,
                    CustomerCount = totalCustomers,
                    ProductCount = totalProducts,
                    CustomerPerPerson = Math.Round((double)totalCustomers / headcountIncludeOnsite, 1),
                    ProductPerPerson = Math.Round((double)totalProducts / headcountIncludeOnsite, 1)
                },
                ExcludeOnsiteAverage = new MonthlyReportSourceMetricBlockDto
                {
                    Headcount = headcountExcludeOnsite,
                    CustomerCount = customersExcludeOnsite,
                    ProductCount = productsExcludeOnsite,
                    CustomerPerPerson = Math.Round((double)customersExcludeOnsite / headcountExcludeOnsite, 1),
                    ProductPerPerson = Math.Round((double)productsExcludeOnsite / headcountExcludeOnsite, 1)
                }
            },
            PersonnelItems = personnelScopes.Select(x => x.PreviewItem).ToList(),
            OnsiteDeductionItems = onsiteDeductionItems,
            Warnings = BuildPreviewWarnings(supervisorName ?? string.Empty, teamTotal, groupProjects.Count, personnelScopes.Select(x => x.PreviewItem).ToList(), sickLeaveCount, personalLeaveCount, otherSpecialCount)
        };

        return new MonthlyReportSourceComputationResult
        {
            PreviewDto = previewDto,
            UpsertDto = new MonthlyReportUpsertDto
            {
                HospitalName = string.Empty,
                ReportMonth = reportMonth ?? string.Empty,
                GroupName = groupLabel,
                Title = $"{groupLabel} {reportMonth ?? string.Empty} 月报",
                Content = "系统自动生成",
                TeamTotal = authorizedHeadcount,
                TeamOnsiteJson = json(onsiteList),
                TeamSummaryJson = JsonSerializer.Serialize(teamSummary),
                ProjectOverviewJson = JsonSerializer.Serialize(projectOverview),
                PerCapitaMetricsJson = JsonSerializer.Serialize(perCapitaMetrics),
                HandoverItemsJson = json(handoverItems),
                WeeklyReportRate = weeklyRate,
                MonthlyReportRate = monthlyRate,
                MajorDemandAcceptanceJson = "[]",
                InspectionRecordsJson = json(inspectionRecords),
                AnnualServiceReportsJson = "[]",
                IncidentsJson = json(incidents),
                NextMonthInspectionPlanJson = json(nextMonthInspectionPlan),
                NextMonthAnnualReportPlanJson = "[]",
                NextMonthOtherPlanJson = "[]",
                Status = "draft"
            }
        };
    }

    private static bool TryParseReportMonth(string reportMonth, out DateTime monthStart)
    {
        monthStart = default;
        if (string.IsNullOrWhiteSpace(reportMonth))
        {
            return false;
        }

        if (!DateTime.TryParse($"{reportMonth.Trim()}-01", out var parsed))
        {
            return false;
        }

        monthStart = new DateTime(parsed.Year, parsed.Month, 1);
        return true;
    }

    private static string NormalizeSubmittedBy(string? submittedBy, string groupName)
    {
        if (!string.IsNullOrWhiteSpace(submittedBy))
        {
            return submittedBy.Trim();
        }

        return string.IsNullOrWhiteSpace(groupName)
            ? "系统自动生成"
            : $"{groupName.Trim()}月报自动生成";
    }

    private static string BuildMonthlyReportCsv(IReadOnlyList<MonthlyReportItemDto> rows)
    {
        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("ID,月份,组别,提交人,标题,团队总人数,团队结构(JSON),项目情况(JSON),人均核算(JSON),周报提交率,月报提交率,驻场人员(JSON),交接事项(JSON),巡检记录(JSON),事件报修(JSON),下月巡检计划(JSON),状态,创建时间");
        foreach (var row in rows.OrderByDescending(x => x.CreatedAt))
        {
            var cols = new[]
            {
                row.Id.ToString(), Csv(row.ReportMonth), Csv(row.GroupName), Csv(row.SubmittedBy), Csv(row.Title),
                row.TeamTotal.ToString(), Csv(row.TeamSummaryJson), Csv(row.ProjectOverviewJson), Csv(row.PerCapitaMetricsJson), row.WeeklyReportRate.ToString("0.####"), row.MonthlyReportRate.ToString("0.####"),
                Csv(row.TeamOnsiteJson), Csv(row.HandoverItemsJson), Csv(row.InspectionRecordsJson), Csv(row.IncidentsJson),
                Csv(row.NextMonthInspectionPlanJson), Csv(row.Status), Csv(row.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"))
            };
            sb.AppendLine(string.Join(',', cols));
        }

        return sb.ToString();
    }

    private static string Csv(string? value)
    {
        var normalized = value ?? string.Empty;
        var escaped = normalized.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static int NonNegative(int? value)
    {
        if (!value.HasValue)
        {
            return 0;
        }

        return Math.Max(0, value.Value);
    }

    private static int PositiveOrDefault(int? value, int defaultValue)
    {
        if (value.HasValue && value.Value > 0)
        {
            return value.Value;
        }

        return Math.Max(0, defaultValue);
    }

    private static bool ContainsText(string? source, string target)
    {
        return !string.IsNullOrWhiteSpace(source)
            && source.Contains(target, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveGroupSupervisorName(
        IReadOnlyCollection<PersonnelItemDto> personnelItems,
        IReadOnlyDictionary<int, PersonnelAccessItemDto> accessById)
    {
        foreach (var person in personnelItems)
        {
            if (!accessById.TryGetValue(person.Id, out var accessItem))
            {
                continue;
            }

            if (string.Equals(accessItem.SystemRole, "supervisor", StringComparison.OrdinalIgnoreCase)
                || string.Equals(accessItem.SystemRole, "manager", StringComparison.OrdinalIgnoreCase))
            {
                return person.Name?.Trim() ?? string.Empty;
            }
        }

        return personnelItems
            .Select(person => accessById.TryGetValue(person.Id, out var accessItem) ? accessItem.SupervisorName?.Trim() ?? string.Empty : string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name!, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault() ?? string.Empty;
    }

    private static string ResolvePersonnelSupervisorName(
        PersonnelItemDto person,
        PersonnelAccessItemDto? accessItem,
        string groupSupervisorName)
    {
        if (accessItem is not null
            && (string.Equals(accessItem.SystemRole, "supervisor", StringComparison.OrdinalIgnoreCase)
                || string.Equals(accessItem.SystemRole, "manager", StringComparison.OrdinalIgnoreCase)))
        {
            return person.Name?.Trim() ?? string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(accessItem?.SupervisorName))
        {
            return accessItem!.SupervisorName!.Trim();
        }

        return groupSupervisorName;
    }

    private static List<Domain.Entities.ProjectEntity> GetAssignedProjectsForPersonnel(
        IEnumerable<Domain.Entities.ProjectEntity> projects,
        string personnelName)
    {
        return projects
            .Where(project => IsPersonnelAssigned(project, personnelName))
            .ToList();
    }

    private static bool IsPersonnelAssigned(Domain.Entities.ProjectEntity project, string personnelName)
    {
        if (string.IsNullOrWhiteSpace(personnelName))
        {
            return false;
        }

        return ContainsPersonnelName(project.MaintenancePersonName, personnelName)
            || ContainsPersonnelName(project.Personnel1, personnelName)
            || ContainsPersonnelName(project.Personnel2, personnelName)
            || ContainsPersonnelName(project.Personnel3, personnelName)
            || ContainsPersonnelName(project.Personnel4, personnelName)
            || ContainsPersonnelName(project.Personnel5, personnelName);
    }

    private static bool ContainsPersonnelName(string? source, string personnelName)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(personnelName))
        {
            return false;
        }

        var normalizedName = personnelName.Trim();
        var tokens = source
            .Split(PersonnelSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.Trim())
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .ToList();

        if (tokens.Any(token => string.Equals(token, normalizedName, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return source.Contains(normalizedName, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveRegionLabel(string? department)
    {
        if (ContainsText(department, "西北") || ContainsText(department, "新疆"))
        {
            return "西北区";
        }

        if (ContainsText(department, "中"))
        {
            return "中区";
        }

        return string.IsNullOrWhiteSpace(department) ? "未分类" : department.Trim();
    }

    private static string FormatProductScope(string? hospitalName, string? productName)
    {
        var hospital = hospitalName?.Trim() ?? string.Empty;
        var product = productName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(hospital) && string.IsNullOrWhiteSpace(product))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(hospital))
        {
            return product;
        }

        if (string.IsNullOrWhiteSpace(product))
        {
            return hospital;
        }

        return $"{hospital} / {product}";
    }

    private static List<string> BuildPreviewWarnings(
        string supervisorName,
        int teamTotal,
        int projectCount,
        IReadOnlyCollection<MonthlyReportSourcePersonnelItemDto> personnelItems,
        int sickLeaveCount,
        int personalLeaveCount,
        int otherSpecialCount)
    {
        var warnings = new List<string>();

        if (teamTotal == 0)
        {
            warnings.Add("当前组别未匹配到人员台账数据。");
        }

        if (projectCount == 0)
        {
            warnings.Add("当前组别未匹配到项目台账数据。");
        }

        if (string.IsNullOrWhiteSpace(supervisorName))
        {
            warnings.Add("当前组别未识别到主管，请检查权限配置中的上级主管与系统角色。");
        }

        var unmatchedNames = personnelItems
            .Where(item => item.ResponsibilityHospitalCount == 0 && item.ResponsibilityProductCount == 0)
            .Select(item => item.Name)
            .ToList();
        if (unmatchedNames.Count > 0)
        {
            warnings.Add($"以下人员暂未匹配到负责医院/产品：{string.Join("、", unmatchedNames)}。");
        }

        if (sickLeaveCount == 0 && personalLeaveCount == 0 && otherSpecialCount == 0)
        {
            warnings.Add("病假、事假和其他特殊情况已改为按工时管理中的对应类型自动统计；当前月份未检索到相关登记记录。");
        }

        return warnings;
    }

    private sealed class MonthlyReportSourceComputationResult
    {
        public MonthlyReportUpsertDto UpsertDto { get; init; } = new();
        public MonthlyReportSourcePreviewDto PreviewDto { get; init; } = new();
    }
}
