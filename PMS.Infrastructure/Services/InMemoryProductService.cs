using PMS.Application.Contracts.Product;
using PMS.Application.Models;
using PMS.Application.Models.Product;
using PMS.Domain.Entities;
using System.Text.RegularExpressions;

namespace PMS.Infrastructure.Services;

public class InMemoryProductService : IProductService
{
    private const string StateKey = "products_custom_overrides";
    private static readonly List<ProductItemDto> CustomProducts = SqliteJsonStore.LoadOrSeed(StateKey, BuildSeedData);

    public Task<ProductSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var products = GetCombinedProducts();
        var summary = new ProductSummaryDto
        {
            Total = products.Count,
            ActiveCount = products.Count(x => x.Status == "运行中"),
            PilotCount = products.Count(x => x.Status == "试运行"),
            RetiredCount = products.Count(x => x.Status == "已停用")
        };

        return Task.FromResult(summary);
    }

    public Task<PagedResult<ProductItemDto>> QueryAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        var list = GetCombinedProducts().AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.ProductName))
        {
            list = list.Where(x => SmartTextMatcher.Match(x.ProductName, query.ProductName));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            list = list.Where(x => SmartTextMatcher.MatchExact(x.Category, query.Category));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            list = list.Where(x => SmartTextMatcher.MatchExact(x.Status, query.Status));
        }

        var total = list.Count();
        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.Size <= 0 ? 20 : query.Size;

        var items = list
            .OrderByDescending(x => x.DeployHospitalCount)
            .ThenBy(x => x.ProductName)
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return Task.FromResult(new PagedResult<ProductItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            Size = size
        });
    }

    public Task<ProductItemDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = GetCombinedProducts().FirstOrDefault(x => x.Id == id);
        return Task.FromResult(item);
    }

    public Task<ProductItemDto> CreateAsync(ProductUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var existing = CustomProducts.FirstOrDefault(x => x.ProductName.Equals(dto.ProductName, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            CustomProducts.Add(new ProductItemDto
            {
                ProductName = dto.ProductName,
                Version = dto.Version,
                Category = dto.Category,
                Status = dto.Status,
                DeployHospitalCount = dto.DeployHospitalCount,
                CreatedAt = DateTime.Now
            });
        }
        else
        {
            existing.Version = dto.Version;
            existing.Category = dto.Category;
            existing.Status = dto.Status;
            existing.DeployHospitalCount = dto.DeployHospitalCount;
            existing.CreatedAt = DateTime.Now;
        }

        Persist();

        var created = GetCombinedProducts()
            .First(x => x.ProductName.Equals(dto.ProductName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(created);
    }

    public Task<ProductItemDto?> UpdateAsync(int id, ProductUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var current = GetCombinedProducts().FirstOrDefault(x => x.Id == id);
        if (current is null)
        {
            return Task.FromResult<ProductItemDto?>(null);
        }

        var custom = CustomProducts.FirstOrDefault(x => x.ProductName.Equals(current.ProductName, StringComparison.OrdinalIgnoreCase));
        if (custom is null)
        {
            custom = new ProductItemDto
            {
                ProductName = current.ProductName,
                Version = current.Version,
                Category = current.Category,
                Status = current.Status,
                DeployHospitalCount = current.DeployHospitalCount,
                CreatedAt = current.CreatedAt
            };
            CustomProducts.Add(custom);
        }

        custom.ProductName = dto.ProductName;
        custom.Version = dto.Version;
        custom.Category = dto.Category;
        custom.Status = dto.Status;
        custom.DeployHospitalCount = dto.DeployHospitalCount;
        custom.CreatedAt = DateTime.Now;

        Persist();

        var updated = GetCombinedProducts().FirstOrDefault(x => x.ProductName.Equals(dto.ProductName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(updated);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var current = GetCombinedProducts().FirstOrDefault(x => x.Id == id);
        if (current is null)
        {
            return Task.FromResult(false);
        }

        var custom = CustomProducts.FirstOrDefault(x => x.ProductName.Equals(current.ProductName, StringComparison.OrdinalIgnoreCase));
        if (custom is null)
        {
            return Task.FromResult(false);
        }

        CustomProducts.Remove(custom);
        Persist();
        return Task.FromResult(true);
    }

    private static void Persist()
    {
        SqliteJsonStore.Save(StateKey, CustomProducts);
    }

    private static List<ProductItemDto> GetCombinedProducts()
    {
        var derived = BuildDerivedProducts()
            .ToDictionary(x => x.ProductName, x => x, StringComparer.OrdinalIgnoreCase);

        foreach (var custom in CustomProducts)
        {
            if (derived.TryGetValue(custom.ProductName, out var existing))
            {
                existing.Version = string.IsNullOrWhiteSpace(custom.Version) ? existing.Version : custom.Version;
                existing.Category = string.IsNullOrWhiteSpace(custom.Category) ? existing.Category : custom.Category;
                existing.Status = string.IsNullOrWhiteSpace(custom.Status) ? existing.Status : custom.Status;
                existing.DeployHospitalCount = custom.DeployHospitalCount > 0 ? custom.DeployHospitalCount : existing.DeployHospitalCount;
                existing.CreatedAt = custom.CreatedAt;
            }
            else
            {
                derived[custom.ProductName] = new ProductItemDto
                {
                    ProductName = custom.ProductName,
                    Version = custom.Version,
                    Category = custom.Category,
                    Status = custom.Status,
                    DeployHospitalCount = custom.DeployHospitalCount,
                    CreatedAt = custom.CreatedAt
                };
            }
        }

        return derived.Values
            .OrderByDescending(x => x.DeployHospitalCount)
            .ThenBy(x => x.ProductName, StringComparer.OrdinalIgnoreCase)
            .Select((x, index) =>
            {
                x.Id = index + 1;
                return x;
            })
            .ToList();
    }

    private static List<ProductItemDto> BuildSeedData()
    {
        return [];
    }

    private static List<ProductItemDto> BuildDerivedProducts()
    {
        var projects = InMemoryProjectDataStore.Projects;

        return projects
            .Select(project =>
            {
                var (name, version) = SplitProductNameAndVersion(project.ProductName);
                var normalized = NormalizeDerivedProduct(name, version);
                if (normalized is null)
                {
                    return null;
                }

                return new
                {
                    ProductName = normalized.Value.productName,
                    Version = normalized.Value.version,
                    project.HospitalName,
                    project.ContractStatus
                };
            })
            .Where(x => x is not null)
            .Select(x => x!)
            .GroupBy(x => x.ProductName, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var versions = group
                    .Select(x => x.Version)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new ProductItemDto
                {
                    ProductName = group.Key,
                    Version = SelectHighestVersion(versions),
                    Category = InferCategory(group.Key),
                    Status = InferStatus(group.Select(x => x.ContractStatus)),
                    DeployHospitalCount = group
                        .Select(x => x.HospitalName)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count(),
                    CreatedAt = DateTime.Now
                };
            })
            .ToList();
    }

    private static (string productName, string version)? NormalizeDerivedProduct(string name, string version)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var normalizedName = name.Trim();
        var normalizedVersion = string.IsNullOrWhiteSpace(version) ? "-" : version.Trim();

        if (normalizedName.Contains("通用产品", StringComparison.OrdinalIgnoreCase)
            || normalizedName.Contains("没有护理", StringComparison.OrdinalIgnoreCase)
            || normalizedName.Contains("只有医生模块", StringComparison.OrdinalIgnoreCase)
            || normalizedName.Contains("个科室", StringComparison.OrdinalIgnoreCase)
            || normalizedName.Contains("84点位", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        normalizedName = normalizedName
            .Replace("临床路基", "临床路径", StringComparison.OrdinalIgnoreCase)
            .Replace("一体化门诊病历", "门诊电子病历", StringComparison.OrdinalIgnoreCase)
            .Replace("门诊病历", "门诊电子病历", StringComparison.OrdinalIgnoreCase)
            .Replace("住院电子病历", "住院电子病历", StringComparison.OrdinalIgnoreCase)
            .Replace("电子病历升级", "住院电子病历", StringComparison.OrdinalIgnoreCase)
            .Replace("CDAS病案归档", "病案归档", StringComparison.OrdinalIgnoreCase)
            .Replace("无纸化病案归档", "病案归档", StringComparison.OrdinalIgnoreCase)
            .Replace("病案归档系统", "病案归档", StringComparison.OrdinalIgnoreCase)
            .Replace("移睿云医生", "移睿医生", StringComparison.OrdinalIgnoreCase)
            .Replace("AI质控", "AI内涵质控", StringComparison.OrdinalIgnoreCase)
            .Replace("内涵质控", "AI内涵质控", StringComparison.OrdinalIgnoreCase)
            .Trim();

        normalizedName = Regex.Replace(normalizedName, "AIAI内涵质控", "AI内涵质控", RegexOptions.IgnoreCase);
        normalizedName = Regex.Replace(normalizedName, "AI\\s+内涵质控", "AI内涵质控", RegexOptions.IgnoreCase);

        if (normalizedName.Contains("手麻", StringComparison.OrdinalIgnoreCase)
            || normalizedName.Contains("手术麻醉", StringComparison.OrdinalIgnoreCase))
        {
            normalizedName = "手术麻醉系统";
        }
        else if (normalizedName.Contains("重症", StringComparison.OrdinalIgnoreCase))
        {
            normalizedName = "重症监护系统";
        }
        else if (normalizedName.Contains("心电", StringComparison.OrdinalIgnoreCase))
        {
            normalizedName = "心电信息管理系统";
        }
        else if (normalizedName.Contains("单病种", StringComparison.OrdinalIgnoreCase))
        {
            normalizedName = "单病种";
        }
        else if (normalizedName.Contains("不良事件", StringComparison.OrdinalIgnoreCase))
        {
            normalizedName = "不良事件上报";
        }
        else if (normalizedName.Contains("随访", StringComparison.OrdinalIgnoreCase))
        {
            normalizedName = "随访管理";
        }
        else if (normalizedName.Contains("护理管理", StringComparison.OrdinalIgnoreCase)
            || normalizedName.Contains("临床护理", StringComparison.OrdinalIgnoreCase))
        {
            normalizedName = "护理管理";
        }

        if (normalizedName.Contains("住院电子病历", StringComparison.OrdinalIgnoreCase)
            || normalizedName.Equals("电子病历", StringComparison.OrdinalIgnoreCase))
        {
            normalizedName = "住院电子病历";
        }
        else if (normalizedName.Contains("门诊电子病历", StringComparison.OrdinalIgnoreCase))
        {
            normalizedName = "门诊电子病历";
        }
        else if (normalizedName.Contains("护理电子病历", StringComparison.OrdinalIgnoreCase))
        {
            normalizedName = "护理电子病历";
        }

        normalizedName = Regex.Replace(normalizedName, @"\(.*?\)", string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return null;
        }

        return (normalizedName, normalizedVersion);
    }

    private static (string productName, string version) SplitProductNameAndVersion(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ("通用产品", "-");
        }

        var text = raw.Trim();
        var match = Regex.Match(text, @"^(.*?)[Vv](\d+(?:\.\d+)*)$");
        if (match.Success)
        {
            var name = match.Groups[1].Value.Trim();
            var version = $"V{match.Groups[2].Value}";
            return (string.IsNullOrWhiteSpace(name) ? text : name, version);
        }

        var inTextMatch = Regex.Match(text, @"[Vv](\d+(?:\.\d+)*)");
        if (inTextMatch.Success)
        {
            var version = $"V{inTextMatch.Groups[1].Value}";
            var name = Regex.Replace(text, @"[Vv]\d+(?:\.\d+)*", string.Empty).Trim();
            return (string.IsNullOrWhiteSpace(name) ? text : name, version);
        }

        return (text, "-");
    }

    private static string SelectHighestVersion(List<string> versions)
    {
        if (versions.Count == 0)
        {
            return "-";
        }

        return versions
            .OrderByDescending(x => ParseVersionWeight(x))
            .ThenByDescending(x => x, StringComparer.OrdinalIgnoreCase)
            .First();
    }

    private static decimal ParseVersionWeight(string version)
    {
        var text = version.Trim().TrimStart('V', 'v');
        return decimal.TryParse(text, out var value) ? value : 0m;
    }

    private static string InferCategory(string productName)
    {
        if (productName.Contains("AI", StringComparison.OrdinalIgnoreCase) || productName.Contains("质控", StringComparison.OrdinalIgnoreCase))
        {
            return "AI";
        }

        if (productName.Contains("病历", StringComparison.OrdinalIgnoreCase) || productName.Contains("EMR", StringComparison.OrdinalIgnoreCase))
        {
            return "EMR";
        }

        if (productName.Contains("移动", StringComparison.OrdinalIgnoreCase) || productName.Contains("云", StringComparison.OrdinalIgnoreCase))
        {
            return "移动";
        }

        if (productName.Contains("归档", StringComparison.OrdinalIgnoreCase)
            || productName.Contains("随访", StringComparison.OrdinalIgnoreCase)
            || productName.Contains("管理", StringComparison.OrdinalIgnoreCase))
        {
            return "管理";
        }

        return "临床辅助";
    }

    private static string InferStatus(IEnumerable<string> statuses)
    {
        var statusList = statuses.ToList();
        if (statusList.Any(x => x.Contains("合同已签署", StringComparison.OrdinalIgnoreCase)
                             || x.Contains("免费维护期", StringComparison.OrdinalIgnoreCase)
                             || x.Contains("有偿维护", StringComparison.OrdinalIgnoreCase)
                             || x.Contains("维保期内", StringComparison.OrdinalIgnoreCase)
                             || x.Contains("质保期内", StringComparison.OrdinalIgnoreCase)
                             || x.Contains("签署中", StringComparison.OrdinalIgnoreCase)))
        {
            return "运行中";
        }

        if (statusList.Any(x => x.Contains("未终验", StringComparison.OrdinalIgnoreCase)
                             || x.Contains("未验收", StringComparison.OrdinalIgnoreCase)
                             || x.Contains("试运行", StringComparison.OrdinalIgnoreCase)))
        {
            return "试运行";
        }

        if (statusList.Any(x => x.Contains("停止", StringComparison.OrdinalIgnoreCase)
                             || x.Contains("停保", StringComparison.OrdinalIgnoreCase)
                             || x.Contains("脱保", StringComparison.OrdinalIgnoreCase)
                             || x.Contains("超期", StringComparison.OrdinalIgnoreCase)))
        {
            return "已停用";
        }

        return "试运行";
    }
}
