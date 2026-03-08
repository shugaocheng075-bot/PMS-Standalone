"""
批量设置运维人员主管 + 更新驻场状态
根据组图信息：
  - 何道飞 -> 主管（14人）
  - 舒高成 -> 主管（18人，陈佳佳未找到）
  - 李贝 -> 主管（10人，张兰兰/张岩未找到）
同时将标注"驻场"的人员 IsOnsite 设为 True
"""
import sqlite3, json, copy, sys
from datetime import datetime, timezone

DB_PATH = r'D:\Projects\PMS-Standalone\PMS.API\pms-data.db'
DRY_RUN = '--dry-run' in sys.argv

conn = sqlite3.connect(DB_PATH)

# ── Load personnel ──
cursor = conn.execute("SELECT JsonValue FROM AppState WHERE StateKey='personnel'")
personnel = json.loads(cursor.fetchone()[0])
name_to_id = {p['Name']: p['Id'] for p in personnel}
id_to_name = {p['Id']: p['Name'] for p in personnel}
personnel_by_id = {p['Id']: p for p in personnel}

# ── Load access ──
cursor2 = conn.execute("SELECT JsonValue FROM AppState WHERE StateKey='personnel_access'")
access_list = json.loads(cursor2.fetchone()[0])
access_by_id = {a['PersonnelId']: a for a in access_list}

# ── Define groups: (supervisor_name, [(member_name, is_onsite_override)]) ──
groups = [
    ("何道飞", [
        ("高丹", None), ("黎冰冰", None), ("王文圣", None), ("陈俊华", None),
        ("程灿", None), ("龚傲", None), ("孟海峰", None), ("吕志勇", None),
        ("李梅", None), ("宋城桂", None),
        ("毛黎明", None),       # 巡场，不改驻场
        ("王珣智", True),       # 驻场
        ("李娜", True),         # 驻场
        ("徐啸", None),         # 病休，不改驻场
    ]),
    ("舒高成", [
        ("汪飘", None), ("林峰", None), ("林康", None), ("董子豪", None),
        ("杨帆", None), ("程雯蒨", None), ("孟卓", None),
        ("陈旭", True),         # 驻场
        ("毕启星", True),       # 驻场
        ("木巴热克·马利克", None), ("苏潮", None), ("陈浩", None),
        ("任丽娜", None), ("贾继文", None), ("闫博", None), ("刘浩", None),
        ("杨玉红", None), ("陈佳佳", None), ("黄瑞", None),
    ]),
    ("李贝", [
        ("李旭", None), ("张成旺", None), ("夏安楠", None), ("何小丽", None),
        ("付依轮", None), ("陈劲松", None),
        ("李佳钊", None),       # 巡场，不改驻场
        ("张兰兰", True),       # 驻场
        ("张岩", True),         # 驻场
        ("周艳明", True),       # 驻场
        ("罗亮", True),         # 驻场
        ("柳志明", True),       # 驻场
    ]),
]

# ── Process ──
supervisor_updates = 0
onsite_updates = 0
not_found = []
now_str = datetime.now(timezone.utc).strftime('%Y-%m-%dT%H:%M:%S.%f')[:-3] + 'Z'

for sup_name, members in groups:
    sup_id = name_to_id.get(sup_name)
    if sup_id is None:
        not_found.append(sup_name)
        print(f"[ERROR] Supervisor '{sup_name}' not found!")
        continue

    for m_name, onsite_override in members:
        mid = name_to_id.get(m_name)
        if mid is None:
            not_found.append(m_name)
            print(f"  [SKIP] '{m_name}' not found in personnel database")
            continue

        # Update supervisor in access_list
        acc = access_by_id.get(mid)
        if acc is None:
            # Create new entry
            acc = {
                "PersonnelId": mid,
                "IsAdmin": False,
                "SystemRole": "operator",
                "SupervisorId": None,
                "PermissionKeys": [],
                "HospitalNames": [],
                "UpdatedAt": now_str
            }
            access_list.append(acc)
            access_by_id[mid] = acc

        old_sup = acc.get('SupervisorId')
        if old_sup != sup_id:
            acc['SupervisorId'] = sup_id
            acc['UpdatedAt'] = now_str
            supervisor_updates += 1
            old_name = id_to_name.get(old_sup, 'None') if old_sup else 'None'
            print(f"  [SUP] {m_name} (ID={mid}): {old_name} -> {sup_name}")
        else:
            print(f"  [OK]  {m_name} (ID={mid}): already {sup_name}")

        # Update IsOnsite in personnel if override specified
        if onsite_override is not None:
            p = personnel_by_id.get(mid)
            if p and p.get('IsOnsite') != onsite_override:
                p['IsOnsite'] = onsite_override
                onsite_updates += 1
                print(f"  [SITE] {m_name} (ID={mid}): IsOnsite -> {onsite_override}")

# ── Also set supervisor roles ──
for sup_name, _ in groups:
    sup_id = name_to_id.get(sup_name)
    if sup_id is None:
        continue
    acc = access_by_id.get(sup_id)
    if acc is None:
        acc = {
            "PersonnelId": sup_id,
            "IsAdmin": False,
            "SystemRole": "supervisor",
            "SupervisorId": None,
            "PermissionKeys": [],
            "HospitalNames": [],
            "UpdatedAt": now_str
        }
        access_list.append(acc)
        access_by_id[sup_id] = acc
    if acc.get('SystemRole') != 'supervisor':
        old_role = acc.get('SystemRole', 'operator')
        acc['SystemRole'] = 'supervisor'
        acc['UpdatedAt'] = now_str
        print(f"  [ROLE] {sup_name} (ID={sup_id}): SystemRole {old_role} -> supervisor")

# ── Save ──
if DRY_RUN:
    print(f"\n[DRY RUN] Would update {supervisor_updates} supervisors, {onsite_updates} onsite flags")
    print(f"[DRY RUN] Not found: {not_found}")
else:
    # Save access
    access_json = json.dumps(access_list, ensure_ascii=False)
    conn.execute(
        "UPDATE AppState SET JsonValue=?, UpdatedAt=? WHERE StateKey='personnel_access'",
        (access_json, now_str)
    )
    # Save personnel (for IsOnsite updates)
    if onsite_updates > 0:
        personnel_json = json.dumps(personnel, ensure_ascii=False)
        conn.execute(
            "UPDATE AppState SET JsonValue=?, UpdatedAt=? WHERE StateKey='personnel'",
            (personnel_json, now_str)
        )
    conn.commit()
    print(f"\n[DONE] Updated {supervisor_updates} supervisors, {onsite_updates} onsite flags")
    print(f"[INFO] Not found (skipped): {not_found}")

conn.close()
