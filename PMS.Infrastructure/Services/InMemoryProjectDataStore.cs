using PMS.Domain.Entities;
using System.Text.RegularExpressions;

namespace PMS.Infrastructure.Services;

public sealed class ProjectDataCleanupStats
{
    public int InputCount { get; set; }
    public int KeptCount { get; set; }
    public int RemovedInvalidCount { get; set; }
    public int DeduplicatedCount { get; set; }
}

public static class InMemoryProjectDataStore
{
    private static readonly object SyncRoot = new();
    private const string StateKey = "projects";
    private static readonly Dictionary<string, string> OfficialHospitalNameAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["南通口腔医院"] = "南通市口腔医院",
        ["西安市大兴医院"] = "西安大兴医院",
        ["西安红庙坡医院"] = "西安大兴医院",
        ["阿克苏市第二人民医院"] = "阿克苏地区第二人民医院"
    };
    private static readonly string[] AdministrativeTokens = ["省", "市", "地区"];
    private static readonly char[] ProductSeparators = [',', '，', '、', ';', '；', '+'];

    private static readonly List<ProjectEntity> ProjectsInternal =
        SqliteJsonStore.LoadOrSeed(StateKey, BuildSeedData);

    public static IReadOnlyList<ProjectEntity> Projects
    {
        get
        {
            lock (SyncRoot)
            {
                return ProjectsInternal
                    .Select(x => new ProjectEntity
                    {
                        Id = x.Id,
                        HospitalName = x.HospitalName,
                        ProductName = x.ProductName,
                        Province = x.Province,
                        GroupName = x.GroupName,
                        HospitalLevel = x.HospitalLevel,
                        ContractStatus = x.ContractStatus,
                        MaintenanceAmount = x.MaintenanceAmount,
                        OverdueDays = x.OverdueDays
                    })
                    .ToList();
            }
        }
    }

    public static void SyncHospitalInfo(string oldHospitalName, string newHospitalName, string newProvince)
    {
        lock (SyncRoot)
        {
            foreach (var project in ProjectsInternal.Where(x => x.HospitalName == oldHospitalName))
            {
                project.HospitalName = newHospitalName;
                project.Province = newProvince;
            }

            Persist();
        }
    }

    public static void RemoveByHospitalName(string hospitalName)
    {
        lock (SyncRoot)
        {
            ProjectsInternal.RemoveAll(x => x.HospitalName == hospitalName);
            Persist();
        }
    }

    public static void ReplaceAll(IReadOnlyList<ProjectEntity> projects)
    {
        lock (SyncRoot)
        {
            var normalized = NormalizeProjects(projects, out _);
            ReplaceInternal(normalized);
        }
    }

    public static ProjectDataCleanupStats NormalizeCurrentData()
    {
        lock (SyncRoot)
        {
            var normalized = NormalizeProjects(ProjectsInternal, out var stats);
            ReplaceInternal(normalized);
            return stats;
        }
    }

    public static ProjectDataCleanupStats ReplaceAllAndNormalize(IReadOnlyList<ProjectEntity> rawProjects)
    {
        lock (SyncRoot)
        {
            var normalized = NormalizeProjects(rawProjects, out var stats);
            ReplaceInternal(normalized);
            return stats;
        }
    }

    public static int CountProjectsByGroup(string groupName)
    {
        lock (SyncRoot)
        {
            return ProjectsInternal.Count(x => x.GroupName == groupName);
        }
    }

    public static int CountOverdueProjectsByGroup(string groupName)
    {
        lock (SyncRoot)
        {
            return ProjectsInternal.Count(x => x.GroupName == groupName && x.OverdueDays > 0);
        }
    }

    public static int CountProjectsForPersonnel(string personName, string groupName)
    {
        lock (SyncRoot)
        {
            return ProjectsInternal.Count(x => IsMatchedGroup(x.GroupName, personName, groupName));
        }
    }

    public static int CountOverdueProjectsForPersonnel(string personName, string groupName)
    {
        lock (SyncRoot)
        {
            return ProjectsInternal.Count(x => IsMatchedGroup(x.GroupName, personName, groupName) && x.OverdueDays > 0);
        }
    }

    public static int CountDistinctHospitals()
    {
        lock (SyncRoot)
        {
            return ProjectsInternal
                .Select(x => x.HospitalName)
                .Distinct()
                .Count();
        }
    }

    public static int ReassignHospitalProductOwner(string hospitalName, string productName, string groupName)
    {
        if (string.IsNullOrWhiteSpace(hospitalName) || string.IsNullOrWhiteSpace(productName) || string.IsNullOrWhiteSpace(groupName))
        {
            return 0;
        }

        lock (SyncRoot)
        {
            var normalizedHospital = NormalizeHospitalName(hospitalName);
            var normalizedProduct = NormalizeProductName(productName);
            var normalizedGroup = groupName.Trim();

            var affected = 0;
            foreach (var item in ProjectsInternal.Where(x =>
                         x.HospitalName.Equals(normalizedHospital, StringComparison.OrdinalIgnoreCase)
                         && x.ProductName.Equals(normalizedProduct, StringComparison.OrdinalIgnoreCase)))
            {
                item.GroupName = normalizedGroup;
                affected++;
            }

            if (affected > 0)
            {
                Persist();
            }

            return affected;
        }
    }

    private static bool IsMatchedGroup(string projectGroup, string personName, string groupName)
    {
        if (string.IsNullOrWhiteSpace(projectGroup))
        {
            return false;
        }

        var candidateSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            personName.Trim(),
            groupName.Trim(),
            NormalizeGroupName(groupName),
            NormalizeGroupName(personName)
        };

        var normalizedProjectGroup = NormalizeGroupName(projectGroup);
        return candidateSet.Contains(projectGroup) || candidateSet.Contains(normalizedProjectGroup);
    }

    private static string NormalizeGroupName(string value)
    {
        return value.Trim().Replace("组", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static void Persist()
    {
        SqliteJsonStore.Save(StateKey, ProjectsInternal);
    }

    private static void ReplaceInternal(IReadOnlyList<ProjectEntity> projects)
    {
        ProjectsInternal.Clear();

        var nextId = 1;
        foreach (var project in projects)
        {
            ProjectsInternal.Add(new ProjectEntity
            {
                Id = nextId++,
                HospitalName = project.HospitalName,
                    ProductName = project.ProductName,
                Province = project.Province,
                GroupName = project.GroupName,
                    HospitalLevel = project.HospitalLevel,
                ContractStatus = project.ContractStatus,
                MaintenanceAmount = project.MaintenanceAmount,
                OverdueDays = project.OverdueDays
            });
        }

        Persist();
    }

    private static IReadOnlyList<ProjectEntity> NormalizeProjects(
        IEnumerable<ProjectEntity> source,
        out ProjectDataCleanupStats stats)
    {
        var sourceList = source.ToList();
        var cleaned = new List<ProjectEntity>();
        var hospitalCanonicalMap = BuildHospitalCanonicalMap(sourceList);
        var validProvinces = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "北京", "天津", "上海", "重庆", "河北", "山西", "辽宁", "吉林", "黑龙江", "江苏", "浙江", "安徽", "福建", "江西",
            "山东", "河南", "湖北", "湖南", "广东", "海南", "四川", "贵州", "云南", "陕西", "甘肃", "青海", "台湾", "内蒙古",
            "广西", "西藏", "宁夏", "新疆", "香港", "澳门"
        };

        foreach (var raw in sourceList)
        {
            var hospitalName = NormalizeHospitalName(raw.HospitalName);
            if (hospitalCanonicalMap.TryGetValue(hospitalName, out var canonicalHospitalName))
            {
                hospitalName = canonicalHospitalName;
            }

            if (!IsValidHospitalName(hospitalName))
            {
                continue;
            }

            var province = NormalizeProvince(raw.Province, validProvinces);
            if (string.IsNullOrWhiteSpace(province))
            {
                continue;
            }

            var groupName = string.IsNullOrWhiteSpace(raw.GroupName) ? "未分组" : raw.GroupName.Trim();
            var productName = NormalizeProductName(raw.ProductName);
            if (string.IsNullOrWhiteSpace(productName))
            {
                continue;
            }

            var hospitalLevel = NormalizeHospitalLevel(raw.HospitalLevel);
            var contractStatus = NormalizeContractStatus(raw.ContractStatus);
            var overdueDays = Math.Clamp(raw.OverdueDays, 0, 3650);
            var amount = raw.MaintenanceAmount < 0 ? 0 : Math.Min(raw.MaintenanceAmount, 2000m);

            cleaned.Add(new ProjectEntity
            {
                HospitalName = hospitalName,
                ProductName = productName,
                Province = province,
                GroupName = groupName,
                HospitalLevel = hospitalLevel,
                ContractStatus = contractStatus,
                MaintenanceAmount = amount,
                OverdueDays = overdueDays
            });
        }

        var grouped = cleaned
            .GroupBy(x => $"{x.HospitalName}||{x.ProductName}", StringComparer.OrdinalIgnoreCase)
            .Select(g => MergeProjectRows(g.ToList()))
            .OrderBy(x => x.Province)
            .ThenBy(x => x.HospitalName)
            .ToList();

        stats = new ProjectDataCleanupStats
        {
            InputCount = sourceList.Count,
            KeptCount = grouped.Count,
            RemovedInvalidCount = sourceList.Count - cleaned.Count,
            DeduplicatedCount = cleaned.Count - grouped.Count
        };

        return grouped;
    }

    private static ProjectEntity MergeProjectRows(List<ProjectEntity> rows)
    {
        var province = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.Province))
            .GroupBy(x => x.Province)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => g.Key)
            .FirstOrDefault() ?? "未知";

        var groupName = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.GroupName))
            .GroupBy(x => x.GroupName)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => g.Key)
            .FirstOrDefault() ?? "未分组";

        var hospitalLevel = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.HospitalLevel))
            .GroupBy(x => x.HospitalLevel)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => g.Key)
            .FirstOrDefault() ?? "未评级";

        var status = rows
            .Select(x => x.ContractStatus)
            .OrderByDescending(GetStatusPriority)
            .FirstOrDefault() ?? "未知";

        return new ProjectEntity
        {
            HospitalName = rows[0].HospitalName,
            ProductName = rows[0].ProductName,
            Province = province,
            GroupName = groupName,
            HospitalLevel = hospitalLevel,
            ContractStatus = status,
            MaintenanceAmount = rows.Max(x => x.MaintenanceAmount),
            OverdueDays = rows.Max(x => x.OverdueDays)
        };
    }

    private static int GetStatusPriority(string status)
    {
        if (status.Contains("超期", StringComparison.OrdinalIgnoreCase)) return 5;
        if (status.Contains("停止", StringComparison.OrdinalIgnoreCase)) return 4;
        if (status.Contains("免费", StringComparison.OrdinalIgnoreCase)) return 3;
        if (status.Contains("签署", StringComparison.OrdinalIgnoreCase)) return 2;
        if (status.Contains("未知", StringComparison.OrdinalIgnoreCase)) return 1;
        return 0;
    }

    private static Dictionary<string, string> BuildHospitalCanonicalMap(IReadOnlyList<ProjectEntity> source)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var normalizedNames = source
            .Select(x => NormalizeHospitalName(x.HospitalName))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (normalizedNames.Count == 0)
        {
            return result;
        }

        var frequencies = normalizedNames
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var groups = frequencies.Keys
            .GroupBy(BuildAdministrativeLooseKey)
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var names = group.ToList();
            var canMerge = true;

            for (var i = 0; i < names.Count && canMerge; i++)
            {
                for (var j = i + 1; j < names.Count && canMerge; j++)
                {
                    if (!AreAdministrativeVariants(names[i], names[j]))
                    {
                        canMerge = false;
                    }
                }
            }

            if (!canMerge)
            {
                continue;
            }

            var canonical = names
                .OrderByDescending(x => frequencies.TryGetValue(x, out var count) ? count : 0)
                .ThenByDescending(x => x.Length)
                .ThenBy(x => x, StringComparer.OrdinalIgnoreCase)
                .First();

            foreach (var name in names)
            {
                result[name] = canonical;
            }
        }

        return result;
    }

    private static bool AreAdministrativeVariants(string left, string right)
    {
        if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(
            BuildAdministrativeLooseKey(left),
            BuildAdministrativeLooseKey(right),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildAdministrativeLooseKey(string hospitalName)
    {
        var key = hospitalName;
        foreach (var token in AdministrativeTokens)
        {
            key = key.Replace(token, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return key;
    }

    private static string NormalizeHospitalName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value
            .Trim()
            .Replace("\t", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("（", "(", StringComparison.Ordinal)
            .Replace("）", ")", StringComparison.Ordinal);

        return OfficialHospitalNameAliases.TryGetValue(normalized, out var officialName)
            ? officialName
            : normalized;
    }

    private static bool IsValidHospitalName(string hospitalName)
    {
        if (string.IsNullOrWhiteSpace(hospitalName) || hospitalName.Length < 4 || hospitalName.Length > 80)
        {
            return false;
        }

        var invalidKeywords = new[] { "汇总", "合计", "总计", "sheet", "工作表", "项目名称", "列表", "统计" };
        if (invalidKeywords.Any(x => hospitalName.Contains(x, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var hospitalKeywords = new[] { "医院", "卫生院", "中医院", "妇幼", "医学院", "附属" };
        return hospitalKeywords.Any(x => hospitalName.Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeProvince(string? raw, HashSet<string> validProvinces)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var province = raw.Trim();
        province = province
            .Replace("省", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("市", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("自治区", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("壮族", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("回族", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("维吾尔", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (province == "内蒙古") return province;
        if (province == "新疆") return province;
        if (province == "广西") return province;
        if (province == "宁夏") return province;
        if (province == "西藏") return province;

        return validProvinces.Contains(province) ? province : string.Empty;
    }

    private static string NormalizeContractStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return "未知";

        var value = status.Trim();
        if (value.Contains("超期", StringComparison.OrdinalIgnoreCase)) return "超期未签署";
        if (value.Contains("停止", StringComparison.OrdinalIgnoreCase)) return "停止维护";
        if (value.Contains("免费", StringComparison.OrdinalIgnoreCase)) return "免费维护期";
        if (value.Contains("签署", StringComparison.OrdinalIgnoreCase) || value.Contains("签订", StringComparison.OrdinalIgnoreCase)) return "合同已签署";

        return "未知";
    }

    private static string NormalizeProductName(string? productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return "通用产品";
        }

        var normalizedRaw = productName
            .Trim()
            .Replace("（", "(", StringComparison.Ordinal)
            .Replace("）", ")", StringComparison.Ordinal)
            .Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

        var tokens = normalizedRaw
            .Split(ProductSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeProductToken)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (tokens.Count == 0)
        {
            return "通用产品";
        }

        if (tokens.Count == 1)
        {
            return tokens[0];
        }

        return string.Join("、", tokens.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
    }

    private static string NormalizeProductToken(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return string.Empty;
        }

        var token = rawToken.Trim();
        token = Regex.Replace(token, @"\s+", string.Empty);
        token = Regex.Replace(token, @"[/／]\d+个手术间.*$", string.Empty, RegexOptions.IgnoreCase);
        token = Regex.Replace(token, @"[/／]\d+个恢复室床位.*$", string.Empty, RegexOptions.IgnoreCase);
        token = Regex.Replace(token, @"[/／]共\d+个智能数据采集盒.*$", string.Empty, RegexOptions.IgnoreCase);

        if (IsOperationalDescriptorToken(token))
        {
            return string.Empty;
        }

        if (token.Contains("CDSS", StringComparison.OrdinalIgnoreCase)
            || token.Contains("临床辅助决策支持系统", StringComparison.OrdinalIgnoreCase))
        {
            return "CDSS";
        }

        if (token.Contains("AI内涵质控", StringComparison.OrdinalIgnoreCase)
            || token.Contains("AI质控", StringComparison.OrdinalIgnoreCase)
            || token.Contains("病历内涵质控", StringComparison.OrdinalIgnoreCase))
        {
            return "AI内涵质控";
        }

        if (token.Contains("心电", StringComparison.OrdinalIgnoreCase))
        {
            return BuildProductWithVersion("心电信息管理系统", token, allowImplicitDecimalVersion: true);
        }

        if (token.Contains("手麻", StringComparison.OrdinalIgnoreCase)
            || token.Contains("手术麻醉", StringComparison.OrdinalIgnoreCase))
        {
            return BuildProductWithVersion("手术麻醉系统", token, allowImplicitDecimalVersion: true);
        }

        if (token.Contains("门诊", StringComparison.OrdinalIgnoreCase)
            && token.Contains("病历", StringComparison.OrdinalIgnoreCase))
        {
            return BuildProductWithVersion("门诊电子病历", token, allowImplicitDecimalVersion: true);
        }

        if (token.Contains("住院", StringComparison.OrdinalIgnoreCase)
            && token.Contains("病历", StringComparison.OrdinalIgnoreCase))
        {
            return BuildProductWithVersion("住院电子病历", token, allowImplicitDecimalVersion: true);
        }

        if (token.Contains("护理", StringComparison.OrdinalIgnoreCase)
            && token.Contains("病历", StringComparison.OrdinalIgnoreCase))
        {
            return BuildProductWithVersion("护理电子病历", token, allowImplicitDecimalVersion: true);
        }

        if (token.Contains("急诊", StringComparison.OrdinalIgnoreCase)
            && token.Contains("病历", StringComparison.OrdinalIgnoreCase))
        {
            return BuildProductWithVersion("急诊电子病历", token, allowImplicitDecimalVersion: true);
        }

        if (token.Contains("产科", StringComparison.OrdinalIgnoreCase)
            && token.Contains("病历", StringComparison.OrdinalIgnoreCase))
        {
            return BuildProductWithVersion("产科电子病历", token, allowImplicitDecimalVersion: true);
        }

        if (token.Contains("电子病历", StringComparison.OrdinalIgnoreCase)
            || string.Equals(token, "病历", StringComparison.OrdinalIgnoreCase))
        {
            return BuildProductWithVersion("电子病历", token, allowImplicitDecimalVersion: true);
        }

        return token;
    }

    private static bool IsOperationalDescriptorToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return true;
        }

        if (Regex.IsMatch(token, @"^\d+(\.\d+)?$"))
        {
            return true;
        }

        if (Regex.IsMatch(token, @"^\d+个"))
        {
            return true;
        }

        var descriptorKeywords = new[]
        {
            "手术间", "恢复室", "床位", "智能数据采集盒", "升级改造", "维保"
        };

        return descriptorKeywords.Any(keyword => token.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildProductWithVersion(string canonicalName, string source, bool allowImplicitDecimalVersion)
    {
        var version = ExtractProductVersion(source, allowImplicitDecimalVersion);
        return string.IsNullOrWhiteSpace(version)
            ? canonicalName
            : $"{canonicalName}{version}";
    }

    private static string ExtractProductVersion(string source, bool allowImplicitDecimalVersion)
    {
        var explicitVersionMatch = Regex.Match(source, @"[Vv]\s*(\d+(?:\.\d+)*)", RegexOptions.IgnoreCase);
        if (explicitVersionMatch.Success)
        {
            return $"V{explicitVersionMatch.Groups[1].Value}";
        }

        if (!allowImplicitDecimalVersion)
        {
            return string.Empty;
        }

        var decimalVersionMatch = Regex.Match(source, @"(?<!\d)(\d+\.\d+)(?!\d)");
        return decimalVersionMatch.Success
            ? $"V{decimalVersionMatch.Groups[1].Value}"
            : string.Empty;
    }

    private static string NormalizeHospitalLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return "未评级";
        }

        var value = level.Trim();
        if (value.Contains("三", StringComparison.OrdinalIgnoreCase)) return "三级";
        if (value.Contains("二", StringComparison.OrdinalIgnoreCase)) return "二级";
        if (value.Contains("一", StringComparison.OrdinalIgnoreCase)) return "一级";
        return value;
    }

    private static List<ProjectEntity> BuildSeedData()
    {
        return
        [
            new() { Id = 1, HospitalName = "北京协和医院", ProductName = "住院电子病历V6", Province = "北京", GroupName = "何道飞", HospitalLevel = "三级", ContractStatus = "合同已签署", MaintenanceAmount = 28.5m, OverdueDays = 0 },
            new() { Id = 2, HospitalName = "南京鼓楼医院", ProductName = "临床路径V6", Province = "江苏", GroupName = "张茹", HospitalLevel = "三级", ContractStatus = "超期未签署", MaintenanceAmount = 18m, OverdueDays = 132 },
            new() { Id = 3, HospitalName = "四川省人民医院", ProductName = "CDSS", Province = "四川", GroupName = "李贝", HospitalLevel = "三级", ContractStatus = "免费维护期", MaintenanceAmount = 12m, OverdueDays = 0 },
            new() { Id = 4, HospitalName = "中南大学湘雅医院", ProductName = "病案归档", Province = "湖南", GroupName = "舒高成", HospitalLevel = "三级", ContractStatus = "维护合同已签署", MaintenanceAmount = 22m, OverdueDays = 0 },
            new() { Id = 5, HospitalName = "西安交通大学第一附属医院", ProductName = "AI内涵质控", Province = "陕西", GroupName = "陈宇", HospitalLevel = "三级", ContractStatus = "维护超期未签署", MaintenanceAmount = 15m, OverdueDays = 45 },
            new() { Id = 6, HospitalName = "复旦大学附属中山医院", ProductName = "住院电子病历V6", Province = "上海", GroupName = "王可可", HospitalLevel = "三级", ContractStatus = "合同已签署", MaintenanceAmount = 25m, OverdueDays = 0 },
            new() { Id = 7, HospitalName = "广东省人民医院", ProductName = "临床路径V6", Province = "广东", GroupName = "唐广才", HospitalLevel = "三级", ContractStatus = "停止维护", MaintenanceAmount = 8m, OverdueDays = 0 },
            new() { Id = 8, HospitalName = "重庆医科大学附属第一医院", ProductName = "CDSS", Province = "重庆", GroupName = "张浩阳", HospitalLevel = "三级", ContractStatus = "超期未签署", MaintenanceAmount = 16m, OverdueDays = 210 },
            new() { Id = 9, HospitalName = "福建省立医院", ProductName = "病案归档", Province = "福建", GroupName = "姚云龙", HospitalLevel = "三级", ContractStatus = "超期未签署", MaintenanceAmount = 19m, OverdueDays = 76 },
            new() { Id = 10, HospitalName = "河南省人民医院", ProductName = "AI内涵质控", Province = "河南", GroupName = "孙强", HospitalLevel = "二级", ContractStatus = "超期未签署", MaintenanceAmount = 14m, OverdueDays = 12 }
        ];
    }
}
