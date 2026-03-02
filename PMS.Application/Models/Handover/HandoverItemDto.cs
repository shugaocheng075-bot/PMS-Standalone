namespace PMS.Application.Models.Handover;

public class HandoverItemDto
{
    public long Id { get; set; }
    public string HandoverNo { get; set; } = string.Empty;
    public string HospitalName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string FromGroup { get; set; } = string.Empty;
    public string FromOwner { get; set; } = string.Empty;
    public string ToOwner { get; set; } = string.Empty;
    public string Batch { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime? EmailSentDate { get; set; }
}
