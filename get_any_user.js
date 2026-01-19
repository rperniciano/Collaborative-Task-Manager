const sqlite3 = require('better-sqlite3');
const db = new sqlite3('CollaborativeTaskManager.db', { readonly: true });

try {
    const user = db.prepare("SELECT UserName, Email FROM AbpUsers LIMIT 1").get();
    if (user) {
        console.log(`Username: ${user.UserName}`);
        console.log(`Email: ${user.Email}`);
    } else {
        console.log('No users found');
    }
} catch (e) {
    console.log('Error:', e.message);
}

db.close();
