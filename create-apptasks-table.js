const sql = require('msnodesqlv8');

const connectionString = 'Driver={ODBC Driver 17 for SQL Server};Server=CIANO\\CIANO;Database=collaborative-task-manager;Trusted_Connection=yes;';

const createTableSql = `
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AppTasks' AND xtype='U')
BEGIN
    CREATE TABLE [AppTasks] (
        [Id] uniqueidentifier NOT NULL,
        [ColumnId] uniqueidentifier NOT NULL,
        [Title] nvarchar(500) NOT NULL,
        [Description] nvarchar(4000) NULL,
        [DueDate] datetime2 NULL,
        [Priority] int NOT NULL DEFAULT 1,
        [AssigneeId] uniqueidentifier NULL,
        [Order] int NOT NULL DEFAULT 0,
        [ExtraProperties] nvarchar(max) NOT NULL DEFAULT '',
        [ConcurrencyStamp] nvarchar(40) NOT NULL DEFAULT '',
        [CreationTime] datetime2 NOT NULL DEFAULT GETUTCDATE(),
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
    SELECT 'AppTasks table created successfully' AS result;
END
ELSE
BEGIN
    SELECT 'AppTasks table already exists' AS result;
END
`;

console.log('Connecting to SQL Server...');

sql.query(connectionString, createTableSql, (err, results) => {
  if (err) {
    console.error('Error:', err.message);
    process.exit(1);
  }
  console.log('Success!');
  console.log('Results:', JSON.stringify(results, null, 2));
  process.exit(0);
});
