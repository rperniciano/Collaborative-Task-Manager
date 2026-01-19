using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Volo.Abp.Users;

namespace CollaborativeTaskManager.Hubs;

/// <summary>
/// SignalR Hub for real-time board communication.
/// Handles presence, typing indicators, and event broadcasting.
/// </summary>
[Authorize]
public class BoardHub : Hub
{
    private readonly ICurrentUser _currentUser;

    // In-memory tracking of connected users per board (for MVP)
    // In production, consider using Redis or a distributed cache
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<string, UserPresence>> _boardConnections = new();

    public BoardHub(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = _currentUser.Id?.ToString();
        var userName = _currentUser.UserName;

        if (!string.IsNullOrEmpty(userId))
        {
            // For now, we'll join a default board group
            // In a multi-board scenario, the client would call JoinBoard
            await Groups.AddToGroupAsync(Context.ConnectionId, "board-default");

            Console.WriteLine($"[SignalR] User {userName} (ID: {userId}) connected with connection ID: {Context.ConnectionId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _currentUser.Id?.ToString();
        var userName = _currentUser.UserName;

        if (!string.IsNullOrEmpty(userId))
        {
            // Remove from all board groups
            foreach (var boardId in _boardConnections.Keys)
            {
                if (_boardConnections.TryGetValue(boardId, out var users))
                {
                    users.TryRemove(userId, out _);
                    await Clients.Group($"board-{boardId}").SendAsync("UserLeft", userId, userName);
                    await SendPresenceUpdate(boardId);
                }
            }

            Console.WriteLine($"[SignalR] User {userName} (ID: {userId}) disconnected");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a specific board's real-time group.
    /// Called when user navigates to board view.
    /// </summary>
    public async Task JoinBoard(string boardId)
    {
        var userId = _currentUser.Id?.ToString();
        var userName = _currentUser.UserName ?? "Unknown";

        if (string.IsNullOrEmpty(userId))
        {
            throw new HubException("User not authenticated");
        }

        var groupName = $"board-{boardId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        // Track user presence
        var boardUsers = _boardConnections.GetOrAdd(boardId, _ => new System.Collections.Concurrent.ConcurrentDictionary<string, UserPresence>());
        boardUsers[userId] = new UserPresence
        {
            UserId = userId,
            UserName = userName,
            ConnectionId = Context.ConnectionId,
            JoinedAt = DateTime.UtcNow
        };

        // Notify others that user joined
        await Clients.OthersInGroup(groupName).SendAsync("UserJoined", userId, userName);

        // Send updated presence list to all in group
        await SendPresenceUpdate(boardId);

        Console.WriteLine($"[SignalR] User {userName} joined board {boardId}");
    }

    /// <summary>
    /// Leave a board's real-time group.
    /// Called when user navigates away from board view.
    /// </summary>
    public async Task LeaveBoard(string boardId)
    {
        var userId = _currentUser.Id?.ToString();
        var userName = _currentUser.UserName ?? "Unknown";

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        var groupName = $"board-{boardId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        // Remove from presence tracking
        if (_boardConnections.TryGetValue(boardId, out var users))
        {
            users.TryRemove(userId, out _);
        }

        // Notify others that user left
        await Clients.OthersInGroup(groupName).SendAsync("UserLeft", userId, userName);

        // Send updated presence list
        await SendPresenceUpdate(boardId);

        Console.WriteLine($"[SignalR] User {userName} left board {boardId}");
    }

    /// <summary>
    /// Send typing indicator to other users in the board.
    /// </summary>
    public async Task SendTyping(string boardId, string context)
    {
        var userId = _currentUser.Id?.ToString();
        var userName = _currentUser.UserName ?? "Unknown";

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        var groupName = $"board-{boardId}";
        await Clients.OthersInGroup(groupName).SendAsync("UserTyping", userId, userName, context);
    }

    /// <summary>
    /// Stop typing indicator.
    /// </summary>
    public async Task StopTyping(string boardId)
    {
        var userId = _currentUser.Id?.ToString();

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        var groupName = $"board-{boardId}";
        await Clients.OthersInGroup(groupName).SendAsync("UserStoppedTyping", userId);
    }

    /// <summary>
    /// Send presence update to all users in a board.
    /// </summary>
    private async Task SendPresenceUpdate(string boardId)
    {
        if (_boardConnections.TryGetValue(boardId, out var users))
        {
            var presenceList = users.Values.Select(u => new { u.UserId, u.UserName }).ToList();
            await Clients.Group($"board-{boardId}").SendAsync("PresenceUpdated", presenceList);
        }
    }

    /// <summary>
    /// Ping method for connection testing.
    /// </summary>
    public async Task Ping()
    {
        await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
    }
}

/// <summary>
/// Represents a user's presence in a board.
/// </summary>
public class UserPresence
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}
