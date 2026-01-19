using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CollaborativeTaskManager.Application.Contracts.Tasks;
using CollaborativeTaskManager.Domain.Boards;
using CollaborativeTaskManager.Realtime;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace CollaborativeTaskManager.Application.Tasks;

/// <summary>
/// Application service for Task operations.
/// </summary>
[RemoteService(IsEnabled = false)]
[Authorize]
public class TaskAppService : CollaborativeTaskManagerAppService, ITaskAppService
{
    private readonly IRepository<BoardTask, Guid> _taskRepository;
    private readonly IRepository<Column, Guid> _columnRepository;
    private readonly IRepository<Board, Guid> _boardRepository;
    private readonly IRepository<BoardMember, Guid> _memberRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IRepository<ChecklistItem, Guid> _checklistItemRepository;
    private readonly IRealTimeNotificationService _realTimeNotificationService;

    public TaskAppService(
        IRepository<BoardTask, Guid> taskRepository,
        IRepository<Column, Guid> columnRepository,
        IRepository<Board, Guid> boardRepository,
        IRepository<BoardMember, Guid> memberRepository,
        IRepository<IdentityUser, Guid> userRepository,
        IRepository<ChecklistItem, Guid> checklistItemRepository,
        IRealTimeNotificationService realTimeNotificationService)
    {
        _taskRepository = taskRepository;
        _columnRepository = columnRepository;
        _boardRepository = boardRepository;
        _memberRepository = memberRepository;
        _userRepository = userRepository;
        _checklistItemRepository = checklistItemRepository;
        _realTimeNotificationService = realTimeNotificationService;
    }

    /// <summary>
    /// Check if the current user has access to the specified board (owner or member).
    /// </summary>
    private async Task<bool> HasBoardAccessAsync(Guid boardId, Guid userId)
    {
        var board = await _boardRepository.GetAsync(boardId);
        if (board.OwnerId == userId)
        {
            return true;
        }

        // Check if user is a member
        var membership = await _memberRepository.FirstOrDefaultAsync(m => m.BoardId == boardId && m.UserId == userId);
        return membership != null;
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

        var taskDto = await MapTaskToDtoAsync(task);

        // Broadcast to other users in the board via SignalR
        await _realTimeNotificationService.BroadcastTaskCreatedAsync(board.Id.ToString(), taskDto);

        return taskDto;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        var currentUserId = CurrentUser.Id!.Value;
        var task = await _taskRepository.GetAsync(id);

        // Verify access (owner or member can delete tasks)
        var column = await _columnRepository.GetAsync(task.ColumnId);
        var board = await _boardRepository.GetAsync(column.BoardId);

        if (!await HasBoardAccessAsync(board.Id, currentUserId))
        {
            throw new BusinessException("You do not have access to delete this task.");
        }

        await _taskRepository.DeleteAsync(id);

        // Broadcast to other users in the board via SignalR
        await _realTimeNotificationService.BroadcastTaskDeletedAsync(board.Id.ToString(), id.ToString());
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

        // Get checklist items for this task (with graceful fallback if table doesn't exist)
        var checklistItemDtos = new List<ChecklistItemDto>();
        try
        {
            var checklistItems = await _checklistItemRepository.GetListAsync(c => c.TaskId == task.Id);
            checklistItemDtos = checklistItems
                .OrderBy(c => c.Order)
                .Select(c => new ChecklistItemDto
                {
                    Id = c.Id,
                    TaskId = c.TaskId,
                    Text = c.Text,
                    IsCompleted = c.IsCompleted,
                    Order = c.Order
                })
                .ToList();
        }
        catch (Exception)
        {
            // Checklist items table may not exist yet - continue without checklist items
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
            LastModificationTime = task.LastModificationTime,
            ChecklistItems = checklistItemDtos
        };
    }

