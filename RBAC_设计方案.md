# PMS 权限管理系统 RBAC 改造方案

## 一、角色定义

| 角色 | SystemRole | 数据范围 | 核心能力 |
| ---- | ---------- | -------- | -------- |
| 普通运维人员 | `operator` | 仅本人项目 | 查看自己项目、添加重大需求/进度、添加报修记录、导出 |
| 运维主管 | `supervisor` | 下属项目 | 管理下属运维人员的项目、查看合同、查看项目信息 |
| 经理 | `manager` | 全部数据 | 分配权限、管理工时/项目/报修/医院/合同/重大需求 |

## 二、数据范围控制 (Data Scope)

- **operator**: `MaintenancePersonName == 当前用户姓名`
- **supervisor**: `MaintenancePersonName IN (本人 + 所有 SupervisorId 指向自己的下属)`
- **manager / admin**: 不限制

## 三、权限键 (Permission Keys)

### 新增权限键

| Key | Name | Module |
| --- | ---- | ------ |
| `repair.view` | 查看报修记录 | 运营管理 |
| `repair.manage` | 维护报修记录 | 运营管理 |
| `workhours.view` | 查看工时 | 运营管理 |
| `workhours.manage` | 维护工时 | 运营管理 |

### 角色默认权限

| 权限 | operator | supervisor | manager |
| ---- | -------- | ---------- | ------- |
| dashboard.view | ✅ | ✅ | ✅ |
| alert-center.view | ❌ | ✅ | ✅ |
| project.view | ✅(仅本人) | ✅(含下属) | ✅ |
| project.manage | ❌ | ✅ | ✅ |
| contract.view | ❌ | ✅ | ✅ |
| major-demand.view | ✅ | ✅ | ✅ |
| major-demand.manage | ✅(仅本人项目) | ✅ | ✅ |
| repair.view | ✅ | ✅ | ✅ |
| repair.manage | ✅(仅本人项目) | ❌ | ✅ |
| workhours.view | ❌ | ❌ | ✅ |
| workhours.manage | ❌ | ❌ | ✅ |
| hospital.view | ❌ | ❌ | ✅ |
| hospital.manage | ❌ | ❌ | ✅ |
| permission.manage | ❌ | ❌ | ✅ |

## 四、数据模型变更

### PersonnelPermissionState (后端存储)

```text
+ SystemRole: string (operator/supervisor/manager)
+ SupervisorId: int? (上级主管的 PersonnelId)
```

### PersonnelAccessProfileDto (API 输出)

```text
+ SystemRole: string
+ SupervisorId: int?
+ SupervisorName: string?
+ DataScope: DataScopeDto
```

### DataScopeDto

```text
ScopeType: string (own/subordinates/all)
AccessiblePersonnelNames: List<string>
```

## 五、新增模块

### 报修记录 (RepairRecord)

- 实体字段: Id, ProjectId, HospitalName, ProductName, ReporterName, Description, Status, CreatedAt, UpdatedAt
- API: CRUD + Export

### 工时管理 (WorkHours)

- 实体字段: Id, PersonnelName, ProjectId, HospitalName, WorkDate, Hours, WorkType, Description, CreatedAt
- API: CRUD + Export
