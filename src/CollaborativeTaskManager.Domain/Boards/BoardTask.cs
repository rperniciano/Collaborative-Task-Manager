using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace CollaborativeTaskManager.Domain.Boards;

/// <summary>
/// Represents a task within a column on a board.
/// Named BoardTask to avoid conflict with System.Threading.Tasks.Task.
/// </summary>
public class BoardTask : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// The ID of the column this task belongs to.
    /// </summary>
    public Guid ColumnId { get; set; }

    /// <summary>
    /// The title of the task (required).
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the task.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional due date for the task.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Priority level: 0 = Low, 1 = Medium, 2 = High
    /// </summary>
    public int Priority { get; set; } = 1; // Default to Medium

    /// <summary>
    /// Optional assignee user ID.
    /// </summary>
    public Guid? AssigneeId { get; set; }

    /// <summary>
    /// The order of this task within its column.
    /// </summary>
    public int Order { get; set; }

    protected BoardTask()
    {
    }

    public BoardTask(Guid id, Guid columnId, string title, int order) : base(id)
    {
        ColumnId = columnId;
        Title = title;
        Order = order;
    }
}
