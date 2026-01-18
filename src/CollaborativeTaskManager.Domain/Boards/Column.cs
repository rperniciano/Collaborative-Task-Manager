using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace CollaborativeTaskManager.Domain.Boards;

/// <summary>
/// Represents a column within a board (e.g., To-Do, In Progress, Done).
/// </summary>
public class Column : FullAuditedEntity<Guid>
{
    /// <summary>
    /// The ID of the board this column belongs to.
    /// </summary>
    public Guid BoardId { get; set; }

    /// <summary>
    /// The name of the column.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The order of this column within the board.
    /// </summary>
    public int Order { get; set; }

    protected Column()
    {
    }

    public Column(Guid id, Guid boardId, string name, int order) : base(id)
    {
        BoardId = boardId;
        Name = name;
        Order = order;
    }
}
