using PMS.Application.Contracts.Product;
using PMS.Application.Models;
using PMS.Application.Models.Product;

namespace PMS.Infrastructure.Services;

public class InMemoryProductService : IProductService
{
    private const string StateKey = "products";
    private static readonly List<ProductItemDto> Products = SqliteJsonStore.LoadOrSeed(StateKey, BuildSeedData);

    public Task<ProductSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var summary = new ProductSummaryDto
        {
            Total = Products.Count,
            ActiveCount = Products.Count(x => x.Status == "运行中"),
            PilotCount = Products.Count(x => x.Status == "试运行"),
            RetiredCount = Products.Count(x => x.Status == "已停用")
        };

        return Task.FromResult(summary);
    }

    public Task<PagedResult<ProductItemDto>> QueryAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        var list = Products
            .Select(HydrateDeployCount)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.ProductName))
        {
            list = list.Where(x => x.ProductName.Contains(query.ProductName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            list = list.Where(x => x.Category == query.Category);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            list = list.Where(x => x.Status == query.Status);
        }

        var total = list.Count();
        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.Size <= 0 ? 20 : query.Size;

        var items = list
            .OrderByDescending(x => x.DeployHospitalCount)
            .ThenBy(x => x.ProductName)
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return Task.FromResult(new PagedResult<ProductItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            Size = size
        });
    }

    public Task<ProductItemDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = Products.FirstOrDefault(x => x.Id == id);
        return Task.FromResult(item is null ? null : HydrateDeployCount(item));
    }

    public Task<ProductItemDto> CreateAsync(ProductUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var nextId = Products.Count == 0 ? 1 : Products.Max(x => x.Id) + 1;
        var item = new ProductItemDto
        {
            Id = nextId,
            ProductName = dto.ProductName,
            Version = dto.Version,
            Category = dto.Category,
            Status = dto.Status,
            DeployHospitalCount = dto.DeployHospitalCount,
            CreatedAt = DateTime.Now
        };

        Products.Add(item);
        Persist();
        return Task.FromResult(HydrateDeployCount(item));
    }

    public Task<ProductItemDto?> UpdateAsync(int id, ProductUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var current = Products.FirstOrDefault(x => x.Id == id);
        if (current is null)
        {
            return Task.FromResult<ProductItemDto?>(null);
        }

        current.ProductName = dto.ProductName;
        current.Version = dto.Version;
        current.Category = dto.Category;
        current.Status = dto.Status;
        current.DeployHospitalCount = dto.DeployHospitalCount;
        Persist();

        return Task.FromResult<ProductItemDto?>(HydrateDeployCount(current));
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var current = Products.FirstOrDefault(x => x.Id == id);
        if (current is null)
        {
            return Task.FromResult(false);
        }

        Products.Remove(current);
        Persist();
        return Task.FromResult(true);
    }

    private static void Persist()
    {
        SqliteJsonStore.Save(StateKey, Products);
    }

    private static ProductItemDto HydrateDeployCount(ProductItemDto item)
    {
        var currentHospitalCount = InMemoryProjectDataStore.CountDistinctHospitals();
        var linkedDeployCount = Math.Min(Math.Max(item.DeployHospitalCount, 0), currentHospitalCount);

        return new ProductItemDto
        {
            Id = item.Id,
            ProductName = item.ProductName,
            Version = item.Version,
            Category = item.Category,
            Status = item.Status,
            DeployHospitalCount = linkedDeployCount,
            CreatedAt = item.CreatedAt
        };
    }

    private static List<ProductItemDto> BuildSeedData()
    {
        return
        [
            new() { Id = 1, ProductName = "住院电子病历V6", Version = "V6", Category = "EMR", Status = "运行中", DeployHospitalCount = 420, CreatedAt = DateTime.Now.AddMonths(-12) },
            new() { Id = 2, ProductName = "临床路径V6", Version = "V6", Category = "临床辅助", Status = "运行中", DeployHospitalCount = 280, CreatedAt = DateTime.Now.AddMonths(-10) },
            new() { Id = 3, ProductName = "CDSS", Version = "V2", Category = "临床辅助", Status = "运行中", DeployHospitalCount = 195, CreatedAt = DateTime.Now.AddMonths(-8) },
            new() { Id = 4, ProductName = "病案归档", Version = "V3", Category = "管理", Status = "运行中", DeployHospitalCount = 170, CreatedAt = DateTime.Now.AddMonths(-7) },
            new() { Id = 5, ProductName = "AI内涵质控", Version = "V1", Category = "AI", Status = "试运行", DeployHospitalCount = 130, CreatedAt = DateTime.Now.AddMonths(-5) },
            new() { Id = 6, ProductName = "互联网医院", Version = "V1", Category = "移动", Status = "已停用", DeployHospitalCount = 42, CreatedAt = DateTime.Now.AddMonths(-4) }
        ];
    }
}
