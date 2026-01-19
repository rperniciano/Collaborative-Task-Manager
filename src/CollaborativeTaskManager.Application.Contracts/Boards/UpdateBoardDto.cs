using System.ComponentModel.DataAnnotations;

namespace CollaborativeTaskManager.Application.Contracts.Boards;

/// <summary>
/// DTO for updating a board.
/// </summary>
public class UpdateBoardDto
{
    /// <summary>
    /// The new name for the board.
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}
