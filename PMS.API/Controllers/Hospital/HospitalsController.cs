using System.Text;
using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.Hospital;
using PMS.Application.Models;
using PMS.Application.Models.Hospital;

namespace PMS.API.Controllers.Hospital;

[ApiController]
[Route("api/hospitals")]
public class HospitalsController(
    IHospitalService hospitalService,
    IAccessControlService accessControlService) : ControllerBase
{
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        if (string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            var result = await hospitalService.GetSummaryAsync(cancellationToken);
            return Ok(ApiResponse<HospitalSummaryDto>.Success(result));
        }
        // Non-manager: compute summary from filtered list
        var all = await hospitalService.QueryHospitalsAsync(new HospitalQuery { Page = 1, Size = 50000 }, cancellationToken);
        var scoped = HospitalScopeHelper.FilterByHospitalScope(dataScope, all.Items, x => x.HospitalName).ToList();
        var scopedSummary = new HospitalSummaryDto
        {
            Total = scoped.Count,
            ThreeTierCount = scoped.Count(x => (x.Tier ?? "").Contains("三")),
            TwoTierCount = scoped.Count(x => (x.Tier ?? "").Contains("二")),
            OneTierCount = scoped.Count(x => (x.Tier ?? "").Contains("一")),
        };
        return Ok(ApiResponse<HospitalSummaryDto>.Success(scopedSummary));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        return await GetStatistics(cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Query(
        [FromQuery] string? hospitalName,
        [FromQuery] string? tier,
        [FromQuery] string? province,
        [FromQuery] string? city,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        var needsScope = !string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase)
                         && dataScope.AccessibleHospitalNames is { Count: > 0 };

        if (needsScope)
        {
            // Fetch all matching, then filter by scope, then paginate
            var all = await hospitalService.QueryHospitalsAsync(new HospitalQuery
            {
                HospitalName = hospitalName, Tier = tier, Province = province, City = city,
                Page = 1, Size = 50000
            }, cancellationToken);
            var scoped = HospitalScopeHelper.FilterByHospitalScope(dataScope, all.Items, x => x.HospitalName).ToList();
            var paged = scoped.Skip((page - 1) * size).Take(size).ToList();
            return Ok(ApiResponse<PagedResult<HospitalItemDto>>.Success(
                new PagedResult<HospitalItemDto> { Items = paged, Total = scoped.Count }));
        }

        var result = await hospitalService.QueryHospitalsAsync(new HospitalQuery
        {
            HospitalName = hospitalName, Tier = tier, Province = province, City = city,
            Page = page, Size = size
        }, cancellationToken);
        return Ok(ApiResponse<PagedResult<HospitalItemDto>>.Success(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var result = await hospitalService.GetByIdAsync(id, cancellationToken);
        if (result is null)
        {
            return NotFound(new { code = 404, message = "hospital not found" });
        }

        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        if (!HospitalScopeHelper.IsHospitalAccessible(dataScope, result.HospitalName))
        {
            return StatusCode(403, new { code = 403, message = "无权访问该医院" });
        }

        return Ok(ApiResponse<HospitalItemDto>.Success(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] HospitalUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var result = await hospitalService.CreateAsync(dto, cancellationToken);
        return Ok(ApiResponse<HospitalItemDto>.Success(result));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] HospitalUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var result = await hospitalService.UpdateAsync(id, dto, cancellationToken);
        if (result is null)
        {
            return NotFound(new { code = 404, message = "hospital not found" });
        }

        return Ok(ApiResponse<HospitalItemDto>.Success(result));
    }

    [HttpPut("{id:int}/rating")]
    public async Task<IActionResult> UpdateRating(int id, [FromBody] HospitalRatingDto dto, CancellationToken cancellationToken = default)
    {
        var result = await hospitalService.UpdateRatingAsync(id, dto, cancellationToken);
        if (result is null)
        {
            return NotFound(new { code = 404, message = "hospital not found" });
        }

        return Ok(ApiResponse<HospitalItemDto>.Success(result));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var result = await hospitalService.DeleteAsync(id, cancellationToken);
        if (!result)
        {
            return NotFound(new { code = 404, message = "hospital not found" });
        }

        return Ok(new { code = 200, message = "success" });
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? hospitalName,
        [FromQuery] string? tier,
        [FromQuery] string? province,
        [FromQuery] string? city,
        CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);

        var result = await hospitalService.QueryHospitalsAsync(new HospitalQuery
        {
            HospitalName = hospitalName,
            Tier = tier,
            Province = province,
            City = city,
            Page = 1,
            Size = 50000
        }, cancellationToken);

        var items = HospitalScopeHelper.FilterByHospitalScope(dataScope, result.Items, x => x.HospitalName).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("\uFEFFID,医院名称,等级,省份,城市,地址,联系人,联系电话,科室数,产品数,合同数,EMR评级,互联互通评级");
        foreach (var item in items)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(item.Id.ToString()),
                EscapeCsv(item.HospitalName),
                EscapeCsv(item.Tier),
                EscapeCsv(item.Province),
                EscapeCsv(item.City),
                EscapeCsv(item.Address),
                EscapeCsv(item.ContactPerson),
                EscapeCsv(item.ContactPhone),
                EscapeCsv(item.DepartmentCount),
                EscapeCsv(item.ProductCount.ToString()),
                EscapeCsv(item.ContractCount.ToString()),
                EscapeCsv(item.EmrRatingLevel ?? ""),
                EscapeCsv(item.InteropRatingLevel ?? "")));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"医院信息-{DateTime.Now:yyyyMMddHHmmss}.csv");
    }

    private static string EscapeCsv(string value)
    {
        var text = value ?? string.Empty;
        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
        {
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }
        return text;
    }
}
