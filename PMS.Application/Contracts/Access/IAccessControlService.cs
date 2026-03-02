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
}