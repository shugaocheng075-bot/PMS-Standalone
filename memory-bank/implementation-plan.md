# Implementation Plan

## Goal
- 继续实施登录后主干业务页门户化收口：在上一轮 KPI、统一预警、工时报表、项目台账基础上，继续推进合同预警与巡检管理的首屏升级，形成连续的一屏决策入口。

## Non-goals
- 不改动后端接口、统计口径与既有计划/结果业务动作。
- 不重写巡检管理结果页详情结构，仅调整首屏入口层级与快捷动作组织。
- 不扩散到待办列表三栏地图页的结构重构。

## Constraints
- 只改 `pms-web/src/views/contract/ContractAlertView.vue` 与 `pms-web/src/views/inspection/InspectionPlanView.vue`。
- 必须保留现有 SummaryMetrics、筛选、导出、计划动作流、结果查询与详情能力。
- 必须完成登录态截图验证与 `PMS: Build Frontend` 构建验证。

## Steps
1. 将 `ContractAlertView.vue` 由旧 `page-head` 升级为合同风险门户 hero，并补齐风险摘要与快捷筛选动作。
2. 将 `InspectionPlanView.vue` 升级为巡检门户 hero，接入计划/结果双视图引导与高频动作入口。
3. 截图验证 `contract/alerts` 与 `inspection/plan`，再执行前端构建回归。

## Status
- 2026-04-24: 已完成。`pms-web/src/views/contract/ContractAlertView.vue` 已升级为合同风险门户首屏，补齐风险总量、高风险项目、严重风险与覆盖范围信号摘要及快捷筛选动作；`pms-web/src/views/inspection/InspectionPlanView.vue` 已升级为巡检门户首屏，补齐计划/结果双视图引导、巡检信号摘要与快捷动作入口，并保留既有计划/结果动作链路。验证：登录态截图复查 `http://localhost:5173/contract/alerts` 与 `http://localhost:5173/inspection/plan` 均正常渲染；`PMS: Build Frontend` 通过，`vue-tsc -b && vite build` 成功完成。

## Goal
- 继续实施登录后高频入口页门户化收口：把 KPI、统一预警中心、工时报表、项目台账接到首页和应用壳的同一套蓝绿企业门户语言上，让登录后主干页面形成连续的首屏体验。

## Non-goals
- 不改动后端接口、数据统计口径或现有 CRUD / 导入导出 / 编辑动作链路。
- 不在本轮扩散到全部业务页，只先覆盖高频入口页与报表页。
- 不引入新的运行时依赖，也不重写 ProTable / AppTableCard 组件本身。

## Constraints
- 优先只改 `pms-web/src/views/dashboard/KpiDashboardView.vue`、`pms-web/src/views/alert/AlertCenterView.vue`、`pms-web/src/views/report/WorkHoursReportView.vue`、`pms-web/src/views/project/ProjectListView.vue`。
- 每页必须保持原有图表、筛选、导出、编辑、批量维护等业务动作可用，只调整首屏组织和视觉层级。
- 必须完成登录态截图验证与 `PMS: Build Frontend` 构建验证。

## Steps
1. 将 `KpiDashboardView.vue` 从旧 `page-head` 收口为绩效门户 hero，保留原图表与指标卡逻辑。
2. 将 `AlertCenterView.vue` 改为风险门户首屏，突出风险总量、来源分布与高频筛选动作。
3. 将 `WorkHoursReportView.vue` 与 `ProjectListView.vue` 从“表格/工具栏直出”提升为带月度摘要或项目摘要的入口工作台。
4. 逐页做登录态截图回归，再执行 `PMS: Build Frontend` 完成统一验证。

## Status
- 2026-04-23: 已完成。`pms-web/src/views/dashboard/KpiDashboardView.vue` 已升级为绩效门户 hero，补齐 KPI 信号摘要与快捷入口；`pms-web/src/views/alert/AlertCenterView.vue` 已升级为风险门户首屏，补齐风险总量、严重风险、来源分布与快捷筛选；`pms-web/src/views/report/WorkHoursReportView.vue` 已补齐月度工时报表 hero、月度控制卡与高频动作区；`pms-web/src/views/project/ProjectListView.vue` 已补齐项目台账 hero、项目风险摘要和快捷筛选入口，同时保留原有图表、筛选、导出、编辑和批量维护链路。验证：登录态截图复查 `http://localhost:5173/dashboard/kpi`、`http://localhost:5173/alert/center`、`http://localhost:5173/report/workhours`、`http://localhost:5173/project/list` 均正常渲染；`PMS: Build Frontend` 通过，`vue-tsc -b && vite build` 成功完成。

## Goal
- 继续实施登录后首页收口：从应用壳与 Dashboard 首页开始，沿用登录页的蓝绿企业门户语言，提升登录后第一屏的聚焦感、快捷入口和工作台层级。

## Non-goals
- 不改动后端接口、权限模型或数据聚合口径。
- 不在本轮同时重做 KPI、Hospital360 或其它业务详情页。
- 不引入新的运行时依赖或复杂拖拽/个性化布局能力。

