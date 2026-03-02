using PMS.Application.Contracts.Personnel;
using PMS.Application.Models;
using PMS.Application.Models.Personnel;

namespace PMS.Infrastructure.Services;

public class InMemoryPersonnelService : IPersonnelService
{
    private const string StateKey = "personnel";
    private static readonly List<PersonnelItemDto> Personnel = SqliteJsonStore.LoadOrSeed(StateKey, BuildSeedData);

    public Task<PersonnelSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var linkedPersonnel = Personnel.Select(HydrateWorkload).ToList();

        var summary = new PersonnelSummaryDto
        {
            Total = linkedPersonnel.Count,
            ServiceCount = linkedPersonnel.Count(x => x.RoleType == "服务"),
            ImplementationCount = linkedPersonnel.Count(x => x.RoleType == "实施"),
            OnsiteCount = linkedPersonnel.Count(x => x.IsOnsite)
        };

        return Task.FromResult(summary);
    }

    public Task<PagedResult<PersonnelItemDto>> QueryAsync(PersonnelQuery query, CancellationToken cancellationToken = default)
    {
        var list = Personnel
            .Select(HydrateWorkload)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            list = list.Where(x => SmartTextMatcher.Match(x.Name, query.Name));
        }

        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            list = list.Where(x => SmartTextMatcher.Match(x.Department, query.Department));
        }

        if (!string.IsNullOrWhiteSpace(query.GroupName))
        {
            list = list.Where(x => SmartTextMatcher.Match(x.GroupName, query.GroupName));
        }

        if (!string.IsNullOrWhiteSpace(query.RoleType))
        {
            list = list.Where(x => SmartTextMatcher.MatchExact(x.RoleType, query.RoleType));
        }

        if (query.IsOnsite.HasValue)
        {
            list = list.Where(x => x.IsOnsite == query.IsOnsite.Value);
        }

        var total = list.Count();
        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.Size <= 0 ? 20 : query.Size;

        var items = list
            .OrderByDescending(x => x.ProjectCount)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return Task.FromResult(new PagedResult<PersonnelItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            Size = size
        });
    }

    public Task<PersonnelItemDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = Personnel.FirstOrDefault(x => x.Id == id);
        return Task.FromResult(item is null ? null : HydrateWorkload(item));
    }

    public Task<PersonnelItemDto> CreateAsync(PersonnelUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var nextId = Personnel.Count == 0 ? 1 : Personnel.Max(x => x.Id) + 1;
        var item = new PersonnelItemDto
        {
            Id = nextId,
            Name = dto.Name,
            Department = dto.Department,
            GroupName = dto.GroupName,
            RoleType = dto.RoleType,
            Phone = dto.Phone,
            IsOnsite = dto.IsOnsite,
            ProjectCount = dto.ProjectCount,
            OverdueCount = dto.OverdueCount,
            CreatedAt = DateTime.Now
        };

        Personnel.Add(item);
        Persist();
        return Task.FromResult(HydrateWorkload(item));
    }

    public Task<PersonnelItemDto?> UpdateAsync(int id, PersonnelUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var current = Personnel.FirstOrDefault(x => x.Id == id);
        if (current is null)
        {
            return Task.FromResult<PersonnelItemDto?>(null);
        }

        current.Name = dto.Name;
        current.Department = dto.Department;
        current.GroupName = dto.GroupName;
        current.RoleType = dto.RoleType;
        current.Phone = dto.Phone;
        current.IsOnsite = dto.IsOnsite;
        current.ProjectCount = dto.ProjectCount;
        current.OverdueCount = dto.OverdueCount;
        Persist();

        return Task.FromResult<PersonnelItemDto?>(HydrateWorkload(current));
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var current = Personnel.FirstOrDefault(x => x.Id == id);
        if (current is null)
        {
            return Task.FromResult(false);
        }

        Personnel.Remove(current);
        Persist();
        return Task.FromResult(true);
    }

    private static void Persist()
    {
        SqliteJsonStore.Save(StateKey, Personnel);
    }

    private static PersonnelItemDto HydrateWorkload(PersonnelItemDto item)
    {
        return new PersonnelItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Department = item.Department,
            GroupName = item.GroupName,
            RoleType = item.RoleType,
            Phone = item.Phone,
            IsOnsite = item.IsOnsite,
            ProjectCount = InMemoryProjectDataStore.CountProjectsForPersonnel(item.Name, item.GroupName),
            OverdueCount = InMemoryProjectDataStore.CountOverdueProjectsForPersonnel(item.Name, item.GroupName),
            CreatedAt = item.CreatedAt
        };
    }

    private static List<PersonnelItemDto> BuildSeedData()
    {
        return
        [
            new() { Id = 1, Name = "李贝", Department = "服务一部", GroupName = "李贝组", RoleType = "服务", Phone = "13800010001", IsOnsite = false, ProjectCount = 28, OverdueCount = 3, CreatedAt = DateTime.Now.AddMonths(-8) },
            new() { Id = 2, Name = "何道飞", Department = "服务一部", GroupName = "何道飞组", RoleType = "服务", Phone = "13800010002", IsOnsite = true, ProjectCount = 35, OverdueCount = 2, CreatedAt = DateTime.Now.AddMonths(-7) },
            new() { Id = 3, Name = "张茹", Department = "服务一部", GroupName = "张茹组", RoleType = "服务", Phone = "13800010003", IsOnsite = false, ProjectCount = 24, OverdueCount = 4, CreatedAt = DateTime.Now.AddMonths(-6) },
            new() { Id = 4, Name = "陈宇", Department = "实施二组", GroupName = "实施二组", RoleType = "实施", Phone = "13800010004", IsOnsite = false, ProjectCount = 19, OverdueCount = 1, CreatedAt = DateTime.Now.AddMonths(-5) },
            new() { Id = 5, Name = "侯海亮", Department = "实施一组", GroupName = "实施一组", RoleType = "实施", Phone = "13800010005", IsOnsite = true, ProjectCount = 22, OverdueCount = 2, CreatedAt = DateTime.Now.AddMonths(-4) },
            new() { Id = 6, Name = "舒高成", Department = "服务一部", GroupName = "舒高成组", RoleType = "服务", Phone = "13800010006", IsOnsite = false, ProjectCount = 17, OverdueCount = 1, CreatedAt = DateTime.Now.AddMonths(-3) }
        ];
    }
}
