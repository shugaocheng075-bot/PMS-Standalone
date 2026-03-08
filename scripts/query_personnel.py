import sqlite3, json

DB_PATH = r'D:\Projects\PMS-Standalone\PMS.API\pms-data.db'

conn = sqlite3.connect(DB_PATH)

# Get personnel
cursor = conn.execute("SELECT JsonValue FROM AppState WHERE StateKey='personnel'")
personnel_json = cursor.fetchone()[0]
personnel = json.loads(personnel_json)

print(f"Total personnel: {len(personnel)}")
if len(personnel) > 0:
    print(f"Sample keys: {list(personnel[0].keys())}")

# Print all names with IDs
for p in personnel:
    pid = p.get('Id') or p.get('id')
    name = p.get('Name') or p.get('name')
    is_onsite = p.get('IsOnsite') or p.get('isOnsite', False)
    print(f"  ID={pid}, Name={name}, IsOnsite={is_onsite}")

print("\n--- Personnel Access (supervisor info) ---")
cursor2 = conn.execute("SELECT JsonValue FROM AppState WHERE StateKey='personnel_access'")
access_json = cursor2.fetchone()[0]
access_list = json.loads(access_json)

print(f"Total access entries: {len(access_list)}")
if len(access_list) > 0:
    print(f"Sample keys: {list(access_list[0].keys())}")

for a in access_list:
    pid = a.get('PersonnelId') or a.get('personnelId')
    sup = a.get('SupervisorId') or a.get('supervisorId')
    role = a.get('SystemRole') or a.get('systemRole')
    if sup:
        print(f"  PersonnelId={pid}, SupervisorId={sup}, SystemRole={role}")

conn.close()
