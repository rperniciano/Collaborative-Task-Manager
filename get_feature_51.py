import sqlite3
import json

conn = sqlite3.connect('features.db')
conn.row_factory = sqlite3.Row
cursor = conn.cursor()

cursor.execute('SELECT * FROM Features WHERE Id = 51')
row = cursor.fetchone()

if row:
    print(json.dumps(dict(row), indent=2))
else:
    print("Feature #51 not found")

conn.close()
