using System;

namespace CollaborativeTaskManager.Application.Contracts.Boards;

/// <summary>
/// DTO for representing a board member.
/// </summary>
public class MemberDto
{
    /// <summary>
    /// The unique identifier of the membership.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user ID of the member.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The email of the member.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the member.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// When the member joined the board.
    /// </summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// Whether this member is the owner of the board.
    /// </summary>
    public bool IsOwner { get; set; }
}
