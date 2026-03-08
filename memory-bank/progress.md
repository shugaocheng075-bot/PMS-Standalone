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
2026-03-08: 修复“点击状态标签有统计但无列表”交互问题：年度报告改为状态归一化比对（消除空格/全角空格差异）并将“商机编号”统一改名“机会号”；同时对重大需求与巡检计划的同类状态交互（筛选/按钮显隐）做归一化加固。验证：pms-web 执行 npm run build 通过。2026-03-09: 全站审计优化批量实施。后端：新增 GlobalExceptionMiddleware 全局异常兜底；CORS 从 AllowAnyOrigin 收紧为可配 WithOrigins。前端：分页 page-sizes 统一为 [15,30,50,100]（14处）；MajorDemandView 分页 layout 补 total/sizes 并接入 useFilterStatePersist；15 个 el-dialog 补 destroy-on-close；PersonnelListView 操作按钮 plain→link；AppLayout router-view 包裹 fade-slide transition；style.css 新增 6 个 CSS 变量 + 过渡动画类；Login 卡片宽度改 min(420px,90vw) 响应式；新增 constants/tableConfig.ts 集中分页常量。