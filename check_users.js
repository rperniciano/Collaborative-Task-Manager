const sqlite3 = require('better-sqlite3');
const db = new sqlite3('CollaborativeTaskManager.db', { readonly: true });

try {
    const users = db.prepare("SELECT Id, UserName, Email, Name FROM AbpUsers WHERE UserName = 'profiletest_1768857567'").all();
    console.log('User profiletest_1768857567:');
    users.forEach(u => {
        console.log(`- Username: ${u.UserName}, Email: ${u.Email}, Name: '${u.Name}'`);
    });
} catch (e) {
    console.log('Error:', e.message);
}

db.close();
