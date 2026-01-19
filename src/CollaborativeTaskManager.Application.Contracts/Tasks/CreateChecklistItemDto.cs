using System;
using System.ComponentModel.DataAnnotations;

namespace CollaborativeTaskManager.Application.Contracts.Tasks;

/// <summary>
/// Data transfer object for creating a new checklist item.
/// </summary>
public class CreateChecklistItemDto
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Text { get; set; } = string.Empty;
}
