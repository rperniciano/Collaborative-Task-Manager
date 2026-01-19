using System.ComponentModel.DataAnnotations;

namespace CollaborativeTaskManager.Application.Contracts.Boards;

/// <summary>
/// DTO for creating a board invitation.
/// </summary>
public class CreateInviteDto
{
    /// <summary>
    /// The email address to send the invitation to.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
}
