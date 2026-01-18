using System;
using Volo.Abp.Application.Dtos;

namespace CollaborativeTaskManager.Application.Contracts.Boards;

/// <summary>
/// DTO for Board entity.
/// </summary>
public class BoardDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public DateTime CreationTime { get; set; }
}
