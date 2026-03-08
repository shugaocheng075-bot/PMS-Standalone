# Architecture Snapshot — PMS-Standalone

## Solution Structure
```
PMS-Standalone/
├── PMS.slnx                    # Solution file
├── PMS.Domain/                 # 领域层：Entities
│   └── Entities/
├── PMS.Application/            # 应用层：Contracts (interfaces) + Models (DTOs)
│   ├── Contracts/              # 服务接口，按模块子目录
│   └── Models/                 # DTO / 请求响应模型
├── PMS.Infrastructure/         # 基础设施层：服务实现 + 持久化
│   ├── Services/               # InMemory*Service 实现
│   └── Persistence/            # SqliteTableStore, SqliteJsonStore
├── PMS.API/                    # API 层：Controllers + Middleware
│   ├── Controllers/            # 按模块子目录 (19 controllers)
│   │   ├── Access/
│   │   ├── Admin/
│   │   ├── Alert/
│   │   ├── AnnualReport/
│   │   ├── Auth/
│   │   ├── Contract/
│   │   ├── Dashboard/
│   │   ├── Handover/
│   │   ├── Hospital/
│   │   ├── Inspection/
│   │   ├── MajorDemand/
│   │   ├── MonthlyReport/
│   │   ├── Personnel/
│   │   ├── Product/
│   │   ├── RepairRecord/
│   │   ├── Report/
│   │   └── WorkHours/
│   └── Middleware/
├── pms-web/                    # 前端 Vue 3 应用
│   └── src/
│       ├── api/modules/        # 按模块的 HTTP 客户端
│       ├── views/              # 按模块的页面组件 (21 views)
│       ├── composables/        # 共享组合式函数
│       ├── components/         # 通用组件
│       ├── layout/             # AppLayout 应用壳
│       ├── router/             # 路由定义 + 权限守卫
│       ├── constants/          # 集中常量
│       ├── types/              # TypeScript 类型
│       └── utils/              # 工具函数
├── scripts/                    # 部署/管理脚本
├── deploy-cn/                  # 国内部署配置
└── memory-bank/                # Vibe 工作流上下文
```

## 数据持久化
- **SQLite** 单文件数据库 (`pms-data.db`), WAL mode, busy_timeout=5000
- **SqliteTableStore<T>**: 泛型关系型行存储，反射建表，row-level CRUD
- **SqliteJsonStore**: JSON 序列化 KV 存储（用于无固定 Id 的实体）
- 8 个服务使用 SqliteTableStore, 6 个服务使用 SqliteJsonStore

## RBAC / 数据范围
- `IAccessControlService.GetDataScopeAsync(username)` → DataScope { ScopeType, AccessiblePersonnelNames, AccessibleHospitalNames }
- ScopeType: Own (个人) / Subordinates (下属) / All (全量)
- 已实现过滤的控制器: Projects, WorkHours, Alerts(合同), AnnualReports, Handovers, RepairRecords, Inspections, MajorDemands
- **未实现过滤**: DashboardController, AlertCenterController

## 前端技术栈
- Vue 3.5 + TypeScript + Vite 8.0
- Element Plus (UI 组件库)
- ECharts (图表)
- 响应式布局 (992px/768px/576px 断点)

## Operational Notes
- Backend 端口: 5111, Frontend 端口: 5173
- Frontend proxy `/api` → `http://localhost:5111`
- 全部服务使用 AddSingleton 注入 (InMemory 实现)

## Update Rule
When adding a new module or moving responsibilities, update this document in the same patch.
