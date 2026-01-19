const sqlite3 = require('sql.js');
const fs = require('fs');
const path = require('path');

async function main() {
    const SQL = await sqlite3();
    const dbPath = path.join(__dirname, 'features.db');
    const buffer = fs.readFileSync(dbPath);
    const db = new SQL.Database(buffer);

    const result = db.exec('SELECT * FROM features WHERE id = 20');
    if (result.length > 0 && result[0].values.length > 0) {
        const columns = result[0].columns;
        const values = result[0].values[0];
        console.log('Feature #20:');
        columns.forEach((col, i) => {
            console.log(`  ${col}: ${values[i]}`);
        });
    }
    db.close();
}

main().catch(console.error);
