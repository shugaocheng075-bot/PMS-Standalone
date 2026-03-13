using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.Personnel;
using PMS.Application.Models;
using PMS.Application.Models.Access;

namespace PMS.Infrastructure.Services;

public class InMemoryAccessControlService(IPersonnelService personnelService) : IAccessControlService
{
    private const int ProtectedAdminPersonnelId = 1;
    private const string StateKey = "personnel_access";
    private static readonly object SyncRoot = new();
    private static readonly List<PersonnelPermissionState> States = SqliteJsonStore.LoadOrSeed(StateKey, () => new List<PersonnelPermissionState>());

    // ── 系统角色常量（4 级层级：运维人员 → 运维服务主管 → 运维区域经理 → 运维经理）──
    public const string RoleOperator = "operator";                 // 运维人员
    public const string RoleSupervisor = "supervisor";             // 运维服务主管
    public const string RoleRegionalManager = "regional_manager"; // 运维区域经理
    public const string RoleManager = "manager";                   // 运维经理

    private static readonly HashSet<string> ValidSystemRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        RoleOperator, RoleSupervisor, RoleRegionalManager, RoleManager
    };

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
        new() { Key = "repair.view", Name = "查看报修记录", Module = "运营管理", Description = "可查看报修记录" },
        new() { Key = "repair.manage", Name = "维护报修记录", Module = "运营管理", Description = "可新增/编辑报修记录" },
        new() { Key = "workhours.view", Name = "查看工时", Module = "运营管理", Description = "可查看工时记录" },
        new() { Key = "workhours.manage", Name = "维护工时", Module = "运营管理", Description = "可新增/编辑/删除工时记录" },
        new() { Key = "monthly-report.view", Name = "查看月报", Module = "运营管理", Description = "可查看月报列表" },
        new() { Key = "monthly-report.manage", Name = "维护月报", Module = "运营管理", Description = "可新增/编辑/删除月报" },
        new() { Key = "maintenance.manage", Name = "数据维护", Module = "运维", Description = "可使用数据维护中心导入能力" },
        new() { Key = "permission.manage", Name = "权限管理", Module = "系统", Description = "可配置人员权限" },
        new() { Key = "audit.view", Name = "查看操作日志", Module = "系统", Description = "可查看系统操作日志" },
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
            .Select(x =>
            {
                var state = GetStateSnapshot(x.Id);
                return new PersonnelActorDto
                {
                    PersonnelId = x.Id,
                    PersonnelName = x.Name,
                    RoleType = x.RoleType,
                    SystemRole = state?.SystemRole ?? RoleOperator
                };
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

        // 预加载所有人员用于 SupervisorName 解析
        var allPersonnel = await personnelService.QueryAsync(new PersonnelQuery { Page = 1, Size = 5000 }, cancellationToken);
        var personnelNameMap = allPersonnel.Items.ToDictionary(x => x.Id, x => x.Name);

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
                SystemRole = state.SystemRole ?? RoleOperator,
                SupervisorId = state.SupervisorId,
                SupervisorName = state.SupervisorId.HasValue && personnelNameMap.TryGetValue(state.SupervisorId.Value, out var sn) ? sn : null,
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

        var dataScope = await BuildDataScopeAsync(personnelId, person.Name, state, cancellationToken);
        return BuildProfile(person.Id, person.Name, person.RoleType, state, dataScope);
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

        PersonnelPermissionState state;
        lock (SyncRoot)
        {
            state = States.FirstOrDefault(x => x.PersonnelId == personnelId)!;
            if (state is null)
            {
                state = new PersonnelPermissionState
                {
                    PersonnelId = personnelId,
                    IsAdmin = false,
                    SystemRole = RoleOperator,
                    PermissionKeys = [],
                    UpdatedAt = DateTime.UtcNow
                };
                States.Add(state);
            }

            if (personnelId == ProtectedAdminPersonnelId)
            {
                state.IsAdmin = true;
                state.SystemRole = RoleManager;
                state.PermissionKeys = PermissionCatalog
                    .Select(x => x.Key)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList();
                state.UpdatedAt = DateTime.UtcNow;
                Persist();
                var ds = BuildDataScopeAsync(person.Id, person.Name, state, cancellationToken).GetAwaiter().GetResult();
                return BuildProfile(person.Id, person.Name, person.RoleType, state, ds);
            }

            state.PermissionKeys = sanitized;
            if (isAdmin.HasValue)
            {
                state.IsAdmin = isAdmin.Value;
            }

            state.UpdatedAt = DateTime.UtcNow;
            Persist();
        }

        var dataScope = await BuildDataScopeAsync(person.Id, person.Name, state, cancellationToken);
        return BuildProfile(person.Id, person.Name, person.RoleType, state, dataScope);
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

        if (path.StartsWith("/api/health") || path.StartsWith("/api/access/me") || path.StartsWith("/api/access/actors")
            || path.StartsWith("/api/system"))
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

        if (path.StartsWith("/api/dashboard"))
        {
            return "dashboard.view";
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

        if (path.StartsWith("/api/repair-records"))
        {
            return httpMethod == "GET" ? "repair.view" : "repair.manage";
        }

        if (path.StartsWith("/api/workhours"))
        {
            return httpMethod == "GET" ? "workhours.view" : "workhours.manage";
        }

        return null;
    }

    // ── 新增接口方法: GetDataScopeAsync ──

    public async Task<DataScopeDto> GetDataScopeAsync(int personnelId, CancellationToken cancellationToken = default)
    {
        var person = await personnelService.GetByIdAsync(personnelId, cancellationToken);
        if (person is null)
        {
            return new DataScopeDto { ScopeType = "own", AccessiblePersonnelNames = [] };
        }

        var state = await EnsureStateAsync(personnelId, person.RoleType, cancellationToken);
        if (state is null)
        {
            return new DataScopeDto { ScopeType = "own", AccessiblePersonnelNames = [person.Name] };
        }

        return await BuildDataScopeAsync(personnelId, person.Name, state, cancellationToken);
    }

    // ── 新增接口方法: SetSystemRoleAsync ──

    public async Task<PersonnelAccessProfileDto?> SetSystemRoleAsync(int personnelId, string systemRole, CancellationToken cancellationToken = default)
    {
        var normalizedRole = (systemRole ?? string.Empty).Trim().ToLowerInvariant();
        if (!ValidSystemRoles.Contains(normalizedRole))
        {
            normalizedRole = RoleOperator;
        }

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

        lock (SyncRoot)
        {
            state.SystemRole = normalizedRole;
            // 更新角色时自动重新分配默认权限
            state.PermissionKeys = BuildRoleDefaultPermissions(normalizedRole);
            if (personnelId == ProtectedAdminPersonnelId)
            {
                state.IsAdmin = true;
                state.SystemRole = RoleManager;
                state.PermissionKeys = PermissionCatalog.Select(x => x.Key).ToList();
            }
            state.UpdatedAt = DateTime.UtcNow;
            Persist();
        }

        var dataScope = await BuildDataScopeAsync(personnelId, person.Name, state, cancellationToken);
        return BuildProfile(person.Id, person.Name, person.RoleType, state, dataScope);
    }

    // ── 新增接口方法: SetSupervisorAsync ──

    public async Task<PersonnelAccessProfileDto?> SetSupervisorAsync(int personnelId, int? supervisorId, CancellationToken cancellationToken = default)
    {
        var person = await personnelService.GetByIdAsync(personnelId, cancellationToken);
        if (person is null)
        {
            return null;
        }

        // 不允许自己指派自己为上级
        if (supervisorId.HasValue && supervisorId.Value == personnelId)
        {
            return null;
        }

        // 验证上级存在
        if (supervisorId.HasValue)
        {
            var supervisor = await personnelService.GetByIdAsync(supervisorId.Value, cancellationToken);
            if (supervisor is null)
            {
                return null;
            }
        }

        var state = await EnsureStateAsync(personnelId, person.RoleType, cancellationToken);
        if (state is null)
        {
            return null;
        }

        lock (SyncRoot)
        {
            state.SupervisorId = supervisorId;
            state.UpdatedAt = DateTime.UtcNow;
            Persist();
        }

        var dataScope = await BuildDataScopeAsync(personnelId, person.Name, state, cancellationToken);
        return BuildProfile(person.Id, person.Name, person.RoleType, state, dataScope);
    }

    // ── 数据范围构建 ──

    private async Task<DataScopeDto> BuildDataScopeAsync(int personnelId, string personnelName, PersonnelPermissionState state, CancellationToken cancellationToken)
    {
        var role = state.SystemRole ?? RoleOperator;

        // admin 或 运维经理: 全部（不限医院）
        if (state.IsAdmin || string.Equals(role, RoleManager, StringComparison.OrdinalIgnoreCase))
        {
            return new DataScopeDto { ScopeType = "all", AccessiblePersonnelNames = [], AccessibleHospitalNames = [] };
        }

        // 运维区域经理: 与主管相同逻辑（本人 + 所有下属）
        // 运维服务主管: 本人 + 所有 SupervisorId 指向自己的下属
        if (string.Equals(role, RoleRegionalManager, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, RoleSupervisor, StringComparison.OrdinalIgnoreCase))
        {
            var names = new List<string> { personnelName };

            // 查找所有 subordinates
            var allPersonnel = await personnelService.QueryAsync(new PersonnelQuery { Page = 1, Size = 5000 }, cancellationToken);
            lock (SyncRoot)
            {
                var subordinateIds = States
                    .Where(s => s.SupervisorId == personnelId)
                    .Select(s => s.PersonnelId)
                    .ToHashSet();

                foreach (var p in allPersonnel.Items)
                {
                    if (subordinateIds.Contains(p.Id) && !names.Contains(p.Name, StringComparer.Ordinal))
                    {
                        names.Add(p.Name);
                    }
                }
            }

            // supervisor: 合并自身 + 所有下属的医院范围
            var hospitalNames = new HashSet<string>(state.HospitalNames, StringComparer.OrdinalIgnoreCase);
            lock (SyncRoot)
            {
                var subordinateIds2 = States
                    .Where(s => s.SupervisorId == personnelId)
                    .ToList();
                foreach (var sub in subordinateIds2)
                {
                    foreach (var h in sub.HospitalNames)
                    {
                        hospitalNames.Add(h);
                    }
                }
            }

            return new DataScopeDto
            {
                ScopeType = "subordinates",
                AccessiblePersonnelNames = names,
                AccessibleHospitalNames = hospitalNames.OrderBy(x => x).ToList()
            };
        }

        // operator: 仅本人
        return new DataScopeDto
        {
            ScopeType = "own",
            AccessiblePersonnelNames = [personnelName],
            AccessibleHospitalNames = state.HospitalNames.ToList()
        };
    }

    // ── 内部辅助方法 ──

    private static PersonnelPermissionState? GetStateSnapshot(int personnelId)
    {
        lock (SyncRoot)
        {
            return States.FirstOrDefault(x => x.PersonnelId == personnelId);
        }
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

                if (personnelId == 1)
                {
                    existing.IsAdmin = true;
                    existing.PermissionKeys = PermissionCatalog
                        .Select(x => x.Key)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x, StringComparer.Ordinal)
                        .ToList();
                    existing.UpdatedAt = DateTime.UtcNow;
                    Persist();
                }

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

        var isAdmin = personnelId == ProtectedAdminPersonnelId;
        var systemRole = isAdmin ? RoleManager : RoleOperator;
        var permissionKeys = isAdmin
            ? PermissionCatalog.Select(x => x.Key).ToList()
            : BuildRoleDefaultPermissions(systemRole);

        PersonnelPermissionState state = new()
        {
            PersonnelId = personnelId,
            IsAdmin = isAdmin,
            SystemRole = systemRole,
            PermissionKeys = permissionKeys,
            UpdatedAt = DateTime.UtcNow
        };

        return Task.FromResult<PersonnelPermissionState?>(state);
    }

    private static List<string> BuildRoleDefaultPermissions(string systemRole)
    {
        var role = (systemRole ?? RoleOperator).Trim().ToLowerInvariant();

        // ── 运维经理: 全部权限 ──
        if (string.Equals(role, RoleManager, StringComparison.OrdinalIgnoreCase))
        {
            return PermissionCatalog.Select(x => x.Key).ToList();
        }

        // ── 运维区域经理: 拥有大部分管理权限 ──
        if (string.Equals(role, RoleRegionalManager, StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                "dashboard.view",
                "alert-center.view",
                "project.view",
                "project.manage",
                "contract.view",
                "contract.manage",
                "handover.view",
                "handover.manage",
                "inspection.view",
                "inspection.manage",
                "annual-report.view",
                "annual-report.manage",
                "hospital.view",
                "hospital.manage",
                "personnel.view",
                "personnel.manage",
                "product.view",
                "major-demand.view",
                "major-demand.manage",
                "repair.view",
                "repair.manage",
                "monthly-report.view",
                "monthly-report.manage",
            ];
        }

        // ── 运维服务主管: 管理下属项目 + 查看合同/项目 ──
        if (string.Equals(role, RoleSupervisor, StringComparison.OrdinalIgnoreCase))
        {
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
                "major-demand.view",
                "major-demand.manage",
                "repair.view",
                "monthly-report.view",
                "monthly-report.manage",
            ];
        }

        // ── 运维人员: 仅自己项目相关 ──
        return
        [
            "dashboard.view",
            "project.view",
            "major-demand.view",
            "major-demand.manage",
            "repair.view",
            "repair.manage",
            "monthly-report.view",
            "monthly-report.manage",
        ];
    }

    private static PersonnelAccessProfileDto BuildProfile(
        int personnelId,
        string personnelName,
        string roleType,
        PersonnelPermissionState state,
        DataScopeDto? dataScope = null)
    {
        var resolvedPersonnelName = personnelId == ProtectedAdminPersonnelId
            ? "admin"
            : personnelName;

        return new PersonnelAccessProfileDto
        {
            PersonnelId = personnelId,
            PersonnelName = resolvedPersonnelName,
            RoleType = roleType,
            SystemRole = state.SystemRole ?? RoleOperator,
            SupervisorId = state.SupervisorId,
            IsAdmin = state.IsAdmin,
            Permissions = state.PermissionKeys
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList(),
            DataScope = dataScope ?? new DataScopeDto()
        };
    }

    // ── 新增接口方法: GetHospitalScopeAsync ──

    public async Task<List<string>> GetHospitalScopeAsync(int personnelId, CancellationToken cancellationToken = default)
    {
        var person = await personnelService.GetByIdAsync(personnelId, cancellationToken);
        if (person is null)
        {
            return [];
        }

        var state = await EnsureStateAsync(personnelId, person.RoleType, cancellationToken);
        if (state is null)
        {
            return [];
        }

        return state.HospitalNames.ToList();
    }

    // ── 新增接口方法: SetHospitalScopeAsync ──

    public async Task<PersonnelAccessProfileDto?> SetHospitalScopeAsync(int personnelId, List<string> hospitalNames, CancellationToken cancellationToken = default)
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

        var sanitized = (hospitalNames ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        lock (SyncRoot)
        {
            state.HospitalNames = sanitized;
            state.UpdatedAt = DateTime.UtcNow;
            Persist();
        }

        var dataScope = await BuildDataScopeAsync(personnelId, person.Name, state, cancellationToken);
        return BuildProfile(person.Id, person.Name, person.RoleType, state, dataScope);
    }

    private static void Persist()
    {
        SqliteJsonStore.Save(StateKey, States);
    }

    private class PersonnelPermissionState
    {
        public int PersonnelId { get; set; }
        public bool IsAdmin { get; set; }
        public string SystemRole { get; set; } = RoleOperator;
        public int? SupervisorId { get; set; }
        public List<string> PermissionKeys { get; set; } = [];
        /// <summary>显式分配的可访问医院名称列表（空 = 未分配，需按角色决定）</summary>
        public List<string> HospitalNames { get; set; } = [];
        public DateTime UpdatedAt { get; set; }
    }
}