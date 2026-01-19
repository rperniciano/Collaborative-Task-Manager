using System.Threading.Tasks;

namespace CollaborativeTaskManager.Realtime;

/// <summary>
/// Service interface for broadcasting real-time notifications via SignalR.
/// Implemented in HttpApi.Host to avoid circular dependencies.
/// </summary>
public interface IRealTimeNotificationService
{
    /// <summary>
    /// Broadcast that a task was created to all users in the board.
    /// </summary>
    Task BroadcastTaskCreatedAsync(string boardId, object task);

    /// <summary>
    /// Broadcast that a task was updated to all users in the board.
    /// </summary>
    Task BroadcastTaskUpdatedAsync(string boardId, object task);

    /// <summary>
    /// Broadcast that a task was deleted to all users in the board.
    /// </summary>
    Task BroadcastTaskDeletedAsync(string boardId, string taskId);

    /// <summary>
    /// Broadcast that a task was moved to all users in the board.
    /// </summary>
    Task BroadcastTaskMovedAsync(string boardId, string taskId, string newColumnId, int newOrder);

    /// <summary>
    /// Broadcast that columns were reordered to all users in the board.
    /// </summary>
    Task BroadcastColumnsReorderedAsync(string boardId, object columns);
}
