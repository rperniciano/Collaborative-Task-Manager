using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CollaborativeTaskManager.Application.Contracts.Boards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CollaborativeTaskManager.Controllers;

/// <summary>
/// API Controller for Board invite and member management.
/// </summary>
[RemoteService]
[Authorize]
[Route("api/app/board")]
public class BoardController : CollaborativeTaskManagerController
{
    private readonly IBoardAppService _boardAppService;

    public BoardController(IBoardAppService boardAppService)
    {
        _boardAppService = boardAppService;
    }

    /// <summary>
    /// Gets all pending invitations for the board.
    /// </summary>
    [HttpGet("invites")]
    public async Task<List<InviteDto>> GetInvitesAsync()
    {
        return await _boardAppService.GetInvitesAsync();
    }

    /// <summary>
    /// Creates an invitation to join the board.
    /// </summary>
    [HttpPost("invites")]
    public async Task<InviteDto> CreateInviteAsync([FromBody] CreateInviteDto input)
    {
        return await _boardAppService.CreateInviteAsync(input);
    }

    /// <summary>
    /// Cancels a pending invitation.
    /// </summary>
    [HttpDelete("invites/{id}")]
    public async Task DeleteInviteAsync(Guid id)
    {
        await _boardAppService.DeleteInviteAsync(id);
    }

    /// <summary>
    /// Gets all members of the board.
    /// </summary>
    [HttpGet("members")]
    public async Task<List<MemberDto>> GetMembersAsync()
    {
        return await _boardAppService.GetMembersAsync();
    }

    /// <summary>
    /// Removes a member from the board.
    /// </summary>
    [HttpDelete("members/{userId}")]
    public async Task DeleteMemberAsync(Guid userId)
    {
        await _boardAppService.DeleteMemberAsync(userId);
    }
}
