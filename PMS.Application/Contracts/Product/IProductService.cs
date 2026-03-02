using PMS.Application.Models;
using PMS.Application.Models.Product;

namespace PMS.Application.Contracts.Product;

public interface IProductService
{
    Task<ProductSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<ProductItemDto>> QueryAsync(ProductQuery query, CancellationToken cancellationToken = default);
    Task<ProductItemDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductItemDto> CreateAsync(ProductUpsertDto dto, CancellationToken cancellationToken = default);
    Task<ProductItemDto?> UpdateAsync(int id, ProductUpsertDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class ProductQuery
{
    public string? ProductName { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}
