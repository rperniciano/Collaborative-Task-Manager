using System;
using System.Threading.Tasks;
using CollaborativeTaskManager.Hubs;
using CollaborativeTaskManager.Realtime;
using Microsoft.AspNetCore.SignalR;
using Volo.Abp.DependencyInjection;

namespace CollaborativeTaskManager.Realtime;

/// <summary>
/// Implementation of real-time notification service using SignalR.
/// </summary>
public class RealTimeNotificationService : IRealTimeNotificationService, ITransientDependency
{
    private readonly IHubContext<BoardHub> _hubContext;

    public RealTimeNotificationService(IHubContext<BoardHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastTaskCreatedAsync(string boardId, object task)
    {
        var groupName = $"board-{boardId}";
        await _hubContext.Clients.Group(groupName).SendAsync("TaskCreated", task);
        Console.WriteLine($"[SignalR] Broadcasted TaskCreated to board {boardId}");
    }

    public async Task BroadcastTaskUpdatedAsync(string boardId, object task)
    {
        var groupName = $"board-{boardId}";
        await _hubContext.Clients.Group(groupName).SendAsync("TaskUpdated", task);
        Console.WriteLine($"[SignalR] Broadcasted TaskUpdated to board {boardId}");
    }

    public async Task BroadcastTaskDeletedAsync(string boardId, string taskId)
    {
        var groupName = $"board-{boardId}";
        await _hubContext.Clients.Group(groupName).SendAsync("TaskDeleted", taskId);
        Console.WriteLine($"[SignalR] Broadcasted TaskDeleted to board {boardId}");
    }

    public async Task BroadcastTaskMovedAsync(string boardId, string taskId, string newColumnId, int newOrder)
    {
        var groupName = $"board-{boardId}";
        await _hubContext.Clients.Group(groupName).SendAsync("TaskMoved", taskId, newColumnId, newOrder);
        Console.WriteLine($"[SignalR] Broadcasted TaskMoved to board {boardId}");
    }

    public async Task BroadcastColumnsReorderedAsync(string boardId, object columns)
    {
        var groupName = $"board-{boardId}";
        await _hubContext.Clients.Group(groupName).SendAsync("ColumnReordered", columns);
        Console.WriteLine($"[SignalR] Broadcasted ColumnReordered to board {boardId}");
    }
}
