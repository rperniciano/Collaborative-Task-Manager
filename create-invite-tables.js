const sql = require('msnodesqlv8');

const connectionString = 'Driver={ODBC Driver 17 for SQL Server};Server=CIANO\\CIANO;Database=collaborative-task-manager;Trusted_Connection=yes;';

const createBoardMembersTable = `
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AppBoardMembers]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AppBoardMembers] (
        [Id] uniqueidentifier NOT NULL,
        [BoardId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [JoinedAt] datetime2(7) NOT NULL,
        [ExtraProperties] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(40) NULL,
        [CreationTime] datetime2(7) NOT NULL,
        [CreatorId] uniqueidentifier NULL,
        [LastModificationTime] datetime2(7) NULL,
        [LastModifierId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL DEFAULT 0,
        [DeleterId] uniqueidentifier NULL,
        [DeletionTime] datetime2(7) NULL,
        CONSTRAINT [PK_AppBoardMembers] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_AppBoardMembers_BoardId] ON [dbo].[AppBoardMembers] ([BoardId]);
    CREATE INDEX [IX_AppBoardMembers_UserId] ON [dbo].[AppBoardMembers] ([UserId]);
    CREATE UNIQUE INDEX [IX_AppBoardMembers_BoardId_UserId] ON [dbo].[AppBoardMembers] ([BoardId], [UserId]);

    ALTER TABLE [dbo].[AppBoardMembers] ADD CONSTRAINT [FK_AppBoardMembers_AppBoards_BoardId]
        FOREIGN KEY ([BoardId]) REFERENCES [dbo].[AppBoards] ([Id]) ON DELETE CASCADE;

    PRINT 'AppBoardMembers table created successfully';
END
ELSE
BEGIN
    PRINT 'AppBoardMembers table already exists';
END
`;

const createBoardInvitesTable = `
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AppBoardInvites]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AppBoardInvites] (
        [Id] uniqueidentifier NOT NULL,
        [BoardId] uniqueidentifier NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [Token] nvarchar(128) NOT NULL,
        [ExpiresAt] datetime2(7) NOT NULL,
        [ExtraProperties] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(40) NULL,
        [CreationTime] datetime2(7) NOT NULL,
        [CreatorId] uniqueidentifier NULL,
        [LastModificationTime] datetime2(7) NULL,
        [LastModifierId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL DEFAULT 0,
        [DeleterId] uniqueidentifier NULL,
        [DeletionTime] datetime2(7) NULL,
        CONSTRAINT [PK_AppBoardInvites] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_AppBoardInvites_BoardId] ON [dbo].[AppBoardInvites] ([BoardId]);
    CREATE UNIQUE INDEX [IX_AppBoardInvites_Token] ON [dbo].[AppBoardInvites] ([Token]);

    ALTER TABLE [dbo].[AppBoardInvites] ADD CONSTRAINT [FK_AppBoardInvites_AppBoards_BoardId]
        FOREIGN KEY ([BoardId]) REFERENCES [dbo].[AppBoards] ([Id]) ON DELETE CASCADE;

    PRINT 'AppBoardInvites table created successfully';
END
ELSE
BEGIN
    PRINT 'AppBoardInvites table already exists';
END
`;

console.log('Creating BoardMembers and BoardInvites tables...');

sql.open(connectionString, (err, conn) => {
    if (err) {
        console.error('Connection error:', err);
        process.exit(1);
    }

    console.log('Connected to database');

    // Create BoardMembers table
    conn.query(createBoardMembersTable, (err, results) => {
        if (err) {
            console.error('Error creating BoardMembers table:', err);
        } else {
            console.log('BoardMembers table check completed');
        }

        // Create BoardInvites table
        conn.query(createBoardInvitesTable, (err, results) => {
            if (err) {
                console.error('Error creating BoardInvites table:', err);
            } else {
                console.log('BoardInvites table check completed');
            }

            conn.close(() => {
                console.log('Done!');
                process.exit(0);
            });
        });
    });
});
