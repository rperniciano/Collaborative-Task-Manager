using System;

namespace CollaborativeTaskManager.Application.Contracts.Boards;

/// <summary>
/// DTO for representing a board invitation.
/// </summary>
public class InviteDto
{
    /// <summary>
    /// The unique identifier of the invitation.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The ID of the board the invitation is for.
    /// </summary>
    public Guid BoardId { get; set; }

    /// <summary>
    /// The name of the board the invitation is for.
    /// </summary>
    public string BoardName { get; set; } = string.Empty;

    /// <summary>
    /// The email address the invitation was sent to.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The invitation token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// When the invitation expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When the invitation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Whether the invitation has expired.
    /// </summary>
    public bool IsExpired { get; set; }
}
