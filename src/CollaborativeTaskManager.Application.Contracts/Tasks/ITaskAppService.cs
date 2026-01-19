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
}
