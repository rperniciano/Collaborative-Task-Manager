using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace CollaborativeTaskManager.Domain.Boards;

/// <summary>
/// Represents an invitation to join a board.
/// </summary>
public class BoardInvite : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// The ID of the board the invitation is for.
    /// </summary>
    public Guid BoardId { get; set; }

    /// <summary>
    /// The email address the invitation was sent to.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Unique token for the invite link.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// When the invitation expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    protected BoardInvite()
    {
    }

    public BoardInvite(Guid id, Guid boardId, string email, string token, DateTime expiresAt) : base(id)
    {
        BoardId = boardId;
        Email = email;
        Token = token;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Checks if the invite has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
