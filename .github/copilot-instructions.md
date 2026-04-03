# PMS-Standalone — Copilot 项目指令

## 项目概要

运维项目管理系统（.NET 8 + Vue 3 全栈），管理医疗设备/IT运维的项目台账、人员、巡检、报修、工时、合同预警等。

## 技术栈速查

| 层 | 技术 | 关键依赖 |
|---|---|---|
| 后端 | ASP.NET Core .NET 8, C# 12 | ClosedXML, ExcelDataReader, Swashbuckle |
| 持久化 | SQLite (WAL mode) | SqliteTableStore（泛型行存储）, SqliteJsonStore（KV 存储） |
| 前端 | Vue 3.5 + TypeScript + Vite 8 | Element Plus, ECharts, Pinia, Axios |
| 部署 | 单机 backend:5111, frontend:5173 | Vite proxy `/api` → `http://localhost:5111` |

## 后端分层（硬约束）

```
PMS.Domain/         → Entities, value objects — 禁止引用 Infrastructure
PMS.Application/     → Contracts(接口), Models(DTO) — 禁止引用 Infrastructure
PMS.Infrastructure/  → Service 实现, SqliteTableStore/SqliteJsonStore 持久化
PMS.API/             → Controllers(薄编排), Middleware — 委托给 Service
```

- 依赖方向：API → Infrastructure → Application → Domain
- 不允许反向依赖或跨层直接访问

## 前端结构

```
pms-web/src/
  api/modules/     → 一个后端模块对应一个 .ts 文件
  views/           → 按模块分子目录的页面组件
  composables/     → 共享组合式函数
  components/      → 通用 UI 组件
  layout/          → AppLayout 应用壳 + 导航
  router/          → 路由 + 权限守卫
  types/           → TypeScript 类型定义
  constants/       → 集中常量
```

## RBAC 模型

- 4 级角色: `operator` → `supervisor` → `regional_manager` → `manager`
- 数据范围: `Own`（个人） / `Subordinates`（下属） / `All`（全量）
- 后端通过 `IAccessControlService.GetDataScopeAsync()` 获取数据范围
- Controller 中用 `HospitalScopeHelper.FilterByHospitalScope()` 做数据隔离
- 前端通过 `canManage` / `canApprove` 等计算属性控制按钮可见性

## 命名约定

### 后端
- Controller: `{Module}Controller` — primary constructor 注入服务
- Service 接口: `I{Module}Service` 放 `PMS.Application/Contracts/{Module}/`
- Service 实现: `InMemory{Module}Service` 放 `PMS.Infrastructure/Services/`
- Entity: `{Module}Entity` 放 `PMS.Domain/Entities/`
- DTO: `{Module}Dto`, `{Module}Query`, `{Module}Upsert` 放 `PMS.Application/Models/{Module}/`

### 前端
- API 客户端: `pms-web/src/api/modules/{module}.ts`
- 类型定义: `pms-web/src/types/{module}.ts`
- 页面组件: `pms-web/src/views/{module}/{Module}View.vue`
- 组合式函数: `pms-web/src/composables/use{Feature}.ts`

## 关键模式

### 响应包装
后端所有 API 返回 `ApiResponse<T>`：`{ code, message, data }`
前端用 `ApiResponse<T>` / `PagedResult<T>` 泛型解析

### 分页
后端: LINQ `Skip((page-1)*size).Take(size)` + 返回 `{ items, total, page, size }`
前端: `el-pagination` 组件绑定

### 数据导出
后端: ClosedXML 生成 `.xlsx`, 返回 `FileContentResult`
前端: Axios `responseType: 'blob'` + `URL.createObjectURL` 下载

## 构建命令

```
dotnet build PMS.slnx                    # 后端构建
cd pms-web && npm run build              # 前端构建
```

## 工作流规则（来自 AGENTS.md）

1. 修改前先读 `memory-bank/project-brief.md` 和 `memory-bank/architecture.md`
2. 一个补丁一个任务（feature / bugfix / cleanup 不混合）
3. API 契约变化时必须同时验证前端受影响点
4. 不要静默修改 `appsettings.json` 或部署脚本
5. 完成后在 `memory-bank/progress.md` 记录变更和验证方式

## 可用 Skills（按需加载详细操作指南）

- `new-crud-module`: 新增 CRUD 模块端到端流程（Entity → Service → Controller → 前端）
- `data-export`: Excel 数据导出模式（ClosedXML + blob 下载）

查看完整操作指南: `prompts/skills/*/SKILL.md`
