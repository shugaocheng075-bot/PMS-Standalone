using PMS.Application.Contracts.Hospital;
using PMS.Application.Models;
using PMS.Application.Models.Hospital;
using PMS.Domain.Entities;

namespace PMS.Infrastructure.Services;

public class InMemoryHospitalService : IHospitalService
{
    private const string TableName = "Hospitals";
    private const string LegacyJsonKey = "hospitals";
    private static readonly List<HospitalItemDto> Hospitals = SqliteTableStore.LoadAll<HospitalItemDto>(TableName, LegacyJsonKey);

    public static void RebuildFromProjects(IReadOnlyList<ProjectEntity> projects)
    {
        var now = DateTime.Now;
        var generated = projects
            .Where(x => !string.IsNullOrWhiteSpace(x.HospitalName))
            .GroupBy(x => new { x.HospitalName, x.Province })
            .Select((group, index) => new HospitalItemDto
            {
                Id = index + 1,
                HospitalName = group.Key.HospitalName,
                Tier = group
                    .Select(x => x.HospitalLevel)
                    .FirstOrDefault(level => level is "三级" or "二级" or "一级") ?? "三级",
                Province = group.Key.Province,
                City = string.Empty,
                Address = string.Empty,
                ContactPerson = string.Empty,
                ContactPhone = string.Empty,
                DepartmentCount = "-",
                ProductCount = group.Select(x => x.ProductName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                ContractCount = group.Count(),
                CreatedAt = now
            })
            .OrderBy(x => x.HospitalName)
            .ToList();

        Hospitals.Clear();
        Hospitals.AddRange(generated);
        SqliteTableStore.ReplaceAll(TableName, Hospitals);
    }

    public Task<HospitalSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var hospitals = Hospitals;
        var summary = new HospitalSummaryDto
        {
            Total = hospitals.Count,
            ThreeTierCount = hospitals.Count(x => x.Tier == "三级"),
            TwoTierCount = hospitals.Count(x => x.Tier == "二级"),
            OneTierCount = hospitals.Count(x => x.Tier == "一级"),
            RegionCounts = hospitals.GroupBy(x => x.Province).ToDictionary(g => g.Key, g => g.Count())
        };

        return Task.FromResult(summary);
    }

