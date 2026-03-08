import sqlite3, json, sys

DB_PATH = r'D:\Projects\PMS-Standalone\PMS.API\pms-data.db'

conn = sqlite3.connect(DB_PATH)

# Load personnel
cursor = conn.execute("SELECT JsonValue FROM AppState WHERE StateKey='personnel'")
personnel = json.loads(cursor.fetchone()[0])
name_to_id = {}
id_to_name = {}
for p in personnel:
    pid = p['Id']
    name = p['Name']
    name_to_id[name] = pid
    id_to_name[pid] = name

# Load access
cursor2 = conn.execute("SELECT JsonValue FROM AppState WHERE StateKey='personnel_access'")
access_list = json.loads(cursor2.fetchone()[0])
access_by_id = {a['PersonnelId']: a for a in access_list}

# Define the three groups
# Group 1: 何道飞 -> supervisor
group1_supervisor = "何道飞"
group1_members = [
    "高丹", "黎冰冰", "王文圣", "陈俊华", "程灿", "龚傲", "孟海峰",
    "吕志勇", "李梅", "宋城桂", "毛黎明", "王珣智", "李娜", "徐啸"
]

# Group 2: 舒高成 -> supervisor
group2_supervisor = "舒高成"
group2_members = [
    "汪飘", "林峰", "林康", "董子豪", "杨帆", "程雯蒨", "孟卓",
    "陈旭", "毕启星", "木巴热克·马利克", "苏潮", "陈浩", "任丽娜",
    "贾继文", "闫博", "刘浩", "杨玉红", "陈佳佳", "黄瑞"
]

# Group 3: 李贝 -> supervisor
group3_supervisor = "李贝"
group3_members = [
    "李旭", "张成旺", "夏安楠", "何小丽", "付依轮", "陈劲松",
    "李佳钊", "张兰兰", "张岩", "周艳明", "罗亮", "柳志明"
]

# Resolve names
all_groups = [
    (group1_supervisor, group1_members),
    (group2_supervisor, group2_members),
    (group3_supervisor, group3_members),
]

errors = []
updates = []

for sup_name, members in all_groups:
    sup_id = name_to_id.get(sup_name)
    if sup_id is None:
        errors.append(f"[ERROR] Supervisor '{sup_name}' not found!")
        continue
    print(f"\n=== Supervisor: {sup_name} (ID={sup_id}) ===")
    for m in members:
        mid = name_to_id.get(m)
        if mid is None:
            # Try fuzzy match
            matches = [n for n in name_to_id if m in n or n in m]
            if matches:
                errors.append(f"  [WARN] '{m}' not found exactly. Close matches: {matches}")
            else:
                errors.append(f"  [ERROR] '{m}' not found in personnel!")
            continue
        current_sup = access_by_id.get(mid, {}).get('SupervisorId')
        current_sup_name = id_to_name.get(current_sup, 'None') if current_sup else 'None'
        print(f"  {m} (ID={mid}): current supervisor = {current_sup_name} (ID={current_sup}) -> will set to {sup_name} (ID={sup_id})")
        updates.append((mid, sup_id, m, sup_name))

if errors:
    print("\n=== WARNINGS/ERRORS ===")
    for e in errors:
        print(e)

print(f"\n=== Summary: {len(updates)} updates to apply, {len(errors)} issues ===")
conn.close()
