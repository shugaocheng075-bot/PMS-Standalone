using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.Personnel;
using PMS.Application.Models;
using PMS.Application.Models.Access;

namespace PMS.Infrastructure.Services;

public class InMemoryAccessControlService(IPersonnelService personnelService) : IAccessControlService
{
    private const string StateKey = "personnel_access";
    private static readonly object SyncRoot = new();
    private static readonly List<PersonnelPermissionState> States = SqliteJsonStore.LoadOrSeed(StateKey, () => new List<PersonnelPermissionState>());

    private static readonly List<PermissionDefinitionDto> PermissionCatalog =
    [
        new() { Key = "dashboard.view", Name = "查看首页", Module = "首页", Description = "可访问首页看板" },
        new() { Key = "alert-center.view", Name = "查看统一预警中心", Module = "首页", Description = "可查看统一预警待办" },
        new() { Key = "project.view", Name = "查看项目台账", Module = "项目管理", Description = "可查询项目台账" },
        new() { Key = "project.manage", Name = "维护项目台账", Module = "项目管理", Description = "可批量编辑项目台账" },
        new() { Key = "contract.view", Name = "查看合同预警", Module = "项目管理", Description = "可查看合同预警" },
        new() { Key = "handover.view", Name = "查看交接管理", Module = "运营管理", Description = "可查看交接列表和看板" },
        new() { Key = "handover.manage", Name = "维护交接状态", Module = "运营管理", Description = "可修改交接阶段" },
        new() { Key = "inspection.view", Name = "查看巡检计划", Module = "运营管理", Description = "可查看巡检计划" },
        new() { Key = "annual-report.view", Name = "查看年度报告", Module = "运营管理", Description = "可查看年度报告" },
        new() { Key = "hospital.view", Name = "查看医院管理", Module = "运营管理", Description = "可查看医院信息" },
        new() { Key = "hospital.manage", Name = "维护医院管理", Module = "运营管理", Description = "可新增/编辑/删除医院信息" },
        new() { Key = "personnel.view", Name = "查看人员管理", Module = "运营管理", Description = "可查看人员台账" },
        new() { Key = "personnel.manage", Name = "维护人员管理", Module = "运营管理", Description = "可新增/编辑/删除人员" },
        new() { Key = "product.view", Name = "查看产品管理", Module = "运营管理", Description = "可查看产品台账" },
        new() { Key = "product.manage", Name = "维护产品管理", Module = "运营管理", Description = "可新增/编辑/删除产品" },
        new() { Key = "major-demand.view", Name = "查看重大需求", Module = "运营管理", Description = "可查看重大需求数据" },
        new() { Key = "major-demand.manage", Name = "维护重大需求", Module = "运营管理", Description = "可更新重大需求状态、负责人、评论与导出" },
        new() { Key = "maintenance.manage", Name = "数据维护", Module = "运维", Description = "可使用数据维护中心导入能力" },
        new() { Key = "permission.manage", Name = "权限管理", Module = "系统", Description = "可配置人员权限" },
    ];

