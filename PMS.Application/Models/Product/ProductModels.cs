namespace PMS.Application.Models.Product;

public class ProductSummaryDto
{
    public int Total { get; set; }
    public int ActiveCount { get; set; }
    public int PilotCount { get; set; }
    public int RetiredCount { get; set; }
}

public class ProductItemDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int DeployHospitalCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProductUpsertDto
{
    public string ProductName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int DeployHospitalCount { get; set; }
}
