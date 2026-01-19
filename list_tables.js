const sqlite3 = require('better-sqlite3');
const db = new sqlite3('CollaborativeTaskManager.db', { readonly: true });
try {
    const tables = db.prepare("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name").all();
    console.log('Tables:', tables.map(t => t.name).join(', '));
} catch (e) {
    console.log('Error:', e.message);
}
db.close();
