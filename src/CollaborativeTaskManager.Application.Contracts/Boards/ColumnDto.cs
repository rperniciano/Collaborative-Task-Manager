using System;
using Volo.Abp.Application.Dtos;

namespace CollaborativeTaskManager.Application.Contracts.Boards;

/// <summary>
/// DTO for Column entity.
/// </summary>
public class ColumnDto : EntityDto<Guid>
{
    public Guid BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
}
