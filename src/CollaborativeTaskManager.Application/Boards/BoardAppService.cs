using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CollaborativeTaskManager.Application.Contracts.Boards;
using CollaborativeTaskManager.Domain.Boards;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Domain.Repositories;

namespace CollaborativeTaskManager.Application.Boards;

/// <summary>
/// Application service for Board operations.
/// </summary>
[Authorize]
public class BoardAppService : CollaborativeTaskManagerAppService, IBoardAppService
{
    private readonly IRepository<Board, Guid> _boardRepository;
    private readonly IRepository<Column, Guid> _columnRepository;

    public BoardAppService(
        IRepository<Board, Guid> boardRepository,
        IRepository<Column, Guid> columnRepository)
    {
        _boardRepository = boardRepository;
        _columnRepository = columnRepository;
    }

    /// <inheritdoc />
    public async Task<BoardWithColumnsDto> GetBoardAsync()
    {
        var currentUserId = CurrentUser.Id!.Value;

        // Try to find existing board for this user
        var board = await _boardRepository.FirstOrDefaultAsync(b => b.OwnerId == currentUserId);

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
}
