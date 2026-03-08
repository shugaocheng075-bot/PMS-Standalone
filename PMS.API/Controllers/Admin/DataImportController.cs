using System.Globalization;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using ExcelDataReader;
using Microsoft.AspNetCore.Mvc;
using PMS.API.Models;
using PMS.Domain.Entities;
using PMS.Infrastructure.Services;

namespace PMS.API.Controllers.Admin;

[ApiController]
[Route("api/admin/import")]
public class DataImportController : ControllerBase
{
    private const string DefaultProjectLedgerExcelPath = @"C:\Users\R9000P\Desktop\项目明细.xlsx";
    private const string DefaultProjectLedgerSheetName = "维护项目明细";
    private const string DefaultMajorDemandExcelPath = @"C:\Users\R9000P\Desktop\项目明细.xlsx";
    private const string DefaultMajorDemandSheetName = "重大需求明细";

    [HttpPost("text")]
    public IActionResult ImportFromText([FromBody] TextImportRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RawText))
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = "请提供要导入的文本内容"
            });
        }

        var projects = ReadProjectsFromText(request.RawText);
        if (projects.Count == 0)
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = "未识别到有效数据行（请确认文本为制表符分隔并包含医院/产品列）"
            });
        }

        var cleanupStats = InMemoryProjectDataStore.ReplaceAllAndNormalize(projects);
        InMemoryHospitalService.RebuildFromProjects(InMemoryProjectDataStore.Projects);

        return Ok(ApiResponse<object>.Success(new
        {
            importedRowCount = projects.Count,
            keptProjectCount = cleanupStats.KeptCount,
            removedInvalidCount = cleanupStats.RemovedInvalidCount,
            deduplicatedCount = cleanupStats.DeduplicatedCount,
            message = "文本数据已全量覆盖导入，旧数据已清空"
        }));
    }

    [HttpPost("text-file")]
    public IActionResult ImportFromTextFile([FromBody] ExcelImportRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.FilePath))
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = "请提供文本文件路径"
            });
        }

        var filePath = Path.GetFullPath(request.FilePath.Trim());
        if (!System.IO.File.Exists(filePath))
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = $"文件不存在: {filePath}"
            });
        }

        var rawText = System.IO.File.ReadAllText(filePath, Encoding.UTF8);
        return ImportFromText(new TextImportRequest { RawText = rawText });
    }

    [HttpPost("excel")]
    public IActionResult ImportFromExcel([FromBody] ExcelImportRequest request)
    {
        var filePaths = CollectFilePaths(request);
        return ImportFromFiles(filePaths, request.SheetName);
    }

    [HttpPost("excel-auto")]
    public IActionResult ImportFromExcelAuto()
    {
        var filePaths = DiscoverExcelFiles();
        return ImportFromFiles(filePaths, string.Empty);
    }

    [HttpPost("major-demand")]
    public IActionResult ImportMajorDemand([FromBody] ExcelImportRequest? request)
    {
        var filePath = string.IsNullOrWhiteSpace(request?.FilePath)
            ? DefaultMajorDemandExcelPath
            : Path.GetFullPath(request.FilePath.Trim());

        var sheetName = string.IsNullOrWhiteSpace(request?.SheetName)
            ? DefaultMajorDemandSheetName
            : request.SheetName.Trim();

        if (!System.IO.File.Exists(filePath))
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = $"Excel文件不存在: {filePath}"
            });
        }

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var majorDemand = ReadMajorDemandFromWorkbook(filePath, sheetName);
        if (majorDemand.Rows.Count == 0)
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = $"未在工作表“{sheetName}”中识别到有效数据"
            });
        }

        InMemoryMajorDemandStore.ReplaceAll(majorDemand.Columns, majorDemand.Rows, filePath, sheetName);

        return Ok(ApiResponse<object>.Success(new
        {
            sourceFilePath = filePath,
            sheetName,
            importedColumnCount = majorDemand.Columns.Count,
            importedRowCount = majorDemand.Rows.Count,
            linkedProjectInputCount = 0,
            linkedProjectKeptCount = 0,
            removedInvalidCount = 0,
            deduplicatedCount = 0,
            message = "重大需求已导入（仅更新重大需求模块，不联动其他页面）"
        }));
    }

    [HttpPost("project-ledger")]
    public IActionResult ImportProjectLedger([FromBody] ExcelImportRequest? request)
    {
        var filePath = string.IsNullOrWhiteSpace(request?.FilePath)
            ? DefaultProjectLedgerExcelPath
            : Path.GetFullPath(request.FilePath.Trim());

        var sheetName = string.IsNullOrWhiteSpace(request?.SheetName)
            ? DefaultProjectLedgerSheetName
            : request.SheetName.Trim();

        if (!System.IO.File.Exists(filePath))
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = $"Excel文件不存在: {filePath}"
            });
        }

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var projects = ReadProjectLedgerFromWorkbook(filePath, sheetName);
        if (projects.Count == 0)
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = $"未在工作表“{sheetName}”中识别到有效数据"
            });
        }

        InMemoryProjectDataStore.ReplaceAllRaw(projects);
        InMemoryHospitalService.RebuildFromProjects(InMemoryProjectDataStore.Projects);

        return Ok(ApiResponse<object>.Success(new
        {
            sourceFilePath = filePath,
            sheetName,
            importedRowCount = projects.Count,
            message = "项目台账已按维护项目明细全量导入"
        }));
    }

    private static IActionResult ImportFromFiles(IReadOnlyList<string> filePaths, string? targetSheetName)
    {
        if (filePaths.Count == 0)
        {
            return new BadRequestObjectResult(new ApiResponse<object>
            {
                Code = 400,
                Message = "未发现可导入的Excel文件"
            });
        }

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var allProjects = new List<ProjectEntity>();
        var importedFiles = new List<object>();

        foreach (var path in filePaths)
        {
            var projects = ReadProjectsFromWorkbook(path, targetSheetName);
            allProjects.AddRange(projects);
            importedFiles.Add(new
            {
                filePath = path,
                rows = projects.Count
            });
        }

        if (allProjects.Count == 0)
        {
            return new BadRequestObjectResult(new ApiResponse<object>
            {
                Code = 400,
                Message = "已扫描文件，但未识别到可导入行（请检查表头是否含医院名称/项目名称等关键列）"
            });
        }

        var cleanupStats = InMemoryProjectDataStore.ReplaceAllAndNormalize(allProjects);
        InMemoryHospitalService.RebuildFromProjects(InMemoryProjectDataStore.Projects);

        return new OkObjectResult(ApiResponse<object>.Success(new
        {
            importedFileCount = filePaths.Count,
            importedRowCount = allProjects.Count,
            keptProjectCount = cleanupStats.KeptCount,
            removedInvalidCount = cleanupStats.RemovedInvalidCount,
            deduplicatedCount = cleanupStats.DeduplicatedCount,
            files = importedFiles
        }));
    }

    [HttpPost("cleanup")]
    public IActionResult CleanupCurrentData()
    {
        var beforeCount = InMemoryProjectDataStore.Projects.Count;
        var stats = InMemoryProjectDataStore.NormalizeCurrentData();
        InMemoryHospitalService.RebuildFromProjects(InMemoryProjectDataStore.Projects);

        return Ok(ApiResponse<object>.Success(new
        {
            beforeCount,
            afterCount = stats.KeptCount,
            removedInvalidCount = stats.RemovedInvalidCount,
            deduplicatedCount = stats.DeduplicatedCount
        }));
    }

    [HttpGet("ownership-audit")]
    public IActionResult GetOwnershipAudit()
    {
        var rows = InMemoryProjectDataStore.Projects;
        var grouped = rows
            .GroupBy(x => new { x.HospitalName, x.ProductName })
            .Select(g => new
            {
                hospitalName = g.Key.HospitalName,
                productName = g.Key.ProductName,
                groupName = g.Select(x => x.GroupName).FirstOrDefault() ?? "未分组",
                hospitalLevel = g.Select(x => x.HospitalLevel).FirstOrDefault() ?? "未评级",
                province = g.Select(x => x.Province).FirstOrDefault() ?? "未知",
                itemCount = g.Count(),
                amount = g.Max(x => x.MaintenanceAmount)
            })
            .OrderBy(x => x.hospitalName)
            .ThenBy(x => x.productName)
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            total = grouped.Count,
            items = grouped
        }));
    }

    [HttpPost("ownership/reassign")]
    public IActionResult ReassignOwnership([FromBody] OwnershipReassignRequest request)
    {
        if (request is null
            || string.IsNullOrWhiteSpace(request.HospitalName)
            || string.IsNullOrWhiteSpace(request.ProductName)
            || string.IsNullOrWhiteSpace(request.GroupName))
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = "请提供医院、产品、归属人"
            });
        }

        var affected = InMemoryProjectDataStore.ReassignHospitalProductOwner(
            request.HospitalName,
            request.ProductName,
            request.GroupName);

        if (affected <= 0)
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = 400,
                Message = "未找到可调整的医院+产品记录"
            });
        }

        InMemoryHospitalService.RebuildFromProjects(InMemoryProjectDataStore.Projects);

        return Ok(ApiResponse<object>.Success(new
        {
            affected,
            hospitalName = request.HospitalName,
            productName = request.ProductName,
            groupName = request.GroupName
        }));
    }

    private static List<ProjectEntity> ReadProjectsFromWorkbook(string filePath, string? targetSheetName)
    {
        var result = new List<ProjectEntity>();
        var normalizedTargetSheet = string.IsNullOrWhiteSpace(targetSheetName)
            ? string.Empty
            : NormalizeHeader(targetSheetName);

        using var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        do
        {
            if (!reader.Read())
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(normalizedTargetSheet))
            {
                var currentSheet = NormalizeHeader(reader.Name ?? string.Empty);
                if (!currentSheet.Equals(normalizedTargetSheet, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            var headers = BuildHeaderIndex(reader);
            var hospitalIndex = FindIndex(headers, "医院名称", "项目名称");
            if (hospitalIndex < 0)
            {
                continue;
            }

            var provinceIndex = FindIndex(headers, "省", "省份", "项目省份", "服务区域");
            var groupIndex = FindIndex(headers, "组别", "原组别");
            var salesIndex = FindIndex(headers, "销售", "销售姓名", "销售人员", "商务");
            var maintenancePersonIndex = FindIndex(headers, "维护员", "维护人员", "售后", "实施人员", "运维人员");
            var productIndex = FindIndex(headers, "产品", "上线产品", "产品名称", "项目类别");
            var levelIndex = FindIndex(headers, "医院等级", "级别", "医院级别", "评审级别");
            var statusIndex = FindIndex(headers, "合同状态", "项目状态", "交接现阶段", "运维阶段");
            var overdueDaysIndex = FindIndex(headers, "超期天数");
            var startDateIndex = FindIndex(headers, "售后开始日期", "维保开始日期", "开始日期", "服务开始日期");
            var endDateIndex = FindIndex(headers, "售后结束日期", "维保结束日期", "结束日期", "服务结束日期");
            var amountIndex = FindIndex(headers, "维护合同额", "金额", "维护金额", "销售合同额", "年度产值");
            var isOverdueIndex = FindIndex(headers, "是否超期");

            var hasUsefulColumns = statusIndex >= 0 || overdueDaysIndex >= 0 || amountIndex >= 0 || provinceIndex >= 0 || groupIndex >= 0;
            if (!hasUsefulColumns)
            {
                continue;
            }

            while (reader.Read())
            {
                var hospitalName = ReadText(reader, hospitalIndex);
                if (string.IsNullOrWhiteSpace(hospitalName))
                {
                    continue;
                }

                var province = ReadText(reader, provinceIndex);
                var groupName = ReadText(reader, groupIndex);
                var salesName = ReadText(reader, salesIndex);
                var maintenancePersonName = ReadText(reader, maintenancePersonIndex);
                var productName = ReadText(reader, productIndex);
                var hospitalLevel = ReadText(reader, levelIndex);
                var contractStatus = ReadText(reader, statusIndex);
                var overdueDays = ReadInt(reader, overdueDaysIndex);
                var startDateText = ReadText(reader, startDateIndex);
                var endDateText = ReadText(reader, endDateIndex);
                var maintenanceAmount = ReadDecimal(reader, amountIndex);

                if (overdueDays <= 0)
                {
                    var isOverdue = ReadText(reader, isOverdueIndex);
                    if (isOverdue is "是" or "Y" or "y" or "true" or "TRUE")
                    {
                        overdueDays = 1;
                    }
                }

                if (overdueDays <= 0)
                {
                    overdueDays = ComputeOverdueDays(endDateText, contractStatus);
                }

                result.Add(new ProjectEntity
                {
                    HospitalName = hospitalName,
                    ProductName = productName,
                    Province = string.IsNullOrWhiteSpace(province) ? "未知" : province,
                    GroupName = string.IsNullOrWhiteSpace(groupName) ? "未分组" : groupName,
                    SalesName = string.IsNullOrWhiteSpace(salesName) ? "未知" : salesName,
                    MaintenancePersonName = string.IsNullOrWhiteSpace(maintenancePersonName) ? "未知" : maintenancePersonName,
                    AfterSalesStartDate = NormalizeDateText(startDateText),
                    AfterSalesEndDate = NormalizeDateText(endDateText),
                    HospitalLevel = hospitalLevel,
                    ContractStatus = string.IsNullOrWhiteSpace(contractStatus) ? "未知" : contractStatus,
                    MaintenanceAmount = maintenanceAmount,
                    OverdueDays = overdueDays
                });
            }
        }
        while (reader.NextResult());

        return result;
    }

    private static List<ProjectEntity> ReadProjectLedgerFromWorkbook(string filePath, string sheetName)
    {
        var result = new List<ProjectEntity>();
        var normalizedTargetSheet = NormalizeHeader(sheetName);

        using var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        do
        {
            if (!reader.Read())
            {
                continue;
            }

            var currentSheet = NormalizeHeader(reader.Name ?? string.Empty);
            if (!currentSheet.Equals(normalizedTargetSheet, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var headers = BuildHeaderIndex(reader);
            var serialNumberIndex = FindIndex(headers, "序号");
            var serviceAreaIndex = FindIndex(headers, "服务区域");
            var provinceIndex = FindIndex(headers, "省", "省份");
            var cityIndex = FindIndex(headers, "市");
            var salesIndex = FindIndex(headers, "销售");
            var groupIndex = FindIndex(headers, "服务组别", "组别");
            var maintenancePersonIndex = FindIndex(headers, "服务人员", "维护人员");
            var hospitalLevelIndex = FindIndex(headers, "医院等级", "医院级别");
            var hospitalNameIndex = FindIndex(headers, "最终用户", "医院名称");
            var productNameIndex = FindIndex(headers, "维护产品", "产品", "产品名称");
            var pointsIndex = FindIndex(headers, "点位");
            var salesAmountIndex = FindIndex(headers, "销售合同额");
            var maintenanceAmountIndex = FindIndex(headers, "维护合同额", "维护金额");
            var annualOutputIndex = FindIndex(headers, "年度产值");
            var contractStatusIndex = FindIndex(headers, "合同状态");
            var afterSalesStartDateIndex = FindIndex(headers, "维护开始日期", "售后开始日期");
            var afterSalesEndDateIndex = FindIndex(headers, "维护结束日期", "售后结束日期");
            var isOverdueIndex = FindIndex(headers, "是否超期");
            var overdueDaysIndex = FindIndex(headers, "超期天数");
            var opportunityNumberIndex = FindIndex(headers, "机会号");
            var stationLocationIndex = FindIndex(headers, "驻地");
            var isStationedOnsiteIndex = FindIndex(headers, "是否驻场");
            var stationedCountIndex = FindIndex(headers, "驻场人数");
            var remarksIndex = FindIndex(headers, "备注");
            var acceptanceDateIndex = FindIndex(headers, "验收日期");

            while (reader.Read())
            {
                var hasAnyValue = Enumerable.Range(0, reader.FieldCount).Any(index => !string.IsNullOrWhiteSpace(ReadText(reader, index)));
                if (!hasAnyValue)
                {
                    continue;
                }

                var hospitalName = ReadText(reader, hospitalNameIndex);
                var productName = ReadText(reader, productNameIndex);
                if (string.IsNullOrWhiteSpace(hospitalName) && string.IsNullOrWhiteSpace(productName))
                {
                    continue;
                }

                var contractStatus = ReadText(reader, contractStatusIndex);
                var afterSalesEndDateText = ReadText(reader, afterSalesEndDateIndex);
                var isOverdue = ReadText(reader, isOverdueIndex);
                var overdueDays = ReadInt(reader, overdueDaysIndex);
                if (overdueDays <= 0 && (isOverdue is "是" or "Y" or "y" or "true" or "TRUE"))
                {
                    overdueDays = 1;
                }

                if (overdueDays <= 0)
                {
                    overdueDays = ComputeOverdueDays(afterSalesEndDateText, contractStatus);
                }

                result.Add(new ProjectEntity
                {
                    SerialNumber = ReadText(reader, serialNumberIndex),
                    ServiceArea = ReadText(reader, serviceAreaIndex),
                    Province = ReadText(reader, provinceIndex),
                    City = ReadText(reader, cityIndex),
                    SalesName = ReadText(reader, salesIndex),
                    GroupName = ReadText(reader, groupIndex),
                    MaintenancePersonName = ReadText(reader, maintenancePersonIndex),
                    HospitalLevel = ReadText(reader, hospitalLevelIndex),
                    HospitalName = hospitalName,
                    ProductName = productName,
                    Points = ReadText(reader, pointsIndex),
                    SalesAmount = ReadDecimal(reader, salesAmountIndex),
                    MaintenanceAmount = ReadDecimal(reader, maintenanceAmountIndex),
                    AnnualOutput = ReadDecimal(reader, annualOutputIndex),
                    ContractStatus = contractStatus,
                    AfterSalesStartDate = NormalizeDateText(ReadText(reader, afterSalesStartDateIndex)),
                    AfterSalesEndDate = NormalizeDateText(afterSalesEndDateText),
                    OverdueDays = overdueDays,
                    OpportunityNumber = ReadText(reader, opportunityNumberIndex),
                    StationLocation = ReadText(reader, stationLocationIndex),
                    IsStationedOnsite = ReadText(reader, isStationedOnsiteIndex),
                    StationedCount = ReadText(reader, stationedCountIndex),
                    Remarks = ReadText(reader, remarksIndex),
                    AcceptanceDate = NormalizeDateText(ReadText(reader, acceptanceDateIndex))
                });
            }

            return result;
        }
        while (reader.NextResult());

        return result;
    }

    private static List<string> CollectFilePaths(ExcelImportRequest? request)
    {
        var list = new List<string>();

        if (request is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.FilePath))
            {
                list.Add(request.FilePath.Trim());
            }

            if (request.FilePaths.Count > 0)
            {
                list.AddRange(request.FilePaths.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
            }
        }

        return list
            .Select(Path.GetFullPath)
            .Where(System.IO.File.Exists)
            .Where(path => IsExcelFile(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> DiscoverExcelFiles()
    {
        var candidates = new List<string>();
        var discoveredDir = DiscoverChromeDownloadDirectory();

        if (!string.IsNullOrWhiteSpace(discoveredDir) && Directory.Exists(discoveredDir))
        {
            candidates.AddRange(Directory.GetFiles(discoveredDir, "*.xls*", SearchOption.TopDirectoryOnly));
        }

        var fallbackDownloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        if (Directory.Exists(fallbackDownloads))
        {
            candidates.AddRange(Directory.GetFiles(fallbackDownloads, "*.xls*", SearchOption.TopDirectoryOnly));
        }

        return candidates
            .Where(IsExcelFile)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(path => new FileInfo(path).LastWriteTime)
            .ToList();
    }

    private static string? DiscoverChromeDownloadDirectory()
    {
        var prefPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Google",
            "Chrome",
            "User Data",
            "Default",
            "Preferences");

        if (!System.IO.File.Exists(prefPath))
        {
            return null;
        }

        try
        {
            using var stream = System.IO.File.OpenRead(prefPath);
            using var document = JsonDocument.Parse(stream);

            if (document.RootElement.TryGetProperty("download", out var downloadNode)
                && downloadNode.TryGetProperty("default_directory", out var dirNode))
            {
                var dir = dirNode.GetString();
                return string.IsNullOrWhiteSpace(dir) ? null : dir;
            }
        }
        catch
        {
        }

        return null;
    }

    private static bool IsExcelFile(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".xls", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".xlsm", StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, int> BuildHeaderIndex(IExcelDataReader reader)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < reader.FieldCount; i++)
        {
            var value = reader.GetValue(i)?.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var normalized = NormalizeHeader(value);
            if (!map.ContainsKey(normalized))
            {
                map[normalized] = i;
            }
        }

        return map;
    }

    private static int FindIndex(Dictionary<string, int> headers, params string[] names)
    {
        foreach (var name in names)
        {
            var normalized = NormalizeHeader(name);
            if (headers.TryGetValue(normalized, out var index))
            {
                return index;
            }
        }

        return -1;
    }

    private static string ReadText(IExcelDataReader reader, int index)
    {
        if (index < 0 || index >= reader.FieldCount)
        {
            return string.Empty;
        }

        return reader.GetValue(index)?.ToString()?.Trim() ?? string.Empty;
    }

    private static int ReadInt(IExcelDataReader reader, int index)
    {
        var text = ReadText(reader, index);
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        text = text.Replace(",", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("天", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue))
        {
            return (int)Math.Round(doubleValue, MidpointRounding.AwayFromZero);
        }

        return 0;
    }

    private static decimal ReadDecimal(IExcelDataReader reader, int index)
    {
        var text = ReadText(reader, index);
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0m;
        }

        text = text.Replace(",", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("元", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue))
        {
            return Convert.ToDecimal(doubleValue);
        }

        return 0m;
    }

    private static string NormalizeHeader(string value)
    {
        return value
            .Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("　", string.Empty, StringComparison.Ordinal)
            .Replace("\t", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
    }

    private static List<ProjectEntity> ReadProjectsFromText(string rawText)
    {
        var rows = new List<ProjectEntity>();
        var lines = rawText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        foreach (var line in lines)
        {
            if (IsHeaderLine(line))
            {
                continue;
            }

            var columns = SplitLineColumns(line);
            if (columns.Count < 6)
            {
                continue;
            }

            var groupName = columns[0];
            var province = columns[1];
            var salesName = columns.Count > 2 ? columns[2] : string.Empty;
            var maintenancePersonName = columns.Count > 3 ? columns[3] : string.Empty;
            var hospitalName = columns[4];
            var productName = columns[5];
            var contractStatus = columns.Count > 6 ? columns[6] : string.Empty;
            var startDateText = columns.Count > 7 ? columns[7] : string.Empty;
            var endDateText = columns.Count > 8 ? columns[8] : string.Empty;

            if (string.IsNullOrWhiteSpace(hospitalName) || string.IsNullOrWhiteSpace(productName))
            {
                continue;
            }

            rows.Add(new ProjectEntity
            {
                HospitalName = hospitalName,
                ProductName = productName,
                Province = string.IsNullOrWhiteSpace(province) ? "未知" : province,
                GroupName = string.IsNullOrWhiteSpace(groupName) ? "未分组" : groupName,
                SalesName = string.IsNullOrWhiteSpace(salesName) ? "未知" : salesName,
                MaintenancePersonName = string.IsNullOrWhiteSpace(maintenancePersonName) ? "未知" : maintenancePersonName,
                AfterSalesStartDate = NormalizeDateText(startDateText),
                AfterSalesEndDate = NormalizeDateText(endDateText),
                HospitalLevel = string.Empty,
                ContractStatus = string.IsNullOrWhiteSpace(contractStatus) ? "未知" : contractStatus,
                MaintenanceAmount = 0m,
                OverdueDays = ComputeOverdueDays(endDateText, contractStatus)
            });
        }

        return rows;
    }

    private static bool IsHeaderLine(string line)
    {
        return line.Contains("组别", StringComparison.OrdinalIgnoreCase)
               && line.Contains("省份", StringComparison.OrdinalIgnoreCase)
               && line.Contains("项目名称", StringComparison.OrdinalIgnoreCase)
               && line.Contains("产品", StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> SplitLineColumns(string line)
    {
        var byTab = line.Split('\t').Select(x => x.Trim()).ToList();
        if (byTab.Count >= 6)
        {
            return byTab;
        }

        return Regex.Split(line, @"\s{2,}")
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private static int ComputeOverdueDays(string endDateText, string contractStatus)
    {
        if (!string.IsNullOrWhiteSpace(endDateText)
            && TryParseDate(endDateText, out var endDate)
            && endDate < DateOnly.FromDateTime(DateTime.Today))
        {
            return (DateOnly.FromDateTime(DateTime.Today).ToDateTime(TimeOnly.MinValue) - endDate.ToDateTime(TimeOnly.MinValue)).Days;
        }

        if (string.IsNullOrWhiteSpace(contractStatus))
        {
            return 0;
        }

        if (contractStatus.Contains("超期", StringComparison.OrdinalIgnoreCase)
            || contractStatus.Contains("到期", StringComparison.OrdinalIgnoreCase)
            || contractStatus.Contains("停保", StringComparison.OrdinalIgnoreCase)
            || contractStatus.Contains("已脱保", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 0;
    }

    private static bool TryParseDate(string raw, out DateOnly date)
    {
        var text = raw.Trim();
        var formats = new[]
        {
            "yyyy/M/d", "yyyy/MM/dd", "yyyy-M-d", "yyyy-MM-dd"
        };

        foreach (var format in formats)
        {
            if (DateOnly.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return true;
            }
        }

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            date = DateOnly.FromDateTime(dateTime);
            return true;
        }

        date = default;
        return false;
    }

    private static string NormalizeDateText(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        return TryParseDate(raw, out var date)
            ? date.ToString("yyyy-MM-dd")
            : raw.Trim();
    }

    private static MajorDemandParsedResult ReadMajorDemandFromWorkbook(string filePath, string sheetName)
    {
        var normalizedTargetSheet = NormalizeHeader(sheetName);

        using var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        do
        {
            if (!reader.Read())
            {
                continue;
            }

            var currentSheet = NormalizeHeader(reader.Name ?? string.Empty);
            if (!currentSheet.Equals(normalizedTargetSheet, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var columns = BuildMajorDemandColumns(reader);
            if (columns.Count == 0)
            {
                return new MajorDemandParsedResult();
            }

            var rows = new List<Dictionary<string, string>>();
            while (reader.Read())
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var hasValue = false;

                for (var i = 0; i < columns.Count; i++)
                {
                    var value = i >= reader.FieldCount
                        ? string.Empty
                        : reader.GetValue(i)?.ToString()?.Trim() ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        hasValue = true;
                    }

                    row[columns[i]] = value;
                }

                if (hasValue)
                {
                    rows.Add(row);
                }
            }

            return new MajorDemandParsedResult
            {
                Columns = columns,
                Rows = rows
            };
        }
        while (reader.NextResult());

        return new MajorDemandParsedResult();
    }

    private static List<string> BuildMajorDemandColumns(IExcelDataReader reader)
    {
        var columns = new List<string>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < reader.FieldCount; i++)
        {
            var raw = reader.GetValue(i)?.ToString()?.Trim() ?? string.Empty;
            var header = string.IsNullOrWhiteSpace(raw) ? $"列{i + 1}" : raw;
            var uniqueHeader = header;
            var suffix = 2;

            while (!used.Add(uniqueHeader))
            {
                uniqueHeader = $"{header}_{suffix}";
                suffix++;
            }

            columns.Add(uniqueHeader);
        }

        return columns;
    }

    private static List<ProjectEntity> ConvertMajorDemandToProjects(IReadOnlyList<Dictionary<string, string>> rows)
    {
        var projects = new List<ProjectEntity>();

        foreach (var row in rows)
        {
            var hospitalName = GetFirstColumnValue(row, "医院名称", "最终用户", "最终客户", "项目名称", "医院", "客户名称");
            if (string.IsNullOrWhiteSpace(hospitalName))
            {
                continue;
            }

            var productName = GetFirstColumnValue(row, "产品名称", "产品", "软件名称", "产品线", "上线产品", "项目类别");
            var province = GetFirstColumnValue(row, "省", "省份", "项目省份", "服务区域");
            var groupName = GetFirstColumnValue(row, "组别", "原组别", "归属组", "归属人");
            var salesName = GetFirstColumnValue(row, "销售", "销售姓名", "销售人员", "商务");
            var maintenancePersonName = GetFirstColumnValue(row, "维护员", "维护人员", "售后", "实施人员", "运维人员");
            var hospitalLevel = GetFirstColumnValue(row, "医院等级", "级别", "医院级别", "评审级别");
            var contractStatus = GetFirstColumnValue(row, "合同状态", "项目状态", "交接现阶段", "运维阶段");
            var startDateText = GetFirstColumnValue(row, "售后开始日期", "维保开始日期", "开始日期", "服务开始日期");
            var endDateText = GetFirstColumnValue(row, "售后结束日期", "维保结束日期", "结束日期", "服务结束日期");
            var overdueText = GetFirstColumnValue(row, "超期天数");
            var amountText = GetFirstColumnValue(row, "维护合同额", "金额", "维护金额", "销售合同额", "年度产值");
            var isOverdue = GetFirstColumnValue(row, "是否超期");

            var overdueDays = ParseInt(overdueText);
            if (overdueDays <= 0 && (isOverdue is "是" or "Y" or "y" or "true" or "TRUE"))
            {
                overdueDays = 1;
            }

            if (overdueDays <= 0)
            {
                overdueDays = ComputeOverdueDays(endDateText, contractStatus);
            }

            projects.Add(new ProjectEntity
            {
                HospitalName = hospitalName,
                ProductName = productName,
                Province = string.IsNullOrWhiteSpace(province) ? "未知" : province,
                GroupName = string.IsNullOrWhiteSpace(groupName) ? "未分组" : groupName,
                SalesName = string.IsNullOrWhiteSpace(salesName) ? "未知" : salesName,
                MaintenancePersonName = string.IsNullOrWhiteSpace(maintenancePersonName) ? "未知" : maintenancePersonName,
                AfterSalesStartDate = NormalizeDateText(startDateText),
                AfterSalesEndDate = NormalizeDateText(endDateText),
                HospitalLevel = string.IsNullOrWhiteSpace(hospitalLevel) ? "未评级" : hospitalLevel,
                ContractStatus = string.IsNullOrWhiteSpace(contractStatus) ? "未知" : contractStatus,
                MaintenanceAmount = ParseDecimal(amountText),
                OverdueDays = overdueDays
            });
        }

        return projects;
    }

    private static string GetFirstColumnValue(IReadOnlyDictionary<string, string> row, params string[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                continue;
            }

            if (row.TryGetValue(alias, out var directValue) && !string.IsNullOrWhiteSpace(directValue))
            {
                return directValue.Trim();
            }

            var normalizedAlias = NormalizeHeader(alias);
            foreach (var pair in row)
            {
                if (!NormalizeHeader(pair.Key).Equals(normalizedAlias, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(pair.Value))
                {
                    return pair.Value.Trim();
                }
            }
        }

        return string.Empty;
    }

    private static int ParseInt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var normalized = text.Replace(",", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("天", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        if (double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue))
        {
            return (int)Math.Round(doubleValue, MidpointRounding.AwayFromZero);
        }

        return 0;
    }

    private static decimal ParseDecimal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0m;
        }

        var normalized = text.Replace(",", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("元", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        if (double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue))
        {
            return Convert.ToDecimal(doubleValue);
        }

        return 0m;
    }

    private sealed class MajorDemandParsedResult
    {
        public List<string> Columns { get; set; } = [];
        public List<Dictionary<string, string>> Rows { get; set; } = [];
    }
}
