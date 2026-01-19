using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace CollaborativeTaskManager.Domain.Boards;

/// <summary>
/// Represents a checklist item within a task.
/// </summary>
public class ChecklistItem : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// The ID of the task this checklist item belongs to.
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// The text/content of the checklist item.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Whether this item is completed.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// The order of this item within the checklist.
    /// </summary>
    public int Order { get; set; }

    protected ChecklistItem()
    {
    }

    public ChecklistItem(Guid id, Guid taskId, string text, int order) : base(id)
    {
        TaskId = taskId;
        Text = text;
        Order = order;
        IsCompleted = false;
    }
}
