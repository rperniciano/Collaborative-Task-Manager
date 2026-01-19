using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace CollaborativeTaskManager.Application.Contracts.Boards;

/// <summary>
/// Application service interface for Board operations.
/// </summary>
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
}
