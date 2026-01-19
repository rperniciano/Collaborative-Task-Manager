using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace CollaborativeTaskManager.Domain.Boards;

/// <summary>
/// Represents a member of a board (excluding the owner).
/// </summary>
public class BoardMember : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// The ID of the board.
    /// </summary>
    public Guid BoardId { get; set; }

    /// <summary>
    /// The ID of the user who is a member of this board.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// When the user joined the board.
    /// </summary>
    public DateTime JoinedAt { get; set; }

    protected BoardMember()
    {
    }

    public BoardMember(Guid id, Guid boardId, Guid userId, DateTime joinedAt) : base(id)
    {
        BoardId = boardId;
        UserId = userId;
        JoinedAt = joinedAt;
    }
}