## Constraints
- 只改 `pms-web/src/layout/AppLayout.vue` 与 `pms-web/src/views/dashboard/DashboardView.vue`，保持现有导航与图表数据逻辑兼容。
- 首页必须继续支持现有图表下钻与快捷跳转，不能牺牲已有管理动作入口。
- 必须完成截图验证与前端构建验证。

## Steps
1. 收紧 `AppLayout.vue` 的顶栏、内容顶区与侧栏视觉层级，让应用壳更接近企业门户入口。
2. 重做 `DashboardView.vue` 的首页首屏，新增欢迎 hero、快捷入口和更明确的指标分组。
3. 截图验证登录后首页效果，再执行 `PMS: Build Frontend` 确认编译通过。

## Status
- 2026-04-23: 已完成。`pms-web/src/layout/AppLayout.vue` 已为登录后应用壳补齐企业门户式侧栏概览、内容顶区标题/角色/说明文案，`pms-web/src/views/dashboard/DashboardView.vue` 已将首页首屏重做为欢迎 hero、状态信号与快捷入口工作台，同时保留现有图表、卡片与下钻逻辑。验证：登录态截图复查 `http://localhost:5173/dashboard` 与 `http://localhost:5173/dashboard/kpi` 均正常渲染；`PMS: Build Frontend` 通过，`vue-tsc -b && vite build` 成功完成。

## Goal
- 参照 `https://pms.bjgoodwill.com/welcome` 的 welcome/login 结构，对本项目登录入口做同风格重构，在不改后端认证接口的前提下补齐机构代码、验证码、扫码切换和双栏品牌展示。

## Non-goals
- 不接入真实扫码登录后端，不改动现有 `/api/auth/login` 契约。
- 不尝试登录目标站点抓取受限页面，也不扩散到登录后的业务页改造。

## Constraints
- 保持现有账号密码登录可用，最终仍只向后端提交 `account/password`。
- 机构代码与验证码交互在前端完整闭环，不引入新的运行时依赖。
- 必须完成前端构建验证。

## Steps
1. 重构 `pms-web/src/views/login/LoginView.vue` 的双栏视觉与表单结构，对齐目标页的品牌区、机构代码、验证码与扫码切换体验。
2. 在前端实现本地验证码生成、刷新和校验，并保留现有登录 API 调用链。
3. 执行 `PMS: Build Frontend` 验证编译通过。

## Status
- 2026-04-23: 已完成。已确认当前工具无法直接执行目标站点登录动作，因此本轮基于公开 welcome 页完成 `pms-web/src/views/login/LoginView.vue` 重构：登录页已切换为双栏企业门户结构，补齐机构代码、本地验证码、扫码切换与二维码倒计时刷新交互，同时保持后端认证仍只提交 `account/password`。验证：`PMS: Build Frontend` 通过，`vue-tsc -b && vite build` 成功完成。

## Goal
- 实施 Phase 2 的第六个可交付切片：把 AnnualReport 从“前端与 upsert 直接改状态/提交日期/评审人”收口为 start / submit / complete / reopen 显式动作流，并补齐 annual-report.manage 权限映射与单条医院范围校验。

## Non-goals
- 不重做年度报告导出格式、自动生成逻辑或统计摘要口径。
- 不扩散到 Dashboard、ContractAlert 等其它模块。
- 不引入新的消息通知、批量审批或复杂规则引擎。

## Constraints
- 保持年度报告查询、导出接口兼容，尽量只在现有 Service / Controller / 页面内增量收口。
- 前后端必须同时落地，不能只修后端状态机而保留前端直改入口。
- 必须完成后端构建、前端构建和至少一条真实 API AnnualReport workflow 回归。

## Steps
1. 在 `IAnnualReportService` / `InMemoryAnnualReportService` 中增加 `GetByIdAsync`、`StartAsync`、`SubmitAsync`、`CompleteAsync`、`ReopenAsync`，并限制 create/update/delete 仅允许工作流边界内操作。
2. 在 `AnnualReportsController` 中新增 workflow PATCH 端点、非法状态 400 返回、创建/单条动作医院范围校验，并将 `/api/annual-reports` 权限映射修正为 GET=`annual-report.view`、非 GET=`annual-report.manage`。
3. 在 `pms-web/src/api/modules/annual-report.ts` 与 `pms-web/src/views/annual-report/AnnualReportView.vue` 接入显式 workflow 按钮，移除 `status/submitDate/reviewer/reviewDate` 的内联编辑与创建时直接选状态。
4. 新增 AnnualReport workflow 回归脚本，覆盖 create -> start -> submit -> complete -> reopen -> delete 链路。
5. 执行 `dotnet build PMS.sln`、`PMS: Build Frontend` 与 annual-report workflow 脚本回归。

