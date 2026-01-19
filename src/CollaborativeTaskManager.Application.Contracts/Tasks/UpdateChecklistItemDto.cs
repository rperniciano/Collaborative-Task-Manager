using System.ComponentModel.DataAnnotations;

namespace CollaborativeTaskManager.Application.Contracts.Tasks;

/// <summary>
/// Data transfer object for updating a checklist item.
/// </summary>
public class UpdateChecklistItemDto
{
    [StringLength(500, MinimumLength = 1)]
    public string? Text { get; set; }

    public bool? IsCompleted { get; set; }
}