    public Task<PagedResult<HospitalItemDto>> QueryHospitalsAsync(HospitalQuery query, CancellationToken cancellationToken = default)
    {
        var linkedHospitals = ApplyLinkedMetrics(Hospitals).AsEnumerable();
        var list = linkedHospitals;

        if (!string.IsNullOrWhiteSpace(query.HospitalName))
        {
            list = list.Where(x => SmartTextMatcher.Match(x.HospitalName, query.HospitalName));
        }

        if (!string.IsNullOrWhiteSpace(query.Tier))
        {
            list = list.Where(x => SmartTextMatcher.MatchExact(x.Tier, query.Tier));
        }

        if (!string.IsNullOrWhiteSpace(query.Province))
        {
            list = list.Where(x => SmartTextMatcher.Match(x.Province, query.Province));
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            list = list.Where(x => SmartTextMatcher.Match(x.City, query.City));
        }

        var total = list.Count();
        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.Size <= 0 ? 20 : query.Size;

        var items = list
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.HospitalName)
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return Task.FromResult(new PagedResult<HospitalItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            Size = size
        });
    }

    public Task<HospitalItemDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = ApplyLinkedMetrics(Hospitals).FirstOrDefault(x => x.Id == id);
        return Task.FromResult(item);
    }

    public Task<HospitalItemDto> CreateAsync(HospitalUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var nextId = Hospitals.Count == 0 ? 1 : Hospitals.Max(x => x.Id) + 1;
        var item = new HospitalItemDto
        {
            Id = nextId,
            HospitalName = dto.HospitalName,
            Tier = dto.Tier,
            Province = dto.Province,
            City = dto.City,
            Address = dto.Address,
            ContactPerson = dto.ContactPerson,
            ContactPhone = dto.ContactPhone,
            DepartmentCount = dto.DepartmentCount,
            ProductCount = 0,
            ContractCount = 0,
            CreatedAt = DateTime.Now
        };

        Hospitals.Add(item);
        SqliteTableStore.Insert(TableName, item);
        return Task.FromResult(item);
    }

    public Task<HospitalItemDto?> UpdateAsync(int id, HospitalUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var current = Hospitals.FirstOrDefault(x => x.Id == id);
        if (current is null)
        {
            return Task.FromResult<HospitalItemDto?>(null);
        }

        var oldHospitalName = current.HospitalName;

        current.HospitalName = dto.HospitalName;
        current.Tier = dto.Tier;
        current.Province = dto.Province;
        current.City = dto.City;
        current.Address = dto.Address;
        current.ContactPerson = dto.ContactPerson;
        current.ContactPhone = dto.ContactPhone;
        current.DepartmentCount = dto.DepartmentCount;

        InMemoryProjectDataStore.SyncHospitalInfo(oldHospitalName, current.HospitalName, current.Province);
        SqliteTableStore.Update(TableName, current, current.Id);

        return Task.FromResult<HospitalItemDto?>(current);
    }

    public Task<HospitalItemDto?> UpdateRatingAsync(int id, HospitalRatingDto dto, CancellationToken cancellationToken = default)
    {
        var current = Hospitals.FirstOrDefault(x => x.Id == id);
        if (current is null)
        {
            return Task.FromResult<HospitalItemDto?>(null);
        }

        current.EmrRatingLevel = dto.EmrRatingLevel;
        current.InteropRatingLevel = dto.InteropRatingLevel;
        SqliteTableStore.Update(TableName, current, current.Id);

        return Task.FromResult<HospitalItemDto?>(current);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var current = Hospitals.FirstOrDefault(x => x.Id == id);
        if (current is null)
        {
            return Task.FromResult(false);
        }

        Hospitals.Remove(current);
        InMemoryProjectDataStore.RemoveByHospitalName(current.HospitalName);
        SqliteTableStore.Delete(TableName, current.Id);
        return Task.FromResult(true);
    }

    private static List<HospitalItemDto> ApplyLinkedMetrics(IEnumerable<HospitalItemDto> hospitals)
    {
        var projects = InMemoryProjectDataStore.Projects;
        var linkedByHospital = projects
            .Where(x => !string.IsNullOrWhiteSpace(x.HospitalName))
            .GroupBy(x => x.HospitalName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    ProductCount = g.Select(x => x.ProductName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                    ContractCount = g.Count()
                },
                StringComparer.OrdinalIgnoreCase);

        return hospitals.Select(x =>
        {
            if (!linkedByHospital.TryGetValue(x.HospitalName, out var metric))
            {
                metric = new { ProductCount = 0, ContractCount = 0 };
            }

            return new HospitalItemDto
            {
                Id = x.Id,
                HospitalName = x.HospitalName,
                Tier = x.Tier,
                Province = x.Province,
                City = x.City,
                Address = x.Address,
                ContactPerson = x.ContactPerson,
                ContactPhone = x.ContactPhone,
                DepartmentCount = x.DepartmentCount,
                ProductCount = metric.ProductCount,
                ContractCount = metric.ContractCount,
                EmrRatingLevel = x.EmrRatingLevel,
                InteropRatingLevel = x.InteropRatingLevel,
                CreatedAt = x.CreatedAt
            };
        }).ToList();
    }

    private static List<HospitalItemDto> BuildSeedData()
    {
        return
        [
            new() { Id = 1, HospitalName = "北京大学人民医院", Tier = "三级", Province = "北京", City = "北京", Address = "北京市西城区西直门南大街11号", ContactPerson = "李明", ContactPhone = "010-88326666", DepartmentCount = "42个", CreatedAt = DateTime.Now.AddMonths(-6) },
            new() { Id = 2, HospitalName = "协和医院", Tier = "三级", Province = "北京", City = "北京", Address = "北京市东城区帅府园1号", ContactPerson = "王芳", ContactPhone = "010-65296114", DepartmentCount = "48个", CreatedAt = DateTime.Now.AddMonths(-5) },
            new() { Id = 3, HospitalName = "上海瑞金医院", Tier = "三级", Province = "上海", City = "上海", Address = "上海市黄浦区瑞金二路197号", ContactPerson = "陈刚", ContactPhone = "021-64370045", DepartmentCount = "45个", CreatedAt = DateTime.Now.AddMonths(-4) },
            new() { Id = 4, HospitalName = "浙江省人民医院", Tier = "三级", Province = "浙江", City = "杭州", Address = "杭州市上塘路158号", ContactPerson = "孙超", ContactPhone = "0571-85893888", DepartmentCount = "38个", CreatedAt = DateTime.Now.AddMonths(-3) },
            new() { Id = 5, HospitalName = "四川省人民医院", Tier = "三级", Province = "四川", City = "成都", Address = "成都市一环路西二段32号", ContactPerson = "王军", ContactPhone = "028-87393888", DepartmentCount = "41个", CreatedAt = DateTime.Now.AddMonths(-2) },
            new() { Id = 6, HospitalName = "北京朝阳医院", Tier = "三级", Province = "北京", City = "北京", Address = "北京市朝阳区工人体育场南路8号", ContactPerson = "李媛", ContactPhone = "010-85231122", DepartmentCount = "36个", CreatedAt = DateTime.Now.AddMonths(-1) },
            new() { Id = 7, HospitalName = "河南省医学科学院附属医院", Tier = "二级", Province = "河南", City = "郑州", Address = "郑州市金水区黄河路33号", ContactPerson = "赵晓", ContactPhone = "0371-65580000", DepartmentCount = "28个", CreatedAt = DateTime.Now.AddMonths(-1) },
            new() { Id = 8, HospitalName = "武汉同济医院", Tier = "三级", Province = "湖北", City = "武汉", Address = "武汉市硚口区解放大道1095号", ContactPerson = "张敏", ContactPhone = "027-83662688", DepartmentCount = "44个", CreatedAt = DateTime.Now },
            new() { Id = 9, HospitalName = "南京鼓楼医院", Tier = "二级", Province = "江苏", City = "南京", Address = "南京市鼓楼区中山路321号", ContactPerson = "王建", ContactPhone = "025-83106666", DepartmentCount = "32个", CreatedAt = DateTime.Now },
            new() { Id = 10, HospitalName = "广东省人民医院", Tier = "三级", Province = "广东", City = "广州", Address = "广州市中山二路106号", ContactPerson = "刘峰", ContactPhone = "020-83827812", DepartmentCount = "50个", CreatedAt = DateTime.Now },
            new() { Id = 11, HospitalName = "陕西省人民医院", Tier = "二级", Province = "陕西", City = "西安", Address = "西安市友谊东路256号", ContactPerson = "高飞", ContactPhone = "029-85251331", DepartmentCount = "30个", CreatedAt = DateTime.Now.AddDays(-7) },
            new() { Id = 12, HospitalName = "山东省立医院", Tier = "一级", Province = "山东", City = "济南", Address = "济南市经十路324号", ContactPerson = "贾玲", ContactPhone = "0531-87938911", DepartmentCount = "25个", CreatedAt = DateTime.Now.AddDays(-14) }
        ];
    }
}
