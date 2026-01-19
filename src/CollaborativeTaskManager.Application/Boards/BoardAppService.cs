using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CollaborativeTaskManager.Application.Contracts.Boards;
using CollaborativeTaskManager.Domain.Boards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace CollaborativeTaskManager.Application.Boards;

/// <summary>
/// Application service for Board operations.
/// </summary>
[Authorize]
public class BoardAppService : CollaborativeTaskManagerAppService, IBoardAppService
{
    private readonly IRepository<Board, Guid> _boardRepository;
    private readonly IRepository<Column, Guid> _columnRepository;
    private readonly IRepository<BoardMember, Guid> _memberRepository;
    private readonly IRepository<BoardInvite, Guid> _inviteRepository;
    private readonly IIdentityUserRepository _userRepository;
    private readonly ILogger<BoardAppService> _logger;

    public BoardAppService(
        IRepository<Board, Guid> boardRepository,
        IRepository<Column, Guid> columnRepository,
        IRepository<BoardMember, Guid> memberRepository,
        IRepository<BoardInvite, Guid> inviteRepository,
        IIdentityUserRepository userRepository,
        ILogger<BoardAppService> logger)
    {
        _boardRepository = boardRepository;
        _columnRepository = columnRepository;
        _memberRepository = memberRepository;
        _inviteRepository = inviteRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<BoardWithColumnsDto> GetBoardAsync()
    {
        var currentUserId = CurrentUser.Id!.Value;

        // Try to find existing board where user is owner
        var board = await _boardRepository.FirstOrDefaultAsync(b => b.OwnerId == currentUserId);

        // If not owner, check if user is a member
        if (board == null)
        {
            var membership = await _memberRepository.FirstOrDefaultAsync(m => m.UserId == currentUserId);
            if (membership != null)
            {
                board = await _boardRepository.GetAsync(membership.BoardId);
            }
        }

        if (board == null)
        {
            // Create a new board for the user
            board = new Board(
                GuidGenerator.Create(),
                "My Board",
                currentUserId
            );
            await _boardRepository.InsertAsync(board);

            // Create default columns
            var columns = new List<Column>
            {
                new Column(GuidGenerator.Create(), board.Id, "To-Do", 0),
                new Column(GuidGenerator.Create(), board.Id, "In Progress", 1),
                new Column(GuidGenerator.Create(), board.Id, "Done", 2)
            };

            await _columnRepository.InsertManyAsync(columns);
        }

        // Fetch columns for this board
        var boardColumns = await _columnRepository.GetListAsync(c => c.BoardId == board.Id);

        return new BoardWithColumnsDto
        {
            Id = board.Id,
            Name = board.Name,
            OwnerId = board.OwnerId,
            CreationTime = board.CreationTime,
            IsOwner = board.OwnerId == currentUserId,
            Columns = boardColumns
                .OrderBy(c => c.Order)
                .Select(c => new ColumnDto
                {
                    Id = c.Id,
                    BoardId = c.BoardId,
                    Name = c.Name,
                    Order = c.Order
                })
                .ToList()
        };
    }

    /// <inheritdoc />
    public async Task<BoardDto> UpdateBoardAsync(UpdateBoardDto input)
    {
        var currentUserId = CurrentUser.Id!.Value;
        var board = await GetUserBoardAsync();

        // Only owner can update board
        if (board.OwnerId != currentUserId)
        {
            throw new BusinessException("Only the board owner can rename the board.");
        }

        board.Name = input.Name;
        await _boardRepository.UpdateAsync(board);

        return new BoardDto
        {
            Id = board.Id,
            Name = board.Name,
            OwnerId = board.OwnerId,
            CreationTime = board.CreationTime
        };
    }

    /// <inheritdoc />
    public async Task<InviteDto> CreateInviteAsync(CreateInviteDto input)
    {
        var currentUserId = CurrentUser.Id!.Value;
        var board = await GetUserBoardAsync();

        // Only owner can create invites
        if (board.OwnerId != currentUserId)
        {
            throw new BusinessException("Only the board owner can invite members.");
        }

        // Check if email is already invited
        var existingInvite = await _inviteRepository.FirstOrDefaultAsync(
            i => i.BoardId == board.Id && i.Email == input.Email.ToLowerInvariant() && i.ExpiresAt > DateTime.UtcNow);
        if (existingInvite != null)
        {
            throw new BusinessException("An active invitation already exists for this email address.");
        }

        // Check if user with this email is already a member
        var existingUser = await _userRepository.FindByNormalizedEmailAsync(input.Email.ToUpperInvariant());
        if (existingUser != null)
        {
            if (existingUser.Id == board.OwnerId)
            {
                throw new BusinessException("You cannot invite yourself to your own board.");
            }

            var existingMembership = await _memberRepository.FirstOrDefaultAsync(
                m => m.BoardId == board.Id && m.UserId == existingUser.Id);
            if (existingMembership != null)
            {
                throw new BusinessException("This user is already a member of the board.");
            }
        }

        // Generate a unique token
        var token = GenerateInviteToken();

        // Create the invite (expires in 7 days)
        var invite = new BoardInvite(
            GuidGenerator.Create(),
            board.Id,
            input.Email.ToLowerInvariant(),
            token,
            DateTime.UtcNow.AddDays(7)
        );

        await _inviteRepository.InsertAsync(invite);

        // Log the invite link for development (since we can't send real emails)
        var inviteLink = $"http://localhost:4200/invite/accept?token={token}";
        _logger.LogInformation("==========================================");
        _logger.LogInformation("INVITE EMAIL (Development Mode)");
        _logger.LogInformation("==========================================");
        _logger.LogInformation("To: {Email}", input.Email);
        _logger.LogInformation("Subject: You've been invited to join '{BoardName}' on CollaBoard", board.Name);
        _logger.LogInformation("Body: Click the link below to join the board:");
        _logger.LogInformation("Link: {InviteLink}", inviteLink);
        _logger.LogInformation("Token: {Token}", token);
        _logger.LogInformation("Expires: {ExpiresAt}", invite.ExpiresAt);
        _logger.LogInformation("==========================================");

        return new InviteDto
        {
            Id = invite.Id,
            BoardId = invite.BoardId,
            Email = invite.Email,
            Token = invite.Token,
            ExpiresAt = invite.ExpiresAt,
            CreatedAt = invite.CreationTime,
            IsExpired = invite.IsExpired
        };
    }

    /// <inheritdoc />
    public async Task<List<InviteDto>> GetInvitesAsync()
    {
        var currentUserId = CurrentUser.Id!.Value;
        var board = await GetUserBoardAsync();

        // Only owner can view invites
        if (board.OwnerId != currentUserId)
        {
            throw new BusinessException("Only the board owner can view invitations.");
        }

        var invites = await _inviteRepository.GetListAsync(i => i.BoardId == board.Id);

        return invites
            .OrderByDescending(i => i.CreationTime)
            .Select(i => new InviteDto
            {
                Id = i.Id,
                BoardId = i.BoardId,
                Email = i.Email,
                Token = i.Token,
                ExpiresAt = i.ExpiresAt,
                CreatedAt = i.CreationTime,
                IsExpired = i.IsExpired
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task DeleteInviteAsync(Guid id)
    {
        var currentUserId = CurrentUser.Id!.Value;
        var board = await GetUserBoardAsync();

        // Only owner can delete invites
        if (board.OwnerId != currentUserId)
        {
            throw new BusinessException("Only the board owner can cancel invitations.");
        }

        var invite = await _inviteRepository.FirstOrDefaultAsync(i => i.Id == id && i.BoardId == board.Id);
        if (invite == null)
        {
            throw new BusinessException("Invitation not found.");
        }

        await _inviteRepository.DeleteAsync(invite);
    }

    /// <inheritdoc />
    public async Task<List<MemberDto>> GetMembersAsync()
    {
        var board = await GetUserBoardAsync();

        // Get the owner
        var owner = await _userRepository.GetAsync(board.OwnerId);

        var members = new List<MemberDto>
        {
            new MemberDto
            {
                Id = Guid.Empty, // Owner doesn't have a membership record
                UserId = owner.Id,
                Email = owner.Email ?? string.Empty,
                DisplayName = owner.Name ?? owner.UserName ?? owner.Email ?? "Owner",
                JoinedAt = board.CreationTime,
                IsOwner = true
            }
        };

        // Get all members
        var memberships = await _memberRepository.GetListAsync(m => m.BoardId == board.Id);
        foreach (var membership in memberships)
        {
            var user = await _userRepository.GetAsync(membership.UserId);
            members.Add(new MemberDto
            {
                Id = membership.Id,
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.Name ?? user.UserName ?? user.Email ?? "Member",
                JoinedAt = membership.JoinedAt,
                IsOwner = false
            });
        }

        return members.OrderBy(m => m.JoinedAt).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteMemberAsync(Guid userId)
    {
        var currentUserId = CurrentUser.Id!.Value;
        var board = await GetUserBoardAsync();

        // Only owner can remove members
        if (board.OwnerId != currentUserId)
        {
            throw new BusinessException("Only the board owner can remove members.");
        }

        // Cannot remove the owner
        if (userId == board.OwnerId)
        {
            throw new BusinessException("Cannot remove the board owner.");
        }

        var membership = await _memberRepository.FirstOrDefaultAsync(m => m.BoardId == board.Id && m.UserId == userId);
        if (membership == null)
        {
            throw new BusinessException("Member not found.");
        }

        await _memberRepository.DeleteAsync(membership);
    }

    /// <inheritdoc />
    public async Task ReorderColumnsAsync(ReorderColumnsDto input)
    {
        var board = await GetUserBoardAsync();

        if (input.ColumnIds == null || input.ColumnIds.Count == 0)
        {
            throw new BusinessException("Column IDs are required.");
        }

        // Get all columns for this board
        var columns = await _columnRepository.GetListAsync(c => c.BoardId == board.Id);
        var columnDict = columns.ToDictionary(c => c.Id);

        // Validate that all provided IDs belong to this board
        foreach (var columnId in input.ColumnIds)
        {
            if (!columnDict.ContainsKey(columnId))
            {
                throw new BusinessException($"Column with ID {columnId} not found on this board.");
            }
        }

        // Update order for each column
        for (int i = 0; i < input.ColumnIds.Count; i++)
        {
            var columnId = input.ColumnIds[i];
            var column = columnDict[columnId];
            column.Order = i;
        }

        await _columnRepository.UpdateManyAsync(columns);

        _logger.LogInformation("Columns reordered for board {BoardId}: {ColumnIds}", board.Id, string.Join(", ", input.ColumnIds));
    }

    /// <inheritdoc />
    [AllowAnonymous]
    public async Task<InviteDto?> GetInviteByTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var invite = await _inviteRepository.FirstOrDefaultAsync(i => i.Token == token);
        if (invite == null)
        {
            return null;
        }

        // Get board name for display
        var board = await _boardRepository.GetAsync(invite.BoardId);

        return new InviteDto
        {
            Id = invite.Id,
            BoardId = invite.BoardId,
            Email = invite.Email,
            Token = invite.Token,
            ExpiresAt = invite.ExpiresAt,
            CreatedAt = invite.CreationTime,
            IsExpired = invite.IsExpired
        };
    }

    /// <inheritdoc />
    public async Task<BoardDto> AcceptInviteAsync(string token)
    {
        var currentUserId = CurrentUser.Id!.Value;

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new BusinessException("Invalid invite token.");
        }

        var invite = await _inviteRepository.FirstOrDefaultAsync(i => i.Token == token);
        if (invite == null)
        {
            throw new BusinessException("Invitation not found.");
        }

        // Check if the invite has expired
        if (invite.IsExpired)
        {
            throw new BusinessException("This invitation has expired.");
        }

        // Get the board
        var board = await _boardRepository.GetAsync(invite.BoardId);
        if (board == null)
        {
            throw new BusinessException("Board not found.");
        }

        // Check if user is already the owner
        if (board.OwnerId == currentUserId)
        {
            throw new BusinessException("You are already the owner of this board.");
        }

        // Check if user is already a member
        var existingMembership = await _memberRepository.FirstOrDefaultAsync(
            m => m.BoardId == board.Id && m.UserId == currentUserId);
        if (existingMembership != null)
        {
            throw new BusinessException("You are already a member of this board.");
        }

        // Create membership
        var membership = new BoardMember(
            GuidGenerator.Create(),
            board.Id,
            currentUserId,
            DateTime.UtcNow
        );
        await _memberRepository.InsertAsync(membership);

        // Delete the used invite
        await _inviteRepository.DeleteAsync(invite);

        _logger.LogInformation(
            "User {UserId} accepted invitation to board {BoardId} and joined as member",
            currentUserId, board.Id);

        return new BoardDto
        {
            Id = board.Id,
            Name = board.Name,
            OwnerId = board.OwnerId,
            CreationTime = board.CreationTime
        };
    }

    private async Task<Board> GetUserBoardAsync()
    {
        var currentUserId = CurrentUser.Id!.Value;

        // Try to find board where user is owner
        var board = await _boardRepository.FirstOrDefaultAsync(b => b.OwnerId == currentUserId);

        // If not owner, check if user is a member
        if (board == null)
        {
            var membership = await _memberRepository.FirstOrDefaultAsync(m => m.UserId == currentUserId);
            if (membership != null)
            {
                board = await _boardRepository.GetAsync(membership.BoardId);
            }
        }

        if (board == null)
        {
            throw new BusinessException("Board not found. Please create a board first.");
        }

        return board;
    }

    /// <inheritdoc />
    public async Task<InviteDto> CreateExpiredInviteAsync(CreateInviteDto input)
    {
        var currentUserId = CurrentUser.Id!.Value;
        var board = await GetUserBoardAsync();

        // Only owner can create invites
        if (board.OwnerId != currentUserId)
        {
            throw new BusinessException("Only the board owner can invite members.");
        }

        // Generate a unique token
        var token = GenerateInviteToken();

        // Create the invite with a past expiration date (already expired)
        var invite = new BoardInvite(
            GuidGenerator.Create(),
            board.Id,
            input.Email.ToLowerInvariant(),
            token,
            DateTime.UtcNow.AddDays(-1) // Expired 1 day ago
        );

        await _inviteRepository.InsertAsync(invite);

        _logger.LogInformation("==========================================");
        _logger.LogInformation("EXPIRED TEST INVITE CREATED (Development Mode)");
        _logger.LogInformation("==========================================");
        _logger.LogInformation("To: {Email}", input.Email);
        _logger.LogInformation("Token: {Token}", token);
        _logger.LogInformation("Expired At: {ExpiresAt}", invite.ExpiresAt);
        _logger.LogInformation("==========================================");

        return new InviteDto
        {
            Id = invite.Id,
            BoardId = invite.BoardId,
            Email = invite.Email,
            Token = invite.Token,
            ExpiresAt = invite.ExpiresAt,
            CreatedAt = invite.CreationTime,
            IsExpired = invite.IsExpired
        };
    }

    private static string GenerateInviteToken()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
