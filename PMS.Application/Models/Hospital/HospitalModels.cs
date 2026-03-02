namespace PMS.Application.Models.Hospital;

public class HospitalSummaryDto
{
    public int Total { get; set; }
    public int ThreeTierCount { get; set; }
    public int TwoTierCount { get; set; }
    public int OneTierCount { get; set; }
    public Dictionary<string, int> RegionCounts { get; set; } = new();
}

public class HospitalItemDto
{
    public int Id { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string DepartmentCount { get; set; } = string.Empty;
    public string? EmrRatingLevel { get; set; }
    public string? InteropRatingLevel { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class HospitalUpsertDto
{
    public string HospitalName { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string DepartmentCount { get; set; } = string.Empty;
}

public class HospitalRatingDto
{
    public string? EmrRatingLevel { get; set; }
    public string? InteropRatingLevel { get; set; }
}
