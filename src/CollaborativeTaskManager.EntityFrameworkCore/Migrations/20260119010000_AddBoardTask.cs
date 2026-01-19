using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollaborativeTaskManager.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL with IF NOT EXISTS to make migration idempotent
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppTasks')
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
                        [ExtraProperties] nvarchar(max) NOT NULL,
                        [ConcurrencyStamp] nvarchar(40) NOT NULL,
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
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AppTasks')
                BEGIN
                    DROP TABLE [AppTasks];
                END
            ");
        }
    }
}
