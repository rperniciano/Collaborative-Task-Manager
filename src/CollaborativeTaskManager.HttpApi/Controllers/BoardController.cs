using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CollaborativeTaskManager.Application.Contracts.Boards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CollaborativeTaskManager.Controllers;

/// <summary>
/// Controller for board-related operations including invites and members.
/// </summary>
[Route("api/app/board")]
public class BoardController : CollaborativeTaskManagerController
{
    private readonly IBoardAppService _boardAppService;

    public BoardController(IBoardAppService boardAppService)
    {
        _boardAppService = boardAppService;
    }

    /// <summary>
    /// Gets the current user's board with columns.
    /// </summary>
    [HttpGet("board")]
    [Authorize]
    public Task<BoardWithColumnsDto> GetBoardAsync()
    {
        return _boardAppService.GetBoardAsync();
    }

    /// <summary>
    /// Updates the board name.
    /// </summary>
    [HttpPut("board")]
    [Authorize]
    public Task<BoardDto> UpdateBoardAsync([FromBody] UpdateBoardDto input)
    {
        return _boardAppService.UpdateBoardAsync(input);
    }

    /// <summary>
    /// Creates an invitation to join the board.
    /// </summary>
    [HttpPost("invite")]
    [Authorize]
    public Task<InviteDto> CreateInviteAsync([FromBody] CreateInviteDto input)
    {
        return _boardAppService.CreateInviteAsync(input);
    }

    /// <summary>
    /// Gets all pending invitations for the board.
    /// </summary>
    [HttpGet("invites")]
    [Authorize]
    public Task<List<InviteDto>> GetInvitesAsync()
    {
        return _boardAppService.GetInvitesAsync();
    }

    /// <summary>
    /// Cancels a pending invitation.
    /// </summary>
    [HttpDelete("{id}/invite")]
    [Authorize]
    public Task DeleteInviteAsync(Guid id)
    {
        return _boardAppService.DeleteInviteAsync(id);
    }

    /// <summary>
    /// Gets an invite by its token. This endpoint is anonymous.
    /// </summary>
    [HttpGet("invite-by-token")]
    [AllowAnonymous]
    public async Task<ActionResult<InviteDto>> GetInviteByTokenAsync([FromQuery] string token)
    {
        var invite = await _boardAppService.GetInviteByTokenAsync(token);
        if (invite == null)
        {
            return NotFound(new { error = new { message = "Invitation not found." } });
        }
        return Ok(invite);
    }

    /// <summary>
    /// Accepts an invitation to join a board.
    /// </summary>
    [HttpPost("accept-invite")]
    [Authorize]
    public async Task<ActionResult<BoardDto>> AcceptInviteAsync([FromQuery] string token)
    {
        try
        {
            var board = await _boardAppService.AcceptInviteAsync(token);
            return Ok(board);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { error = new { message = ex.Message } });
        }
    }

    /// <summary>
    /// Gets all members of the board.
    /// </summary>
    [HttpGet("members")]
    [Authorize]
    public Task<List<MemberDto>> GetMembersAsync()
    {
        return _boardAppService.GetMembersAsync();
    }

    /// <summary>
    /// Removes a member from the board.
    /// </summary>
    [HttpDelete("member/{userId}")]
    [Authorize]
    public Task DeleteMemberAsync(Guid userId)
    {
        return _boardAppService.DeleteMemberAsync(userId);
    }

    /// <summary>
    /// Reorders the columns on the board.
    /// </summary>
    [HttpPost("reorder-columns")]
    [Authorize]
    public Task ReorderColumnsAsync([FromBody] ReorderColumnsDto input)
    {
        return _boardAppService.ReorderColumnsAsync(input);
    }

    /// <summary>
    /// Creates an expired invite for testing purposes. Development only.
    /// </summary>
    [HttpPost("create-expired-invite")]
    [Authorize]
    public async Task<ActionResult<InviteDto>> CreateExpiredInviteAsync([FromBody] CreateInviteDto input)
    {
        try
        {
            var invite = await _boardAppService.CreateExpiredInviteAsync(input);
            return Ok(invite);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { error = new { message = ex.Message } });
        }
    }
}
