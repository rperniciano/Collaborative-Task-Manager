using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace CollaborativeTaskManager.Application.Contracts.Boards;

/// <summary>
/// Application service interface for Board operations.
/// RemoteService disabled because we use a manual BoardController for custom routing.
/// </summary>
[RemoteService(IsEnabled = false)]
public interface IBoardAppService : IApplicationService
{
    /// <summary>
    /// Gets the current user's board with its columns.
    /// If the user doesn't have a board, creates one automatically.
    /// </summary>
    Task<BoardWithColumnsDto> GetBoardAsync();

    /// <summary>
    /// Updates the board name. Only the owner can perform this action.
    /// </summary>
    Task<BoardDto> UpdateBoardAsync(UpdateBoardDto input);

    /// <summary>
    /// Creates an invitation to join the board. Only the owner can perform this action.
    /// </summary>
    Task<InviteDto> CreateInviteAsync(CreateInviteDto input);

    /// <summary>
    /// Gets all pending invitations for the board. Only the owner can perform this action.
    /// </summary>
    Task<List<InviteDto>> GetInvitesAsync();

    /// <summary>
    /// Cancels a pending invitation. Only the owner can perform this action.
    /// </summary>
    Task DeleteInviteAsync(Guid id);

    /// <summary>
    /// Gets all members of the board (including the owner).
    /// </summary>
    Task<List<MemberDto>> GetMembersAsync();

    /// <summary>
    /// Removes a member from the board. Only the owner can perform this action.
    /// </summary>
    Task DeleteMemberAsync(Guid userId);

    /// <summary>
    /// Reorders the columns on the board. Any board member can perform this action.
    /// </summary>
    Task ReorderColumnsAsync(ReorderColumnsDto input);

    /// <summary>
    /// Gets an invite by its token. Used for the invite acceptance flow.
    /// Does not require authentication.
    /// </summary>
    Task<InviteDto?> GetInviteByTokenAsync(string token);

    /// <summary>
    /// Accepts an invitation to join a board. Requires authentication.
    /// The invite must not be expired.
    /// </summary>
    Task<BoardDto> AcceptInviteAsync(string token);

    /// <summary>
    /// Creates an expired invite for testing purposes. Development only.
    /// </summary>
    Task<InviteDto> CreateExpiredInviteAsync(CreateInviteDto input);
}
