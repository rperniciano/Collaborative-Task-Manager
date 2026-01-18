using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace CollaborativeTaskManager.Domain.Boards;

/// <summary>
/// Represents a collaborative task board owned by a user.
/// </summary>
public class Board : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// The name of the board.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the user who owns this board.
    /// </summary>
    public Guid OwnerId { get; set; }

    protected Board()
    {
    }

    public Board(Guid id, string name, Guid ownerId) : base(id)
    {
        Name = name;
        OwnerId = ownerId;
    }
}
