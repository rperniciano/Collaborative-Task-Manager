const sqlite3 = require('better-sqlite3');
const db = new sqlite3('CollaborativeTaskManager.db', { readonly: true });

try {
    // Get the most recently created user with test pattern
    const user = db.prepare("SELECT UserName, Email FROM AbpUsers WHERE UserName LIKE '%test%' ORDER BY CreationTime DESC LIMIT 1").get();
    if (user) {
        console.log(`Username: ${user.UserName}`);
        console.log(`Email: ${user.Email}`);
        console.log(`Password: Test123456!`); // Standard test password
    } else {
        console.log('No test users found');
    }
} catch (e) {
    console.log('Error:', e.message);
}

db.close();