## Status
- 2026-04-23: 已完成。`PMS.Application/Contracts/AnnualReport/IAnnualReportService.cs`、`PMS.Infrastructure/Services/InMemoryAnnualReportService.cs` 已补齐 `GetByIdAsync`、`StartAsync`、`SubmitAsync`、`CompleteAsync`、`ReopenAsync`，并将 create 固定为 `未开始`、update/delete 限制到 `未开始/编写中`，阻断通过 upsert 直接改 `Status/SubmitDate/Reviewer/ReviewDate`；`PMS.API/Controllers/AnnualReport/AnnualReportsController.cs` 已新增 workflow PATCH 端点、非法状态 400 返回以及创建/单条动作的医院范围校验；`PMS.Infrastructure/Services/InMemoryAccessControlService.cs` 已将 `/api/annual-reports` 权限映射修正为 GET=`annual-report.view`、非 GET=`annual-report.manage`。前端 `pms-web/src/api/modules/annual-report.ts`、`pms-web/src/views/annual-report/AnnualReportView.vue` 已切换到“开始编写 / 提交评审 / 完成评审 / 重开”显式动作流，移除状态、提交日期、评审人、评审日期的内联编辑以及新增时直接选状态入口。验证：`dotnet build PMS.sln` 通过（0 error，保留既有 CS8625 warnings）；`PMS: Build Frontend` 通过；`powershell -ExecutionPolicy Bypass -File scripts/verify-annual-report-workflow.ps1` 实测 create -> start -> submit -> complete -> reopen -> delete 全链路成功，输出 `createdId=221`、`reopenStatus=编写中`、`cleanup=deleted`。

## Goal
- 实施 Phase 2 的第五个可交付切片：把 Handover 从“前端直接按 nextStage 线性推进、后端通用 targetStage 更新”收口为 send-email / start / complete / rollback 显式动作流，并补齐关键时间节点与详情责任信息。

## Non-goals
- 不重做交接数据来源或新增独立 handover 实体。
- 不扩散到 AnnualReport、Dashboard 等其它模块。
- 不引入新的通知任务、批量调度或复杂审批规则。

## Constraints
- 保持现有 `/api/handovers/{id}/stage` 兼容，不破坏已有调用方。
- 仅在现有分层内增量扩展：Application / Infrastructure / API / 对应前端页面与 API client。
- 必须完成后端构建、前端构建和至少一条真实 API handover workflow 回归。

## Steps
1. 在 `IHandoverService` / `InMemoryHandoverService` 中补齐 `send-email`、`start`、`complete`、`rollback` 显式动作，并为交接记录补齐 `StartedAt`、`CompletedAt`。
2. 在 `HandoversController` 中新增显式动作端点、详情端点、医院范围校验和审计日志，并把 `/api/handovers` 的权限映射从“仅 PUT 需要 manage”修正为“非 GET 都需要 manage”。
3. 在 `pms-web/src/api/modules/handover.ts`、`pms-web/src/types/handover.ts`、`pms-web/src/views/handover/HandoverListView.vue` 接入显式动作按钮、详情时间字段与责任节点展示，移除前端 `nextStage` 线性推进语义。
4. 新增 handover workflow 回归脚本，基于现有交接记录执行动作并恢复原始阶段。
5. 执行 `dotnet build PMS.sln`、`PMS: Build Frontend` 与 handover workflow 脚本回归。

## Status
- 2026-04-23: 已完成。`PMS.Application/Models/Handover/HandoverItemDto.cs`、`PMS.Application/Contracts/Handover/IHandoverService.cs`、`PMS.Infrastructure/Services/InMemoryHandoverService.cs` 已为交接记录补齐 `StartedAt`、`CompletedAt` 以及 `send-email`、`start`、`complete`、`rollback` 显式动作，且修正了已交接记录被服务层直接移除、无法在列表和汇总中保留的问题；`PMS.API/Controllers/Handover/HandoversController.cs` 已补齐 `GET /api/handovers/{id}`、workflow PATCH 端点、医院范围校验和审计日志；`PMS.Infrastructure/Services/InMemoryAccessControlService.cs` 已将 `/api/handovers` 权限映射收口为 GET=`handover.view`、非 GET=`handover.manage`。前端 `pms-web/src/api/modules/handover.ts`、`pms-web/src/types/handover.ts`、`pms-web/src/views/handover/HandoverListView.vue` 已切到显式动作按钮，并补齐责任节点、邮件发送时间、开始交接时间、完成时间展示。验证：`dotnet build PMS.sln` 通过（停掉 dev 服务后 0 warning 0 error）；`PMS: Build Frontend` 通过；`powershell -ExecutionPolicy Bypass -File scripts/verify-handover-workflow.ps1` 已在现有 `已交接` 记录上实测 rollback -> complete -> restore 原始阶段成功。

## Goal
- 实施 Phase 2 的第四个可交付切片：把 WorkHours 从“只有编辑/删除，没有 submit / confirm / reject 闭环”补齐为可提交、可确认、可退回的动作流，并补齐状态展示与详情审批信息。

## Non-goals
- 不重做工时报表、统计口径或导出模板。
- 不扩散到 AnnualReport、Handover 等其它模块。
- 不引入新的消息通知、自动催办或复杂审批规则。

## Constraints
- 保持现有工时查询、创建、编辑接口兼容，不重构数据模型主结构。
- 仅在现有分层内增量扩展：Infrastructure / API / 对应前端页面与 API client。
- 必须完成后端构建、前端构建和至少一条真实 API 工时 workflow 回归。

