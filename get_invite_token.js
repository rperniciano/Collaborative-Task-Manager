const sqlite3 = require('better-sqlite3');
const db = new sqlite3('CollaborativeTaskManager.db', { readonly: true });

try {
    const invite = db.prepare("SELECT Token, Email, BoardId FROM BoardInvites WHERE Email = 'newmember_test_1768860141@test.com' ORDER BY CreationTime DESC LIMIT 1").get();
    if (invite) {
        console.log(`Email: ${invite.Email}`);
        console.log(`Token: ${invite.Token}`);
        console.log(`Invite Link: http://localhost:4200/invite/${invite.Token}`);
    } else {
        console.log('No invite found');
    }
} catch (e) {
    console.log('Error:', e.message);
}

db.close();
