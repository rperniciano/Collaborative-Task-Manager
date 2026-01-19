const sqlite3 = require('better-sqlite3');
const db = new sqlite3('features.db', { readonly: true });

const row = db.prepare('SELECT id, name, category, passes FROM features WHERE id = 14').get();

if (row) {
    console.log('Feature #14:', row.name);
    console.log('Category:', row.category);
    console.log('Passing:', row.passes === 1 ? 'YES' : 'NO');
} else {
    console.log('Feature #14 not found');
}

db.close();