## Steps
1. 在 `InMemoryWorkHoursService` 中为 update/delete 增加状态边界，禁止已提交记录被直接删除，并允许 `rejected -> submitted` 重新提交流转。
2. 在 `WorkHoursController` 中为 `GET /{id}`、`PATCH /submit|confirm|reject` 补齐医院范围校验，并让 workflow 端点返回更新后的 `WorkHoursItemDto`。
3. 在 `pms-web/src/api/modules/workhours.ts`、`pms-web/src/types/workhours.ts`、`pms-web/src/views/workhours/WorkHoursView.vue` 接入状态字段、提交/确认/退回动作按钮与详情审批信息展示。
4. 新增工时 workflow 回归脚本，覆盖 create -> submit -> reject -> update -> submit -> confirm -> delete 链路。
5. 执行 `dotnet build PMS.sln`、`PMS: Build Frontend` 与工时 workflow 脚本回归。

## Status
- 2026-04-23: 已完成。`PMS.Infrastructure/Services/InMemoryWorkHoursService.cs` 已将编辑约束收口到 `draft/rejected`，阻止 `submitted` 记录被删除，并允许 `rejected` 记录重新 `submit`；`PMS.API/Controllers/WorkHours/WorkHoursController.cs` 已为 `GET /api/workhours/{id}` 与 workflow 端点补齐医院范围校验，并在 submit/confirm/reject 后直接返回最新 `WorkHoursItemDto`。前端 `pms-web/src/api/modules/workhours.ts`、`pms-web/src/types/workhours.ts`、`pms-web/src/views/workhours/WorkHoursView.vue` 已接入 `status/confirmedBy/confirmedAt` 字段、状态列、提交/确认/退回按钮以及详情抽屉中的审批动作。验证：`dotnet build PMS.sln` 通过（0 warning 0 error，停掉 dev 服务后执行）；`PMS: Build Frontend` 通过；`powershell -ExecutionPolicy Bypass -File scripts/verify-workhours-workflow.ps1` 实测 create -> submit -> reject -> update -> submit -> confirm -> delete 全链路成功。

## Goal
- 实施 Phase 2 的第三个可交付切片：把 MonthlyReport 从“新增/编辑表单直接改状态”收口为 submit / approve / reject 动作流，并补齐审批信息展示与权限映射。

## Non-goals
- 不重做月报自动生成、结构化内容区块或导出能力。
- 不扩散到 WorkHours、AnnualReport 等其它审批模块。
- 不引入新的后台审批任务、消息队列或复杂规则引擎。

## Constraints
- 保持现有月报查询、创建、编辑接口兼容，`MonthlyReportUpsertDto.Status` 可继续保留但不再驱动状态流转。
- 仅在现有分层内增量扩展：Infrastructure / API / 对应前端页面与 API client。
- 必须完成后端构建、前端构建和至少一条真实 API 月报动作流回归。

## Steps
1. 为 `/api/monthly-reports` 补齐权限映射，确保 GET 使用 `monthly-report.view`，非 GET 使用 `monthly-report.manage`。
2. 在 `InMemoryMonthlyReportService` 中禁止通过 create/update 直接改 `Status`，改为仅允许 `submit` / `approve` / `reject` 专用动作改变状态。
3. 在 `monthly-report.ts`、`monthly-report.ts` 类型定义与 `MonthlyReportView.vue` 接入提交/通过/驳回动作，移除表单中的直接状态选择，并展示审核人/审核时间/驳回原因。
4. 新增月报 workflow 回归脚本，覆盖 create -> submit -> reject -> update -> submit -> approve -> delete 链路。
5. 执行 `dotnet build PMS.sln`、`PMS: Build Frontend` 与月报 workflow 脚本回归。

## Status
- 2026-04-23: 已完成。`PMS.Infrastructure/Services/InMemoryAccessControlService.cs` 已为 `/api/monthly-reports` 补齐 GET=`monthly-report.view`、非 GET=`monthly-report.manage` 权限映射；`PMS.Infrastructure/Services/InMemoryMonthlyReportService.cs` 已禁止通过 create/update 直接写入 `Status`，仅允许 `submit` / `approve` / `reject` 专用动作改变状态，并对编辑态约束为 `draft/rejected`；`PMS.API/Controllers/MonthlyReport/MonthlyReportsController.cs` 已将非法编辑态转换为 400 返回。前端 `pms-web/src/api/modules/monthly-report.ts`、`pms-web/src/types/monthly-report.ts`、`pms-web/src/views/monthly-report/MonthlyReportView.vue` 已接入提交/通过/驳回动作，移除表单中的直接状态选择，并补齐审核人、审核时间、驳回原因展示。验证：`dotnet build PMS.sln` 通过（0 error，保留既有 nullable warnings）；`PMS: Build Frontend` 通过；`powershell -ExecutionPolicy Bypass -File scripts/verify-monthly-report-workflow.ps1` 实测 create -> submit -> reject -> update -> submit -> approve -> delete 全链路成功。

## Goal
- 实施 Phase 2 的第二个可交付切片：把 MajorDemand 与 Inspection 从“列表/表单直接改状态”补齐为可受理/开始、可完成、可重开的动作化流程，并在前端补齐 SLA/截止时间提示与详情时间字段。

## Non-goals
- 不重做 MajorDemand / Inspection 的整体 CRUD 结构。
- 不引入新的后台任务调度、消息通知或复杂规则引擎。
- 不改动其它业务模块的状态流转方式。

