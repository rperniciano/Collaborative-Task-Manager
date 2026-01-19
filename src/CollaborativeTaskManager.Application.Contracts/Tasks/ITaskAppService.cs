using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace CollaborativeTaskManager.Application.Contracts.Tasks;

/// <summary>
/// Application service interface for Task operations.
/// </summary>
public interface ITaskAppService : IApplicationService
{
    /// <summary>
    /// Gets all tasks for a specific column.
    /// </summary>
    Task<List<TaskDto>> GetTasksByColumnAsync(Guid columnId);

    /// <summary>
    /// Gets all tasks for the current user's board.
    /// </summary>
    Task<List<TaskDto>> GetAllTasksAsync();

    /// <summary>
    /// Gets a specific task by ID.
    /// </summary>
    Task<TaskDto> GetAsync(Guid id);

    /// <summary>
    /// Creates a new task.
    /// </summary>
    Task<TaskDto> CreateAsync(CreateTaskDto input);

    /// <summary>
    /// Deletes a task.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Adds a checklist item to a task.
    /// </summary>
    Task<ChecklistItemDto> AddChecklistItemAsync(Guid taskId, CreateChecklistItemDto input);

    /// <summary>
    /// Updates a checklist item.
    /// </summary>
    Task<ChecklistItemDto> UpdateChecklistItemAsync(Guid taskId, Guid itemId, UpdateChecklistItemDto input);

    /// <summary>
    /// Deletes a checklist item.
    /// </summary>
    Task DeleteChecklistItemAsync(Guid taskId, Guid itemId);

    /// <summary>
    /// Gets all checklist items for a task.
    /// </summary>
    Task<List<ChecklistItemDto>> GetChecklistItemsAsync(Guid taskId);
}
