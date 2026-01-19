using System;
using System.ComponentModel.DataAnnotations;

namespace CollaborativeTaskManager.Application.Contracts.Tasks;

/// <summary>
/// Data transfer object for creating a new task.
/// </summary>
public class CreateTaskDto
{
    /// <summary>
    /// The ID of the column to create the task in.
    /// </summary>
    [Required]
    public Guid ColumnId { get; set; }

    /// <summary>
    /// The title of the task (required).
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the task.
    /// </summary>
    [StringLength(4000)]
    public string? Description { get; set; }

    /// <summary>
    /// Optional due date for the task.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Priority level: 0 = Low, 1 = Medium, 2 = High. Default is 1 (Medium).
    /// </summary>
    [Range(0, 2)]
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Optional assignee user ID.
    /// </summary>
    public Guid? AssigneeId { get; set; }
}
