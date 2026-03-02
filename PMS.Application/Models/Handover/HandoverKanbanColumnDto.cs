namespace PMS.Application.Models.Handover;

public class HandoverKanbanColumnDto
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }
    public IReadOnlyList<HandoverItemDto> Items { get; set; } = [];
}
