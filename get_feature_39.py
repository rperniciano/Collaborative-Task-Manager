import sqlite3
import json

conn = sqlite3.connect('features.db')
conn.row_factory = sqlite3.Row
cursor = conn.cursor()
cursor.execute('SELECT * FROM features WHERE id = 39')
row = cursor.fetchone()
if row:
    print(json.dumps(dict(row), indent=2))
else:
    print("Feature not found")
conn.close()
