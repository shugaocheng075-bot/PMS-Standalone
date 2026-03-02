using System.Globalization;
using System.Text.Json;
using System.Text;
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
    [HttpPost("excel")]
    public IActionResult ImportFromExcel([FromBody] ExcelImportRequest request)
    {
        var filePaths = CollectFilePaths(request);
        return ImportFromFiles(filePaths);
    }

    [HttpPost("excel-auto")]
    public IActionResult ImportFromExcelAuto()
    {
        var filePaths = DiscoverExcelFiles();
        return ImportFromFiles(filePaths);
    }

    private static IActionResult ImportFromFiles(IReadOnlyList<string> filePaths)
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
            var projects = ReadProjectsFromWorkbook(path);
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

    private static List<ProjectEntity> ReadProjectsFromWorkbook(string filePath)
    {
        var result = new List<ProjectEntity>();

        using var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        do
        {
            if (!reader.Read())
            {
                continue;
            }

            var headers = BuildHeaderIndex(reader);
            var hospitalIndex = FindIndex(headers, "医院名称", "项目名称");
            if (hospitalIndex < 0)
            {
                continue;
            }

            var provinceIndex = FindIndex(headers, "省", "省份", "项目省份", "服务区域");
            var groupIndex = FindIndex(headers, "组别", "原组别", "服务主管", "维护员", "原实施主管");
            var productIndex = FindIndex(headers, "产品", "上线产品", "产品名称", "项目类别");
            var levelIndex = FindIndex(headers, "医院等级", "级别", "医院级别", "评审级别");
            var statusIndex = FindIndex(headers, "合同状态", "项目状态", "交接现阶段", "运维阶段");
            var overdueDaysIndex = FindIndex(headers, "超期天数");
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
                var productName = ReadText(reader, productIndex);
                var hospitalLevel = ReadText(reader, levelIndex);
                var contractStatus = ReadText(reader, statusIndex);
                var overdueDays = ReadInt(reader, overdueDaysIndex);
                var maintenanceAmount = ReadDecimal(reader, amountIndex);

                if (overdueDays <= 0)
                {
                    var isOverdue = ReadText(reader, isOverdueIndex);
                    if (isOverdue is "是" or "Y" or "y" or "true" or "TRUE")
                    {
                        overdueDays = 1;
                    }
                }

                result.Add(new ProjectEntity
                {
                    HospitalName = hospitalName,
                    ProductName = productName,
                    Province = string.IsNullOrWhiteSpace(province) ? "未知" : province,
                    GroupName = string.IsNullOrWhiteSpace(groupName) ? "未分组" : groupName,
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
}
