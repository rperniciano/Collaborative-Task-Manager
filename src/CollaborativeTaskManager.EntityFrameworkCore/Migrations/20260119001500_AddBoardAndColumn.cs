using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollaborativeTaskManager.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardAndColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL with IF NOT EXISTS to make migration idempotent
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppBoards')
                BEGIN
                    CREATE TABLE [AppBoards] (
                        [Id] uniqueidentifier NOT NULL,
                        [Name] nvarchar(256) NOT NULL,
                        [OwnerId] uniqueidentifier NOT NULL,
                        [ExtraProperties] nvarchar(max) NOT NULL,
                        [ConcurrencyStamp] nvarchar(40) NOT NULL,
                        [CreationTime] datetime2 NOT NULL,
                        [CreatorId] uniqueidentifier NULL,
                        [LastModificationTime] datetime2 NULL,
                        [LastModifierId] uniqueidentifier NULL,
                        [IsDeleted] bit NOT NULL DEFAULT 0,
                        [DeleterId] uniqueidentifier NULL,
                        [DeletionTime] datetime2 NULL,
                        CONSTRAINT [PK_AppBoards] PRIMARY KEY ([Id])
                    );

                    CREATE INDEX [IX_AppBoards_OwnerId] ON [AppBoards] ([OwnerId]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppColumns')
                BEGIN
                    CREATE TABLE [AppColumns] (
                        [Id] uniqueidentifier NOT NULL,
                        [BoardId] uniqueidentifier NOT NULL,
                        [Name] nvarchar(128) NOT NULL,
                        [Order] int NOT NULL,
                        [CreationTime] datetime2 NOT NULL,
                        [CreatorId] uniqueidentifier NULL,
                        [LastModificationTime] datetime2 NULL,
                        [LastModifierId] uniqueidentifier NULL,
                        [IsDeleted] bit NOT NULL DEFAULT 0,
                        [DeleterId] uniqueidentifier NULL,
                        [DeletionTime] datetime2 NULL,
                        CONSTRAINT [PK_AppColumns] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_AppColumns_AppBoards_BoardId] FOREIGN KEY ([BoardId]) REFERENCES [AppBoards] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_AppColumns_BoardId] ON [AppColumns] ([BoardId]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AppColumns')
                BEGIN
                    DROP TABLE [AppColumns];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AppBoards')
                BEGIN
                    DROP TABLE [AppBoards];
                END
            ");
        }
    }
}
