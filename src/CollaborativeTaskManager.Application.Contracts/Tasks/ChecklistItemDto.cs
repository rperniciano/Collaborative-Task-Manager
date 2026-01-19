using System;

namespace CollaborativeTaskManager.Application.Contracts.Tasks;

/// <summary>
/// Data transfer object for a checklist item.
/// </summary>
public class ChecklistItemDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int Order { get; set; }
}
