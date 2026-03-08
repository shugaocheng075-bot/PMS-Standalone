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
    private const string TableName = "Projects";
    private const string LegacyJsonKey = "projects";
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
        SqliteTableStore.LoadAll<ProjectEntity>(TableName, LegacyJsonKey);

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
                        SerialNumber = x.SerialNumber,
                        OpportunityNumber = x.OpportunityNumber,
                        HospitalName = x.HospitalName,
                        ProductName = x.ProductName,
                        Province = x.Province,
                        GroupName = x.GroupName,
                        SalesName = x.SalesName,
                        MaintenancePersonName = x.MaintenancePersonName,
                        AfterSalesStartDate = x.AfterSalesStartDate,
                        AfterSalesEndDate = x.AfterSalesEndDate,
                        HospitalLevel = x.HospitalLevel,
                        ContractStatus = x.ContractStatus,
                        ContractValidityStatus = x.ContractValidityStatus,
                        MaintenanceAmount = x.MaintenanceAmount,
                        OverdueDays = x.OverdueDays,
                        ImplementationStatus = x.ImplementationStatus,
                        WorkHoursManDays = x.WorkHoursManDays,
                        PersonnelCount = x.PersonnelCount,
                        Personnel1 = x.Personnel1,
                        Personnel2 = x.Personnel2,
                        Personnel3 = x.Personnel3,
                        Personnel4 = x.Personnel4,
                        Personnel5 = x.Personnel5,
                        AfterSalesProjectType = x.AfterSalesProjectType,
                        Remarks = x.Remarks,
                        ServiceArea = x.ServiceArea,
                        City = x.City,
                        Points = x.Points,
                        SalesAmount = x.SalesAmount,
                        AnnualOutput = x.AnnualOutput,
                        StationLocation = x.StationLocation,
                        IsStationedOnsite = x.IsStationedOnsite,
                        StationedCount = x.StationedCount,
                        AcceptanceDate = x.AcceptanceDate
                    })
                    .ToList();
            }
        }
    }

    public static ProjectEntity? GetById(long id)
    {
        lock (SyncRoot)
        {
            var found = ProjectsInternal.FirstOrDefault(x => x.Id == id);
            if (found is null)
            {
                return null;
            }

            return new ProjectEntity
            {
                Id = found.Id,
                SerialNumber = found.SerialNumber,
                OpportunityNumber = found.OpportunityNumber,
                HospitalName = found.HospitalName,
                ProductName = found.ProductName,
                Province = found.Province,
                GroupName = found.GroupName,
                SalesName = found.SalesName,
                MaintenancePersonName = found.MaintenancePersonName,
                AfterSalesStartDate = found.AfterSalesStartDate,
                AfterSalesEndDate = found.AfterSalesEndDate,
                HospitalLevel = found.HospitalLevel,
                ContractStatus = found.ContractStatus,
                ContractValidityStatus = found.ContractValidityStatus,
                MaintenanceAmount = found.MaintenanceAmount,
                OverdueDays = found.OverdueDays,
                ImplementationStatus = found.ImplementationStatus,
                WorkHoursManDays = found.WorkHoursManDays,
                PersonnelCount = found.PersonnelCount,
                Personnel1 = found.Personnel1,
                Personnel2 = found.Personnel2,
                Personnel3 = found.Personnel3,
                Personnel4 = found.Personnel4,
                Personnel5 = found.Personnel5,
                AfterSalesProjectType = found.AfterSalesProjectType,
                Remarks = found.Remarks,
                ServiceArea = found.ServiceArea,
                City = found.City,
                Points = found.Points,
                SalesAmount = found.SalesAmount,
                AnnualOutput = found.AnnualOutput,
                StationLocation = found.StationLocation,
                IsStationedOnsite = found.IsStationedOnsite,
                StationedCount = found.StationedCount,
                AcceptanceDate = found.AcceptanceDate
            };
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

    public static void ReplaceAllRaw(IReadOnlyList<ProjectEntity> projects)
    {
        lock (SyncRoot)
        {
            ReplaceInternal(projects);
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

    public static int BatchUpdateProjects(
        IReadOnlyCollection<long> projectIds,
        string? contractStatus,
        string? groupName,
        string? salesName,
        string? maintenancePersonName,
        string? hospitalLevel)
    {
        if (projectIds.Count == 0)
        {
            return 0;
        }

        lock (SyncRoot)
        {
            var idSet = projectIds.ToHashSet();
            var affected = 0;

            foreach (var item in ProjectsInternal.Where(x => idSet.Contains(x.Id)))
            {
                var changed = false;

                if (!string.IsNullOrWhiteSpace(contractStatus))
                {
                    var normalizedContractStatus = NormalizeContractStatus(contractStatus);
                    if (!string.Equals(item.ContractStatus, normalizedContractStatus, StringComparison.Ordinal))
                    {
                        item.ContractStatus = normalizedContractStatus;
                        changed = true;
                    }
                }

                if (!string.IsNullOrWhiteSpace(groupName))
                {
                    var normalizedGroupName = groupName.Trim();
                    if (!string.Equals(item.GroupName, normalizedGroupName, StringComparison.Ordinal))
                    {
                        item.GroupName = normalizedGroupName;
                        changed = true;
                    }
                }

                if (!string.IsNullOrWhiteSpace(salesName))
                {
                    var normalizedSalesName = NormalizeSalesName(salesName);
                    if (!string.Equals(item.SalesName, normalizedSalesName, StringComparison.Ordinal))
                    {
                        item.SalesName = normalizedSalesName;
                        changed = true;
                    }
                }

                if (!string.IsNullOrWhiteSpace(maintenancePersonName))
                {
                    var normalizedMaintenancePersonName = NormalizeMaintenancePersonName(maintenancePersonName);
                    if (!string.Equals(item.MaintenancePersonName, normalizedMaintenancePersonName, StringComparison.Ordinal))
                    {
                        item.MaintenancePersonName = normalizedMaintenancePersonName;
                        changed = true;
                    }
                }

                if (!string.IsNullOrWhiteSpace(hospitalLevel))
                {
                    var normalizedHospitalLevel = NormalizeHospitalLevel(hospitalLevel);
                    if (!string.Equals(item.HospitalLevel, normalizedHospitalLevel, StringComparison.Ordinal))
                    {
                        item.HospitalLevel = normalizedHospitalLevel;
                        changed = true;
                    }
                }

                if (changed)
                {
                    affected++;
                }
            }

            if (affected > 0)
            {
                Persist();
            }

            return affected;
        }
    }

    public static int DeleteProjects(IReadOnlyCollection<long> projectIds)
    {
        if (projectIds.Count == 0)
        {
            return 0;
        }

        lock (SyncRoot)
        {
            var idSet = projectIds.ToHashSet();
            var removed = ProjectsInternal.RemoveAll(x => idSet.Contains(x.Id));
            if (removed > 0)
            {
                Persist();
            }

            return removed;
        }
    }

    /// <summary>
    /// 按 Id 更新单个项目的任意字段并持久化。
    /// </summary>
    public static void UpdateSingleProject(long projectId, Action<ProjectEntity> updater)
    {
        lock (SyncRoot)
        {
            var project = ProjectsInternal.FirstOrDefault(p => p.Id == projectId);
            if (project is null) return;
            updater(project);
            Persist();
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
        SqliteTableStore.ReplaceAll(TableName, ProjectsInternal);
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
                SerialNumber = project.SerialNumber,
                OpportunityNumber = project.OpportunityNumber,
                HospitalName = project.HospitalName,
                ProductName = project.ProductName,
                Province = project.Province,
                GroupName = project.GroupName,
                SalesName = project.SalesName,
                MaintenancePersonName = project.MaintenancePersonName,
                AfterSalesStartDate = project.AfterSalesStartDate,
                AfterSalesEndDate = project.AfterSalesEndDate,
                HospitalLevel = project.HospitalLevel,
                ContractStatus = project.ContractStatus,
                ContractValidityStatus = project.ContractValidityStatus,
                MaintenanceAmount = project.MaintenanceAmount,
                OverdueDays = project.OverdueDays,
                ImplementationStatus = project.ImplementationStatus,
                WorkHoursManDays = project.WorkHoursManDays,
                PersonnelCount = project.PersonnelCount,
                Personnel1 = project.Personnel1,
                Personnel2 = project.Personnel2,
                Personnel3 = project.Personnel3,
                Personnel4 = project.Personnel4,
                Personnel5 = project.Personnel5,
                AfterSalesProjectType = project.AfterSalesProjectType,
                Remarks = project.Remarks,
                ServiceArea = project.ServiceArea,
                City = project.City,
                Points = project.Points,
                SalesAmount = project.SalesAmount,
                AnnualOutput = project.AnnualOutput,
                StationLocation = project.StationLocation,
                IsStationedOnsite = project.IsStationedOnsite,
                StationedCount = project.StationedCount,
                AcceptanceDate = project.AcceptanceDate
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
        var validRawCount = 0;
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
            var salesName = NormalizeSalesName(raw.SalesName);
            var maintenancePersonName = NormalizeMaintenancePersonName(raw.MaintenancePersonName);
            var afterSalesStartDate = NormalizeDateText(raw.AfterSalesStartDate);
            var afterSalesEndDate = NormalizeDateText(raw.AfterSalesEndDate);
            var productNames = NormalizeProductNames(raw.ProductName);
            if (productNames.Count == 0)
            {
                continue;
            }

            validRawCount++;

            var hospitalLevel = NormalizeHospitalLevel(raw.HospitalLevel);
            var contractStatus = NormalizeContractStatus(raw.ContractStatus);
            var overdueDays = Math.Clamp(raw.OverdueDays, 0, 3650);
            var amount = raw.MaintenanceAmount < 0 ? 0 : Math.Min(raw.MaintenanceAmount, 2000m);

            foreach (var productName in productNames)
            {
                cleaned.Add(new ProjectEntity
                {
                    SerialNumber = raw.SerialNumber,
                    HospitalName = hospitalName,
                    ProductName = productName,
                    Province = province,
                    GroupName = groupName,
                    SalesName = salesName,
                    MaintenancePersonName = maintenancePersonName,
                    AfterSalesStartDate = afterSalesStartDate,
                    AfterSalesEndDate = afterSalesEndDate,
                    HospitalLevel = hospitalLevel,
                    ContractStatus = contractStatus,
                    MaintenanceAmount = amount,
                    OverdueDays = overdueDays,
                    OpportunityNumber = raw.OpportunityNumber,
                    ImplementationStatus = raw.ImplementationStatus,
                    WorkHoursManDays = raw.WorkHoursManDays,
                    PersonnelCount = raw.PersonnelCount,
                    Personnel1 = raw.Personnel1,
                    Personnel2 = raw.Personnel2,
                    Personnel3 = raw.Personnel3,
                    Personnel4 = raw.Personnel4,
                    Personnel5 = raw.Personnel5,
                    AfterSalesProjectType = raw.AfterSalesProjectType,
                    Remarks = raw.Remarks,
                    ServiceArea = raw.ServiceArea,
                    City = raw.City,
                    Points = raw.Points,
                    SalesAmount = raw.SalesAmount,
                    AnnualOutput = raw.AnnualOutput,
                    StationLocation = raw.StationLocation,
                    IsStationedOnsite = raw.IsStationedOnsite,
                    StationedCount = raw.StationedCount,
                    AcceptanceDate = raw.AcceptanceDate
                });
            }
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
            RemovedInvalidCount = sourceList.Count - validRawCount,
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

        var salesName = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.SalesName))
            .GroupBy(x => x.SalesName)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => g.Key)
            .FirstOrDefault() ?? "未知";

        var maintenancePersonName = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.MaintenancePersonName))
            .GroupBy(x => x.MaintenancePersonName)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => g.Key)
            .FirstOrDefault() ?? "未知";

        var afterSalesStartDate = rows
            .Select(x => x.AfterSalesStartDate)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .OrderBy(x => x, StringComparer.Ordinal)
            .FirstOrDefault() ?? string.Empty;

        var afterSalesEndDate = rows
            .Select(x => x.AfterSalesEndDate)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .OrderByDescending(x => x, StringComparer.Ordinal)
            .FirstOrDefault() ?? string.Empty;

        var status = rows
            .Select(x => x.ContractStatus)
            .OrderByDescending(GetStatusPriority)
            .FirstOrDefault() ?? "未知";

        var opportunityNumber = rows
            .Select(x => x.OpportunityNumber)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

        var serialNumber = rows
            .Select(x => x.SerialNumber)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

        var implementationStatus = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.ImplementationStatus))
            .GroupBy(x => x.ImplementationStatus)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => g.Key)
            .FirstOrDefault() ?? string.Empty;

        var afterSalesProjectType = rows
            .Select(x => x.AfterSalesProjectType)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

        var remarks = rows
            .Select(x => x.Remarks)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

        var serviceArea = rows
            .Select(x => x.ServiceArea)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

        var city = rows
            .Select(x => x.City)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

        var points = rows
            .Select(x => x.Points)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

        var stationLocation = rows
            .Select(x => x.StationLocation)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

        var isStationedOnsite = rows
            .Select(x => x.IsStationedOnsite)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

        var stationedCount = rows
            .Select(x => x.StationedCount)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

        var acceptanceDate = rows
            .Select(x => x.AcceptanceDate)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

        return new ProjectEntity
        {
            SerialNumber = serialNumber,
            HospitalName = rows[0].HospitalName,
            ProductName = rows[0].ProductName,
            Province = province,
            GroupName = groupName,
            SalesName = salesName,
            MaintenancePersonName = maintenancePersonName,
            AfterSalesStartDate = afterSalesStartDate,
            AfterSalesEndDate = afterSalesEndDate,
            HospitalLevel = hospitalLevel,
            ContractStatus = status,
            MaintenanceAmount = rows.Max(x => x.MaintenanceAmount),
            OverdueDays = rows.Max(x => x.OverdueDays),
            OpportunityNumber = opportunityNumber,
            ImplementationStatus = implementationStatus,
            WorkHoursManDays = rows.Max(x => x.WorkHoursManDays),
            PersonnelCount = rows.Max(x => x.PersonnelCount),
            Personnel1 = rows.Select(x => x.Personnel1).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty,
            Personnel2 = rows.Select(x => x.Personnel2).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty,
            Personnel3 = rows.Select(x => x.Personnel3).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty,
            Personnel4 = rows.Select(x => x.Personnel4).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty,
            Personnel5 = rows.Select(x => x.Personnel5).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty,
            AfterSalesProjectType = afterSalesProjectType,
            Remarks = remarks,
            ServiceArea = serviceArea,
            City = city,
            Points = points,
            SalesAmount = rows.Max(x => x.SalesAmount),
            AnnualOutput = rows.Max(x => x.AnnualOutput),
            StationLocation = stationLocation,
            IsStationedOnsite = isStationedOnsite,
            StationedCount = stationedCount,
            AcceptanceDate = acceptanceDate
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
        if (value.Contains("超期", StringComparison.OrdinalIgnoreCase)
            || value.Contains("到期", StringComparison.OrdinalIgnoreCase)
            || value.Contains("过期", StringComparison.OrdinalIgnoreCase)
            || value.Contains("停保", StringComparison.OrdinalIgnoreCase)
            || value.Contains("脱保", StringComparison.OrdinalIgnoreCase)) return "超期未签署";
        if (value.Contains("停止", StringComparison.OrdinalIgnoreCase)) return "停止维护";
        if (value.Contains("免费", StringComparison.OrdinalIgnoreCase)) return "免费维护期";
        if (value.Contains("签署", StringComparison.OrdinalIgnoreCase) || value.Contains("签订", StringComparison.OrdinalIgnoreCase)) return "合同已签署";

        return "未知";
    }

    private static List<string> NormalizeProductNames(string? productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return ["通用产品"];
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
            return ["通用产品"];
        }

        return tokens
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeProductName(string? productName)
    {
        var products = NormalizeProductNames(productName);
        if (products.Count == 0)
        {
            return "通用产品";
        }

        return products.Count == 1
            ? products[0]
            : string.Join("、", products);
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

    private static string NormalizeSalesName(string? salesName)
    {
        if (string.IsNullOrWhiteSpace(salesName))
        {
            return "未知";
        }

        var normalized = salesName
            .Trim()
            .Replace("，", "、", StringComparison.Ordinal)
            .Replace(",", "、", StringComparison.Ordinal)
            .Replace("/", "、", StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "未知";
        }

        return normalized;
    }

    private static string NormalizeMaintenancePersonName(string? maintenancePersonName)
    {
        if (string.IsNullOrWhiteSpace(maintenancePersonName))
        {
            return "未知";
        }

        var normalized = maintenancePersonName
            .Trim()
            .Replace("，", "、", StringComparison.Ordinal)
            .Replace(",", "、", StringComparison.Ordinal)
            .Replace("/", "、", StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        return string.IsNullOrWhiteSpace(normalized) ? "未知" : normalized;
    }

    private static string NormalizeDateText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var text = raw.Trim();
        var formats = new[]
        {
            "yyyy/M/d", "yyyy/MM/dd", "yyyy-M-d", "yyyy-MM-dd"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(text, format, null, System.Globalization.DateTimeStyles.None, out var dt))
            {
                return dt.ToString("yyyy-MM-dd");
            }
        }

        if (DateTime.TryParse(text, out var date))
        {
            return date.ToString("yyyy-MM-dd");
        }

        return text;
    }

    private static List<ProjectEntity> BuildSeedData()
    {
        return
        [
            new() { Id = 1, HospitalName = "北京协和医院", ProductName = "住院电子病历V6", Province = "北京", GroupName = "何道飞", HospitalLevel = "三级", ContractStatus = "合同已签署", MaintenanceAmount = 28.5m, OverdueDays = 0 },
            new() { Id = 2, HospitalName = "南京鼓楼医院", ProductName = "临床路径V6", Province = "江苏", GroupName = "张茹", SalesName = "未知", HospitalLevel = "三级", ContractStatus = "超期未签署", MaintenanceAmount = 18m, OverdueDays = 132 },
            new() { Id = 3, HospitalName = "四川省人民医院", ProductName = "CDSS", Province = "四川", GroupName = "李贝", SalesName = "未知", HospitalLevel = "三级", ContractStatus = "免费维护期", MaintenanceAmount = 12m, OverdueDays = 0 },
            new() { Id = 4, HospitalName = "中南大学湘雅医院", ProductName = "病案归档", Province = "湖南", GroupName = "舒高成", SalesName = "未知", HospitalLevel = "三级", ContractStatus = "维护合同已签署", MaintenanceAmount = 22m, OverdueDays = 0 },
            new() { Id = 5, HospitalName = "西安交通大学第一附属医院", ProductName = "AI内涵质控", Province = "陕西", GroupName = "陈宇", SalesName = "未知", HospitalLevel = "三级", ContractStatus = "维护超期未签署", MaintenanceAmount = 15m, OverdueDays = 45 },
            new() { Id = 6, HospitalName = "复旦大学附属中山医院", ProductName = "住院电子病历V6", Province = "上海", GroupName = "王可可", SalesName = "未知", HospitalLevel = "三级", ContractStatus = "合同已签署", MaintenanceAmount = 25m, OverdueDays = 0 },
            new() { Id = 7, HospitalName = "广东省人民医院", ProductName = "临床路径V6", Province = "广东", GroupName = "唐广才", SalesName = "未知", HospitalLevel = "三级", ContractStatus = "停止维护", MaintenanceAmount = 8m, OverdueDays = 0 },
            new() { Id = 8, HospitalName = "重庆医科大学附属第一医院", ProductName = "CDSS", Province = "重庆", GroupName = "张浩阳", SalesName = "未知", HospitalLevel = "三级", ContractStatus = "超期未签署", MaintenanceAmount = 16m, OverdueDays = 210 },
            new() { Id = 9, HospitalName = "福建省立医院", ProductName = "病案归档", Province = "福建", GroupName = "姚云龙", SalesName = "未知", HospitalLevel = "三级", ContractStatus = "超期未签署", MaintenanceAmount = 19m, OverdueDays = 76 },
            new() { Id = 10, HospitalName = "河南省人民医院", ProductName = "AI内涵质控", Province = "河南", GroupName = "孙强", SalesName = "未知", HospitalLevel = "二级", ContractStatus = "超期未签署", MaintenanceAmount = 14m, OverdueDays = 12 }
        ];
    }
}
