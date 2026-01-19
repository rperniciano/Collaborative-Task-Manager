const sqlite3 = require('sqlite3').verbose();
const db = new sqlite3.Database('./features.db');

db.get('SELECT * FROM Features WHERE Id = 51', (err, row) => {
  if (err) {
    console.error(err);
  } else {
    console.log(JSON.stringify(row, null, 2));
  }
  db.close();
});