## Constraints
- 保持现有接口与前端列表筛选、导出能力兼容。
- 仅在现有分层内增量扩展：Application / Infrastructure / API / 对应前端页面。
- 必须完成后端构建、前端构建和至少一条真实 API 动作流回归。

## Steps
1. 修正 `/api/inspections` 权限映射，改为 GET 使用 `inspection.view`，非 GET 使用 `inspection.manage`。
2. 为 Inspection 计划补齐 `StartedAt`、`CompletedAt`、`SlaDueAt` 与 `start` / `complete` / `reopen` 动作端点，并写入审计日志。
3. 为 MajorDemand workflow 补齐 `AcceptedAt`、`CompletedAt` 与 `accept` / `complete` / `reopen` 单条动作。
4. 在 `MajorDemandView` 与 `InspectionPlanView` 增加动作按钮、SLA/截止提示、详情时间字段，并修正前端按钮权限判断。
5. 执行 `dotnet build PMS.sln`、`PMS: Build Frontend` 与 `scripts/verify-major-demand-inspection-workflows.ps1` 运行态回归。

## Status
- 2026-04-23: 已完成。后端已为 `PMS.API/Controllers/MajorDemand/MajorDemandsController.cs` 新增 `accept` / `complete` / `reopen` 动作端点，并在 `PMS.Infrastructure/Services/InMemoryMajorDemandStore.cs` 中补齐 `AcceptedAt`、`CompletedAt` 与动作日志；`PMS.API/Controllers/Inspection/InspectionsController.cs` 已新增 `start` / `complete` / `reopen` 动作端点与 `IAuditLogService` 记录，`PMS.Infrastructure/Services/InMemoryInspectionService.cs` 补齐 `StartedAt`、`CompletedAt`、`SlaDueAt`、动作流方法以及 custom row 可见性修复，避免新建巡检计划“能删不能查”。前端 `pms-web/src/views/major-demand/MajorDemandView.vue` 已补齐受理/完成/重开按钮与截止倒计时，`pms-web/src/views/inspection/InspectionPlanView.vue` 已补齐开始执行/完成巡检/重开计划按钮、SLA 提示和详情时间字段，并修正按钮权限从 `inspection.view` 改为 `inspection.manage`。验证：`dotnet build PMS.sln` 通过（0 error 0 warning）；`PMS: Build Frontend` 通过；`scripts/verify-major-demand-inspection-workflows.ps1` 实测重大需求与巡检计划两条动作流均完成 create/add -> action -> reopen -> cleanup 全链路回归，其中 inspection 审计日志命中 3 条动作记录。

## Goal
- 实施 Phase 2 的首个可交付切片：把 Repair 从“直接改状态”升级为“可签收 / 可完成 / 可重开”的动作化工单流，并补齐处理 SLA 截止时间与基础时间线。

## Non-goals
- 本轮不扩散到 MajorDemand / Inspection。
- 不引入 HostedService、消息队列或复杂 SLA 策略配置中心。
- 不重做 AuditLog 页面，只保证动作被记录。

## Constraints
- 保持现有 Repair CRUD 与导出接口兼容。
- 仅在现有分层内增量改造：Entity / DTO / Service / Controller / RepairRecordView。
- 必须完成后端构建、前端构建和至少一条真实 API 工作流回归。

## Steps
1. 为 `RepairRecordEntity` / DTO 增加 `AcceptedAt`、`SlaDueAt`，并在 `InMemoryRepairRecordService` 中实现轻量 SLA 计算。
2. 新增 `accept` / `resolve` / `reopen` 动作端点，并把动作写入 `IAuditLogService`。
3. 将 `RepairRecordView` 从表单直接改状态切换为动作按钮流转，列表增加 SLA 列，详情补时间线。
4. 执行 `dotnet build PMS.sln`、`PMS: Build Frontend` 与临时工单全链路回归脚本验证。

## Status
- 2026-04-23: 已完成。Repair 现已支持动作化流转：`/accept` 签收会写入 `AcceptedAt` 并转为“处理中”；`/resolve` 会强制填写处理结果并写入 `CompletedAt`；`/reopen` 会回到“待处理”并重置处理节点，同时三类动作全部写入审计日志。前端 `RepairRecordView` 已新增 SLA 列、签收/完成/重开按钮，移除表单直接改状态入口，并在详情抽屉补齐处理节点时间线。验证：`dotnet build PMS.sln` 通过（0 error，保留既有 nullable warnings）；`PMS: Build Frontend` 通过；`scripts/verify-repair-workflow.ps1` 实测创建临时报修 → 签收 → 完成 → 重开 → 删除全链路成功，且审计日志命中 3 条 repair 动作记录。

## Goal
- 基于全盘审查结果，实施高优先级后端安全加固：备份端点鉴权、数据导入路径白名单、认证中间件公开前缀精确匹配。

## Non-goals
- 不改动前端业务流程和后端服务分层结构。
- 不在本轮处理 P1/P2 的其他项（如 InMemory*Store 与 IoC 生命周期、Hospital360 缺失面板、全量 `size:100000` 拉取等）。

