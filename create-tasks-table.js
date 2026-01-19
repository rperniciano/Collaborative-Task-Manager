const sql = require('mssql');

// Try with integrated security via connection string approach
const config = {
    server: 'CIANO\\CIANO',
    database: 'collaborative-task-manager',
    options: {
        trustServerCertificate: true,
        encrypt: false,
        enableArithAbort: true,
        integratedSecurity: true
    },
    // Use Windows Authentication via NTLM
    authentication: {
        type: 'ntlm',
        options: {
            domain: '',
            userName: '',
            password: ''
        }
    }
};

async function createTasksTable() {
    let pool;
    try {
        console.log('Connecting to SQL Server...');
        pool = await sql.connect(config);
        console.log('Connected!');

        const createTableSql = `
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
                SELECT 'AppTasks table created successfully!' as Result;
            END
            ELSE
            BEGIN
                SELECT 'AppTasks table already exists.' as Result;
            END
        `;

        console.log('Executing SQL to create AppTasks table...');
        const result = await pool.request().query(createTableSql);
        console.log('SQL executed successfully!');
        if (result.recordset && result.recordset.length > 0) {
            console.log('Result:', result.recordset[0].Result);
        }

        // Check if table exists now
        const checkResult = await pool.request().query("SELECT name FROM sysobjects WHERE name='AppTasks' AND xtype='U'");
        if (checkResult.recordset.length > 0) {
            console.log('Verification: AppTasks table exists!');
        } else {
            console.log('Verification: AppTasks table was NOT created.');
        }

    } catch (err) {
        console.error('Error:', err.message);
        console.error('Full error:', err);
    } finally {
        if (pool) {
            await pool.close();
        }
    }
}

createTasksTable();
