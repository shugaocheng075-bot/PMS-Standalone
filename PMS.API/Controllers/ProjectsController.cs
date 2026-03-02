using Microsoft.AspNetCore.Mvc;
using PMS.API.Models;
using PMS.Application.Contracts;
using PMS.Application.Models;
using PMS.Domain.Entities;

namespace PMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController(IProjectQueryService projectQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? hospitalName,
        [FromQuery] string? productName,
        [FromQuery] string? province,
        [FromQuery] string? groupName,
        [FromQuery] string? hospitalLevel,
        [FromQuery] string? contractStatus,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await projectQueryService.QueryAsync(new ProjectQuery
        {
            HospitalName = hospitalName,
            ProductName = productName,
            Province = province,
            GroupName = groupName,
            HospitalLevel = hospitalLevel,
            ContractStatus = contractStatus,
            Page = page,
            Size = size
        }, cancellationToken);

        return Ok(ApiResponse<PagedResult<ProjectEntity>>.Success(result));
    }
}
