using PMS.Application.Models;
using PMS.Application.Models.Personnel;

namespace PMS.Application.Contracts.Personnel;

public interface IPersonnelService
{
    Task<PersonnelSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<PersonnelItemDto>> QueryAsync(PersonnelQuery query, CancellationToken cancellationToken = default);
    Task<PersonnelItemDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PersonnelExternalSyncResultDto> SyncFromExternalAsync(bool force = false, CancellationToken cancellationToken = default);
    Task<PersonnelExternalSyncResultDto> ImportJsonAsync(string jsonData, bool clearExisting = false, CancellationToken cancellationToken = default);
    Task<PersonnelItemDto> CreateAsync(PersonnelUpsertDto dto, CancellationToken cancellationToken = default);
    Task<PersonnelItemDto?> UpdateAsync(int id, PersonnelUpsertDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class PersonnelQuery
{
    public string? Name { get; set; }
    public string? Department { get; set; }
    public string? GroupName { get; set; }
    public string? RoleType { get; set; }
    public bool? IsOnsite { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}