## Constraints
- 改动仅限鉴权/权限/数据导入链路。
- 不破坏既有 admin 能力（备份下载、Data 目录默认路径导入）。

## Steps
1. `InMemoryAccessControlService.ResolveRequiredPermission` 把 `/api/system` 通配放行改为仅放行 `/api/system/info`，并为 `/api/system/backup` 要求 `maintenance.manage`。
2. `DataImportController` 新增 `TryResolveAllowedFilePath` 白名单校验，限定 `text-file`、`major-demand`、`project-ledger` 端点的 `FilePath` 必须在 `AppContext.BaseDirectory/Data` 内。
3. `AuthMiddleware.IsPublicPath` / `PermissionMiddleware` 的 `/api/auth/login` 与 `/api/health` 改为精确匹配，避免 `*/api/healthxxx` 型前缀绕过。
4. 本地构建 + API 行为回归：admin 备份下载 200，越权文件路径 400，精确匹配仍允许 admin 登录。

## Status
- 2026-04-23: 已完成。后端构建 0 error 0 warning（本轮改动范围内）；`/api/system/backup/download` 对 admin 返回 200 且下载 1.6MB 快照；`/api/admin/import/text-file` 传入 `C:/Windows/win.ini` 返回 400 且消息为“仅允许读取应用 Data 目录下的文件”；`major-demand` 端点传入 `../../../../etc/hosts` 同样返回 400。公开前缀从 `StartsWith` 改为精确 HashSet 匹配，且默认角色（operator/supervisor/regional_manager）均不含 `maintenance.manage` 权限键，非 admin 用户会被 403 拦截（按代码路径验证）。

## Goal
- 为 Hospital360 增加到项目、告警、工时、报修页面的联动入口，让页面更像可操作的详情工作台，而不只是聚合看板。

## Non-goals
- 不改动后端接口和目标页面的数据结构。
- 不新增独立详情页路由。

## Constraints
- 变更优先限制在 `pms-web/src/views/hospital/Hospital360View.vue`。
- 复用目标页面现有的 `route.query` 过滤和弹窗/详情打开能力。

## Steps
1. 为当前激活面板增加“进入对应页面”的总入口。
2. 为各表格增加行级联动按钮，携带医院、产品、状态等查询参数跳转。
3. 执行前端构建与页面回归验证。

## Status
- 2026-04-23: 已完成。Hospital360 已补齐面板级“进入对应页面”和行级联动入口，可跳转到项目、告警、工时、报修页面并自动带上医院/产品/状态等筛选参数。验证：`PMS: Build Frontend` 通过；Playwright 登录态回归 `hospital/360`，运行时 `ISSUES=0`，并确认按钮点击后成功跳转到对应列表页。

## Goal
- 继续细化 Hospital360 视图，在不改接口的前提下补齐页头经营摘要、面板内关键指标和更适合医院级巡检的展示顺序。

## Non-goals
- 不新增后端接口、字段或跨模块联动逻辑。
- 不改动医院列表页或其它详情页。

## Constraints
- 变更限制在 `pms-web/src/views/hospital/Hospital360View.vue` 与 memory-bank 记录。
- 完成后必须执行前端构建验证。

## Steps
1. 为页头增加医院级关键摘要，让项目、风险、工时、开放工单一眼可见。
2. 为每个 tab 增加业务摘要提示，并按更利于判断的顺序展示数据。
3. 执行前端构建验证并回写进度。

## Status
- 2026-04-23: 已完成。Hospital360 视图已补齐医院级经营摘要、各 tab 关键指标卡和更利于排查的默认排序。验证：`PMS: Build Frontend` 通过；Playwright 登录态回归 `hospital/360`，运行时 `ISSUES=0`，截图 `regression-hospital360-premium.png` 已刷新。

## Goal
- 将 Hospital360 视图主体从“摘要卡 + 裸 tabs/table”收口到统一的商务化详情页结构，补齐数据面板层级、tab 摘要与标签化信息表达。

## Non-goals
- 不改动医院 360 的接口调用、路由参数和聚合逻辑。
- 不扩散到其它医院管理页面。

## Constraints
- 主要改动限制在 `pms-web/src/views/hospital/Hospital360View.vue`，优先复用现有 `SummaryMetrics`、`AppTableCard` 与全局设计系统。
- 改完后必须执行前端构建验证。

## Steps
1. 将摘要卡改为与 tab 联动，明确当前激活数据面板。
2. 为主内容区增加统一头部说明、记录计数与内容卡片容器。
3. 用标签和说明条提升项目/告警/报修表格的可扫读性。
4. 执行前端构建验证并回写进度。

## Status
- 2026-04-23: 已完成。Hospital360 视图已切换到“页头概览 + 可切换摘要卡 + 统一内容卡”结构，补齐 tab 计数、面板说明和标签化状态表达。验证：`PMS: Build Frontend` 通过；Playwright 登录态回归 `hospital/360`，运行时 `ISSUES=0`，截图已刷新。

## Goal
- 将 PMS 的首页看板、项目列表/待办任务页、抽屉/弹窗表单和全局布局统一提升到更接近上线交付的简约商务高级风，重点解决标题层级混乱、图表默认感强、列表阅读密度差和壳层缺少路径秩序的问题。

