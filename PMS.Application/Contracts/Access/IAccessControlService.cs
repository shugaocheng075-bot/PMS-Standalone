using PMS.Application.Models;
using PMS.Application.Models.Access;

namespace PMS.Application.Contracts.Access;

public interface IAccessControlService
{
    Task<IReadOnlyList<PermissionDefinitionDto>> GetPermissionCatalogAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PersonnelActorDto>> GetActorsAsync(CancellationToken cancellationToken = default);
    Task<PersonnelAccessProfileDto?> GetCurrentAsync(int personnelId, CancellationToken cancellationToken = default);
    Task<PagedResult<PersonnelAccessItemDto>> QueryUsersAsync(PersonnelAccessQuery query, CancellationToken cancellationToken = default);
    Task<PersonnelAccessProfileDto?> GetUserProfileAsync(int personnelId, CancellationToken cancellationToken = default);
    Task<PersonnelAccessProfileDto?> SaveUserPermissionsAsync(
        int personnelId,
        IReadOnlyCollection<string> permissionKeys,
        bool? isAdmin,
        CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(int personnelId, string permissionKey, CancellationToken cancellationToken = default);
    string? ResolveRequiredPermission(string method, string requestPath);

    /// <summary>获取用户的数据范围（可访问哪些人员的项目）</summary>
    Task<DataScopeDto> GetDataScopeAsync(int personnelId, CancellationToken cancellationToken = default);

    /// <summary>设置用户的系统角色 (operator/supervisor/manager)</summary>
    Task<PersonnelAccessProfileDto?> SetSystemRoleAsync(int personnelId, string systemRole, CancellationToken cancellationToken = default);

    /// <summary>设置用户的上级主管</summary>
    Task<PersonnelAccessProfileDto?> SetSupervisorAsync(int personnelId, int? supervisorId, CancellationToken cancellationToken = default);

    /// <summary>获取用户的医院范围分配</summary>
    Task<List<string>> GetHospitalScopeAsync(int personnelId, CancellationToken cancellationToken = default);

    /// <summary>设置用户可访问的医院列表（显式分配）</summary>
    Task<PersonnelAccessProfileDto?> SetHospitalScopeAsync(int personnelId, List<string> hospitalNames, CancellationToken cancellationToken = default);
}