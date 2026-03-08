using Microsoft.AspNetCore.Mvc;
using PMS.API.Middleware;
using PMS.API.Models;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.Product;
using PMS.Application.Models;
using PMS.Application.Models.Product;

namespace PMS.API.Controllers.Product;

[ApiController]
[Route("api/products")]
public class ProductController(
    IProductService productService,
    IAccessControlService accessControlService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var result = await productService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<ProductSummaryDto>.Success(result));
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken = default)
    {
        var result = await productService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<ProductSummaryDto>.Success(result));
    }

    [HttpGet]
    public async Task<IActionResult> Query(
        [FromQuery] string? productName,
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await productService.QueryAsync(new ProductQuery
        {
            ProductName = productName,
            Category = category,
            Status = status,
            Page = page,
            Size = size
        }, cancellationToken);

        return Ok(ApiResponse<PagedResult<ProductItemDto>>.Success(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var result = await productService.GetByIdAsync(id, cancellationToken);
        if (result is null)
        {
            return NotFound(new { code = 404, message = "product not found" });
        }

        return Ok(ApiResponse<ProductItemDto>.Success(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, new { code = 403, message = "仅经理角色可创建产品" });
        }

        var result = await productService.CreateAsync(dto, cancellationToken);
        return Ok(ApiResponse<ProductItemDto>.Success(result));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, new { code = 403, message = "仅经理角色可修改产品" });
        }

        var result = await productService.UpdateAsync(id, dto, cancellationToken);
        if (result is null)
        {
            return NotFound(new { code = 404, message = "product not found" });
        }

        return Ok(ApiResponse<ProductItemDto>.Success(result));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, new { code = 403, message = "仅经理角色可删除产品" });
        }

        var result = await productService.DeleteAsync(id, cancellationToken);
        if (!result)
        {
            return NotFound(new { code = 404, message = "product not found" });
        }

        return Ok(new { code = 200, message = "success" });
    }

    [HttpPost("batch-delete")]
    public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteRequest request, CancellationToken cancellationToken = default)
    {
        var personnelId = HttpContext.GetCurrentPersonnelId();
        var dataScope = await accessControlService.GetDataScopeAsync(personnelId, cancellationToken);
        if (!string.Equals(dataScope.ScopeType, "all", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, new { code = 403, message = "仅经理角色可批量删除产品" });
        }

        if (request.Ids is not { Count: > 0 })
        {
            return BadRequest(new { code = 400, message = "ids is required" });
        }

        var count = await productService.BatchDeleteAsync(request.Ids, cancellationToken);
        return Ok(new { code = 200, message = $"成功删除 {count} 条产品" });
    }
}

public class BatchDeleteRequest
{
    public List<int> Ids { get; set; } = [];
}