## Non-goals
- 不改动任何业务接口、权限逻辑、接口契约与路由路径。
- 不重写页面数据流，优先通过共享组件、设计系统与少量关键页面模板重构完成体验升级。

## Constraints
- 保持 Element Plus 与现有组件兼容，不新增运行时依赖。
- 需要覆盖桌面端主场景，并以构建与登录态截图回归作为收口标准。

## Steps
1. 启用独立设计系统文件，统一 page head、表格、表单、按钮、卡片和分页的商务化低饱和规则。
2. 升级 `AppLayout`，补齐面包屑、侧栏折叠、页脚和更克制的顶层辅助栏。
3. 升级 `ProTable`、`AppTableCard`、`ProDrawer`、`AppFormDialog`，统一长列表与表单容器的阅读节奏。
4. 重构 `DashboardView` 与 `MS010001013001View` 的信息层级和图表/看板视觉，避免默认组件感。
5. 执行前端构建与登录态 Playwright 截图回归，并记录结果。

## Status
- 2026-04-22: 已完成步骤 1-5。已启用 `premium-ui.css` 作为独立设计系统覆盖层，统一全局标题/表格/表单/按钮风格；`AppLayout` 已补齐面包屑、折叠侧栏和页脚；`ProTable`、`AppTableCard`、`ProDrawer`、`AppFormDialog` 已切换到统一商务容器样式；`DashboardView` 与 `MS010001013001View` 已完成看板层级与低饱和配色收口。验证：`powershell -ExecutionPolicy Bypass -File c:\Users\Administrator\PMS-Standalone\scripts\build-frontend.ps1` 通过；登录态 Playwright 截图复核 `dashboard`、`project/list`、`complex-model/ms010001-013-001` 和项目编辑抽屉，运行时 `ISSUES=0`。
- 2026-04-22: 已完成本轮最终一致性收口。三角色工作台、`AuditLogView`、`KpiDashboardView`、`PersonnelListView` 已统一到共享指标卡/容器语言；补齐 `pms-web/src/utils/echarts-basic.ts` 的 `GraphicComponent` 注册，消除 KPI 页面运行时告警。验证：`powershell -ExecutionPolicy Bypass -File c:\Users\Administrator\PMS-Standalone\scripts\build-frontend.ps1` 通过；Playwright 复验 `dashboard/kpi`、`audit/log`、`permission/manage` 及多角色 `dashboard`，运行时 `ISSUES=0`，并生成最新截图回归。
- 2026-04-22: 已完成第二轮列表页统计区共享化。新增 `pms-web/src/components/SummaryMetrics.vue` 作为共享摘要卡组件，并将 `AlertCenterView`、`ContractAlertView`、`RepairRecordView`、`WorkHoursView`、`InspectionPlanView`、`AnnualReportView`、`HospitalListView`、`HandoverListView`、`ProductListView`、`PersonnelListView`、`Hospital360View` 的旧 `stats-row/stat-card` 结构迁移到统一指标卡体系。验证：`PMS: Build Frontend` 通过；Playwright 复验 `annual-report/list`、`hospital/list`、`handover/list`、`product/list`、`permission/manage`、`hospital/360` 以及前一批 `contract/alerts`、`repair/list`、`workhours/list`、`inspection/plan`，运行时 `ISSUES=0`，新截图回归已生成。

## Goal
- 将 PMS 前端整体页面、按钮和交互风格统一升级为更接近成熟大厂企业产品的体验，优先完成全局设计系统、应用壳和登录入口的风格收口。

## Non-goals
- 不修改任何业务接口、权限逻辑、路由结构与页面数据流。
- 不逐页重写业务模板，优先通过全局样式和共用组件完成视觉统一。

## Constraints
- 保持 Element Plus 兼容，不新增运行时依赖。
- 保持桌面端与移动端可用性，并通过构建命令做收口验证。

## Steps
1. 重塑全局设计令牌、按钮、输入框、卡片、表格、弹窗等共用视觉。
2. 升级 `ProTable`、`AppLayout`、`LoginView` 的布局层次与交互反馈。
3. 执行前端构建验证，确认样式改造未破坏编译与入口渲染。
4. 在 `progress.md` 记录本次改造与验证结果。

## Status
- 2026-04-22: 已完成步骤 1-4。已统一全局设计令牌、列表容器、应用壳和登录入口的视觉与交互风格，并完成构建与浏览器验证。
- 2026-04-22: 已补完第二轮细节收口。抽屉、弹窗、工作台卡片、空状态、加载态和个人资料详情页已统一到同一套交互语言，并通过前端构建验证。

## Goal
- 继续实施列表页紧凑化第二批收口：巡检计划、交接管理、合同预警、医院管理四页在保持现有数据结构不变的前提下，压缩排版并尽量避免关键列与操作区换行。

## Non-goals
- 不调整接口、权限、筛选逻辑或 workflow 动作语义。
- 不再次大改全局设计系统，仅处理本页列宽和局部容器样式。

## Constraints
- 保持现有表格字段、按钮文案和跳转逻辑兼容。
- 优先通过列宽、nowrap、ellipsis 收口，不引入新的交互层。
- 必须完成前端构建验证。