    /// <inheritdoc />
    public async Task<ChecklistItemDto> AddChecklistItemAsync(Guid taskId, CreateChecklistItemDto input)
    {
        var currentUserId = CurrentUser.Id!.Value;
        var task = await _taskRepository.GetAsync(taskId);

        // Verify access
        var column = await _columnRepository.GetAsync(task.ColumnId);
        var board = await _boardRepository.GetAsync(column.BoardId);

        if (!await HasBoardAccessAsync(board.Id, currentUserId))
        {
            throw new BusinessException("You do not have access to this task.");
        }

        // Get the next order number
        var existingItems = await _checklistItemRepository.GetListAsync(c => c.TaskId == taskId);
        var maxOrder = existingItems.Any() ? existingItems.Max(c => c.Order) : -1;

        var checklistItem = new ChecklistItem(
            GuidGenerator.Create(),
            taskId,
            input.Text,
            maxOrder + 1
        );

        await _checklistItemRepository.InsertAsync(checklistItem);

        return new ChecklistItemDto
        {
            Id = checklistItem.Id,
            TaskId = checklistItem.TaskId,
            Text = checklistItem.Text,
            IsCompleted = checklistItem.IsCompleted,
            Order = checklistItem.Order
        };
    }

    /// <inheritdoc />
    public async Task<ChecklistItemDto> UpdateChecklistItemAsync(Guid taskId, Guid itemId, UpdateChecklistItemDto input)
    {
        var currentUserId = CurrentUser.Id!.Value;
        var task = await _taskRepository.GetAsync(taskId);

        // Verify access
        var column = await _columnRepository.GetAsync(task.ColumnId);
        var board = await _boardRepository.GetAsync(column.BoardId);

        if (!await HasBoardAccessAsync(board.Id, currentUserId))
        {
            throw new BusinessException("You do not have access to this task.");
        }

        var checklistItem = await _checklistItemRepository.GetAsync(itemId);
        if (checklistItem.TaskId != taskId)
        {
            throw new BusinessException("Checklist item does not belong to this task.");
        }

        if (input.Text != null)
        {
            checklistItem.Text = input.Text;
        }

        if (input.IsCompleted.HasValue)
        {
            checklistItem.IsCompleted = input.IsCompleted.Value;
        }

        await _checklistItemRepository.UpdateAsync(checklistItem);

        return new ChecklistItemDto
        {
            Id = checklistItem.Id,
            TaskId = checklistItem.TaskId,
            Text = checklistItem.Text,
            IsCompleted = checklistItem.IsCompleted,
            Order = checklistItem.Order
        };
    }

    /// <inheritdoc />
    public async Task DeleteChecklistItemAsync(Guid taskId, Guid itemId)
    {
        var currentUserId = CurrentUser.Id!.Value;
        var task = await _taskRepository.GetAsync(taskId);

        // Verify access
        var column = await _columnRepository.GetAsync(task.ColumnId);
        var board = await _boardRepository.GetAsync(column.BoardId);

        if (!await HasBoardAccessAsync(board.Id, currentUserId))
        {
            throw new BusinessException("You do not have access to this task.");
        }

        var checklistItem = await _checklistItemRepository.GetAsync(itemId);
        if (checklistItem.TaskId != taskId)
        {
            throw new BusinessException("Checklist item does not belong to this task.");
        }

        await _checklistItemRepository.DeleteAsync(itemId);
    }

    /// <inheritdoc />
    public async Task<List<ChecklistItemDto>> GetChecklistItemsAsync(Guid taskId)
    {
        var currentUserId = CurrentUser.Id!.Value;
        var task = await _taskRepository.GetAsync(taskId);

        // Verify access
        var column = await _columnRepository.GetAsync(task.ColumnId);
        var board = await _boardRepository.GetAsync(column.BoardId);

        if (!await HasBoardAccessAsync(board.Id, currentUserId))
        {
            throw new BusinessException("You do not have access to this task.");
        }

        var checklistItems = await _checklistItemRepository.GetListAsync(c => c.TaskId == taskId);
        return checklistItems
            .OrderBy(c => c.Order)
            .Select(c => new ChecklistItemDto
            {
                Id = c.Id,
                TaskId = c.TaskId,
                Text = c.Text,
                IsCompleted = c.IsCompleted,
                Order = c.Order
            })
            .ToList();
    }
}
