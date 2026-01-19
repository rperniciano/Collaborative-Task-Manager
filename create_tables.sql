-- Create AppBoards table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AppBoards' AND xtype='U')
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
END;

-- Create AppColumns table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AppColumns' AND xtype='U')
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
END;

-- Create AppTasks table
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
END;

-- Add migration record for Board and Column
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260119001500_AddBoardAndColumn')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260119001500_AddBoardAndColumn', '10.0.0');
END;

-- Add migration record for Task
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260119010000_AddBoardTask')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260119010000_AddBoardTask', '10.0.0');
END;