## Steps
1. 调整 `InspectionPlanView.vue` 的计划表/结果表关键列宽，并让 `deadline-cell` 与操作区保持单行优先。
2. 调整 `HandoverListView.vue` 的医院/产品/组别/批次/阶段列宽，并让详情动作区和看板 meta 单行省略。
3. 调整 `ContractAlertView.vue` 与 `HospitalListView.vue` 的关键文本列、评级列和操作区宽度，补齐局部 nowrap 样式。
4. 执行 `PMS: Build Frontend` 验证本轮布局补丁。

## Status
- 2026-04-23: 进行中。已确认本轮控制路径是“本页关键列偏窄 + action/meta 容器允许换行”，准备在 `pms-web/src/views/inspection/InspectionPlanView.vue`、`pms-web/src/views/handover/HandoverListView.vue`、`pms-web/src/views/contract/ContractAlertView.vue`、`pms-web/src/views/hospital/HospitalListView.vue` 做局部列宽和 nowrap 收口，然后执行前端构建验证。
- 2026-04-22: 已完成最终截图巡检收口。`PersonnelListView` 权限管理表取消 `fixed-right` 操作列，解决真实数据下的列叠压与透字问题；使用 admin 有效会话对 `dashboard`、`profile`、`permission/manage` 及编辑抽屉进行 Playwright 实页验证，运行时 `ISSUES=0`。

## Goal
- 彻底修复本地开发环境下 PMS 打不开且页面空白的问题，消除仓库迁移后 Vite 缓存指向旧绝对路径导致的入口加载失败。

## Non-goals
- 不修改任何业务接口、权限契约与页面功能流程。
- 不调整生产部署脚本与服务端口配置。

## Constraints
- 变更范围限制在前端入口、Vite 启动链路和本地开发防呆。
- 每次改动后必须以可执行命令验证，而不是仅看代码。

## Steps
1. 修复 Vite 启动时对旧仓库路径缓存的自愈逻辑。
2. 为前端挂载与应用壳初始化增加首屏兜底，避免异常直接落成空白页。
3. 重启本地前端并验证首页与 /src/main.ts 均返回 200。
4. 执行前端构建验证并记录结果。

## Status
- 2026-04-22: 步骤 1-4 已完成。已修复迁移路径下的 Vite stale cache / root 解析问题，并补齐首屏初始化兜底。

## Goal
- 提升 PMS 前端整体观感，优先改造应用壳与登录页视觉层次，解决“页面偏朴素”问题。

## Non-goals
- 不改动任何业务接口、权限逻辑与路由结构。
- 不调整页面功能交互流程。

## Constraints
- 保持现有 Vue 组件结构，优先通过样式与轻量模板增强实现。
- 保持移动端可用性，不引入新的运行时依赖。

## Steps
1. 统一全局设计令牌与基础视觉（色板、背景、卡片、按钮、动效）。
2. 优化 `AppLayout` 顶栏、侧栏、主内容区层次和选中态。
3. 优化 `LoginView` 品牌表达、表单观感与首屏质感。
4. 执行前端构建验证，并记录结果。

## Status
- 2026-04-22: 已完成步骤 1-3。
- 2026-04-22: 步骤 4 已执行；构建失败，原因为现有 Vite/Rolldown 配置在当前路径下触发 `vite:build-html` 的 `fileName` 绝对路径报错（与本次样式改动无直接关系）。

## Verification
- `powershell -ExecutionPolicy Bypass -File c:\Users\Administrator\PMS-Standalone\scripts\build-frontend.ps1` 已通过，`vite build` 成功产出 dist。
- 本地联调验证已通过：前端 `http://127.0.0.1:5173/` 返回 200，`/src/main.ts` 返回 200，后端 `http://127.0.0.1:5111/api/health` 返回 401（鉴权拦截，服务存活）。

## Goal
- 将 `tanweai/pua` 以项目级方式接入 PMS，支持在当前仓库内手动触发与技能调用。
- 不覆盖现有 `copilot-instructions.md`，避免影响既有项目规则。

## Non-goals
- 不修改任何业务代码与 API 契约。
- 不替换现有全局 Copilot 指令文件。

## Constraints
- 仅新增 AI 工具链配置文件（`.agents/`、`.github/prompts/`）。
- 采用上游文件直拷贝，便于后续同步升级。

## Steps
1. 通过 SSH 拉取 `tanweai/pua` 仓库到本机临时目录。
2. 复制 Codex 项目级技能文件到 `.agents/skills/pua/SKILL.md`。
3. 复制手动触发 prompt 到 `.agents/prompts/pua.md` 与 `.github/prompts/pua.prompt.md`。
4. 验证文件存在并可在当前仓库使用。

## Status
- 2026-04-22: 已完成步骤 1-4。
- 2026-04-22: 已按实际使用体验去重入口，移除 `.agents/prompts/pua.md` 与 `.github/prompts/pua.prompt.md`，仅保留 `.agents/skills/pua/SKILL.md`。

## Verification
- `.agents/skills/pua/SKILL.md` 已生成（29106 bytes）。
- 已验证当前仅保留一个 pua 入口来源（skill）。