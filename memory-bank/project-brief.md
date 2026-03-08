# Project Brief — PMS-Standalone

## 项目名称
PMS-Standalone — 运维项目管理系统

## 项目定位
面向医疗设备/IT运维服务公司的内部项目管理平台，支持项目台账、人员管理、巡检、报修、工时、合同预警、交接管理等全生命周期运维管理。

## 目标用户（三种角色）
1. **经理（Manager）**：查看所有区域所有项目所有人员所有信息，全局决策视角。
2. **运维主管（Supervisor）**：查看所负责区域的医院、产品、人员等信息，区域管理视角。
3. **普通运维服务人员（Operator）**：管理自己负责的医院、产品、报修、重大需求等，个人执行视角。

## 技术栈
- **后端**：ASP.NET Core .NET 8, SQLite (WAL mode), SqliteTableStore (关系型行存储)
- **前端**：Vue 3 + TypeScript + Vite, Element Plus UI, ECharts
- **部署**：单机部署，backend :5111, frontend :5173, proxy /api

## 核心模块
| 模块 | 后端 | 前端 |
|------|------|------|
| 项目台账 | ProjectsController | ProjectListView / ProjectDetailsView |
| 人员管理 | PersonnelController | PersonnelListView |
| 医院管理 | HospitalController | HospitalListView |
| 产品管理 | ProductController | ProductView |
| 报修记录 | RepairRecordController | RepairRecordView |
| 巡检管理 | InspectionController | InspectionResultView / InspectionPlanView |
| 重大需求 | MajorDemandController | MajorDemandView |
| 工时管理 | WorkHoursController | WorkHoursView |
| 月度报告 | MonthlyReportController | MonthlyReportView |
| 年度报告 | AnnualReportsController | AnnualReportView |
| 合同预警 | AlertsController | ContractAlertView |
| 交接管理 | HandoversController | HandoverView |
| Dashboard | DashboardController | DashboardView / KpiDashboardView |
| 统一预警 | AlertCenterController | AlertCenterView |
| 权限管理 | AccessController / AuthController | PersonnelListView (权限配置弹窗) |
| 数据导入 | DataImportController | (仅 API) |

## RBAC 体系
- 4 级角色：operator → supervisor → regional_manager → manager
- 25 个权限键，按模块分 5 组
- 数据范围引擎：own / subordinates / all，由 `BuildDataScopeAsync` 实现
