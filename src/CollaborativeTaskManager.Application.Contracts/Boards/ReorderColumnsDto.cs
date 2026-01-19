using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CollaborativeTaskManager.Application.Contracts.Boards;

/// <summary>
/// DTO for reordering columns on a board.
/// </summary>
public class ReorderColumnsDto
{
    /// <summary>
    /// The column IDs in their new order (index = new order position).
    /// </summary>
    [Required]
    public List<Guid> ColumnIds { get; set; } = new();
}
