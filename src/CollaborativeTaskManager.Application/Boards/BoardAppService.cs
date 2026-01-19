using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CollaborativeTaskManager.Application.Contracts.Boards;
using CollaborativeTaskManager.Domain.Boards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EntityFrameworkCore;

namespace CollaborativeTaskManager.Application.Boards;

/// <summary>
/// Application service for Board operations.
/// </summary>
[Authorize]
public class BoardAppService : CollaborativeTaskManagerAppService, IBoardAppService
{
    private readonly IRepository<Board, Guid> _boardRepository;
    private readonly IRepository<Column, Guid> _columnRepository;
    private readonly IDbContextProvider<CollaborativeTaskManager.EntityFrameworkCore.CollaborativeTaskManagerDbContext> _dbContextProvider;

    public BoardAppService(
        IRepository<Board, Guid> boardRepository,
        IRepository<Column, Guid> columnRepository,
        IDbContextProvider<CollaborativeTaskManager.EntityFrameworkCore.CollaborativeTaskManagerDbContext> dbContextProvider)
    {
        _boardRepository = boardRepository;
        _columnRepository = columnRepository;
        _dbContextProvider = dbContextProvider;
    }

    /// <summary>
    /// Ensures the AppTasks table exists in the database.
    /// </summary>
    public async Task<string> EnsureTasksTableAsync()
    {
        var dbContext = await _dbContextProvider.GetDbContextAsync();

        var createTasksTableSql = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AppTasks' AND xtype='U')
            BEGIN
                CREATE TABLE [AppTasks] (
                    [Id] uniqueidentifier NOT NULL,
                    [ColumnId] uniqueidentifier NOT NULL,
                    [Title] nvarchar(500) NOT NULL,
                    [Description] nvarchar(4000) NULL,
                    [DueDate] datetime2 NULL,
                    [Priority] int NOT NULL,
                    [AssigneeId] uniqueidentifier NULL,
                    [Order] int NOT NULL,
                    [ExtraProperties] nvarchar(max) NOT NULL DEFAULT '',
                    [ConcurrencyStamp] nvarchar(40) NOT NULL DEFAULT '',
                    [CreationTime] datetime2 NOT NULL,
                    [CreatorId] uniqueidentifier NULL,
                    [LastModificationTime] datetime2 NULL,
                    [LastModifierId] uniqueidentifier NULL,
                    [IsDeleted] bit NOT NULL DEFAULT 0,
                    [DeleterId] uniqueidentifier NULL,
                    [DeletionTime] datetime2 NULL,
                    CONSTRAINT [PK_AppTasks] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_AppTasks_AppColumns_ColumnId] FOREIGN KEY ([ColumnId]) REFERENCES [AppColumns] ([Id]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_AppTasks_ColumnId] ON [AppTasks] ([ColumnId]);
            END";

        await dbContext.Database.ExecuteSqlRawAsync(createTasksTableSql);
        return "AppTasks table ensured.";
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
