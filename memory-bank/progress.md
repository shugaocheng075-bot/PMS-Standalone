# Progress

2026-03-07: 巡检计划、年度报告补齐前端编辑态；月报来源页到交接/巡检/年报的跳转补充 action 与更精确的医院/产品上下文，并在目标页支持单条命中自动打开。验证：前端诊断无错，npm run build 通过。
2026-03-07: 权限配置页打开与保存后强制刷新上级主管候选人，确保新设为运维主管的人员能立即被其他人员选择为上级主管。验证：PersonnelListView 诊断无错，pms-web 构建通过。
2026-03-07: 权限配置“上级主管”新增兜底候选（无 supervisor/manager 时允许选择服务角色人员）；月度报告生成“组别”改为优先按运维主管(systemRole=supervisor)筛选，缺失时再回退经理组/全组别。验证：pms-web 执行 npm run build 通过。
2026-03-07: 权限配置页收紧“上级主管”候选：仅保留 systemRole=supervisor 或人员岗位口径包含“服务主管/运维服务主管”，不再出现普通运维。验证：PersonnelListView 诊断无错，pms-web 构建通过。
2026-03-07: 权限管理补齐人员编辑扩展字段（性别/婚姻状况/常住地/家庭省市/岗位/用户状态）并持久化到 sourceColumns；上级主管候选调整为运维主管+运维区域经理+运维经理口径，标签显示岗位/角色。验证：前端诊断无错，pms-web 构建通过；后端编译受运行中 PMS.API 锁文件影响未完成。
2026-03-07: 月度报告生成“组别”补齐运维主管口径：岗位识别新增“运维主管/岗位名称”字段别名，并将岗位命中与系统角色 supervisor 统一为“运维主管”标签展示，避免组别下拉缺失运维主管标识。验证：pms-web 执行 npm run build 通过。
2026-03-07: 月度报告生成“组别”增加兜底展示口径：即使无 supervisor 数据也显示“运维主管：未配置/经理代管”，避免只显示纯组名导致误判未生效；并重启 5173 预览服务。验证：pms-web 执行 npm run build 通过，vite dev 启动成功。
2026-03-07: 人员扩展字段（性别等）保存失败排查并修复：确认根因为后端旧进程未重启导致新 DTO 字段未生效；停止旧 PMS.API 进程后重建并重启服务。验证：dotnet build PMS.slnx 通过，Swagger(5111) 返回 200。
2026-03-08: 修复巡检计划页编辑、详情和双击无反馈问题：将巡检计划详情/编辑弹窗从巡检结果 Tab 内移到页面根层，避免在计划页触发时弹窗挂在隐藏容器下不可见。验证：InspectionPlanView 前端诊断无错。
2026-03-08: 排查“重大需求”菜单偶发显示错字，确认前端未做标题字符串计算或改写；为侧边栏菜单强制指定中文字体、字重并关闭字体合成，降低小字号下中文字形误判。验证：AppLayout 前端诊断无错。
2026-03-08: 继续收口菜单栏名称异常显示问题：为桌面/移动菜单栏容器增加 zh-CN 与 translate=no，并统一菜单栏文本字体与 text-size-adjust，避免菜单名称被浏览器翻译或自动缩放干扰。验证：AppLayout 前端诊断无错。
2026-03-08: 修复重大需求单条状态修改串行污染问题：后端为重大需求导入/存量快照统一强制唯一 RowId，并在服务启动时自动修复既有 RowId 冲突；前端表格补充 row-key 稳定行标识，避免状态渲染和选择复用错位。验证：重大需求相关文件诊断通过，后端构建待验证。
2026-03-08: 修复“点击状态标签有统计但无列表”交互问题：年度报告改为状态归一化比对（消除空格/全角空格差异）并将“商机编号”统一改名“机会号”；同时对重大需求与巡检计划的同类状态交互（筛选/按钮显隐）做归一化加固。验证：pms-web 执行 npm run build 通过。
2026-03-09: 全站审计优化批量实施。后端：新增 GlobalExceptionMiddleware 全局异常兜底；CORS 从 AllowAnyOrigin 收紧为可配 WithOrigins。前端：分页 page-sizes 统一为 [15,30,50,100]（14处）；MajorDemandView 分页 layout 补 total/sizes 并接入 useFilterStatePersist；15 个 el-dialog 补 destroy-on-close；PersonnelListView 操作按钮 plain→link；AppLayout router-view 包裹 fade-slide transition；style.css 新增 6 个 CSS 变量 + 过渡动画类；Login 卡片宽度改 min(420px,90vw) 响应式；新增 constants/tableConfig.ts 集中分页常量。
2026-03-10: Task 1 (P0) — Dashboard + AlertCenter 数据范围修复。DashboardController v2/v3 注入 IAccessControlService，所有聚合数据（events、projects、repairs、workHours、inspections）均按 HospitalScope 过滤；AlertCenterController 同步加固。验证：dotnet build 0 错误。
2026-03-10: Task 2 (P1-a) — 个人工作台。后端新增 GET /api/dashboard/workbench 端点，聚合临期合同(30天)、待巡检、未处理报修、本月工时、项目数。前端新建 PersonalWorkbench.vue（3列待办卡片+4统计指标），DashboardView.vue 根据 isManager() 自动切换管理仪表盘/个人工作台。验证：dotnet build 0 错误，npm run build 通过。
2026-03-10: Task 3 (P1-b) — 通知系统。后端新增 NotificationEntity、INotificationService/InMemoryNotificationService（SQLite 持久化，_nextId 自增模式）、NotificationController（4端点：summary/query/mark-read/mark-all-read）。前端新增 types/notification.ts、api/modules/notification.ts，AppLayout.vue 铃铛图标升级为 el-popover+el-badge 通知面板（60秒轮询、标已读、点击导航）。验证：dotnet build 0 错误，npm run build 通过。
2026-03-10: Task 4 (P2-a) — 数据导入UX升级。DataMaintenanceView 导入流程增加文件预校验、进度条反馈、错误明细展示，提升导入体验。验证：npm run build 通过。
2026-03-10: Task 5 (P2-b) — 缺失模块导出补齐。后端为巡检计划、交接记录、医院、年度报告、工时5个模块新增 GET export 端点（CSV 导出）；前端对应6个视图增加导出按钮与 API 调用。验证：dotnet build 0 错误，npm run build 通过。
2026-03-10: Task 6 (P2-c) — 筛选/搜索增强。AlertCenterView 接入 useFilterStatePersist+useLinkedRealtimeRefresh+useResilientLoad 三合一；ProductListView 接入 useFilterStatePersist；ProjectListView/MajorDemandView 接入 useResilientLoad。验证：npm run build 通过。
2026-03-10: Task 7 (P3-a) — 批量操作扩展。产品管理模块新增批量删除：后端 IProductService.BatchDeleteAsync + InMemoryProductService 实现 + ProductController [HttpPost("batch-delete")]；前端 ProductListView 增加多选+批量删除按钮+确认弹窗。验证：dotnet build 0 错误，npm run build 通过。
2026-03-10: Task 8 (P3-b) — 操作日志。全栈实现：AuditLogEntity + IAuditLogService/InMemoryAuditLogService + AuditLogController（分页查询+汇总）；前端 AuditLogView.vue（统计卡片+筛选表单+表格+分页）；路由/权限/菜单全部注册。验证：dotnet build 0 错误 0 警告，npm run build 通过。
2026-03-10: Task 9 (P3-c) — 移动端深度适配。style.css 新增 ~150 行全局响应式 CSS：pointer:coarse 触控优化（44px 最小触控区、触觉反馈）、768px 弹窗/抽屉/表单自适应、576px 超小屏紧凑模式。验证：npm run build 通过。
2026-03-10: Task 10 (P3-d) — 数据分析深化。DashboardView 接入 v2 API (fetchDashboardV2)，新增"告警月度趋势"堆叠面积图（近6个月严重/警告/提醒三线）和"责任人工作量 TOP12"水平堆叠柱状图；analysis-grid 双列布局+1280px 响应式。验证：npm run build 通过。
2026-03-10: Task 11 (P4-a) — 批量导入校验。DataImportController 新增 ImportBatchValidationResult 详情返回，前端增加导入前预检与错误明细。
2026-03-10: Task 12 (P4-b) — Excel 导出。报修记录、巡检计划、交接记录、工时报表的 CSV 导出升级为 ClosedXML Excel 导出（.xlsx），WorkHoursReportView 导出按钮改用 downloadBlob 调用。验证：dotnet build 0 错误，vue-tsc 0 错误。
2026-03-10: Task 13 (P4-c) — 仪表盘时间范围选择器。DashboardView 新增 el-select（1/3/6/12月），fetchDashboardV2 增加 months 参数，后端按 months 过滤事件/告警数据。验证：vue-tsc 0 错误。
2026-03-10: Task 14 (P4-d) — 个人工作台增强。PersonalWorkbench 新增第4列统计卡片（项目总数）、stat cards 增加点击跳转。
2026-03-10: Task 15 (P5-a) — 医院360°视图。新建 Hospital360View.vue（医院选择器+4统计卡+4 tab 数据面板：项目/告警/工时/报修），路由 /hospital/360 注册。验证：vue-tsc 0 错误，dotnet build 0 错误。
2026-03-10: Task 16 (P5-b) — 用户资料页。新建 UserProfileView.vue（基本信息、修改密码表单、数据范围、权限列表），路由 /profile 注册，AppLayout 头部增加"资料"按钮。
2026-03-10: Task 17 (P5-c) — 系统设置页。后端新建 SystemController（GET /api/system/info 返回版本/OS 等元数据），前端新建 SystemSettingsView.vue（系统信息+快捷入口+前端信息），路由 /settings 注册，/api/system 加入权限跳过列表。
2026-03-10: Task 18 (P5-d) — 备份恢复。SqliteJsonStore 暴露 GetDbPath()+CreateConnection()。新建 BackupController（GET download 用 VACUUM INTO 一致快照+POST restore 校验上传 DB 有效性后替换）。前端 system.ts 增加 downloadBackup/uploadRestore，SystemSettingsView 增加备份下载/恢复上传 UI。
2026-03-10: Task 19 (P5-e) — 打印/PDF。新建 composables/usePrint.ts（打开新窗口+注入样式+隐藏非打印元素+window.print），WorkHoursReportView、MonthlyReportGenerateView、AnnualReportView 三个报表视图均增加"打印"按钮+pageRef。
2026-03-10: Task 20 (P5-f) — UX 加固。5个 Dashboard/Workbench 视图主容器增加 v-loading 指令消除首屏空壳闪烁；MS010001013001View（项目地图看板）新增 mapLoading 状态；Controllers/System 文件夹重命名为 Controllers/Infra 修复 C# System 命名空间冲突。验证：vue-tsc 0 错误，dotnet build 0 错误 17 警告。
2026-03-09: 全面审查收口补丁 — 补齐 AuditLogView 的全局组件规范化：筛选/表格容器迁移到 AppFilterCard/AppTableCard，分页尺寸统一改用 tableConfig 常量（PAGE_SIZES/DEFAULT_PAGE_SIZE），并移除页面局部 .filter-card/.table-card 冗余样式。验证：vue-tsc 0 错误，npm run build 通过。
2026-03-09: 继续收口（规范+性能）— AppFormDialog 增强统一行为（size 预设 + close-on-press-escape 默认开启）；Vite 增加 manualChunks 分包；MS010001013001View 改为懒加载 echarts 与 china.geo.json。结果：complex-model-view 由 ~1891.96kB 降至 ~197.48kB，vue-tsc 0 错误，npm run build 通过；当前主要大包剩余 vendor-element-plus / vendor-echarts / china.geo。
2026-03-09: 继续性能压缩 — 前端切换 Element Plus 按需加载（移除 main.ts 全量 app.use(ElementPlus) 与全量样式），改为 Vite unplugin-auto-import + unplugin-vue-components + ElementPlusResolver；MS010001013001View 的 china.geo.json 从 JS 动态导入改为 URL fetch 静态资源加载，避免生成大型 JS 数据块。验证：待本轮构建结果确认。
2026-03-09: 性能压缩结果确认 — 构建通过（vue-tsc 0 错误）；Element Plus 体积显著下降（历史 ~806kB → ~140kB 后再由默认分包拆散），complex-model 业务块降至约 10~13kB 级；china.geo 从 JS chunk 变为静态 json 资源。当前仅剩 ECharts 相关 chunk 约 645kB 超出 500k 警告阈值，属于图表库体积告警。
2026-03-09: ECharts 终轮拆分 — 新增 `utils/echarts-basic.ts` 与 `utils/echarts-map.ts`，Dashboard/KPI 改用 basic，项目地图改用 map；移除旧 `utils/echarts.ts`。构建结果：ECharts 拆为 `echarts-basic`(~96.93kB)、`echarts-map`(~87.05kB) 与 `installCanvasRenderer`(~465.33kB)，不再出现 >500k chunk 警告；vue-tsc 0 错误，npm run build 通过。
