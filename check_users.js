const sqlite3 = require('better-sqlite3');
const db = new sqlite3('CollaborativeTaskManager.db', { readonly: true });

try {
    const users = db.prepare("SELECT Id, UserName, Email, Name FROM AbpUsers ORDER BY CreationTime DESC LIMIT 5").all();
    console.log('Recent users:');
    users.forEach(u => {
        console.log(`- Username: ${u.UserName}, Email: ${u.Email}, Name: '${u.Name}'`);
    });
} catch (e) {
    console.log('Error:', e.message);
}

db.close();
