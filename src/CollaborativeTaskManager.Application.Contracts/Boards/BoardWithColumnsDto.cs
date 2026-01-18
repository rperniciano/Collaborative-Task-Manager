using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace CollaborativeTaskManager.Application.Contracts.Boards;

/// <summary>
/// DTO for Board with its columns.
/// </summary>
public class BoardWithColumnsDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public DateTime CreationTime { get; set; }
    public List<ColumnDto> Columns { get; set; } = new();
}
