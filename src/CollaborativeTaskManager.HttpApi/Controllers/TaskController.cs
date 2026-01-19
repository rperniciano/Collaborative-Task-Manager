using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CollaborativeTaskManager.Application.Contracts.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollaborativeTaskManager.Controllers;

/// <summary>
/// Controller for task-related operations including checklist items.
/// </summary>
[Route("api/app/task")]
[Authorize]
public class TaskController : CollaborativeTaskManagerController
{
    private readonly ITaskAppService _taskAppService;

    public TaskController(ITaskAppService taskAppService)
    {
        _taskAppService = taskAppService;
    }

    /// <summary>
    /// Gets all tasks for the current user's board.
    /// </summary>
    [HttpGet("tasks")]
    public Task<List<TaskDto>> GetAllTasksAsync()
    {
        return _taskAppService.GetAllTasksAsync();
    }

    /// <summary>
    /// Gets a specific task by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public Task<TaskDto> GetAsync(Guid id)
    {
        return _taskAppService.GetAsync(id);
    }

    /// <summary>
    /// Creates a new task.
    /// </summary>
    [HttpPost]
    public Task<TaskDto> CreateAsync([FromBody] CreateTaskDto input)
    {
        return _taskAppService.CreateAsync(input);
    }

    /// <summary>
    /// Deletes a task.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public Task DeleteAsync(Guid id)
    {
        return _taskAppService.DeleteAsync(id);
    }

    // --- Checklist Item Endpoints ---

    /// <summary>
    /// Gets all checklist items for a task.
    /// </summary>
    [HttpGet("{taskId:guid}/checklist-items")]
    public Task<List<ChecklistItemDto>> GetChecklistItemsAsync(Guid taskId)
    {
        return _taskAppService.GetChecklistItemsAsync(taskId);
    }

    /// <summary>
    /// Adds a checklist item to a task.
    /// </summary>
    [HttpPost("{taskId:guid}/checklist-items")]
    public Task<ChecklistItemDto> AddChecklistItemAsync(Guid taskId, [FromBody] CreateChecklistItemDto input)
    {
        return _taskAppService.AddChecklistItemAsync(taskId, input);
    }

    /// <summary>
    /// Updates a checklist item.
    /// </summary>
    [HttpPut("{taskId:guid}/checklist-items/{itemId:guid}")]
    public Task<ChecklistItemDto> UpdateChecklistItemAsync(Guid taskId, Guid itemId, [FromBody] UpdateChecklistItemDto input)
    {
        return _taskAppService.UpdateChecklistItemAsync(taskId, itemId, input);
    }

    /// <summary>
    /// Deletes a checklist item.
    /// </summary>
    [HttpDelete("{taskId:guid}/checklist-items/{itemId:guid}")]
    public Task DeleteChecklistItemAsync(Guid taskId, Guid itemId)
    {
        return _taskAppService.DeleteChecklistItemAsync(taskId, itemId);
    }
}
