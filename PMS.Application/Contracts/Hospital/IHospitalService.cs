using PMS.Application.Models;
using PMS.Application.Models.Hospital;

namespace PMS.Application.Contracts.Hospital;

public interface IHospitalService
{
    Task<HospitalSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<HospitalItemDto>> QueryHospitalsAsync(HospitalQuery query, CancellationToken cancellationToken = default);
    Task<HospitalItemDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<HospitalItemDto> CreateAsync(HospitalUpsertDto dto, CancellationToken cancellationToken = default);
    Task<HospitalItemDto?> UpdateAsync(int id, HospitalUpsertDto dto, CancellationToken cancellationToken = default);
    Task<HospitalItemDto?> UpdateRatingAsync(int id, HospitalRatingDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class HospitalQuery
{
    public string? HospitalName { get; set; }
    public string? Tier { get; set; }
    public string? Province { get; set; }
    public string? City { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}
