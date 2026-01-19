using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CollaborativeTaskManager.Application.Contracts.Tasks;
using CollaborativeTaskManager.Domain.Boards;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace CollaborativeTaskManager.Application.Tasks;

/// <summary>
/// Application service for Task operations.
/// </summary>
[Authorize]
public class TaskAppService : CollaborativeTaskManagerAppService, ITaskAppService
{
    private readonly IRepository<BoardTask, Guid> _taskRepository;
    private readonly IRepository<Column, Guid> _columnRepository;
    private readonly IRepository<Board, Guid> _boardRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;

    public TaskAppService(
        IRepository<BoardTask, Guid> taskRepository,
        IRepository<Column, Guid> columnRepository,
        IRepository<Board, Guid> boardRepository,
        IRepository<IdentityUser, Guid> userRepository)
    {
        _taskRepository = taskRepository;
        _columnRepository = columnRepository;
        _boardRepository = boardRepository;
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    public async Task<List<TaskDto>> GetTasksByColumnAsync(Guid columnId)
    {
        var currentUserId = CurrentUser.Id!.Value;

        // Verify user has access to this column via board ownership
        var column = await _columnRepository.GetAsync(columnId);
        var board = await _boardRepository.GetAsync(column.BoardId);

        if (board.OwnerId != currentUserId)
        {
            throw new BusinessException("You do not have access to this board.");
        }

        var tasks = await _taskRepository.GetListAsync(t => t.ColumnId == columnId);
        return await MapTasksToDtosAsync(tasks);
    }

    /// <inheritdoc />
    public async Task<List<TaskDto>> GetAllTasksAsync()
    {
        var currentUserId = CurrentUser.Id!.Value;

        // Get the user's board
        var board = await _boardRepository.FirstOrDefaultAsync(b => b.OwnerId == currentUserId);
        if (board == null)
        {
            return new List<TaskDto>();
        }

        // Get all columns for this board
        var columns = await _columnRepository.GetListAsync(c => c.BoardId == board.Id);
        var columnIds = columns.Select(c => c.Id).ToList();

        // Get all tasks for these columns
        var tasks = await _taskRepository.GetListAsync(t => columnIds.Contains(t.ColumnId));
        return await MapTasksToDtosAsync(tasks);
    }

    /// <inheritdoc />
    public async Task<TaskDto> GetAsync(Guid id)
    {
        var currentUserId = CurrentUser.Id!.Value;
        var task = await _taskRepository.GetAsync(id);

        // Verify access
        var column = await _columnRepository.GetAsync(task.ColumnId);
        var board = await _boardRepository.GetAsync(column.BoardId);

        if (board.OwnerId != currentUserId)
        {
            throw new BusinessException("You do not have access to this task.");
        }

        return await MapTaskToDtoAsync(task);
    }

    /// <inheritdoc />
    public async Task<TaskDto> CreateAsync(CreateTaskDto input)
    {
        var currentUserId = CurrentUser.Id!.Value;

        // Verify user has access to this column via board ownership
        var column = await _columnRepository.GetAsync(input.ColumnId);
        var board = await _boardRepository.GetAsync(column.BoardId);

        if (board.OwnerId != currentUserId)
        {
            throw new BusinessException("You do not have access to this board.");
        }

        // Get the next order number for this column
        var existingTasks = await _taskRepository.GetListAsync(t => t.ColumnId == input.ColumnId);
        var maxOrder = existingTasks.Any() ? existingTasks.Max(t => t.Order) : -1;

        var task = new BoardTask(
            GuidGenerator.Create(),
            input.ColumnId,
            input.Title,
            maxOrder + 1
        )
        {
            Description = input.Description,
            DueDate = input.DueDate,
            Priority = input.Priority,
            AssigneeId = input.AssigneeId
        };

        await _taskRepository.InsertAsync(task);

        return await MapTaskToDtoAsync(task);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        var currentUserId = CurrentUser.Id!.Value;
        var task = await _taskRepository.GetAsync(id);

        // Verify access
        var column = await _columnRepository.GetAsync(task.ColumnId);
        var board = await _boardRepository.GetAsync(column.BoardId);

        if (board.OwnerId != currentUserId)
        {
            throw new BusinessException("You do not have access to delete this task.");
        }

        await _taskRepository.DeleteAsync(id);
    }

    private async Task<List<TaskDto>> MapTasksToDtosAsync(List<BoardTask> tasks)
    {
        var result = new List<TaskDto>();
        foreach (var task in tasks.OrderBy(t => t.Order))
        {
            result.Add(await MapTaskToDtoAsync(task));
        }
        return result;
    }

    private async Task<TaskDto> MapTaskToDtoAsync(BoardTask task)
    {
        string? assigneeName = null;
        if (task.AssigneeId.HasValue)
        {
            var user = await _userRepository.FindAsync(task.AssigneeId.Value);
            assigneeName = user?.Name ?? user?.UserName;
        }

        return new TaskDto
        {
            Id = task.Id,
            ColumnId = task.ColumnId,
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            Priority = task.Priority,
            AssigneeId = task.AssigneeId,
            AssigneeName = assigneeName,
            Order = task.Order,
            CreationTime = task.CreationTime,
            LastModificationTime = task.LastModificationTime
        };
    }
}
