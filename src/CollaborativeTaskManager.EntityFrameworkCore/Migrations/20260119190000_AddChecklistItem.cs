using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollaborativeTaskManager.Migrations
{
    /// <inheritdoc />
    public partial class AddChecklistItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL with IF NOT EXISTS to make migration idempotent
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppChecklistItems')
                BEGIN
                    CREATE TABLE [AppChecklistItems] (
                        [Id] uniqueidentifier NOT NULL,
                        [TaskId] uniqueidentifier NOT NULL,
                        [Text] nvarchar(500) NOT NULL,
                        [IsCompleted] bit NOT NULL,
                        [Order] int NOT NULL,
                        [ExtraProperties] nvarchar(max) NOT NULL DEFAULT '{}',
                        [ConcurrencyStamp] nvarchar(40) NOT NULL DEFAULT '',
                        [CreationTime] datetime2 NOT NULL,
                        [CreatorId] uniqueidentifier NULL,
                        [LastModificationTime] datetime2 NULL,
                        [LastModifierId] uniqueidentifier NULL,
                        [IsDeleted] bit NOT NULL DEFAULT 0,
                        [DeleterId] uniqueidentifier NULL,
                        [DeletionTime] datetime2 NULL,
                        CONSTRAINT [PK_AppChecklistItems] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_AppChecklistItems_AppTasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [AppTasks] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_AppChecklistItems_TaskId] ON [AppChecklistItems] ([TaskId]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AppChecklistItems')
                BEGIN
                    DROP TABLE [AppChecklistItems];
                END
            ");
        }
    }
}
