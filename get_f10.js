const sqlite3 = require('sqlite3');
const db = new sqlite3.Database('./features.db');
db.get("SELECT * FROM Features WHERE Id = 10", (err, row) => {
  if (err) console.error(err);
  else if (row) {
    console.log("ID:", row.Id);
    console.log("Category:", row.Category);
    console.log("Name:", row.Name);
    console.log("Description:", row.Description);
    console.log("Steps:", row.Steps);
    console.log("Passes:", row.Passes);
    console.log("In Progress:", row.InProgress);
    console.log("Dependencies:", row.Dependencies);
  } else {
    console.log("Feature #10 not found");
  }
  db.close();
});