    private static readonly HashSet<string> PermissionKeySet = PermissionCatalog
        .Select(x => x.Key)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyList<PermissionDefinitionDto>> GetPermissionCatalogAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PermissionDefinitionDto>>(PermissionCatalog);
    }

    public async Task<IReadOnlyList<PersonnelActorDto>> GetActorsAsync(CancellationToken cancellationToken = default)
    {
        var result = await personnelService.QueryAsync(new PersonnelQuery
        {
            Page = 1,
            Size = 5000
        }, cancellationToken);

        return result.Items
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .Select(x => new PersonnelActorDto
            {
                PersonnelId = x.Id,
                PersonnelName = x.Name,
                RoleType = x.RoleType
            })
            .ToList();
    }

    public Task<PersonnelAccessProfileDto?> GetCurrentAsync(int personnelId, CancellationToken cancellationToken = default)
    {
        return GetUserProfileAsync(personnelId, cancellationToken);
    }

    public async Task<PagedResult<PersonnelAccessItemDto>> QueryUsersAsync(PersonnelAccessQuery query, CancellationToken cancellationToken = default)
    {
        var personnelResult = await personnelService.QueryAsync(new PersonnelQuery
        {
            Name = query.Name,
            Page = query.Page,
            Size = query.Size
        }, cancellationToken);

        var items = new List<PersonnelAccessItemDto>();
        foreach (var person in personnelResult.Items)
        {
            var state = await EnsureStateAsync(person.Id, person.RoleType, cancellationToken);
            if (state is null)
            {
                continue;
            }

            items.Add(new PersonnelAccessItemDto
            {
                PersonnelId = person.Id,
                PersonnelName = person.Name,
                Department = person.Department,
                GroupName = person.GroupName,
                RoleType = person.RoleType,
                IsAdmin = state.IsAdmin,
                PermissionCount = state.PermissionKeys.Count,
                UpdatedAt = state.UpdatedAt
            });
        }

        return new PagedResult<PersonnelAccessItemDto>
        {
            Items = items,
            Total = personnelResult.Total,
            Page = personnelResult.Page,
            Size = personnelResult.Size
        };
    }

    public async Task<PersonnelAccessProfileDto?> GetUserProfileAsync(int personnelId, CancellationToken cancellationToken = default)
    {
        var person = await personnelService.GetByIdAsync(personnelId, cancellationToken);
        if (person is null)
        {
            return null;
        }

        var state = await EnsureStateAsync(personnelId, person.RoleType, cancellationToken);
        if (state is null)
        {
            return null;
        }

        return BuildProfile(person.Id, person.Name, person.RoleType, state);
    }

    public async Task<PersonnelAccessProfileDto?> SaveUserPermissionsAsync(
        int personnelId,
        IReadOnlyCollection<string> permissionKeys,
        bool? isAdmin,
        CancellationToken cancellationToken = default)
    {
        var person = await personnelService.GetByIdAsync(personnelId, cancellationToken);
        if (person is null)
        {
            return null;
        }

        var sanitized = permissionKeys
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Where(x => PermissionKeySet.Contains(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        lock (SyncRoot)
        {
            var state = States.FirstOrDefault(x => x.PersonnelId == personnelId);
            if (state is null)
            {
                state = new PersonnelPermissionState
                {
                    PersonnelId = personnelId,
                    IsAdmin = false,
                    PermissionKeys = [],
                    UpdatedAt = DateTime.UtcNow
                };
                States.Add(state);
            }

            state.PermissionKeys = sanitized;
            if (isAdmin.HasValue)
            {
                state.IsAdmin = isAdmin.Value;
            }

            state.UpdatedAt = DateTime.UtcNow;
            Persist();

            return BuildProfile(person.Id, person.Name, person.RoleType, state);
        }
    }

    public async Task<bool> HasPermissionAsync(int personnelId, string permissionKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permissionKey))
        {
            return true;
        }

        var profile = await GetUserProfileAsync(personnelId, cancellationToken);
        if (profile is null)
        {
            return false;
        }

        if (profile.IsAdmin)
        {
            return true;
        }

        return profile.Permissions.Contains(permissionKey, StringComparer.OrdinalIgnoreCase);
    }

    public string? ResolveRequiredPermission(string method, string requestPath)
    {
        if (string.IsNullOrWhiteSpace(requestPath))
        {
            return null;
        }

        var path = requestPath.Trim().ToLowerInvariant();
        var httpMethod = method.Trim().ToUpperInvariant();

        if (!path.StartsWith("/api/"))
        {
            return null;
        }

        if (path.StartsWith("/api/health") || path.StartsWith("/api/access/me") || path.StartsWith("/api/access/actors"))
        {
            return null;
        }

        if (path.StartsWith("/api/access/permissions") || path.StartsWith("/api/access/users"))
        {
            return "permission.manage";
        }

        if (path.StartsWith("/api/alerts/center"))
        {
            return "alert-center.view";
        }

        if (path.StartsWith("/api/projects"))
        {
            return httpMethod == "GET" ? "project.view" : "project.manage";
        }

        if (path.StartsWith("/api/contracts/alerts"))
        {
            return "contract.view";
        }

        if (path.StartsWith("/api/handovers"))
        {
            return httpMethod == "PUT" ? "handover.manage" : "handover.view";
        }

        if (path.StartsWith("/api/inspections"))
        {
            return "inspection.view";
        }

        if (path.StartsWith("/api/annual-reports"))
        {
            return "annual-report.view";
        }

        if (path.StartsWith("/api/hospitals"))
        {
            return httpMethod == "GET" ? "hospital.view" : "hospital.manage";
        }

        if (path.StartsWith("/api/personnel"))
        {
            return httpMethod == "GET" ? "personnel.view" : "personnel.manage";
        }

        if (path.StartsWith("/api/products"))
        {
            return httpMethod == "GET" ? "product.view" : "product.manage";
        }

        if (path.StartsWith("/api/major-demands"))
        {
            return httpMethod == "GET" ? "major-demand.view" : "major-demand.manage";
        }

        if (path.StartsWith("/api/admin/import"))
        {
            return "maintenance.manage";
        }

        return null;
    }

    private async Task<PersonnelPermissionState?> EnsureStateAsync(int personnelId, string roleType, CancellationToken cancellationToken)
    {
        lock (SyncRoot)
        {
            var existing = States.FirstOrDefault(x => x.PersonnelId == personnelId);
            if (existing is not null)
            {
                existing.PermissionKeys = existing.PermissionKeys
                    .Where(x => PermissionKeySet.Contains(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList();

                return existing;
            }
        }

        var defaultState = await BuildDefaultStateAsync(personnelId, roleType, cancellationToken);
        if (defaultState is null)
        {
            return null;
        }

        lock (SyncRoot)
        {
            var existing = States.FirstOrDefault(x => x.PersonnelId == personnelId);
            if (existing is not null)
            {
                return existing;
            }

            States.Add(defaultState);
            Persist();
            return defaultState;
        }
    }

    private static Task<PersonnelPermissionState?> BuildDefaultStateAsync(int personnelId, string roleType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var isAdmin = personnelId == 1;
        var permissionKeys = isAdmin
            ? PermissionCatalog.Select(x => x.Key).ToList()
            : BuildRoleDefaultPermissions(roleType);

        PersonnelPermissionState state = new()
        {
            PersonnelId = personnelId,
            IsAdmin = isAdmin,
            PermissionKeys = permissionKeys,
            UpdatedAt = DateTime.UtcNow
        };

        return Task.FromResult<PersonnelPermissionState?>(state);
    }

    private static List<string> BuildRoleDefaultPermissions(string roleType)
    {
        var normalized = roleType.Trim();
        if (string.Equals(normalized, "实施", StringComparison.Ordinal))
        {
            return
            [
                "dashboard.view",
                "alert-center.view",
                "project.view",
                "handover.view",
                "inspection.view",
                "annual-report.view",
                "hospital.view",
                "personnel.view",
                "product.view",
                "major-demand.view",
                "major-demand.manage"
            ];
        }

        return
        [
            "dashboard.view",
            "alert-center.view",
            "project.view",
                "project.manage",
            "contract.view",
            "handover.view",
            "inspection.view",
            "annual-report.view",
            "hospital.view",
            "personnel.view",
            "product.view",
            "major-demand.view"
        ];
    }

    private static PersonnelAccessProfileDto BuildProfile(
        int personnelId,
        string personnelName,
        string roleType,
        PersonnelPermissionState state)
    {
        return new PersonnelAccessProfileDto
        {
            PersonnelId = personnelId,
            PersonnelName = personnelName,
            RoleType = roleType,
            IsAdmin = state.IsAdmin,
            Permissions = state.PermissionKeys
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList()
        };
    }

    private static void Persist()
    {
        SqliteJsonStore.Save(StateKey, States);
    }

    private class PersonnelPermissionState
    {
        public int PersonnelId { get; set; }
        public bool IsAdmin { get; set; }
        public List<string> PermissionKeys { get; set; } = [];
        public DateTime UpdatedAt { get; set; }
    }
}