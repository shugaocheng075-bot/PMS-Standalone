namespace PMS.Application.Models.Handover;

public class HandoverQuery
{
    public string? Stage { get; set; }
    public string? Batch { get; set; }
    public string? Type { get; set; }
    public string? FromGroup { get; set; }
    public string? ToOwner { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}
