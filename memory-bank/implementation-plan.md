# Implementation Plan

## Goal
- Rework PMS around operations users as the primary audience and deliver a role-based operations command workflow.
- Keep sales fields only as background project attributes; new task aggregation, dashboard entry points, and workbench prioritization must be organized around operations ownership and execution.

## Current Slice
- Add an `Operations` backend aggregation module that exposes a unified task queue over projects, repairs, inspections, handovers, major demands, annual reports, monthly reports, and data hygiene issues.
- Status: implemented in backend; controller now serves `/api/operations/tasks` and `/api/operations/tasks/summary` with hospital-scope filtering at the API layer.

## Non-goals
- Do not redesign unrelated CRUD modules or replace existing project/import/report data models in one pass.
- Do not remove legacy dashboard/contracts/workbench APIs that existing pages already consume.
- Do not convert this step into a full database schema migration.

## Constraints
- Reuse existing data sources first: projects, repair records, inspections, handovers, major demands, annual reports, monthly reports, and maintenance audit/import endpoints.
- Preserve RBAC and hospital-scope filtering; all new operations aggregation must respect the current access-control scope.
- Keep implementation split into bounded slices so parallel workers can edit disjoint files and merge safely.

## Acceptance criteria
- Dashboard/workbench entry pages show operations-facing summaries and task queues instead of sales-led grouping.
- A new operations task aggregation API provides `summary` and paged `tasks` data with operations ownership, severity, overdue state, and deep links.
- Frontend can render an operations task panel and integrate it into the dashboard/workbench flow for the relevant roles.
- Known UX breakages in the current workbench shell are fixed, including card headers and incorrect inspection routes.
- Backend `Operations` module keeps existing contracts intact and reuses hospital-scope filtering at the controller layer.

## Parallel workstreams
1. Supervisor track: coordinate merge order, verify interface contracts, integrate role-based dashboard/workbench entry pages, and run final build/browser checks.
2. Worker A: fix `AppTableCard` header slot passthrough and personal workbench inspection route links.
3. Worker B: add backend `Operations` aggregation contracts, service, controller, and DI registration for `/api/operations/tasks` and `/api/operations/tasks/summary`.
4. Worker C: add frontend operations types/API client plus a reusable operations task panel for dashboard/workbench integration.

## Status
- 2026-06-04: Completed this slice. Backend aggregation, frontend consumer layer, dashboard/workbench integration, and the two known shell fixes are all in place and validated with `dotnet build PMS.sln`, `cd pms-web && npm run build`, and authenticated API spot checks.
- 2026-06-04: Extended the same slice with reusable task-panel filtering. The operations panel now supports level/source/keyword filtering, summary-card quick filtering, filter reset, and refresh across manager, supervisor, regional manager, and personal workbenches without changing backend contracts.
- 2026-06-04: Continued the same slice with owner and hospital filtering. The shared operations panel now supports narrowing tasks by responsible person and hospital on all dashboard/workbench entry pages, which better matches supervisor and regional-manager triage workflows.
- 2026-06-04: Continue the same slice by turning `/maintenance/data` into the landing zone for data-quality tasks. Keep the existing maintenance APIs unchanged, but let the page consume `hospitalName`/`productName` route query, focus the audit table, prefill the reassignment form, and support row-to-form actions so operations staff can move directly from task triage into ownership correction.
- 2026-06-04: Continued the same operations-first rollout by upgrading `pms-web/src/views/repair/RepairRecordView.vue` into a repair service desk. The page now loads a filtered overview dataset alongside the paged table, renders a hero area with service signals, quick actions, hospital/owner/issue insight cards, and keeps route-filter, refresh, and mutation flows in sync with both the overview cards and the detail table.
- 2026-06-04: Continued the same operations-first rollout by upgrading `pms-web/src/views/annual-report/AnnualReportView.vue` into an annual-report service desk. Keep backend contracts unchanged, derive the first-screen signals and insight cards from the current filtered dataset already loaded on the frontend, and preserve the existing create/edit/workflow/print/deep-link behaviors while reorganizing the page into a role-friendly operations workbench.
- 2026-06-05: Continue the same operations-first rollout by upgrading `pms-web/src/views/monthly-report/MonthlyReportView.vue` into a monthly-report service desk. Keep backend contracts unchanged, add a frontend-derived overview dataset for first-screen signals and insight cards, and preserve the existing create/edit/submit/approve/reject/delete/deep-link flows while reorganizing the page into a calmer operations workbench.
- 2026-06-05: Continue the same operations-first rollout by upgrading `pms-web/src/views/hospital/HospitalListView.vue` into a hospital service desk. Keep backend contracts unchanged, derive first-screen signals, summary metrics, a data-quality queue, and province/tier/rating insight cards from the current filtered hospital dataset, and preserve the existing create/edit/rating/detail/delete flows while reorganizing the page into a hospital master-data workbench.
- 2026-06-05: Continue the same operations-first rollout by upgrading `pms-web/src/views/product/ProductListView.vue` into a product service desk. Keep backend contracts unchanged, derive first-screen signals, deployment ranking, category/status insights, and quick focus actions from the current filtered product dataset, and preserve the existing create/edit/detail/delete/batch-delete/deep-link flows while reorganizing the page into a cleaner product master-data workbench.
- 2026-06-05: Continue the same operations-first rollout by upgrading `pms-web/src/views/personnel/PersonnelListView.vue` into a personnel service desk. Keep backend contracts unchanged, derive first-screen signals, group/supervisor/on-site insights, and quick focus actions from the current filtered personnel dataset, and preserve the existing create/edit/detail/permission/deep-link/external-sync flows while reorganizing the page into a cleaner permission-and-staff master-data workbench.

## Goal
- Change the Dashboard workload bar chart from operations owner/personnel grouping to sales-only risk data.
- Keep the chart useful for sales follow-up by grouping contract alerts by sales name and retaining severe/warning/reminder segments.

## Non-goals
- Do not change Dashboard trend/source/summary data scope.
- Do not change Alert Center table contracts or project data.
- Do not introduce new chart libraries or redesign the full Dashboard page.

## Constraints
- Keep the existing `/api/dashboard/v2` response shape compatible for the frontend.
- Only exclude non-sales sources from the TOP workload chart grouping; contract alert data remains the source for sales risk.
- Preserve existing access-control hospital scope filtering.

## Acceptance criteria
- Dashboard chart title/subtitle clearly describe sales risk, not responsibility/personnel workload.
- TOP chart categories are sales names from contract alerts, with missing sales grouped as 鏈垎閰嶉攢鍞?
- `pms-web` build passes and `/dashboard` browser preview shows the sales-only chart.

## Steps
1. Update Dashboard v2 workload aggregation to group contract-alert events by sales only.
2. Rename the frontend chart copy and local variable names toward sales risk while preserving API compatibility.
3. Build and verify the Dashboard page in the in-app browser.
4. Record the completed work in progress.

## Goal
- Redesign the project ledger page around real PMS project data and business operations, not just generic styling.
- Turn `/project/list` into a project business desk that surfaces filtered portfolio context, renewal/overdue risk, commercial value, service ownership, and row-level detail actions.

## Non-goals
- Do not change backend project APIs, permissions, data scope rules, or database writes.
- Do not change other modules in this step.
- Do not introduce new frontend runtime dependencies.

## Constraints
- Use existing `GET /api/projects`, export, single update, and batch update contracts.
- Keep existing filters, pagination, export, batch edit, and edit behavior available.
- Scope implementation to `pms-web/src/views/project/ProjectListView.vue` plus small final CSS refinements if needed.

## Acceptance criteria
- Project list page shows a business overview based on the current filtered dataset: hospital/product coverage, overdue/due-soon status, signed/onsite counts, sales/maintenance/annual value, and key owner/product distributions.
- Current filtered row can open a detail drawer showing opportunity number, hospital/product/location, service owner, commercial figures, contract/service dates, onsite information, and remarks.
- Table columns prioritize business scan fields and include a visible detail action.
- Frontend build passes and `/project/list` is checked in the in-app browser with the current hospital/product filter.

## Steps
1. Load a lightweight overview dataset for the current project filters through the existing list API.
2. Add computed business metrics, top risk/project cards, distribution chips, and selected project detail state.
3. Update the project page template/table/actions/drawer to expose the business design.
4. Build and verify in browser, then record progress.

## Goal
- Refine the PMS frontend into a quieter, premium operational UI across the app shell, login entry, global buttons, forms, tables, cards, dialogs, and drawers.

## Non-goals
- Do not change backend APIs, permission logic, routing contracts, or business data behavior.
- Do not rewrite individual business page data flows or introduce new runtime dependencies.
- Do not add marketing-style art, decorative imagery, or large page-specific redesigns.

## Constraints
- Keep the change scoped to `pms-web/src` UI styles and existing Vue components.
- Preserve Element Plus compatibility and existing page actions.
- Prefer restrained surfaces, crisp typography, 8px-oriented radii, low shadows, and clear scan density.

## Acceptance criteria
- App shell, login page, cards, tables, filters, buttons, pagination, dialogs, drawers, and popovers read as one consistent minimalist business UI.
- Existing global gradients, large-radius cards, hover lift, and heavy shadows are reduced by the final CSS layer.
- Frontend build passes and at least one logged-in page plus login page are visually checked.

## Steps
1. Extend the final `clean-ui.css` layer with a design-token refresh and component overrides.
2. Tune `AppLayout.vue` scoped styles for a compact white header, sidebar, content topbar, and notification panel.
3. Tune `LoginView.vue` scoped styles so the first screen feels aligned with the business app.
4. Build and verify with local browser preview, then record progress.

## Goal
- 灏嗗綋鍓?PMS 宸ヤ綔鍖虹殑鏈夋晥椤圭洰鏀瑰姩鎻愪氦鍒?Git 浠撳簱锛屽苟涓?`origin/main` 瀵归綈鍚庢帹閫併€?## Non-goals
- 涓嶅洖婊氱幇鏈夊伐浣滃尯鏀瑰姩锛屼笉娓呯悊鐢ㄦ埛鏈湴澶囦唤銆佹埅鍥炬垨杩愯鏃ュ織銆?- 涓嶉澶栭噸鏋勪笟鍔′唬鐮佹垨鎵╁ぇ鏈鎻愪氦鑼冨洿銆?## Constraints
- 浠呮彁浜ゅ簲杩涗粨搴撶殑婧愮爜銆佹暟鎹簱鍜?`memory-bank` 璁板綍锛屼笉绾冲叆 `.tmp/`銆乣backups/`銆乣*.db-wal`銆乣*.db-shm` 绛夋湰鍦颁骇鐗┿€?- 浠ュ綋鍓?`main` 鍒嗘敮涓哄熀纭€锛屽厛纭涓?`origin/main` 鐨勫樊寮傦紝鍐嶆墽琛屾彁浜や笌鎺ㄩ€併€?- 淇濈暀浠撳簱閲屾棦鏈?`.slnx` 鏋勫缓闂璁板綍锛屽瀹炶鏄庨獙璇佺粨鏋溿€?## Acceptance criteria
- 褰撳墠鏈夋晥鏀瑰姩瀹屾垚涓€娆℃槑纭?commit锛屽苟鎴愬姛鎺ㄩ€佸埌 `origin/main`銆?- 鎺ㄩ€佸墠鍚庢湰鍦板垎鏀笌杩滅鍒嗘敮淇濇寔瀵归綈銆?- 鏈鎻愪氦涓庢帹閫佺粨鏋滆褰曞埌 `progress.md`銆?## Steps
1. 妫€鏌ュ垎鏀€佽繙绔拰宸ヤ綔鍖虹姸鎬侊紝鍖哄垎婧愮爜鏀瑰姩涓庢湰鍦颁骇鐗┿€?2. 鏆傚瓨鏈夋晥鏂囦欢骞跺垱寤烘彁浜ゃ€?3. 鎺ㄩ€佸埌杩滅骞剁‘璁?`main` 涓?`origin/main` 瀵归綈銆?
## Goal
- 淇鏈湴 PMS 棰勮绔欑偣鏃犳硶浣跨敤绠＄悊鍛樿处鍙风櫥褰曠殑闂銆?## Non-goals
- 涓嶉噸鍋氱櫥褰曢〉 UI锛屼笉鏀瑰姩鏉冮檺妯″瀷鎴栦笟鍔℃ā鍧?API銆?- 涓嶆墽琛屾暟鎹鍏ャ€侀」鐩悓姝ャ€佸浠芥仮澶嶇瓑鏃犲叧鎿嶄綔銆?## Constraints
- 淇濇寔鐜版湁 `/api/auth/login` 濂戠害鍏煎锛屽墠绔粛鎻愪氦 `account/password`銆?- 浼樺厛澶嶇幇鐧诲綍鎺ュ彛杩斿洖锛屽啀鎸夎璇佹湇鍔℃渶灏忚寖鍥翠慨澶嶃€?- 淇鍚庡繀椤婚獙璇?API 鐧诲綍鍜屽墠绔〉闈㈢櫥褰曢摼璺€?## Acceptance criteria
- `admin/123456` 鑳芥垚鍔熻皟鐢ㄧ櫥褰曟帴鍙ｅ苟杩斿洖 token銆?- 鍓嶇 `http://localhost:5173/` 鍙畬鎴愮櫥褰曞苟杩涘叆涓氬姟椤甸潰銆?- 鐧诲綍淇鍜岄獙璇佺粨鏋滆褰曞埌 `progress.md`銆?## Steps
1. 澶嶇幇褰撳墠鐧诲綍澶辫触锛岀‘璁ゅけ璐ョ偣鍦ㄥ墠绔€佷唬鐞嗚繕鏄悗绔璇併€?2. 妫€鏌?`AuthController`銆佸墠绔櫥褰?API 涓?`InMemoryAuthService` 鐨勮处鍙风瀛愰€昏緫銆?3. 鍋氭渶灏忎慨澶嶆垨杩愯绾т慨姝ｏ紝骞堕噸鍚?楠岃瘉鍓嶅悗绔櫥褰曢摼璺€?
## Goal
- 鍚姩 PMS 鏈湴鍓嶅悗绔紑鍙戞湇鍔★紝鎻愪緵鍙搷浣滅殑缃戠珯棰勮鍏ュ彛銆?## Non-goals
- 涓嶄慨鏀逛笟鍔′唬鐮併€丄PI 濂戠害銆侀厤缃粯璁ゅ€兼垨鏁版嵁搴撳唴瀹广€?- 涓嶆墽琛屾暟鎹鍏ャ€佸悓姝ャ€佸浠芥仮澶嶇瓑浼氭敼鍙樹笟鍔℃暟鎹殑鎿嶄綔銆?## Constraints
- 鍚庣浣跨敤鐜版湁 Development/http 閰嶇疆鐩戝惉 `http://localhost:5111`銆?- 鍓嶇浣跨敤鐜版湁 Vite 閰嶇疆鐩戝惉 `http://localhost:5173`锛屽苟閫氳繃 `/api` 浠ｇ悊鍒?`http://127.0.0.1:5111`銆?- 鑻ョ鍙ｅ凡鍗犵敤锛屽厛纭鍗犵敤鏉ユ簮锛屼笉闈欓粯鏀归粯璁ょ鍙ｃ€?## Acceptance criteria
- `5111` 鍚庣绔彛鍜?`5173` 鍓嶇绔彛鍧囧浜庣洃鍚姸鎬併€?- 鍓嶇棣栭〉鍙€氳繃娴忚鍣ㄨ闂紝鍚庣 API 鍙€氳繃鍋ュ悍鎴栬璇佸彈淇濇姢鎺ュ彛纭鏈嶅姟瀛樻椿銆?- 鏈鍚姩鍜岄獙璇佺粨鏋滆褰曞埌 `progress.md`銆?## Steps
1. 妫€鏌ラ粯璁ょ鍙ｅ崰鐢ㄥ拰鏈湴鍚姩鑴氭湰銆?2. 浠ュ悗鍙拌繘绋嬪惎鍔?PMS.API 涓?pms-web銆?3. 楠岃瘉鍓嶇椤甸潰鍜屽悗绔帴鍙ｅ彲杈撅紝骞舵墦寮€娴忚鍣ㄩ瑙堝叆鍙ｃ€?
## Goal
- 浠庨噾灞卞叡浜枃妗ｅ悓姝ラ」鐩彴璐︼細PMS 涓凡瀛樺湪鐨勯」鐩粎鏇存柊浜у搧銆佺淮鎶ゆ棩鏈熴€侀攢鍞瓑瀛楁锛汸MS 涓笉瀛樺湪浣嗗叡浜枃妗ｇ粍鍒负鈥滆垝楂樻垚鈥濈殑椤圭洰鏂板鍒?PMS锛涘悓姝ュ悗鍒锋柊鍏宠仈妯″潡鏁版嵁銆?
## Non-goals
- 涓嶆敼鍔ㄩ」鐩尮閰嶄富瑙勫垯锛屼笉閲嶅仛鍚庣椤圭洰瀵煎叆绠楁硶銆?- 涓嶆墿鏁ｅ埌閲嶅ぇ闇€姹傘€佸伐鏃舵姤琛ㄧ瓑鏃犲叧瀵煎叆娴佺▼銆?- 涓嶆妸鏈鍚屾鏀规垚鍏ㄩ噺瑕嗙洊瀵煎叆銆?
## Constraints
- 浼樺厛澶嶇敤鐜版湁鍚庣澧為噺鍚屾鎺ュ彛 `/api/admin/import/upload/sync-project-ledger`銆?- 鍓嶇鏀瑰姩浠呴檺 `pms-web/src/api/modules/maintenance.ts` 涓?`pms-web/src/views/maintenance/DataMaintenanceView.vue`锛屼互鍙?memory-bank 璁板綍銆?- 鍚屾瀹屾垚鍚庡繀椤昏Е鍙戝叏灞€鏁版嵁鍒锋柊淇″彿锛屽苟瀹屾垚鍓嶇鏋勫缓楠岃瘉涓庝竴娆″疄闄呭悓姝ラ獙璇併€?
## Acceptance criteria
- 鏁版嵁缁存姢涓績鍙洿鎺ユ墽琛屸€滃閲忓悓姝ラ」鐩彴璐︹€濓紝骞跺睍绀?matched/updated/added 绛夌粨鏋溿€?- 鍏变韩鏂囨。鍚屾鍚庯細宸叉湁 PMS 椤圭洰瀛楁琚洿鏂帮紝缂哄け浣嗙粍鍒负鈥滆垝楂樻垚鈥濈殑椤圭洰琚柊澧烇紝鍏朵綑鏈尮閰?PMS 椤圭洰淇濈暀涓嶅垹銆?- 瀹為檯瀹屾垚涓€娆″叡浜枃妗ｅ悓姝ワ紝骞舵娊鏍锋牳瀵归」鐩彴璐﹀強鑷冲皯涓€涓叧鑱旀ā鍧楁暟鎹凡鏇存柊銆?
## Steps
1. 鍦ㄧ淮鎶や腑蹇冩帴鍏ョ幇鏈?`sync-project-ledger` API锛岃ˉ榻愪笂浼犳寜閽笌缁撴灉灞曠ず銆?2. 鏋勫缓鍓嶇骞剁‘璁ょ淮鎶や腑蹇冨叆鍙ｅ彲鐢ㄣ€?3. 浠庨噾灞卞叡浜枃妗ｅ鍑虹洰鏍囧伐浣滆〃锛屾墽琛屼竴娆″閲忓悓姝ャ€?4. 鎶芥牱鏍稿椤圭洰鍙拌处涓庡叧鑱旀ā鍧楁暟鎹紝骞惰褰曠粨鏋滃埌 `progress.md`銆?
# Implementation Plan

## Goal
- 恢复 `memory-bank/progress.md` 删除前的历史内容，并修复可逆的历史乱码。

## Non-goals
- 不修改业务代码、数据库、API 合同或前端页面行为。
- 不尝试把已经不可逆损坏的每一条历史记录逐字还原。

## Constraints
- 先从 Git 历史恢复原有进度日志，不用摘要替代历史记录。
- 对典型 UTF-8/GBK 误解码内容做机械逆转，已不可逆损坏的字符保留原样。
- 本次改动限定在 memory-bank 文档记录。

## Acceptance criteria
- `memory-bank/progress.md` 保留删除前的历史记录。
- 可逆的 mojibake 行已恢复为 UTF-8 中文。
- 文件头部包含本次恢复记录、Ponytail/插件配置记录和历史进展。

## Goal
- Install the `DietrichGebert/ponytail` agent rules into this repository and make them apply project-wide for future coding tasks.

## Non-goals
- Do not change PMS backend/frontend runtime behavior, dependencies, or deployment defaults.
- Do not replace the existing PMS project guardrails with Ponytail; integrate Ponytail underneath the current rules.

## Constraints
- Keep the change scoped to agent/instruction files plus required memory-bank records.
- Preserve the existing `AGENTS.md` workflow, architecture, and verification requirements.
- Reuse the upstream Ponytail wording where practical so future updates stay understandable.

## Acceptance criteria
- Root `AGENTS.md` explicitly states that Ponytail's minimal-implementation ladder applies across this repository.
- A local `.agents/skills/ponytail/SKILL.md` exists so the Ponytail skill can be discovered from this project.
- The work is recorded in `memory-bank/progress.md` with a verification note.

## Steps
1. Inspect the upstream Ponytail repository and determine the smallest project-level integration path.
2. Merge Ponytail guidance into the repository's root `AGENTS.md` without weakening the PMS-specific rules.
3. Add a local Ponytail skill file under `.agents/skills/`.
4. Verify the files exist and record the result in `progress.md`.

## Goal
- 灏嗛」鐩彴璐︽寜鍏变韩鏂囨。鈥滅淮鎶ゆ儏鍐电粺璁¤〃20260527 / 椤圭洰鏄庣粏.xlsx鈥濈殑 `缁存姢椤圭洰鏄庣粏` 宸ヤ綔琛ㄥ悓姝ュ埌 PMS锛氬彧鏇存柊 PMS 椤圭洰鍙拌处涓凡瀛樺湪鐨勯」鐩紝鎴栨柊澧炲叡浜枃妗ｄ腑 `鏈嶅姟缁勫埆=鑸掗珮鎴恅 鐨勯」鐩€?- 鍚屾鏃朵繚鐣欑幇鏈夐」鐩?`Id`锛岄伩鍏嶅勾搴︽姤鍛娿€佸贰妫€銆佷氦鎺ャ€佸憡璀︺€佸伐鏃剁瓑渚濊禆椤圭洰 ID 鐨勬ā鍧楄鍏ㄩ噺閲嶆帓鎵撴柇銆?
## Non-goals
- 涓嶅仛鍏ㄩ噺鏇挎崲銆佷笉鍒犻櫎 PMS 閲屽綋鍓嶅瓨鍦ㄤ絾鍏变韩鏂囨。鏈懡涓殑椤圭洰銆?- 涓嶆敼鍙樺墠绔」鐩彴璐﹀垪琛?API 濂戠害锛屼笉璋冩暣椤甸潰鏍峰紡鍜屼笟鍔℃祦绋嬨€?- 涓嶆妸 KDocs 鐧诲綍鎬併€丆ookie 鎴栧閮ㄥ鍑鸿兘鍔涘浐鍖栧埌浠ｇ爜涓紝鏈浠ュ凡鍚屾鍒版湰鏈虹殑 Excel 鏂囦欢浣滀负鍏变韩鏂囨。鍙婧愩€?
## Constraints
- 鍚庣浠嶉伒瀹堢幇鏈夊垎灞傦紝瀵煎叆瑙ｆ瀽鐣欏湪 `PMS.API`锛岄」鐩暟鎹悎骞剁暀鍦?`PMS.Infrastructure`銆?- 鍚堝苟蹇呴』鎸変笟鍔￠敭鍖归厤骞朵繚鐣?`Id`锛氫紭鍏?`鏈轰細鍙?鍖婚櫌+浜у搧`锛屽叾娆￠潪姝т箟鐨?`鍖婚櫌+浜у搧`銆?- 鍐欏簱鍓嶅繀椤讳繚鐣欐暟鎹簱澶囦唤锛涘啓搴撳悗蹇呴』閲嶅缓鍖婚櫌妯″潡娲剧敓鏁版嵁銆?
## Acceptance criteria
- 鏂板涓€涓閲忓悓姝ュ叆鍙ｏ紝鑳藉涓婁紶椤圭洰鏄庣粏 Excel 骞舵寜涓婅堪瑙勫垯鍚堝苟銆?- 鍚屾鍚庨」鐩彴璐︺€佸尰闄㈡淳鐢熸暟鎹€佸悎鍚岄璀︺€丏ashboard銆佸勾搴︽姤鍛娿€佸贰妫€銆佷氦鎺ョ瓑鍩轰簬椤圭洰搴撶殑妯″潡璇诲彇鍒版渶鏂伴」鐩瓧娈点€?- `dotnet build PMS.sln` 閫氳繃锛屽苟瀹屾垚涓€娆＄湡瀹炴簮鏂囦欢鍚屾楠岃瘉銆?
## Steps
1. 澧炲姞椤圭洰鍙拌处澧為噺鍚堝苟鏂规硶锛屼繚鐣欑幇鏈?`Id` 骞惰繑鍥炲尮閰嶃€佹洿鏂般€佹柊澧炪€佽烦杩囩粺璁°€?2. 澧炲姞 `upload/sync-project-ledger` 鍚庣鍏ュ彛锛屽鐢ㄧ幇鏈?Excel 瑙ｆ瀽骞惰Е鍙戝尰闄㈡淳鐢熸暟鎹噸寤恒€?3. 澶囦唤鏁版嵁搴撳悗锛岀敤鏈満 WPS/KDocs 鍚屾鏂囦欢鎵ц鐪熷疄鍚屾銆?4. 鏋勫缓骞堕€氳繃 API/椤甸潰鎶芥牱楠岃瘉鍏宠仈妯″潡鏁版嵁銆?
## Status
- 2026-05-27: 宸插畬鎴愩€傛柊澧炰笂浼犲紡椤圭洰鍙拌处澧為噺鍚屾鍏ュ彛 `POST /api/admin/import/upload/sync-project-ledger`锛屾寜 `鏈轰細鍙?鍖婚櫌+浜у搧` / 闈炴涔?`鍖婚櫌+浜у搧` 鍖归厤骞朵繚鐣欓」鐩?`Id`锛涘凡鐢ㄦ湰鏈?WPS/KDocs 鍚屾鏂囦欢 `椤圭洰鏄庣粏.xlsx` 鎵ц鐪熷疄鍚屾锛氭簮琛?222 琛岋紝鍖归厤 215 涓」鐩紝鏇存柊 122 涓紝鏂板 7 涓紝淇濈暀鏈懡涓幇鏈夐」鐩?5 涓紝鏈€缁堥」鐩暟 227锛涘悓姝ュ悗閲嶅缓鍖婚櫌娲剧敓鏁版嵁銆?
## Goal
- 缁х画瀹炴柦鐧诲綍鍚庝富骞蹭笟鍔￠〉闂ㄦ埛鍖栨敹鍙ｏ細鍦ㄤ笂涓€杞?KPI銆佺粺涓€棰勮銆佸伐鏃舵姤琛ㄣ€侀」鐩彴璐﹀熀纭€涓婏紝缁х画鎺ㄨ繘鍚堝悓棰勮涓庡贰妫€绠＄悊鐨勯灞忓崌绾э紝褰㈡垚杩炵画鐨勪竴灞忓喅绛栧叆鍙ｃ€?

## Non-goals
- 涓嶆敼鍔ㄥ悗绔帴鍙ｃ€佺粺璁″彛寰勪笌鏃㈡湁璁″垝/缁撴灉涓氬姟鍔ㄤ綔銆?
- 涓嶉噸鍐欏贰妫€绠＄悊缁撴灉椤佃鎯呯粨鏋勶紝浠呰皟鏁撮灞忓叆鍙ｅ眰绾т笌蹇嵎鍔ㄤ綔缁勭粐銆?
- 涓嶆墿鏁ｅ埌寰呭姙鍒楄〃涓夋爮鍦板浘椤电殑缁撴瀯閲嶆瀯銆?

## Constraints
- 鍙敼 `pms-web/src/views/contract/ContractAlertView.vue` 涓?`pms-web/src/views/inspection/InspectionPlanView.vue`銆?
- 蹇呴』淇濈暀鐜版湁 SummaryMetrics銆佺瓫閫夈€佸鍑恒€佽鍒掑姩浣滄祦銆佺粨鏋滄煡璇笌璇︽儏鑳藉姏銆?
- 蹇呴』瀹屾垚鐧诲綍鎬佹埅鍥鹃獙璇佷笌 `PMS: Build Frontend` 鏋勫缓楠岃瘉銆?

## Steps
1. 灏?`ContractAlertView.vue` 鐢辨棫 `page-head` 鍗囩骇涓哄悎鍚岄闄╅棬鎴?hero锛屽苟琛ラ綈椋庨櫓鎽樿涓庡揩鎹风瓫閫夊姩浣溿€?
2. 灏?`InspectionPlanView.vue` 鍗囩骇涓哄贰妫€闂ㄦ埛 hero锛屾帴鍏ヨ鍒?缁撴灉鍙岃鍥惧紩瀵间笌楂橀鍔ㄤ綔鍏ュ彛銆?
3. 鎴浘楠岃瘉 `contract/alerts` 涓?`inspection/plan`锛屽啀鎵ц鍓嶇鏋勫缓鍥炲綊銆?

## Status
- 2026-04-24: 宸插畬鎴愩€俙pms-web/src/views/contract/ContractAlertView.vue` 宸插崌绾т负鍚堝悓椋庨櫓闂ㄦ埛棣栧睆锛岃ˉ榻愰闄╂€婚噺銆侀珮椋庨櫓椤圭洰銆佷弗閲嶉闄╀笌瑕嗙洊鑼冨洿淇″彿鎽樿鍙婂揩鎹风瓫閫夊姩浣滐紱`pms-web/src/views/inspection/InspectionPlanView.vue` 宸插崌绾т负宸℃闂ㄦ埛棣栧睆锛岃ˉ榻愯鍒?缁撴灉鍙岃鍥惧紩瀵笺€佸贰妫€淇″彿鎽樿涓庡揩鎹峰姩浣滃叆鍙ｏ紝骞朵繚鐣欐棦鏈夎鍒?缁撴灉鍔ㄤ綔閾捐矾銆傞獙璇侊細鐧诲綍鎬佹埅鍥惧鏌?`http://localhost:5173/contract/alerts` 涓?`http://localhost:5173/inspection/plan` 鍧囨甯告覆鏌擄紱`PMS: Build Frontend` 閫氳繃锛宍vue-tsc -b && vite build` 鎴愬姛瀹屾垚銆?

## Goal
- 缁х画瀹炴柦鐧诲綍鍚庨珮棰戝叆鍙ｉ〉闂ㄦ埛鍖栨敹鍙ｏ細鎶?KPI銆佺粺涓€棰勮涓績銆佸伐鏃舵姤琛ㄣ€侀」鐩彴璐︽帴鍒伴椤靛拰搴旂敤澹崇殑鍚屼竴濂楄摑缁夸紒涓氶棬鎴疯瑷€涓婏紝璁╃櫥褰曞悗涓诲共椤甸潰褰㈡垚杩炵画鐨勯灞忎綋楠屻€?

## Non-goals
- 涓嶆敼鍔ㄥ悗绔帴鍙ｃ€佹暟鎹粺璁″彛寰勬垨鐜版湁 CRUD / 瀵煎叆瀵煎嚭 / 缂栬緫鍔ㄤ綔閾捐矾銆?
- 涓嶅湪鏈疆鎵╂暎鍒板叏閮ㄤ笟鍔￠〉锛屽彧鍏堣鐩栭珮棰戝叆鍙ｉ〉涓庢姤琛ㄩ〉銆?
- 涓嶅紩鍏ユ柊鐨勮繍琛屾椂渚濊禆锛屼篃涓嶉噸鍐?ProTable / AppTableCard 缁勪欢鏈韩銆?

## Constraints
- 浼樺厛鍙敼 `pms-web/src/views/dashboard/KpiDashboardView.vue`銆乣pms-web/src/views/alert/AlertCenterView.vue`銆乣pms-web/src/views/report/WorkHoursReportView.vue`銆乣pms-web/src/views/project/ProjectListView.vue`銆?
- 姣忛〉蹇呴』淇濇寔鍘熸湁鍥捐〃銆佺瓫閫夈€佸鍑恒€佺紪杈戙€佹壒閲忕淮鎶ょ瓑涓氬姟鍔ㄤ綔鍙敤锛屽彧璋冩暣棣栧睆缁勭粐鍜岃瑙夊眰绾с€?
- 蹇呴』瀹屾垚鐧诲綍鎬佹埅鍥鹃獙璇佷笌 `PMS: Build Frontend` 鏋勫缓楠岃瘉銆?

## Steps
1. 灏?`KpiDashboardView.vue` 浠庢棫 `page-head` 鏀跺彛涓虹哗鏁堥棬鎴?hero锛屼繚鐣欏師鍥捐〃涓庢寚鏍囧崱閫昏緫銆?
2. 灏?`AlertCenterView.vue` 鏀逛负椋庨櫓闂ㄦ埛棣栧睆锛岀獊鍑洪闄╂€婚噺銆佹潵婧愬垎甯冧笌楂橀绛涢€夊姩浣溿€?
3. 灏?`WorkHoursReportView.vue` 涓?`ProjectListView.vue` 浠庘€滆〃鏍?宸ュ叿鏍忕洿鍑衡€濇彁鍗囦负甯︽湀搴︽憳瑕佹垨椤圭洰鎽樿鐨勫叆鍙ｅ伐浣滃彴銆?
4. 閫愰〉鍋氱櫥褰曟€佹埅鍥惧洖褰掞紝鍐嶆墽琛?`PMS: Build Frontend` 瀹屾垚缁熶竴楠岃瘉銆?

## Status
- 2026-04-23: 宸插畬鎴愩€俙pms-web/src/views/dashboard/KpiDashboardView.vue` 宸插崌绾т负缁╂晥闂ㄦ埛 hero锛岃ˉ榻?KPI 淇″彿鎽樿涓庡揩鎹峰叆鍙ｏ紱`pms-web/src/views/alert/AlertCenterView.vue` 宸插崌绾т负椋庨櫓闂ㄦ埛棣栧睆锛岃ˉ榻愰闄╂€婚噺銆佷弗閲嶉闄┿€佹潵婧愬垎甯冧笌蹇嵎绛涢€夛紱`pms-web/src/views/report/WorkHoursReportView.vue` 宸茶ˉ榻愭湀搴﹀伐鏃舵姤琛?hero銆佹湀搴︽帶鍒跺崱涓庨珮棰戝姩浣滃尯锛沗pms-web/src/views/project/ProjectListView.vue` 宸茶ˉ榻愰」鐩彴璐?hero銆侀」鐩闄╂憳瑕佸拰蹇嵎绛涢€夊叆鍙ｏ紝鍚屾椂淇濈暀鍘熸湁鍥捐〃銆佺瓫閫夈€佸鍑恒€佺紪杈戝拰鎵归噺缁存姢閾捐矾銆傞獙璇侊細鐧诲綍鎬佹埅鍥惧鏌?`http://localhost:5173/dashboard/kpi`銆乣http://localhost:5173/alert/center`銆乣http://localhost:5173/report/workhours`銆乣http://localhost:5173/project/list` 鍧囨甯告覆鏌擄紱`PMS: Build Frontend` 閫氳繃锛宍vue-tsc -b && vite build` 鎴愬姛瀹屾垚銆?

## Goal
- 缁х画瀹炴柦鐧诲綍鍚庨椤垫敹鍙ｏ細浠庡簲鐢ㄥ３涓?Dashboard 棣栭〉寮€濮嬶紝娌跨敤鐧诲綍椤电殑钃濈豢浼佷笟闂ㄦ埛璇█锛屾彁鍗囩櫥褰曞悗绗竴灞忕殑鑱氱劍鎰熴€佸揩鎹峰叆鍙ｅ拰宸ヤ綔鍙板眰绾с€?

## Non-goals
- 涓嶆敼鍔ㄥ悗绔帴鍙ｃ€佹潈闄愭ā鍨嬫垨鏁版嵁鑱氬悎鍙ｅ緞銆?
- 涓嶅湪鏈疆鍚屾椂閲嶅仛 KPI銆丠ospital360 鎴栧叾瀹冧笟鍔¤鎯呴〉銆?
- 涓嶅紩鍏ユ柊鐨勮繍琛屾椂渚濊禆鎴栧鏉傛嫋鎷?涓€у寲甯冨眬鑳藉姏銆?

## Constraints
- 鍙敼 `pms-web/src/layout/AppLayout.vue` 涓?`pms-web/src/views/dashboard/DashboardView.vue`锛屼繚鎸佺幇鏈夊鑸笌鍥捐〃鏁版嵁閫昏緫鍏煎銆?
- 棣栭〉蹇呴』缁х画鏀寔鐜版湁鍥捐〃涓嬮捇涓庡揩鎹疯烦杞紝涓嶈兘鐗虹壊宸叉湁绠＄悊鍔ㄤ綔鍏ュ彛銆?
- 蹇呴』瀹屾垚鎴浘楠岃瘉涓庡墠绔瀯寤洪獙璇併€?

## Steps
1. 鏀剁揣 `AppLayout.vue` 鐨勯《鏍忋€佸唴瀹归《鍖轰笌渚ф爮瑙嗚灞傜骇锛岃搴旂敤澹虫洿鎺ヨ繎浼佷笟闂ㄦ埛鍏ュ彛銆?
2. 閲嶅仛 `DashboardView.vue` 鐨勯椤甸灞忥紝鏂板娆㈣繋 hero銆佸揩鎹峰叆鍙ｅ拰鏇存槑纭殑鎸囨爣鍒嗙粍銆?
3. 鎴浘楠岃瘉鐧诲綍鍚庨椤垫晥鏋滐紝鍐嶆墽琛?`PMS: Build Frontend` 纭缂栬瘧閫氳繃銆?

## Status
- 2026-04-23: 宸插畬鎴愩€俙pms-web/src/layout/AppLayout.vue` 宸蹭负鐧诲綍鍚庡簲鐢ㄥ３琛ラ綈浼佷笟闂ㄦ埛寮忎晶鏍忔瑙堛€佸唴瀹归《鍖烘爣棰?瑙掕壊/璇存槑鏂囨锛宍pms-web/src/views/dashboard/DashboardView.vue` 宸插皢棣栭〉棣栧睆閲嶅仛涓烘杩?hero銆佺姸鎬佷俊鍙蜂笌蹇嵎鍏ュ彛宸ヤ綔鍙帮紝鍚屾椂淇濈暀鐜版湁鍥捐〃銆佸崱鐗囦笌涓嬮捇閫昏緫銆傞獙璇侊細鐧诲綍鎬佹埅鍥惧鏌?`http://localhost:5173/dashboard` 涓?`http://localhost:5173/dashboard/kpi` 鍧囨甯告覆鏌擄紱`PMS: Build Frontend` 閫氳繃锛宍vue-tsc -b && vite build` 鎴愬姛瀹屾垚銆?

## Goal
- 鍙傜収 `https://pms.bjgoodwill.com/welcome` 鐨?welcome/login 缁撴瀯锛屽鏈」鐩櫥褰曞叆鍙ｅ仛鍚岄鏍奸噸鏋勶紝鍦ㄤ笉鏀瑰悗绔璇佹帴鍙ｇ殑鍓嶆彁涓嬭ˉ榻愭満鏋勪唬鐮併€侀獙璇佺爜銆佹壂鐮佸垏鎹㈠拰鍙屾爮鍝佺墝灞曠ず銆?

## Non-goals
- 涓嶆帴鍏ョ湡瀹炴壂鐮佺櫥褰曞悗绔紝涓嶆敼鍔ㄧ幇鏈?`/api/auth/login` 濂戠害銆?
- 涓嶅皾璇曠櫥褰曠洰鏍囩珯鐐规姄鍙栧彈闄愰〉闈紝涔熶笉鎵╂暎鍒扮櫥褰曞悗鐨勪笟鍔￠〉鏀归€犮€?

## Constraints
- 淇濇寔鐜版湁璐﹀彿瀵嗙爜鐧诲綍鍙敤锛屾渶缁堜粛鍙悜鍚庣鎻愪氦 `account/password`銆?
- 鏈烘瀯浠ｇ爜涓庨獙璇佺爜浜や簰鍦ㄥ墠绔畬鏁撮棴鐜紝涓嶅紩鍏ユ柊鐨勮繍琛屾椂渚濊禆銆?
- 蹇呴』瀹屾垚鍓嶇鏋勫缓楠岃瘉銆?

## Steps
1. 閲嶆瀯 `pms-web/src/views/login/LoginView.vue` 鐨勫弻鏍忚瑙変笌琛ㄥ崟缁撴瀯锛屽榻愮洰鏍囬〉鐨勫搧鐗屽尯銆佹満鏋勪唬鐮併€侀獙璇佺爜涓庢壂鐮佸垏鎹綋楠屻€?
2. 鍦ㄥ墠绔疄鐜版湰鍦伴獙璇佺爜鐢熸垚銆佸埛鏂板拰鏍￠獙锛屽苟淇濈暀鐜版湁鐧诲綍 API 璋冪敤閾俱€?
3. 鎵ц `PMS: Build Frontend` 楠岃瘉缂栬瘧閫氳繃銆?

## Status
- 2026-04-23: 宸插畬鎴愩€傚凡纭褰撳墠宸ュ叿鏃犳硶鐩存帴鎵ц鐩爣绔欑偣鐧诲綍鍔ㄤ綔锛屽洜姝ゆ湰杞熀浜庡叕寮€ welcome 椤靛畬鎴?`pms-web/src/views/login/LoginView.vue` 閲嶆瀯锛氱櫥褰曢〉宸插垏鎹负鍙屾爮浼佷笟闂ㄦ埛缁撴瀯锛岃ˉ榻愭満鏋勪唬鐮併€佹湰鍦伴獙璇佺爜銆佹壂鐮佸垏鎹笌浜岀淮鐮佸€掕鏃跺埛鏂颁氦浜掞紝鍚屾椂淇濇寔鍚庣璁よ瘉浠嶅彧鎻愪氦 `account/password`銆傞獙璇侊細`PMS: Build Frontend` 閫氳繃锛宍vue-tsc -b && vite build` 鎴愬姛瀹屾垚銆?

## Goal
- 瀹炴柦 Phase 2 鐨勭鍏釜鍙氦浠樺垏鐗囷細鎶?AnnualReport 浠庘€滃墠绔笌 upsert 鐩存帴鏀圭姸鎬?鎻愪氦鏃ユ湡/璇勫浜衡€濇敹鍙ｄ负 start / submit / complete / reopen 鏄惧紡鍔ㄤ綔娴侊紝骞惰ˉ榻?annual-report.manage 鏉冮檺鏄犲皠涓庡崟鏉″尰闄㈣寖鍥存牎楠屻€?

## Non-goals
- 涓嶉噸鍋氬勾搴︽姤鍛婂鍑烘牸寮忋€佽嚜鍔ㄧ敓鎴愰€昏緫鎴栫粺璁℃憳瑕佸彛寰勩€?
- 涓嶆墿鏁ｅ埌 Dashboard銆丆ontractAlert 绛夊叾瀹冩ā鍧椼€?
- 涓嶅紩鍏ユ柊鐨勬秷鎭€氱煡銆佹壒閲忓鎵规垨澶嶆潅瑙勫垯寮曟搸銆?

## Constraints
- 淇濇寔骞村害鎶ュ憡鏌ヨ銆佸鍑烘帴鍙ｅ吋瀹癸紝灏介噺鍙湪鐜版湁 Service / Controller / 椤甸潰鍐呭閲忔敹鍙ｃ€?
- 鍓嶅悗绔繀椤诲悓鏃惰惤鍦帮紝涓嶈兘鍙慨鍚庣鐘舵€佹満鑰屼繚鐣欏墠绔洿鏀瑰叆鍙ｃ€?
- 蹇呴』瀹屾垚鍚庣鏋勫缓銆佸墠绔瀯寤哄拰鑷冲皯涓€鏉＄湡瀹?API AnnualReport workflow 鍥炲綊銆?

## Steps
1. 鍦?`IAnnualReportService` / `InMemoryAnnualReportService` 涓鍔?`GetByIdAsync`銆乣StartAsync`銆乣SubmitAsync`銆乣CompleteAsync`銆乣ReopenAsync`锛屽苟闄愬埗 create/update/delete 浠呭厑璁稿伐浣滄祦杈圭晫鍐呮搷浣溿€?
2. 鍦?`AnnualReportsController` 涓柊澧?workflow PATCH 绔偣銆侀潪娉曠姸鎬?400 杩斿洖銆佸垱寤?鍗曟潯鍔ㄤ綔鍖婚櫌鑼冨洿鏍￠獙锛屽苟灏?`/api/annual-reports` 鏉冮檺鏄犲皠淇涓?GET=`annual-report.view`銆侀潪 GET=`annual-report.manage`銆?
3. 鍦?`pms-web/src/api/modules/annual-report.ts` 涓?`pms-web/src/views/annual-report/AnnualReportView.vue` 鎺ュ叆鏄惧紡 workflow 鎸夐挳锛岀Щ闄?`status/submitDate/reviewer/reviewDate` 鐨勫唴鑱旂紪杈戜笌鍒涘缓鏃剁洿鎺ラ€夌姸鎬併€?
4. 鏂板 AnnualReport workflow 鍥炲綊鑴氭湰锛岃鐩?create -> start -> submit -> complete -> reopen -> delete 閾捐矾銆?
5. 鎵ц `dotnet build PMS.sln`銆乣PMS: Build Frontend` 涓?annual-report workflow 鑴氭湰鍥炲綊銆?

## Status
- 2026-04-23: 宸插畬鎴愩€俙PMS.Application/Contracts/AnnualReport/IAnnualReportService.cs`銆乣PMS.Infrastructure/Services/InMemoryAnnualReportService.cs` 宸茶ˉ榻?`GetByIdAsync`銆乣StartAsync`銆乣SubmitAsync`銆乣CompleteAsync`銆乣ReopenAsync`锛屽苟灏?create 鍥哄畾涓?`鏈紑濮媊銆乽pdate/delete 闄愬埗鍒?`鏈紑濮?缂栧啓涓璥锛岄樆鏂€氳繃 upsert 鐩存帴鏀?`Status/SubmitDate/Reviewer/ReviewDate`锛沗PMS.API/Controllers/AnnualReport/AnnualReportsController.cs` 宸叉柊澧?workflow PATCH 绔偣銆侀潪娉曠姸鎬?400 杩斿洖浠ュ強鍒涘缓/鍗曟潯鍔ㄤ綔鐨勫尰闄㈣寖鍥存牎楠岋紱`PMS.Infrastructure/Services/InMemoryAccessControlService.cs` 宸插皢 `/api/annual-reports` 鏉冮檺鏄犲皠淇涓?GET=`annual-report.view`銆侀潪 GET=`annual-report.manage`銆傚墠绔?`pms-web/src/api/modules/annual-report.ts`銆乣pms-web/src/views/annual-report/AnnualReportView.vue` 宸插垏鎹㈠埌鈥滃紑濮嬬紪鍐?/ 鎻愪氦璇勫 / 瀹屾垚璇勫 / 閲嶅紑鈥濇樉寮忓姩浣滄祦锛岀Щ闄ょ姸鎬併€佹彁浜ゆ棩鏈熴€佽瘎瀹′汉銆佽瘎瀹℃棩鏈熺殑鍐呰仈缂栬緫浠ュ強鏂板鏃剁洿鎺ラ€夌姸鎬佸叆鍙ｃ€傞獙璇侊細`dotnet build PMS.sln` 閫氳繃锛? error锛屼繚鐣欐棦鏈?CS8625 warnings锛夛紱`PMS: Build Frontend` 閫氳繃锛沗powershell -ExecutionPolicy Bypass -File scripts/verify-annual-report-workflow.ps1` 瀹炴祴 create -> start -> submit -> complete -> reopen -> delete 鍏ㄩ摼璺垚鍔燂紝杈撳嚭 `createdId=221`銆乣reopenStatus=缂栧啓涓璥銆乣cleanup=deleted`銆?

## Goal
- 瀹炴柦 Phase 2 鐨勭浜斾釜鍙氦浠樺垏鐗囷細鎶?Handover 浠庘€滃墠绔洿鎺ユ寜 nextStage 绾挎€ф帹杩涖€佸悗绔€氱敤 targetStage 鏇存柊鈥濇敹鍙ｄ负 send-email / start / complete / rollback 鏄惧紡鍔ㄤ綔娴侊紝骞惰ˉ榻愬叧閿椂闂磋妭鐐逛笌璇︽儏璐ｄ换淇℃伅銆?

## Non-goals
- 涓嶉噸鍋氫氦鎺ユ暟鎹潵婧愭垨鏂板鐙珛 handover 瀹炰綋銆?
- 涓嶆墿鏁ｅ埌 AnnualReport銆丏ashboard 绛夊叾瀹冩ā鍧椼€?
- 涓嶅紩鍏ユ柊鐨勯€氱煡浠诲姟銆佹壒閲忚皟搴︽垨澶嶆潅瀹℃壒瑙勫垯銆?

## Constraints
- 淇濇寔鐜版湁 `/api/handovers/{id}/stage` 鍏煎锛屼笉鐮村潖宸叉湁璋冪敤鏂广€?
- 浠呭湪鐜版湁鍒嗗眰鍐呭閲忔墿灞曪細Application / Infrastructure / API / 瀵瑰簲鍓嶇椤甸潰涓?API client銆?
- 蹇呴』瀹屾垚鍚庣鏋勫缓銆佸墠绔瀯寤哄拰鑷冲皯涓€鏉＄湡瀹?API handover workflow 鍥炲綊銆?

## Steps
1. 鍦?`IHandoverService` / `InMemoryHandoverService` 涓ˉ榻?`send-email`銆乣start`銆乣complete`銆乣rollback` 鏄惧紡鍔ㄤ綔锛屽苟涓轰氦鎺ヨ褰曡ˉ榻?`StartedAt`銆乣CompletedAt`銆?
2. 鍦?`HandoversController` 涓柊澧炴樉寮忓姩浣滅鐐广€佽鎯呯鐐广€佸尰闄㈣寖鍥存牎楠屽拰瀹¤鏃ュ織锛屽苟鎶?`/api/handovers` 鐨勬潈闄愭槧灏勪粠鈥滀粎 PUT 闇€瑕?manage鈥濅慨姝ｄ负鈥滈潪 GET 閮介渶瑕?manage鈥濄€?
3. 鍦?`pms-web/src/api/modules/handover.ts`銆乣pms-web/src/types/handover.ts`銆乣pms-web/src/views/handover/HandoverListView.vue` 鎺ュ叆鏄惧紡鍔ㄤ綔鎸夐挳銆佽鎯呮椂闂村瓧娈典笌璐ｄ换鑺傜偣灞曠ず锛岀Щ闄ゅ墠绔?`nextStage` 绾挎€ф帹杩涜涔夈€?
4. 鏂板 handover workflow 鍥炲綊鑴氭湰锛屽熀浜庣幇鏈変氦鎺ヨ褰曟墽琛屽姩浣滃苟鎭㈠鍘熷闃舵銆?
5. 鎵ц `dotnet build PMS.sln`銆乣PMS: Build Frontend` 涓?handover workflow 鑴氭湰鍥炲綊銆?

## Status
- 2026-04-23: 宸插畬鎴愩€俙PMS.Application/Models/Handover/HandoverItemDto.cs`銆乣PMS.Application/Contracts/Handover/IHandoverService.cs`銆乣PMS.Infrastructure/Services/InMemoryHandoverService.cs` 宸蹭负浜ゆ帴璁板綍琛ラ綈 `StartedAt`銆乣CompletedAt` 浠ュ強 `send-email`銆乣start`銆乣complete`銆乣rollback` 鏄惧紡鍔ㄤ綔锛屼笖淇浜嗗凡浜ゆ帴璁板綍琚湇鍔″眰鐩存帴绉婚櫎銆佹棤娉曞湪鍒楄〃鍜屾眹鎬讳腑淇濈暀鐨勯棶棰橈紱`PMS.API/Controllers/Handover/HandoversController.cs` 宸茶ˉ榻?`GET /api/handovers/{id}`銆亀orkflow PATCH 绔偣銆佸尰闄㈣寖鍥存牎楠屽拰瀹¤鏃ュ織锛沗PMS.Infrastructure/Services/InMemoryAccessControlService.cs` 宸插皢 `/api/handovers` 鏉冮檺鏄犲皠鏀跺彛涓?GET=`handover.view`銆侀潪 GET=`handover.manage`銆傚墠绔?`pms-web/src/api/modules/handover.ts`銆乣pms-web/src/types/handover.ts`銆乣pms-web/src/views/handover/HandoverListView.vue` 宸插垏鍒版樉寮忓姩浣滄寜閽紝骞惰ˉ榻愯矗浠昏妭鐐广€侀偖浠跺彂閫佹椂闂淬€佸紑濮嬩氦鎺ユ椂闂淬€佸畬鎴愭椂闂村睍绀恒€傞獙璇侊細`dotnet build PMS.sln` 閫氳繃锛堝仠鎺?dev 鏈嶅姟鍚?0 warning 0 error锛夛紱`PMS: Build Frontend` 閫氳繃锛沗powershell -ExecutionPolicy Bypass -File scripts/verify-handover-workflow.ps1` 宸插湪鐜版湁 `宸蹭氦鎺 璁板綍涓婂疄娴?rollback -> complete -> restore 鍘熷闃舵鎴愬姛銆?

## Goal
- 瀹炴柦 Phase 2 鐨勭鍥涗釜鍙氦浠樺垏鐗囷細鎶?WorkHours 浠庘€滃彧鏈夌紪杈?鍒犻櫎锛屾病鏈?submit / confirm / reject 闂幆鈥濊ˉ榻愪负鍙彁浜ゃ€佸彲纭銆佸彲閫€鍥炵殑鍔ㄤ綔娴侊紝骞惰ˉ榻愮姸鎬佸睍绀轰笌璇︽儏瀹℃壒淇℃伅銆?

## Non-goals
- 涓嶉噸鍋氬伐鏃舵姤琛ㄣ€佺粺璁″彛寰勬垨瀵煎嚭妯℃澘銆?
- 涓嶆墿鏁ｅ埌 AnnualReport銆丠andover 绛夊叾瀹冩ā鍧椼€?
- 涓嶅紩鍏ユ柊鐨勬秷鎭€氱煡銆佽嚜鍔ㄥ偓鍔炴垨澶嶆潅瀹℃壒瑙勫垯銆?

## Constraints
- 淇濇寔鐜版湁宸ユ椂鏌ヨ銆佸垱寤恒€佺紪杈戞帴鍙ｅ吋瀹癸紝涓嶉噸鏋勬暟鎹ā鍨嬩富缁撴瀯銆?
- 浠呭湪鐜版湁鍒嗗眰鍐呭閲忔墿灞曪細Infrastructure / API / 瀵瑰簲鍓嶇椤甸潰涓?API client銆?
- 蹇呴』瀹屾垚鍚庣鏋勫缓銆佸墠绔瀯寤哄拰鑷冲皯涓€鏉＄湡瀹?API 宸ユ椂 workflow 鍥炲綊銆?

## Steps
1. 鍦?`InMemoryWorkHoursService` 涓负 update/delete 澧炲姞鐘舵€佽竟鐣岋紝绂佹宸叉彁浜よ褰曡鐩存帴鍒犻櫎锛屽苟鍏佽 `rejected -> submitted` 閲嶆柊鎻愪氦娴佽浆銆?
2. 鍦?`WorkHoursController` 涓负 `GET /{id}`銆乣PATCH /submit|confirm|reject` 琛ラ綈鍖婚櫌鑼冨洿鏍￠獙锛屽苟璁?workflow 绔偣杩斿洖鏇存柊鍚庣殑 `WorkHoursItemDto`銆?
3. 鍦?`pms-web/src/api/modules/workhours.ts`銆乣pms-web/src/types/workhours.ts`銆乣pms-web/src/views/workhours/WorkHoursView.vue` 鎺ュ叆鐘舵€佸瓧娈点€佹彁浜?纭/閫€鍥炲姩浣滄寜閽笌璇︽儏瀹℃壒淇℃伅灞曠ず銆?
4. 鏂板宸ユ椂 workflow 鍥炲綊鑴氭湰锛岃鐩?create -> submit -> reject -> update -> submit -> confirm -> delete 閾捐矾銆?
5. 鎵ц `dotnet build PMS.sln`銆乣PMS: Build Frontend` 涓庡伐鏃?workflow 鑴氭湰鍥炲綊銆?

## Status
- 2026-04-23: 宸插畬鎴愩€俙PMS.Infrastructure/Services/InMemoryWorkHoursService.cs` 宸插皢缂栬緫绾︽潫鏀跺彛鍒?`draft/rejected`锛岄樆姝?`submitted` 璁板綍琚垹闄わ紝骞跺厑璁?`rejected` 璁板綍閲嶆柊 `submit`锛沗PMS.API/Controllers/WorkHours/WorkHoursController.cs` 宸蹭负 `GET /api/workhours/{id}` 涓?workflow 绔偣琛ラ綈鍖婚櫌鑼冨洿鏍￠獙锛屽苟鍦?submit/confirm/reject 鍚庣洿鎺ヨ繑鍥炴渶鏂?`WorkHoursItemDto`銆傚墠绔?`pms-web/src/api/modules/workhours.ts`銆乣pms-web/src/types/workhours.ts`銆乣pms-web/src/views/workhours/WorkHoursView.vue` 宸叉帴鍏?`status/confirmedBy/confirmedAt` 瀛楁銆佺姸鎬佸垪銆佹彁浜?纭/閫€鍥炴寜閽互鍙婅鎯呮娊灞変腑鐨勫鎵瑰姩浣溿€傞獙璇侊細`dotnet build PMS.sln` 閫氳繃锛? warning 0 error锛屽仠鎺?dev 鏈嶅姟鍚庢墽琛岋級锛沗PMS: Build Frontend` 閫氳繃锛沗powershell -ExecutionPolicy Bypass -File scripts/verify-workhours-workflow.ps1` 瀹炴祴 create -> submit -> reject -> update -> submit -> confirm -> delete 鍏ㄩ摼璺垚鍔熴€?

## Goal
- 瀹炴柦 Phase 2 鐨勭涓変釜鍙氦浠樺垏鐗囷細鎶?MonthlyReport 浠庘€滄柊澧?缂栬緫琛ㄥ崟鐩存帴鏀圭姸鎬佲€濇敹鍙ｄ负 submit / approve / reject 鍔ㄤ綔娴侊紝骞惰ˉ榻愬鎵逛俊鎭睍绀轰笌鏉冮檺鏄犲皠銆?

## Non-goals
- 涓嶉噸鍋氭湀鎶ヨ嚜鍔ㄧ敓鎴愩€佺粨鏋勫寲鍐呭鍖哄潡鎴栧鍑鸿兘鍔涖€?
- 涓嶆墿鏁ｅ埌 WorkHours銆丄nnualReport 绛夊叾瀹冨鎵规ā鍧椼€?
- 涓嶅紩鍏ユ柊鐨勫悗鍙板鎵逛换鍔°€佹秷鎭槦鍒楁垨澶嶆潅瑙勫垯寮曟搸銆?

## Constraints
- 淇濇寔鐜版湁鏈堟姤鏌ヨ銆佸垱寤恒€佺紪杈戞帴鍙ｅ吋瀹癸紝`MonthlyReportUpsertDto.Status` 鍙户缁繚鐣欎絾涓嶅啀椹卞姩鐘舵€佹祦杞€?
- 浠呭湪鐜版湁鍒嗗眰鍐呭閲忔墿灞曪細Infrastructure / API / 瀵瑰簲鍓嶇椤甸潰涓?API client銆?
- 蹇呴』瀹屾垚鍚庣鏋勫缓銆佸墠绔瀯寤哄拰鑷冲皯涓€鏉＄湡瀹?API 鏈堟姤鍔ㄤ綔娴佸洖褰掋€?

## Steps
1. 涓?`/api/monthly-reports` 琛ラ綈鏉冮檺鏄犲皠锛岀‘淇?GET 浣跨敤 `monthly-report.view`锛岄潪 GET 浣跨敤 `monthly-report.manage`銆?
2. 鍦?`InMemoryMonthlyReportService` 涓姝㈤€氳繃 create/update 鐩存帴鏀?`Status`锛屾敼涓轰粎鍏佽 `submit` / `approve` / `reject` 涓撶敤鍔ㄤ綔鏀瑰彉鐘舵€併€?
3. 鍦?`monthly-report.ts`銆乣monthly-report.ts` 绫诲瀷瀹氫箟涓?`MonthlyReportView.vue` 鎺ュ叆鎻愪氦/閫氳繃/椹冲洖鍔ㄤ綔锛岀Щ闄よ〃鍗曚腑鐨勭洿鎺ョ姸鎬侀€夋嫨锛屽苟灞曠ず瀹℃牳浜?瀹℃牳鏃堕棿/椹冲洖鍘熷洜銆?
4. 鏂板鏈堟姤 workflow 鍥炲綊鑴氭湰锛岃鐩?create -> submit -> reject -> update -> submit -> approve -> delete 閾捐矾銆?
5. 鎵ц `dotnet build PMS.sln`銆乣PMS: Build Frontend` 涓庢湀鎶?workflow 鑴氭湰鍥炲綊銆?

## Status
- 2026-04-23: 宸插畬鎴愩€俙PMS.Infrastructure/Services/InMemoryAccessControlService.cs` 宸蹭负 `/api/monthly-reports` 琛ラ綈 GET=`monthly-report.view`銆侀潪 GET=`monthly-report.manage` 鏉冮檺鏄犲皠锛沗PMS.Infrastructure/Services/InMemoryMonthlyReportService.cs` 宸茬姝㈤€氳繃 create/update 鐩存帴鍐欏叆 `Status`锛屼粎鍏佽 `submit` / `approve` / `reject` 涓撶敤鍔ㄤ綔鏀瑰彉鐘舵€侊紝骞跺缂栬緫鎬佺害鏉熶负 `draft/rejected`锛沗PMS.API/Controllers/MonthlyReport/MonthlyReportsController.cs` 宸插皢闈炴硶缂栬緫鎬佽浆鎹负 400 杩斿洖銆傚墠绔?`pms-web/src/api/modules/monthly-report.ts`銆乣pms-web/src/types/monthly-report.ts`銆乣pms-web/src/views/monthly-report/MonthlyReportView.vue` 宸叉帴鍏ユ彁浜?閫氳繃/椹冲洖鍔ㄤ綔锛岀Щ闄よ〃鍗曚腑鐨勭洿鎺ョ姸鎬侀€夋嫨锛屽苟琛ラ綈瀹℃牳浜恒€佸鏍告椂闂淬€侀┏鍥炲師鍥犲睍绀恒€傞獙璇侊細`dotnet build PMS.sln` 閫氳繃锛? error锛屼繚鐣欐棦鏈?nullable warnings锛夛紱`PMS: Build Frontend` 閫氳繃锛沗powershell -ExecutionPolicy Bypass -File scripts/verify-monthly-report-workflow.ps1` 瀹炴祴 create -> submit -> reject -> update -> submit -> approve -> delete 鍏ㄩ摼璺垚鍔熴€?

## Goal
- 瀹炴柦 Phase 2 鐨勭浜屼釜鍙氦浠樺垏鐗囷細鎶?MajorDemand 涓?Inspection 浠庘€滃垪琛?琛ㄥ崟鐩存帴鏀圭姸鎬佲€濊ˉ榻愪负鍙彈鐞?寮€濮嬨€佸彲瀹屾垚銆佸彲閲嶅紑鐨勫姩浣滃寲娴佺▼锛屽苟鍦ㄥ墠绔ˉ榻?SLA/鎴鏃堕棿鎻愮ず涓庤鎯呮椂闂村瓧娈点€?

## Non-goals
- 涓嶉噸鍋?MajorDemand / Inspection 鐨勬暣浣?CRUD 缁撴瀯銆?
- 涓嶅紩鍏ユ柊鐨勫悗鍙颁换鍔¤皟搴︺€佹秷鎭€氱煡鎴栧鏉傝鍒欏紩鎿庛€?
- 涓嶆敼鍔ㄥ叾瀹冧笟鍔℃ā鍧楃殑鐘舵€佹祦杞柟寮忋€?

## Constraints
- 淇濇寔鐜版湁鎺ュ彛涓庡墠绔垪琛ㄧ瓫閫夈€佸鍑鸿兘鍔涘吋瀹广€?
- 浠呭湪鐜版湁鍒嗗眰鍐呭閲忔墿灞曪細Application / Infrastructure / API / 瀵瑰簲鍓嶇椤甸潰銆?
- 蹇呴』瀹屾垚鍚庣鏋勫缓銆佸墠绔瀯寤哄拰鑷冲皯涓€鏉＄湡瀹?API 鍔ㄤ綔娴佸洖褰掋€?

## Steps
1. 淇 `/api/inspections` 鏉冮檺鏄犲皠锛屾敼涓?GET 浣跨敤 `inspection.view`锛岄潪 GET 浣跨敤 `inspection.manage`銆?
2. 涓?Inspection 璁″垝琛ラ綈 `StartedAt`銆乣CompletedAt`銆乣SlaDueAt` 涓?`start` / `complete` / `reopen` 鍔ㄤ綔绔偣锛屽苟鍐欏叆瀹¤鏃ュ織銆?
3. 涓?MajorDemand workflow 琛ラ綈 `AcceptedAt`銆乣CompletedAt` 涓?`accept` / `complete` / `reopen` 鍗曟潯鍔ㄤ綔銆?
4. 鍦?`MajorDemandView` 涓?`InspectionPlanView` 澧炲姞鍔ㄤ綔鎸夐挳銆丼LA/鎴鎻愮ず銆佽鎯呮椂闂村瓧娈碉紝骞朵慨姝ｅ墠绔寜閽潈闄愬垽鏂€?
5. 鎵ц `dotnet build PMS.sln`銆乣PMS: Build Frontend` 涓?`scripts/verify-major-demand-inspection-workflows.ps1` 杩愯鎬佸洖褰掋€?

## Status
- 2026-04-23: 宸插畬鎴愩€傚悗绔凡涓?`PMS.API/Controllers/MajorDemand/MajorDemandsController.cs` 鏂板 `accept` / `complete` / `reopen` 鍔ㄤ綔绔偣锛屽苟鍦?`PMS.Infrastructure/Services/InMemoryMajorDemandStore.cs` 涓ˉ榻?`AcceptedAt`銆乣CompletedAt` 涓庡姩浣滄棩蹇楋紱`PMS.API/Controllers/Inspection/InspectionsController.cs` 宸叉柊澧?`start` / `complete` / `reopen` 鍔ㄤ綔绔偣涓?`IAuditLogService` 璁板綍锛宍PMS.Infrastructure/Services/InMemoryInspectionService.cs` 琛ラ綈 `StartedAt`銆乣CompletedAt`銆乣SlaDueAt`銆佸姩浣滄祦鏂规硶浠ュ強 custom row 鍙鎬т慨澶嶏紝閬垮厤鏂板缓宸℃璁″垝鈥滆兘鍒犱笉鑳芥煡鈥濄€傚墠绔?`pms-web/src/views/major-demand/MajorDemandView.vue` 宸茶ˉ榻愬彈鐞?瀹屾垚/閲嶅紑鎸夐挳涓庢埅姝㈠€掕鏃讹紝`pms-web/src/views/inspection/InspectionPlanView.vue` 宸茶ˉ榻愬紑濮嬫墽琛?瀹屾垚宸℃/閲嶅紑璁″垝鎸夐挳銆丼LA 鎻愮ず鍜岃鎯呮椂闂村瓧娈碉紝骞朵慨姝ｆ寜閽潈闄愪粠 `inspection.view` 鏀逛负 `inspection.manage`銆傞獙璇侊細`dotnet build PMS.sln` 閫氳繃锛? error 0 warning锛夛紱`PMS: Build Frontend` 閫氳繃锛沗scripts/verify-major-demand-inspection-workflows.ps1` 瀹炴祴閲嶅ぇ闇€姹備笌宸℃璁″垝涓ゆ潯鍔ㄤ綔娴佸潎瀹屾垚 create/add -> action -> reopen -> cleanup 鍏ㄩ摼璺洖褰掞紝鍏朵腑 inspection 瀹¤鏃ュ織鍛戒腑 3 鏉″姩浣滆褰曘€?

## Goal
- 瀹炴柦 Phase 2 鐨勯涓彲浜や粯鍒囩墖锛氭妸 Repair 浠庘€滅洿鎺ユ敼鐘舵€佲€濆崌绾т负鈥滃彲绛炬敹 / 鍙畬鎴?/ 鍙噸寮€鈥濈殑鍔ㄤ綔鍖栧伐鍗曟祦锛屽苟琛ラ綈澶勭悊 SLA 鎴鏃堕棿涓庡熀纭€鏃堕棿绾裤€?

## Non-goals
- 鏈疆涓嶆墿鏁ｅ埌 MajorDemand / Inspection銆?
- 涓嶅紩鍏?HostedService銆佹秷鎭槦鍒楁垨澶嶆潅 SLA 绛栫暐閰嶇疆涓績銆?
- 涓嶉噸鍋?AuditLog 椤甸潰锛屽彧淇濊瘉鍔ㄤ綔琚褰曘€?

## Constraints
- 淇濇寔鐜版湁 Repair CRUD 涓庡鍑烘帴鍙ｅ吋瀹广€?
- 浠呭湪鐜版湁鍒嗗眰鍐呭閲忔敼閫狅細Entity / DTO / Service / Controller / RepairRecordView銆?
- 蹇呴』瀹屾垚鍚庣鏋勫缓銆佸墠绔瀯寤哄拰鑷冲皯涓€鏉＄湡瀹?API 宸ヤ綔娴佸洖褰掋€?

## Steps
1. 涓?`RepairRecordEntity` / DTO 澧炲姞 `AcceptedAt`銆乣SlaDueAt`锛屽苟鍦?`InMemoryRepairRecordService` 涓疄鐜拌交閲?SLA 璁＄畻銆?
2. 鏂板 `accept` / `resolve` / `reopen` 鍔ㄤ綔绔偣锛屽苟鎶婂姩浣滃啓鍏?`IAuditLogService`銆?
3. 灏?`RepairRecordView` 浠庤〃鍗曠洿鎺ユ敼鐘舵€佸垏鎹负鍔ㄤ綔鎸夐挳娴佽浆锛屽垪琛ㄥ鍔?SLA 鍒楋紝璇︽儏琛ユ椂闂寸嚎銆?
4. 鎵ц `dotnet build PMS.sln`銆乣PMS: Build Frontend` 涓庝复鏃跺伐鍗曞叏閾捐矾鍥炲綊鑴氭湰楠岃瘉銆?

## Status
- 2026-04-23: 宸插畬鎴愩€俁epair 鐜板凡鏀寔鍔ㄤ綔鍖栨祦杞細`/accept` 绛炬敹浼氬啓鍏?`AcceptedAt` 骞惰浆涓衡€滃鐞嗕腑鈥濓紱`/resolve` 浼氬己鍒跺～鍐欏鐞嗙粨鏋滃苟鍐欏叆 `CompletedAt`锛沗/reopen` 浼氬洖鍒扳€滃緟澶勭悊鈥濆苟閲嶇疆澶勭悊鑺傜偣锛屽悓鏃朵笁绫诲姩浣滃叏閮ㄥ啓鍏ュ璁℃棩蹇椼€傚墠绔?`RepairRecordView` 宸叉柊澧?SLA 鍒椼€佺鏀?瀹屾垚/閲嶅紑鎸夐挳锛岀Щ闄よ〃鍗曠洿鎺ユ敼鐘舵€佸叆鍙ｏ紝骞跺湪璇︽儏鎶藉眽琛ラ綈澶勭悊鑺傜偣鏃堕棿绾裤€傞獙璇侊細`dotnet build PMS.sln` 閫氳繃锛? error锛屼繚鐣欐棦鏈?nullable warnings锛夛紱`PMS: Build Frontend` 閫氳繃锛沗scripts/verify-repair-workflow.ps1` 瀹炴祴鍒涘缓涓存椂鎶ヤ慨 鈫?绛炬敹 鈫?瀹屾垚 鈫?閲嶅紑 鈫?鍒犻櫎鍏ㄩ摼璺垚鍔燂紝涓斿璁℃棩蹇楀懡涓?3 鏉?repair 鍔ㄤ綔璁板綍銆?

## Goal
- 鍩轰簬鍏ㄧ洏瀹℃煡缁撴灉锛屽疄鏂介珮浼樺厛绾у悗绔畨鍏ㄥ姞鍥猴細澶囦唤绔偣閴存潈銆佹暟鎹鍏ヨ矾寰勭櫧鍚嶅崟銆佽璇佷腑闂翠欢鍏紑鍓嶇紑绮剧‘鍖归厤銆?

## Non-goals
- 涓嶆敼鍔ㄥ墠绔笟鍔℃祦绋嬪拰鍚庣鏈嶅姟鍒嗗眰缁撴瀯銆?
- 涓嶅湪鏈疆澶勭悊 P1/P2 鐨勫叾浠栭」锛堝 InMemory*Store 涓?IoC 鐢熷懡鍛ㄦ湡銆丠ospital360 缂哄け闈㈡澘銆佸叏閲?`size:100000` 鎷夊彇绛夛級銆?

## Constraints
- 鏀瑰姩浠呴檺閴存潈/鏉冮檺/鏁版嵁瀵煎叆閾捐矾銆?
- 涓嶇牬鍧忔棦鏈?admin 鑳藉姏锛堝浠戒笅杞姐€丏ata 鐩綍榛樿璺緞瀵煎叆锛夈€?

## Steps
1. `InMemoryAccessControlService.ResolveRequiredPermission` 鎶?`/api/system` 閫氶厤鏀捐鏀逛负浠呮斁琛?`/api/system/info`锛屽苟涓?`/api/system/backup` 瑕佹眰 `maintenance.manage`銆?
2. `DataImportController` 鏂板 `TryResolveAllowedFilePath` 鐧藉悕鍗曟牎楠岋紝闄愬畾 `text-file`銆乣major-demand`銆乣project-ledger` 绔偣鐨?`FilePath` 蹇呴』鍦?`AppContext.BaseDirectory/Data` 鍐呫€?
3. `AuthMiddleware.IsPublicPath` / `PermissionMiddleware` 鐨?`/api/auth/login` 涓?`/api/health` 鏀逛负绮剧‘鍖归厤锛岄伩鍏?`*/api/healthxxx` 鍨嬪墠缂€缁曡繃銆?
4. 鏈湴鏋勫缓 + API 琛屼负鍥炲綊锛歛dmin 澶囦唤涓嬭浇 200锛岃秺鏉冩枃浠惰矾寰?400锛岀簿纭尮閰嶄粛鍏佽 admin 鐧诲綍銆?

## Status
- 2026-04-23: 宸插畬鎴愩€傚悗绔瀯寤?0 error 0 warning锛堟湰杞敼鍔ㄨ寖鍥村唴锛夛紱`/api/system/backup/download` 瀵?admin 杩斿洖 200 涓斾笅杞?1.6MB 蹇収锛沗/api/admin/import/text-file` 浼犲叆 `C:/Windows/win.ini` 杩斿洖 400 涓旀秷鎭负鈥滀粎鍏佽璇诲彇搴旂敤 Data 鐩綍涓嬬殑鏂囦欢鈥濓紱`major-demand` 绔偣浼犲叆 `../../../../etc/hosts` 鍚屾牱杩斿洖 400銆傚叕寮€鍓嶇紑浠?`StartsWith` 鏀逛负绮剧‘ HashSet 鍖归厤锛屼笖榛樿瑙掕壊锛坥perator/supervisor/regional_manager锛夊潎涓嶅惈 `maintenance.manage` 鏉冮檺閿紝闈?admin 鐢ㄦ埛浼氳 403 鎷︽埅锛堟寜浠ｇ爜璺緞楠岃瘉锛夈€?

## Goal
- 涓?Hospital360 澧炲姞鍒伴」鐩€佸憡璀︺€佸伐鏃躲€佹姤淇〉闈㈢殑鑱斿姩鍏ュ彛锛岃椤甸潰鏇村儚鍙搷浣滅殑璇︽儏宸ヤ綔鍙帮紝鑰屼笉鍙槸鑱氬悎鐪嬫澘銆?

## Non-goals
- 涓嶆敼鍔ㄥ悗绔帴鍙ｅ拰鐩爣椤甸潰鐨勬暟鎹粨鏋勩€?
- 涓嶆柊澧炵嫭绔嬭鎯呴〉璺敱銆?

## Constraints
- 鍙樻洿浼樺厛闄愬埗鍦?`pms-web/src/views/hospital/Hospital360View.vue`銆?
- 澶嶇敤鐩爣椤甸潰鐜版湁鐨?`route.query` 杩囨护鍜屽脊绐?璇︽儏鎵撳紑鑳藉姏銆?

## Steps
1. 涓哄綋鍓嶆縺娲婚潰鏉垮鍔犫€滆繘鍏ュ搴旈〉闈⑩€濈殑鎬诲叆鍙ｃ€?
2. 涓哄悇琛ㄦ牸澧炲姞琛岀骇鑱斿姩鎸夐挳锛屾惡甯﹀尰闄€佷骇鍝併€佺姸鎬佺瓑鏌ヨ鍙傛暟璺宠浆銆?
3. 鎵ц鍓嶇鏋勫缓涓庨〉闈㈠洖褰掗獙璇併€?

## Status
- 2026-04-23: 宸插畬鎴愩€侶ospital360 宸茶ˉ榻愰潰鏉跨骇鈥滆繘鍏ュ搴旈〉闈⑩€濆拰琛岀骇鑱斿姩鍏ュ彛锛屽彲璺宠浆鍒伴」鐩€佸憡璀︺€佸伐鏃躲€佹姤淇〉闈㈠苟鑷姩甯︿笂鍖婚櫌/浜у搧/鐘舵€佺瓑绛涢€夊弬鏁般€傞獙璇侊細`PMS: Build Frontend` 閫氳繃锛汸laywright 鐧诲綍鎬佸洖褰?`hospital/360`锛岃繍琛屾椂 `ISSUES=0`锛屽苟纭鎸夐挳鐐瑰嚮鍚庢垚鍔熻烦杞埌瀵瑰簲鍒楄〃椤点€?

## Goal
- 缁х画缁嗗寲 Hospital360 瑙嗗浘锛屽湪涓嶆敼鎺ュ彛鐨勫墠鎻愪笅琛ラ綈椤靛ご缁忚惀鎽樿銆侀潰鏉垮唴鍏抽敭鎸囨爣鍜屾洿閫傚悎鍖婚櫌绾у贰妫€鐨勫睍绀洪『搴忋€?

## Non-goals
- 涓嶆柊澧炲悗绔帴鍙ｃ€佸瓧娈垫垨璺ㄦā鍧楄仈鍔ㄩ€昏緫銆?
- 涓嶆敼鍔ㄥ尰闄㈠垪琛ㄩ〉鎴栧叾瀹冭鎯呴〉銆?

## Constraints
- 鍙樻洿闄愬埗鍦?`pms-web/src/views/hospital/Hospital360View.vue` 涓?memory-bank 璁板綍銆?
- 瀹屾垚鍚庡繀椤绘墽琛屽墠绔瀯寤洪獙璇併€?

## Steps
1. 涓洪〉澶村鍔犲尰闄㈢骇鍏抽敭鎽樿锛岃椤圭洰銆侀闄┿€佸伐鏃躲€佸紑鏀惧伐鍗曚竴鐪煎彲瑙併€?
2. 涓烘瘡涓?tab 澧炲姞涓氬姟鎽樿鎻愮ず锛屽苟鎸夋洿鍒╀簬鍒ゆ柇鐨勯『搴忓睍绀烘暟鎹€?
3. 鎵ц鍓嶇鏋勫缓楠岃瘉骞跺洖鍐欒繘搴︺€?

## Status
- 2026-04-23: 宸插畬鎴愩€侶ospital360 瑙嗗浘宸茶ˉ榻愬尰闄㈢骇缁忚惀鎽樿銆佸悇 tab 鍏抽敭鎸囨爣鍗″拰鏇村埄浜庢帓鏌ョ殑榛樿鎺掑簭銆傞獙璇侊細`PMS: Build Frontend` 閫氳繃锛汸laywright 鐧诲綍鎬佸洖褰?`hospital/360`锛岃繍琛屾椂 `ISSUES=0`锛屾埅鍥?`regression-hospital360-premium.png` 宸插埛鏂般€?

## Goal
- 灏?Hospital360 瑙嗗浘涓讳綋浠庘€滄憳瑕佸崱 + 瑁?tabs/table鈥濇敹鍙ｅ埌缁熶竴鐨勫晢鍔″寲璇︽儏椤电粨鏋勶紝琛ラ綈鏁版嵁闈㈡澘灞傜骇銆乼ab 鎽樿涓庢爣绛惧寲淇℃伅琛ㄨ揪銆?

## Non-goals
- 涓嶆敼鍔ㄥ尰闄?360 鐨勬帴鍙ｈ皟鐢ㄣ€佽矾鐢卞弬鏁板拰鑱氬悎閫昏緫銆?
- 涓嶆墿鏁ｅ埌鍏跺畠鍖婚櫌绠＄悊椤甸潰銆?

## Constraints
- 涓昏鏀瑰姩闄愬埗鍦?`pms-web/src/views/hospital/Hospital360View.vue`锛屼紭鍏堝鐢ㄧ幇鏈?`SummaryMetrics`銆乣AppTableCard` 涓庡叏灞€璁捐绯荤粺銆?
- 鏀瑰畬鍚庡繀椤绘墽琛屽墠绔瀯寤洪獙璇併€?

## Steps
1. 灏嗘憳瑕佸崱鏀逛负涓?tab 鑱斿姩锛屾槑纭綋鍓嶆縺娲绘暟鎹潰鏉裤€?
2. 涓轰富鍐呭鍖哄鍔犵粺涓€澶撮儴璇存槑銆佽褰曡鏁颁笌鍐呭鍗＄墖瀹瑰櫒銆?
3. 鐢ㄦ爣绛惧拰璇存槑鏉℃彁鍗囬」鐩?鍛婅/鎶ヤ慨琛ㄦ牸鐨勫彲鎵鎬с€?
4. 鎵ц鍓嶇鏋勫缓楠岃瘉骞跺洖鍐欒繘搴︺€?

## Status
- 2026-04-23: 宸插畬鎴愩€侶ospital360 瑙嗗浘宸插垏鎹㈠埌鈥滈〉澶存瑙?+ 鍙垏鎹㈡憳瑕佸崱 + 缁熶竴鍐呭鍗♀€濈粨鏋勶紝琛ラ綈 tab 璁℃暟銆侀潰鏉胯鏄庡拰鏍囩鍖栫姸鎬佽〃杈俱€傞獙璇侊細`PMS: Build Frontend` 閫氳繃锛汸laywright 鐧诲綍鎬佸洖褰?`hospital/360`锛岃繍琛屾椂 `ISSUES=0`锛屾埅鍥惧凡鍒锋柊銆?

## Goal
- 灏?PMS 鐨勯椤电湅鏉裤€侀」鐩垪琛?寰呭姙浠诲姟椤点€佹娊灞?寮圭獥琛ㄥ崟鍜屽叏灞€甯冨眬缁熶竴鎻愬崌鍒版洿鎺ヨ繎涓婄嚎浜や粯鐨勭畝绾﹀晢鍔￠珮绾ч锛岄噸鐐硅В鍐虫爣棰樺眰绾ф贩涔便€佸浘琛ㄩ粯璁ゆ劅寮恒€佸垪琛ㄩ槄璇诲瘑搴﹀樊鍜屽３灞傜己灏戣矾寰勭З搴忕殑闂銆?

## Non-goals
- 涓嶆敼鍔ㄤ换浣曚笟鍔℃帴鍙ｃ€佹潈闄愰€昏緫銆佹帴鍙ｅ绾︿笌璺敱璺緞銆?
- 涓嶉噸鍐欓〉闈㈡暟鎹祦锛屼紭鍏堥€氳繃鍏变韩缁勪欢銆佽璁＄郴缁熶笌灏戦噺鍏抽敭椤甸潰妯℃澘閲嶆瀯瀹屾垚浣撻獙鍗囩骇銆?

## Constraints
- 淇濇寔 Element Plus 涓庣幇鏈夌粍浠跺吋瀹癸紝涓嶆柊澧炶繍琛屾椂渚濊禆銆?
- 闇€瑕佽鐩栨闈㈢涓诲満鏅紝骞朵互鏋勫缓涓庣櫥褰曟€佹埅鍥惧洖褰掍綔涓烘敹鍙ｆ爣鍑嗐€?

## Steps
1. 鍚敤鐙珛璁捐绯荤粺鏂囦欢锛岀粺涓€ page head銆佽〃鏍笺€佽〃鍗曘€佹寜閽€佸崱鐗囧拰鍒嗛〉鐨勫晢鍔″寲浣庨ケ鍜岃鍒欍€?
2. 鍗囩骇 `AppLayout`锛岃ˉ榻愰潰鍖呭睉銆佷晶鏍忔姌鍙犮€侀〉鑴氬拰鏇村厠鍒剁殑椤跺眰杈呭姪鏍忋€?
3. 鍗囩骇 `ProTable`銆乣AppTableCard`銆乣ProDrawer`銆乣AppFormDialog`锛岀粺涓€闀垮垪琛ㄤ笌琛ㄥ崟瀹瑰櫒鐨勯槄璇昏妭濂忋€?
4. 閲嶆瀯 `DashboardView` 涓?`MS010001013001View` 鐨勪俊鎭眰绾у拰鍥捐〃/鐪嬫澘瑙嗚锛岄伩鍏嶉粯璁ょ粍浠舵劅銆?
5. 鎵ц鍓嶇鏋勫缓涓庣櫥褰曟€?Playwright 鎴浘鍥炲綊锛屽苟璁板綍缁撴灉銆?

## Status
- 2026-04-22: 宸插畬鎴愭楠?1-5銆傚凡鍚敤 `premium-ui.css` 浣滀负鐙珛璁捐绯荤粺瑕嗙洊灞傦紝缁熶竴鍏ㄥ眬鏍囬/琛ㄦ牸/琛ㄥ崟/鎸夐挳椋庢牸锛沗AppLayout` 宸茶ˉ榻愰潰鍖呭睉銆佹姌鍙犱晶鏍忓拰椤佃剼锛沗ProTable`銆乣AppTableCard`銆乣ProDrawer`銆乣AppFormDialog` 宸插垏鎹㈠埌缁熶竴鍟嗗姟瀹瑰櫒鏍峰紡锛沗DashboardView` 涓?`MS010001013001View` 宸插畬鎴愮湅鏉垮眰绾т笌浣庨ケ鍜岄厤鑹叉敹鍙ｃ€傞獙璇侊細`powershell -ExecutionPolicy Bypass -File c:\Users\Administrator\PMS-Standalone\scripts\build-frontend.ps1` 閫氳繃锛涚櫥褰曟€?Playwright 鎴浘澶嶆牳 `dashboard`銆乣project/list`銆乣complex-model/ms010001-013-001` 鍜岄」鐩紪杈戞娊灞夛紝杩愯鏃?`ISSUES=0`銆?
- 2026-04-22: 宸插畬鎴愭湰杞渶缁堜竴鑷存€ф敹鍙ｃ€備笁瑙掕壊宸ヤ綔鍙般€乣AuditLogView`銆乣KpiDashboardView`銆乣PersonnelListView` 宸茬粺涓€鍒板叡浜寚鏍囧崱/瀹瑰櫒璇█锛涜ˉ榻?`pms-web/src/utils/echarts-basic.ts` 鐨?`GraphicComponent` 娉ㄥ唽锛屾秷闄?KPI 椤甸潰杩愯鏃跺憡璀︺€傞獙璇侊細`powershell -ExecutionPolicy Bypass -File c:\Users\Administrator\PMS-Standalone\scripts\build-frontend.ps1` 閫氳繃锛汸laywright 澶嶉獙 `dashboard/kpi`銆乣audit/log`銆乣permission/manage` 鍙婂瑙掕壊 `dashboard`锛岃繍琛屾椂 `ISSUES=0`锛屽苟鐢熸垚鏈€鏂版埅鍥惧洖褰掋€?
- 2026-04-22: 宸插畬鎴愮浜岃疆鍒楄〃椤电粺璁″尯鍏变韩鍖栥€傛柊澧?`pms-web/src/components/SummaryMetrics.vue` 浣滀负鍏变韩鎽樿鍗＄粍浠讹紝骞跺皢 `AlertCenterView`銆乣ContractAlertView`銆乣RepairRecordView`銆乣WorkHoursView`銆乣InspectionPlanView`銆乣AnnualReportView`銆乣HospitalListView`銆乣HandoverListView`銆乣ProductListView`銆乣PersonnelListView`銆乣Hospital360View` 鐨勬棫 `stats-row/stat-card` 缁撴瀯杩佺Щ鍒扮粺涓€鎸囨爣鍗′綋绯汇€傞獙璇侊細`PMS: Build Frontend` 閫氳繃锛汸laywright 澶嶉獙 `annual-report/list`銆乣hospital/list`銆乣handover/list`銆乣product/list`銆乣permission/manage`銆乣hospital/360` 浠ュ強鍓嶄竴鎵?`contract/alerts`銆乣repair/list`銆乣workhours/list`銆乣inspection/plan`锛岃繍琛屾椂 `ISSUES=0`锛屾柊鎴浘鍥炲綊宸茬敓鎴愩€?

## Goal
- 灏?PMS 鍓嶇鏁翠綋椤甸潰銆佹寜閽拰浜や簰椋庢牸缁熶竴鍗囩骇涓烘洿鎺ヨ繎鎴愮啛澶у巶浼佷笟浜у搧鐨勪綋楠岋紝浼樺厛瀹屾垚鍏ㄥ眬璁捐绯荤粺銆佸簲鐢ㄥ３鍜岀櫥褰曞叆鍙ｇ殑椋庢牸鏀跺彛銆?

## Non-goals
- 涓嶄慨鏀逛换浣曚笟鍔℃帴鍙ｃ€佹潈闄愰€昏緫銆佽矾鐢辩粨鏋勪笌椤甸潰鏁版嵁娴併€?
- 涓嶉€愰〉閲嶅啓涓氬姟妯℃澘锛屼紭鍏堥€氳繃鍏ㄥ眬鏍峰紡鍜屽叡鐢ㄧ粍浠跺畬鎴愯瑙夌粺涓€銆?

## Constraints
- 淇濇寔 Element Plus 鍏煎锛屼笉鏂板杩愯鏃朵緷璧栥€?
- 淇濇寔妗岄潰绔笌绉诲姩绔彲鐢ㄦ€э紝骞堕€氳繃鏋勫缓鍛戒护鍋氭敹鍙ｉ獙璇併€?

## Steps
1. 閲嶅鍏ㄥ眬璁捐浠ょ墝銆佹寜閽€佽緭鍏ユ銆佸崱鐗囥€佽〃鏍笺€佸脊绐楃瓑鍏辩敤瑙嗚銆?
2. 鍗囩骇 `ProTable`銆乣AppLayout`銆乣LoginView` 鐨勫竷灞€灞傛涓庝氦浜掑弽棣堛€?
3. 鎵ц鍓嶇鏋勫缓楠岃瘉锛岀‘璁ゆ牱寮忔敼閫犳湭鐮村潖缂栬瘧涓庡叆鍙ｆ覆鏌撱€?
4. 鍦?`progress.md` 璁板綍鏈鏀归€犱笌楠岃瘉缁撴灉銆?

## Status
- 2026-04-22: 宸插畬鎴愭楠?1-4銆傚凡缁熶竴鍏ㄥ眬璁捐浠ょ墝銆佸垪琛ㄥ鍣ㄣ€佸簲鐢ㄥ３鍜岀櫥褰曞叆鍙ｇ殑瑙嗚涓庝氦浜掗鏍硷紝骞跺畬鎴愭瀯寤轰笌娴忚鍣ㄩ獙璇併€?
- 2026-04-22: 宸茶ˉ瀹岀浜岃疆缁嗚妭鏀跺彛銆傛娊灞夈€佸脊绐椼€佸伐浣滃彴鍗＄墖銆佺┖鐘舵€併€佸姞杞芥€佸拰涓汉璧勬枡璇︽儏椤靛凡缁熶竴鍒板悓涓€濂椾氦浜掕瑷€锛屽苟閫氳繃鍓嶇鏋勫缓楠岃瘉銆?

## Goal
- 缁х画瀹炴柦鍒楄〃椤电揣鍑戝寲绗簩鎵规敹鍙ｏ細宸℃璁″垝銆佷氦鎺ョ鐞嗐€佸悎鍚岄璀︺€佸尰闄㈢鐞嗗洓椤靛湪淇濇寔鐜版湁鏁版嵁缁撴瀯涓嶅彉鐨勫墠鎻愪笅锛屽帇缂╂帓鐗堝苟灏介噺閬垮厤鍏抽敭鍒椾笌鎿嶄綔鍖烘崲琛屻€?

## Non-goals
- 涓嶈皟鏁存帴鍙ｃ€佹潈闄愩€佺瓫閫夐€昏緫鎴?workflow 鍔ㄤ綔璇箟銆?
- 涓嶅啀娆″ぇ鏀瑰叏灞€璁捐绯荤粺锛屼粎澶勭悊鏈〉鍒楀鍜屽眬閮ㄥ鍣ㄦ牱寮忋€?

## Constraints
- 淇濇寔鐜版湁琛ㄦ牸瀛楁銆佹寜閽枃妗堝拰璺宠浆閫昏緫鍏煎銆?
- 浼樺厛閫氳繃鍒楀銆乶owrap銆乪llipsis 鏀跺彛锛屼笉寮曞叆鏂扮殑浜や簰灞傘€?
- 蹇呴』瀹屾垚鍓嶇鏋勫缓楠岃瘉銆?

## Steps
1. 璋冩暣 `InspectionPlanView.vue` 鐨勮鍒掕〃/缁撴灉琛ㄥ叧閿垪瀹斤紝骞惰 `deadline-cell` 涓庢搷浣滃尯淇濇寔鍗曡浼樺厛銆?
2. 璋冩暣 `HandoverListView.vue` 鐨勫尰闄?浜у搧/缁勫埆/鎵规/闃舵鍒楀锛屽苟璁╄鎯呭姩浣滃尯鍜岀湅鏉?meta 鍗曡鐪佺暐銆?
3. 璋冩暣 `ContractAlertView.vue` 涓?`HospitalListView.vue` 鐨勫叧閿枃鏈垪銆佽瘎绾у垪鍜屾搷浣滃尯瀹藉害锛岃ˉ榻愬眬閮?nowrap 鏍峰紡銆?
4. 鎵ц `PMS: Build Frontend` 楠岃瘉鏈疆甯冨眬琛ヤ竵銆?

## Status
- 2026-04-23: 杩涜涓€傚凡纭鏈疆鎺у埗璺緞鏄€滄湰椤靛叧閿垪鍋忕獎 + action/meta 瀹瑰櫒鍏佽鎹㈣鈥濓紝鍑嗗鍦?`pms-web/src/views/inspection/InspectionPlanView.vue`銆乣pms-web/src/views/handover/HandoverListView.vue`銆乣pms-web/src/views/contract/ContractAlertView.vue`銆乣pms-web/src/views/hospital/HospitalListView.vue` 鍋氬眬閮ㄥ垪瀹藉拰 nowrap 鏀跺彛锛岀劧鍚庢墽琛屽墠绔瀯寤洪獙璇併€?
- 2026-04-22: 宸插畬鎴愭渶缁堟埅鍥惧贰妫€鏀跺彛銆俙PersonnelListView` 鏉冮檺绠＄悊琛ㄥ彇娑?`fixed-right` 鎿嶄綔鍒楋紝瑙ｅ喅鐪熷疄鏁版嵁涓嬬殑鍒楀彔鍘嬩笌閫忓瓧闂锛涗娇鐢?admin 鏈夋晥浼氳瘽瀵?`dashboard`銆乣profile`銆乣permission/manage` 鍙婄紪杈戞娊灞夎繘琛?Playwright 瀹為〉楠岃瘉锛岃繍琛屾椂 `ISSUES=0`銆?

## Goal
- 褰诲簳淇鏈湴寮€鍙戠幆澧冧笅 PMS 鎵撲笉寮€涓旈〉闈㈢┖鐧界殑闂锛屾秷闄や粨搴撹縼绉诲悗 Vite 缂撳瓨鎸囧悜鏃х粷瀵硅矾寰勫鑷寸殑鍏ュ彛鍔犺浇澶辫触銆?

## Non-goals
- 涓嶄慨鏀逛换浣曚笟鍔℃帴鍙ｃ€佹潈闄愬绾︿笌椤甸潰鍔熻兘娴佺▼銆?
- 涓嶈皟鏁寸敓浜ч儴缃茶剼鏈笌鏈嶅姟绔彛閰嶇疆銆?

## Constraints
- 鍙樻洿鑼冨洿闄愬埗鍦ㄥ墠绔叆鍙ｃ€乂ite 鍚姩閾捐矾鍜屾湰鍦板紑鍙戦槻鍛嗐€?
- 姣忔鏀瑰姩鍚庡繀椤讳互鍙墽琛屽懡浠ら獙璇侊紝鑰屼笉鏄粎鐪嬩唬鐮併€?

## Steps
1. 淇 Vite 鍚姩鏃跺鏃т粨搴撹矾寰勭紦瀛樼殑鑷剤閫昏緫銆?
2. 涓哄墠绔寕杞戒笌搴旂敤澹冲垵濮嬪寲澧炲姞棣栧睆鍏滃簳锛岄伩鍏嶅紓甯哥洿鎺ヨ惤鎴愮┖鐧介〉銆?
3. 閲嶅惎鏈湴鍓嶇骞堕獙璇侀椤典笌 /src/main.ts 鍧囪繑鍥?200銆?
4. 鎵ц鍓嶇鏋勫缓楠岃瘉骞惰褰曠粨鏋溿€?

## Status
- 2026-04-22: 姝ラ 1-4 宸插畬鎴愩€傚凡淇杩佺Щ璺緞涓嬬殑 Vite stale cache / root 瑙ｆ瀽闂锛屽苟琛ラ綈棣栧睆鍒濆鍖栧厹搴曘€?

## Goal
- 鎻愬崌 PMS 鍓嶇鏁翠綋瑙傛劅锛屼紭鍏堟敼閫犲簲鐢ㄥ３涓庣櫥褰曢〉瑙嗚灞傛锛岃В鍐斥€滈〉闈㈠亸鏈寸礌鈥濋棶棰樸€?

## Non-goals
- 涓嶆敼鍔ㄤ换浣曚笟鍔℃帴鍙ｃ€佹潈闄愰€昏緫涓庤矾鐢辩粨鏋勩€?
- 涓嶈皟鏁撮〉闈㈠姛鑳戒氦浜掓祦绋嬨€?

## Constraints
- 淇濇寔鐜版湁 Vue 缁勪欢缁撴瀯锛屼紭鍏堥€氳繃鏍峰紡涓庤交閲忔ā鏉垮寮哄疄鐜般€?
- 淇濇寔绉诲姩绔彲鐢ㄦ€э紝涓嶅紩鍏ユ柊鐨勮繍琛屾椂渚濊禆銆?

## Steps
1. 缁熶竴鍏ㄥ眬璁捐浠ょ墝涓庡熀纭€瑙嗚锛堣壊鏉裤€佽儗鏅€佸崱鐗囥€佹寜閽€佸姩鏁堬級銆?
2. 浼樺寲 `AppLayout` 椤舵爮銆佷晶鏍忋€佷富鍐呭鍖哄眰娆″拰閫変腑鎬併€?
3. 浼樺寲 `LoginView` 鍝佺墝琛ㄨ揪銆佽〃鍗曡鎰熶笌棣栧睆璐ㄦ劅銆?
4. 鎵ц鍓嶇鏋勫缓楠岃瘉锛屽苟璁板綍缁撴灉銆?

## Status
- 2026-04-22: 宸插畬鎴愭楠?1-3銆?
- 2026-04-22: 姝ラ 4 宸叉墽琛岋紱鏋勫缓澶辫触锛屽師鍥犱负鐜版湁 Vite/Rolldown 閰嶇疆鍦ㄥ綋鍓嶈矾寰勪笅瑙﹀彂 `vite:build-html` 鐨?`fileName` 缁濆璺緞鎶ラ敊锛堜笌鏈鏍峰紡鏀瑰姩鏃犵洿鎺ュ叧绯伙級銆?

## Verification
- `powershell -ExecutionPolicy Bypass -File c:\Users\Administrator\PMS-Standalone\scripts\build-frontend.ps1` 宸查€氳繃锛宍vite build` 鎴愬姛浜у嚭 dist銆?
- 鏈湴鑱旇皟楠岃瘉宸查€氳繃锛氬墠绔?`http://127.0.0.1:5173/` 杩斿洖 200锛宍/src/main.ts` 杩斿洖 200锛屽悗绔?`http://127.0.0.1:5111/api/health` 杩斿洖 401锛堥壌鏉冩嫤鎴紝鏈嶅姟瀛樻椿锛夈€?

## Goal
- 灏?`tanweai/pua` 浠ラ」鐩骇鏂瑰紡鎺ュ叆 PMS锛屾敮鎸佸湪褰撳墠浠撳簱鍐呮墜鍔ㄨЕ鍙戜笌鎶€鑳借皟鐢ㄣ€?
- 涓嶈鐩栫幇鏈?`copilot-instructions.md`锛岄伩鍏嶅奖鍝嶆棦鏈夐」鐩鍒欍€?

## Non-goals
- 涓嶄慨鏀逛换浣曚笟鍔′唬鐮佷笌 API 濂戠害銆?
- 涓嶆浛鎹㈢幇鏈夊叏灞€ Copilot 鎸囦护鏂囦欢銆?

## Constraints
- 浠呮柊澧?AI 宸ュ叿閾鹃厤缃枃浠讹紙`.agents/`銆乣.github/prompts/`锛夈€?
- 閲囩敤涓婃父鏂囦欢鐩存嫹璐濓紝渚夸簬鍚庣画鍚屾鍗囩骇銆?

## Steps
1. 閫氳繃 SSH 鎷夊彇 `tanweai/pua` 浠撳簱鍒版湰鏈轰复鏃剁洰褰曘€?
2. 澶嶅埗 Codex 椤圭洰绾ф妧鑳芥枃浠跺埌 `.agents/skills/pua/SKILL.md`銆?
3. 澶嶅埗鎵嬪姩瑙﹀彂 prompt 鍒?`.agents/prompts/pua.md` 涓?`.github/prompts/pua.prompt.md`銆?
4. 楠岃瘉鏂囦欢瀛樺湪骞跺彲鍦ㄥ綋鍓嶄粨搴撲娇鐢ㄣ€?

## Status
- 2026-04-22: 宸插畬鎴愭楠?1-4銆?
- 2026-04-22: 宸叉寜瀹為檯浣跨敤浣撻獙鍘婚噸鍏ュ彛锛岀Щ闄?`.agents/prompts/pua.md` 涓?`.github/prompts/pua.prompt.md`锛屼粎淇濈暀 `.agents/skills/pua/SKILL.md`銆?

## Verification
- `.agents/skills/pua/SKILL.md` 宸茬敓鎴愶紙29106 bytes锛夈€?- 宸查獙璇佸綋鍓嶄粎淇濈暀涓€涓?pua 鍏ュ彛鏉ユ簮锛坰kill锛夈€?
## Goal
- 瀹℃煡 PMS 椤圭洰鍓嶅悗绔唬鐮侊紝瑕嗙洊鍚庣鏉冮檺/鏁版嵁鑼冨洿銆佽璇併€佸叧閿?API 濂戠害浠ュ強鍓嶇鐧诲綍鎬併€佽矾鐢卞拰鏋勫缓鐘舵€併€?
## Non-goals
- 涓嶄慨鏀逛笟鍔′唬鐮併€佷笉閲嶆瀯妯″潡銆佷笉璋冩暣鎺ュ彛濂戠害銆?- 涓嶄慨澶嶅鏌ヤ腑鍙戠幇鐨勯棶棰橈紝浠呰緭鍑洪闄╂竻鍗曞拰楠岃瘉缁撴灉銆?
## Constraints
- 閬靛畧鐜版湁鍚庣鍒嗗眰鍜屽墠绔洰褰曠害鏉熴€?- 淇濈暀褰撳墠宸ヤ綔鍖哄凡鏈夋敼鍔紝涓嶅洖婊?`pms-web/src/views/login/LoginView.vue` 鎴栨暟鎹簱鏂囦欢銆?
## Acceptance criteria
- 瀹℃煡杈撳嚭鍖呭惈鍚庣鍜屽墠绔彂鐜帮紝鎸夐闄╂帓搴忓苟鏍囨槑鏂囦欢/琛屽彿銆?- 瀹屾垚鍚庣鍜屽墠绔瀯寤洪獙璇侊紝璁板綍 `.slnx` 涓庝紶缁?`.sln` 鐨勯獙璇佸樊寮傘€?
## Steps
1. 闃呰椤圭洰 brief銆佹灦鏋勮鏄庛€佸綋鍓嶅疄鐜拌鍒掑拰杩涘害銆?2. 瀹℃煡璁よ瘉銆佹潈闄愭槧灏勩€佹暟鎹寖鍥磋繃婊ゃ€佹姤琛?瀹¤/API 鍏抽敭閾捐矾銆?3. 瀹℃煡鍓嶇璇锋眰灏佽銆佽矾鐢辨潈闄愩€佺櫥褰曟€佸拰鍏抽敭椤甸潰 API 璋冪敤銆?4. 鎵ц `dotnet build PMS.slnx`銆乣dotnet build PMS.sln` 涓?`npm run build`銆?5. 璁板綍瀹℃煡杩涘害涓庢渶缁堝彂鐜般€?
## Status
- 2026-05-27: 宸插畬鎴愬墠鍚庣瀹℃煡銆備富瑕佸彂鐜板寘鎷?`/api/reports` 鏉冮檺鏄犲皠缂哄け銆佽璇佺瀛愰€昏緫閲嶇疆鏀瑰瘑璐﹀彿銆侀儴鍒嗗尰闄㈣寖鍥翠负绌烘椂鍒楄〃/姹囨€诲洖閫€鍏ㄩ噺銆丠andover 鐪嬫澘鏈寜鏁版嵁鑼冨洿杩囨护銆佽嫢骞叉槑缁?鍐欐帴鍙ｇ己灏戠洰鏍囪褰曡寖鍥存牎楠屻€?
## Verification
- `dotnet build PMS.slnx` 澶辫触锛氬綋鍓?SDK 涓嶈瘑鍒?`.slnx` 鐨?`<Solution>` 鏍煎紡銆?- `dotnet build PMS.sln` 閫氳繃锛? warning, 0 error銆?- `cd pms-web && npm run build` 閫氳繃锛沄ite 鎶ュ憡涓€涓害 590KB 鐨?chunk size warning銆?
## Goal
- 灏?PMS 鍚勪笟鍔℃ā鍧楃晫闈㈢粺涓€鏀跺彛涓虹畝绾︺€佸共鍑€銆佷綆瑙嗚鍣煶鐨勫悗鍙颁骇鍝侀鏍硷紝閲嶇偣缁熶竴瀛椾綋銆佽璁′护鐗屻€佸崱鐗囥€佽〃鏍笺€佺瓫閫夊尯銆佹娊灞変笌寮圭獥浣撻獙銆?
## Non-goals
- 涓嶆敼鍚庣 API銆佹潈闄愭ā鍨嬨€佷笟鍔℃祦绋嬬姸鎬佽涔夈€?- 涓嶉噸鍐欏悇涓氬姟椤甸潰鐨勬暟鎹祦锛屼笉鍋氳法妯″潡淇℃伅鏋舵瀯澶ц縼绉汇€?- 涓嶆柊澧炶惀閿€寮忔彃鐢绘垨瑁呴グ鍥剧墖锛涘綋鍓嶆槸杩愮淮鍚庡彴锛屼紭鍏堢敤鍏嬪埗 UI 璇█缁熶竴銆?
## Constraints
- 涓昏鏀瑰姩闄愬埗鍦?`pms-web/src` 鐨勫叏灞€鏍峰紡涓庨€氱敤缁勪欢銆?- 淇濈暀 Element Plus 鍏煎锛屼笉鏂板杩愯鏃朵緷璧栥€?- 椤甸潰鎿嶄綔鍏ュ彛鍜岀幇鏈変笟鍔″姩浣滃繀椤讳繚鎸佸彲鐢ㄣ€?
## Acceptance criteria
- 鍏ㄧ珯鑳屾櫙銆佸瓧浣撱€佹寜閽€佽〃鏍笺€佺瓫閫夈€佸崱鐗囥€佸脊绐?鎶藉眽瑙嗚璇█缁熶竴銆?- 閬垮厤缁х画鍙犲姞娓愬彉銆佸厜鏅曘€佹诞鍔ㄩ槾褰辩瓑瑙嗚鍣煶銆?- 鑷冲皯澶勭悊涓€涓槑鏄句笉涓€鑷寸殑鍘熺敓 `el-drawer` 浣跨敤鐐广€?- 鍓嶇鏋勫缓閫氳繃銆?
## Steps
1. 鏂板缁熶竴 clean UI 瑕嗙洊灞傦紝鏀惧湪鐜版湁鍏ㄥ眬鏍峰紡涔嬪悗鍔犺浇锛岄泦涓敹鍙ｈ璁′护鐗屽拰 Element Plus 缁勪欢瑙嗚銆?2. 璋冩暣 `ProTable`銆乣AppTableCard`銆乣AppFormDialog`銆乣ProDrawer` 鐨勫眬閮ㄦ牱寮忥紝閬垮厤涓庡叏灞€鏍峰紡浜掔浉鎵撴灦銆?3. 灏?`MajorDemandView` 鐨勫師鐢?`el-drawer` 鏀逛负缁熶竴 `ProDrawer`銆?4. 鎵ц `pms-web` 鍓嶇鏋勫缓楠岃瘉锛屽苟璁板綍杩涘害銆?
## Status
- 2026-05-27: 宸插畬鎴愭湰杞墠绔?UI 缁熶竴鏀跺彛銆傛柊澧?`clean-ui.css` 浣滀负鏈€鍚庡姞杞界殑缁熶竴瑕嗙洊灞傦紝鏀舵暃瀛椾綋銆佽儗鏅€侀鑹层€佸渾瑙掋€侀槾褰便€佹寜閽€佺瓫閫夊尯銆佽〃鏍笺€佸崱鐗囥€佸脊绐楀拰鎶藉眽锛涘悓姝ヨ皟鏁?`ProTable`銆乣AppTableCard`銆乣AppFormDialog`銆乣ProDrawer`锛屽苟灏嗛噸澶ч渶姹傝鎯呬粠鍘熺敓 `el-drawer` 鏀逛负 `ProDrawer`銆?
## Verification
- `cd pms-web && npm run build` 閫氳繃锛沄ite 浠嶆彁绀烘棦鏈夌害 590KB chunk size warning銆?- 鏈湴 Vite `http://127.0.0.1:5173/` 杩斿洖 200銆?- Playwright headless 鐧诲綍 `admin/123456` 鍚庤闂?`/major-demand/list`锛岀‘璁ら〉闈㈤潪绌恒€乣--pms-primary=#2563eb` 涓?`--pms-bg=#f5f7fa` 鐢熸晥锛岃〃鏍煎崱鐗囧急闃村奖鍜屽皬鍦嗚鐢熸晥锛岃鎯呮娊灞夌敱 `.pro-drawer` 娓叉煋銆?
## Goal
- 寮哄寲 PMS 鍓嶇璺ㄦā鍧楁暟鎹仈鍔ㄥ拰涓撲笟鍖栦氦浜掕川鎰燂細浠讳竴妯″潡鍐欐搷浣滄垚鍔熷悗锛岀浉鍏虫ā鍧椼€侀椤点€侀璀︺€佹姤琛ㄧ瓑渚濊禆瑙嗗浘鑳借嚜鍔ㄦ敹鍒板埛鏂颁俊鍙凤紝鍑忓皯鈥滄敼涓€澶勫叾浠栧湴鏂逛笉鍔ㄢ€濈殑鍓茶鎰熴€?
## Non-goals
- 涓嶆敼鍙樺悗绔?API 濂戠害銆佷笉鏂板鍚庣鎺ュ彛銆佷笉鏀逛笟鍔＄姸鎬佹満璇箟銆?- 涓嶄竴娆℃€ч噸鍐欐墍鏈夐〉闈㈡暟鎹祦锛涙湰杞厛钀藉湴缁熶竴鍙樻洿骞挎挱鍜屼緷璧栧埛鏂板熀纭€璁炬柦銆?- 涓嶅紩鍏ユ柊鐨勭姸鎬佺鐞嗗簱鎴栬繍琛屾椂渚濊禆銆?
## Constraints
- 淇濇寔鐜版湁 Vue 3 + Element Plus 鏋舵瀯锛屼紭鍏堝鐢?`useLinkedRealtimeRefresh`銆?- 閬垮厤椤甸潰鍒濆鍔犺浇閲嶅璇锋眰椋庢毚锛屼簨浠跺埛鏂伴渶瑕佽妭娴?鍘婚噸銆?- 淇濇寔鐜版湁鎵嬪姩 `notifyDataChanged` 璋冪敤鍏煎銆?
## Acceptance criteria
- 鍐欐帴鍙ｆ垚鍔熷悗鑳藉鑷姩骞挎挱妯″潡鏁版嵁鍙樻洿銆?- 椤甸潰榛樿鐩戝惉鏁版嵁鍙樻洿浜嬩欢锛屼笉鍐嶅洜涓?`enableAutoRefresh=false` 鑰屽け鏁堛€?- 鏀寔妯″潡渚濊禆鎵╂暎锛屼緥濡傞」鐩?鍖婚櫌/浜哄憳/宸ユ椂/鎶ヤ慨鍙樻洿浼氬埛鏂伴椤点€侀璀︽垨鎶ヨ〃绛夊叧鑱旇鍥俱€?- 瑙嗚涓婅繘涓€姝ュ帇浣?hero 娓愬彉鍜岃楗板櫔闊筹紝闈犳暟鎹崱鐗囧拰娓呮櫚灞傜骇鍛堢幇涓撲笟鍚庡彴鎰熴€?- 鍓嶇鏋勫缓閫氳繃锛屽苟瀹屾垚鑷冲皯涓€涓笟鍔￠〉鑱斿姩楠岃瘉銆?
## Steps
1. 鏂板鍓嶇鏁版嵁鍚屾宸ュ叿锛岄泦涓淮鎶?API 璺緞鍒颁笟鍔?scope 鐨勬槧灏勩€乻cope 渚濊禆鎵╂暎鍜屾祻瑙堝櫒浜嬩欢骞挎挱銆?2. 鏀归€?`useLinkedRealtimeRefresh`锛氶粯璁ょ洃鍚暟鎹彉鏇翠簨浠讹紝淇濈暀瀹氭椂/鑱氱劍鍒锋柊寮€鍏筹紝骞跺姞鍏ュ埛鏂板幓閲嶃€?3. 鍦?Axios 鍝嶅簲鎷︽埅鍣ㄤ腑瀵规垚鍔熷啓鎿嶄綔鑷姩骞挎挱鍙樻洿锛岃鐩?POST/PUT/PATCH/DELETE銆?4. 琛ュ厖 `clean-ui.css` 瀵规ā鍧?hero/淇″彿鍗?鎺у埗鍗＄殑涓撲笟鍖栨敹鍙ｏ紝鍑忓皯澶ч潰绉笎鍙樺拰寮鸿楗般€?5. 鎵ц鍓嶇鏋勫缓涓庢湰鍦伴〉闈㈤獙璇侊紝骞惰褰曡繘搴︺€?
## Status
- 2026-05-27: 宸插畬鎴愭湰杞暟鎹仈鍔ㄥ拰涓撲笟鍖栬瑙夋敹鍙ｃ€傛柊澧?`dataSync.ts` 闆嗕腑缁存姢鍙樻洿浜嬩欢銆丄PI 鍐欐搷浣?scope 鎺ㄦ柇鍜岃法妯″潡渚濊禆鎵╂暎锛涙敼閫?`useLinkedRealtimeRefresh` 浣块〉闈㈤粯璁ょ洃鍚彉鏇翠簨浠跺苟鑺傛祦鍒锋柊锛涘湪 Axios 鍝嶅簲鎷︽埅鍣ㄤ腑瀵规垚鍔熷啓鎿嶄綔鑷姩骞挎挱鍙樻洿锛涜ˉ鍏?`clean-ui.css` 瀵规ā鍧?hero 涓庝俊鍙峰崱鐨勪綆鍣煶瑕嗙洊銆?
## Verification
- `cd pms-web && npm run build` 閫氳繃锛沄ite 浠嶆彁绀烘棦鏈夌害 593KB chunk size warning銆?- 鏂板紑骞插噣 Vite 瀹炰緥 `http://127.0.0.1:5174/` 杩斿洖 200銆?- Playwright headless 鐧诲綍 `admin/123456` 鍚庤闂?`/dashboard`锛屾ā鎷?`workhours` 鏁版嵁鍙樻洿浜嬩欢锛岄椤佃嚜鍔ㄦ柊澧炶姹?`/api/projects`銆乣/api/alerts/center`銆乣/api/annual-reports/summary`銆乣/api/dashboard/v2`銆?- 鍚屼竴楠岃瘉涓‘璁?`clean-ui.css` 瑙勫垯宸插姞杞斤紝棣栭〉 hero 鑳屾櫙涓虹櫧搴曠嚎鎬у眰銆佹枃瀛椾负娣辫壊銆佸渾瑙掍负 10px銆侀槾褰变负寮遍槾褰便€?
## Goal
- 缁熶竴 PMS 鍚勬ā鍧楀垎椤垫帶浠惰瑙夛紝璁╅〉鐮併€佹€绘暟銆佹瘡椤垫潯鏁般€佽烦杞緭鍏ュ拰鍓嶅悗缈婚〉鎸夐挳绗﹀悎绠€绾︺€佷笓涓氥€佷綆鍣煶鍚庡彴椋庢牸銆?
## Non-goals
- 涓嶄慨鏀瑰垎椤垫暟鎹€昏緫銆侀粯璁?page size銆佹帴鍙ｅ弬鏁版垨琛ㄦ牸缁勪欢濂戠害銆?- 涓嶉€愰〉鏇挎崲 `el-pagination` 妯℃澘銆?
## Constraints
- 鍙湪鏈€缁堝叏灞€瑕嗙洊灞?`clean-ui.css` 涓鐞嗭紝閬垮厤缁х画澧炲姞椤甸潰绾ф牱寮忓啿绐併€?- 蹇呴』鍏煎宸叉湁 `background` 妯″紡鍜屾櫘閫?`el-pagination` 鐢ㄦ硶銆?- 绉诲姩绔渶瑕佸彲妯悜婊氬姩锛屼笉鎸ゅ帇琛ㄦ牸甯冨眬銆?
## Acceptance criteria
- 鍏ㄧ珯鍒嗛〉鎸夐挳灏哄銆侀棿璺濄€佽竟妗嗐€佹縺娲绘€併€佺鐢ㄦ€佺粺涓€銆?- 鍘婚櫎鏃ф牱寮忛噷鐨勬笎鍙樸€佷笂娴€侀噸闃村奖銆?- 姣忛〉鏉℃暟閫夋嫨鍣ㄣ€佽烦杞緭鍏ャ€佹€绘暟瀛椾綋涓庢暣浣?UI 鍙橀噺涓€鑷淬€?- 鍓嶇鏋勫缓閫氳繃锛屽苟閫氳繃椤甸潰瀹炴祴纭鍒嗛〉鏍峰紡鐢熸晥銆?
## Steps
1. 鎵╁睍 `clean-ui.css` 鐨勫垎椤垫渶缁堣鐩栬鍒欍€?2. 鎵ц鍓嶇鏋勫缓銆?3. 鐢?Playwright 妫€鏌ヨ嚦灏戜竴涓湁鍒嗛〉鐨勪笟鍔￠〉锛岀‘璁?computed style 鐢熸晥銆?
## Status
- 2026-05-27: 宸插畬鎴愬垎椤电粺涓€鏀跺彛銆俙clean-ui.css` 鐜板湪瑕嗙洊 `.pager`銆乣.el-pagination`銆侀〉鐮併€佷笂涓€椤?涓嬩竴椤点€佹€绘暟銆佹瘡椤垫潯鏁伴€夋嫨鍣ㄣ€佽烦杞緭鍏ャ€佺鐢ㄦ€佸拰绉诲姩绔í鍚戞粴鍔紝鍘婚櫎鏃у垎椤电殑娓愬彉銆佷笂娴拰閲嶉槾褰便€?
## Verification
- `cd pms-web && npm run build` 閫氳繃锛沄ite 浠嶆彁绀烘棦鏈夌害 593KB chunk size warning銆?- Playwright headless 鐧诲綍 `admin/123456` 鍚庤闂?`http://127.0.0.1:5174/project/list`锛岀‘璁ゅ垎椤佃鍒欏凡鍔犺浇锛屾櫘閫氶〉鐮佸拰婵€娲婚〉鐮佸潎涓?32px 楂樸€?px 鍦嗚銆佹棤闃村奖鏃犱笂娴紱婵€娲绘€佷负 `#2563eb`锛涙€绘暟鍜屾瘡椤垫潯鏁伴€夋嫨鍣ㄦ牱寮忕粺涓€銆?
## Goal
- 淇 Dashboard 閲嶅ぇ闇€姹傛槑缁嗕腑鍖婚櫌/瀹㈡埛銆佽礋璐ｄ汉銆佽鍒掑畬鎴愮瓑瀛楁鏄剧ず `--` 鐨勯棶棰橈紝骞朵妇涓€鍙嶄笁缁熶竴閲嶅ぇ闇€姹傚師濮嬪鍏ュ垪鐨勫瓧娈佃В鏋愯鍒欍€?
## Non-goals
- 涓嶄慨鏀归噸澶ч渶姹傚悗绔帴鍙ｅ绾︼紝涓嶈縼绉绘暟鎹紝涓嶆敼鍙?workflow 鐘舵€佹満銆?- 涓嶈皟鏁村浘琛ㄧ粨鏋勩€佽矾鐢辩粨鏋勬垨鏉冮檺閫昏緫銆?
## Constraints
- 瀛楁瑙ｆ瀽搴斿吋瀹?Excel 鍘熷鍒楀悕銆佸巻鍙插鍏ュ垪鍚嶅拰 `_RowId`銆?- 涓嶈兘鎶?`XXXX`銆乣--`銆乣鏈缃甡 绛夊崰浣嶅€煎綋鎴愭湁鏁堜笟鍔″瓧娈点€?- Dashboard 鍜岄噸澶ч渶姹傚垪琛ㄩ〉搴斿叡鐢ㄥ悓涓€濂楄В鏋愰€昏緫锛岄伩鍏嶄笅娆″瓧娈靛悕鍙樺寲鏃跺啀娆＄┖鐧姐€?
## Acceptance criteria
- Dashboard 閲嶅ぇ闇€姹傛槑缁嗚兘鏄剧ず鐪熷疄鍖婚櫌銆佽礋璐ｄ汉銆侀渶姹傛爣棰樸€?- 閲嶅ぇ闇€姹傚垪琛ㄩ〉璐熻矗浜鸿兘浠?`鏈嶅姟浜哄憳` 绛夊師濮嬪垪鍏滃簳灞曠ず銆?- 琛岃烦杞粛浣跨敤姝ｇ‘ rowId銆?- 鍓嶇鏋勫缓閫氳繃锛屽苟閫氳繃瀹為〉楠岃瘉銆?
## Steps
1. 鏂板 `majorDemandFields.ts`锛岄泦涓В鏋?rowId銆侀渶姹傛爣棰樸€佸尰闄?瀹㈡埛銆佽礋璐ｄ汉銆佽鍒掑畬鎴愩€佺姸鎬併€?2. 鏀归€?`DashboardView` 閲嶅ぇ闇€姹?drill 鏁版嵁婧愶紝鏀寔 `_RowId` 骞朵娇鐢ㄨВ鏋愬伐鍏枫€?3. 鏀归€?`MajorDemandView` 鐨?owner/filter/displayRows锛屼娇鐢ㄨВ鏋愬伐鍏蜂綔涓?workflow 瀛楁鍏滃簳銆?4. 鏋勫缓楠岃瘉骞剁敤 Playwright 妫€鏌?Dashboard 涓庨噸澶ч渶姹傚垪琛ㄩ〉瀛楁涓嶅啀绌虹櫧銆?
## Status
- 2026-05-27: 宸插畬鎴愩€傛柊澧為噸澶ч渶姹傚瓧娈佃В鏋愬伐鍏凤紝Dashboard 閲嶅ぇ闇€姹?drill 鏀逛负閫氳繃 `_RowId` 鍏宠仈鍘熷琛屽苟缁熶竴瑙ｆ瀽鍖婚櫌/瀹㈡埛銆佽礋璐ｄ汉銆佹爣棰樸€佺姸鎬佸拰璁″垝瀹屾垚锛涢噸澶ч渶姹傚垪琛ㄩ〉鐨勮礋璐ｄ汉绛涢€変笌灞曠ず鍚屾浣跨敤鍚屼竴濂楀厹搴曡鍒欙紝閬垮厤 workflow 瀛楁涓虹┖鏃堕〉闈㈠啀娆℃樉绀虹┖鐧姐€?
## Verification
- `cd pms-web && npm run build` 閫氳繃锛沄ite 浠嶆彁绀烘棦鏈夌害 593KB chunk size warning銆?- `dotnet build PMS.slnx` 澶辫触锛氬綋鍓?.NET SDK/MSBuild 涓嶈瘑鍒?`.slnx` 鐨?`<Solution>` 鏍煎紡銆?- `dotnet build PMS.sln` 閫氳繃锛? warning, 0 error銆?- Playwright headless 鐧诲綍 `admin/123456` 鍚庤闂?`http://127.0.0.1:5174/dashboard`锛屽垏鍒伴噸澶ч渶姹傛爣绛撅紝纭棣栧睆琛屾樉绀?`姝︽眽鍎跨鍖婚櫌`銆乣鑸掗珮鎴恅銆乣浼犳煋鐥呬笂鎶锛屼笉鍐嶆槸鍖婚櫌/璐熻矗浜哄叏 `--`銆?- Playwright headless 璁块棶 `http://127.0.0.1:5174/major-demand/list`锛岀‘璁ゅ垪琛ㄨ礋璐ｄ汉鍙粠鍘熷 `鏈嶅姟浜哄憳` 鍒楀厹搴曟樉绀猴紝濡?`鑸掗珮鎴恅銆乣瀛熷崜`銆乣鏋楀嘲`銆?
## Goal
- 寮哄寲鍚勬ā鍧楅《閮ㄥ伐浣滃彴/hero 鐨勪笓涓氬寲瑙嗚鍖哄垎锛岃棣栧睆椤跺尯鏄庢樉鎵挎媴鈥滄ā鍧楀伐浣滃彴鈥濊鑹诧紝鑰屼笉鏄笌鏅€氬唴瀹瑰崱鐗囨贩鍦ㄤ竴璧枫€?
## Non-goals
- 涓嶉€愰〉閲嶅啓椤甸潰妯℃澘锛屼笉鏀瑰彉涓氬姟鍏ュ彛銆佺瓫閫夐€昏緫銆佹寜閽姩浣滄垨鍚庣 API銆?- 涓嶆柊澧炲浘鐗囥€佹彃鐢绘垨瑁呴グ鎬ц祫婧愩€?
## Constraints
- 浼樺厛鍦ㄦ渶缁堝叏灞€瑕嗙洊灞?`clean-ui.css` 涓敹鍙ｏ紝閬垮厤缁х画鍒堕€犻〉闈㈢骇鏍峰紡鍒嗗弶銆?- 淇濇寔绠€绾︺€佸共鍑€銆佷綆瑙嗚鍣煶锛屼娇鐢ㄧ粨鏋勩€佽竟妗嗐€佹祬鑹插垎鍖哄拰妯″潡鑹叉爣鍖哄垎锛屼笉鍥炲埌澶ч潰绉己娓愬彉銆?- 闇€瑕佽鐩栧凡鏈?hero 绫诲拰浠嶄娇鐢?`.page-head` 鐨勬櫘閫氭ā鍧楅《閮ㄣ€?
## Acceptance criteria
- 椤堕儴宸ヤ綔鍙颁笌涓嬫柟琛ㄦ牸/绛涢€夊尯鏈夋竻鏅拌瑙夎竟鐣屻€?- 宸︿晶鏍囬鍖恒€佹寚鏍囧尯銆佸彸渚у姩浣滃尯灞傜骇娓呮锛屾枃瀛椾笉鍐嶅洜涓虹櫧搴?浣庡姣旇€屽彂娣°€?- project/contract/alert/inspection/handover/map/kpi/report/dashboard 绛?hero 鐨勬帶鍒跺崱銆佸揩鎹峰姩浣滃崱椋庢牸缁熶竴銆?- 鍓嶇鏋勫缓閫氳繃锛屽苟瀹炴祴鑷冲皯宸℃绠＄悊椤甸《閮ㄥ伐浣滃彴鏍峰紡鐢熸晥銆?
## Steps
1. 鎵╁睍 `clean-ui.css` 鐨勯《閮ㄥ伐浣滃彴瑙勫垯锛岃ˉ榻愬悇妯″潡 hero銆乻ignal銆乧ontrol銆乹uick action 鐨勭粺涓€瑕嗙洊銆?2. 瀵?`.page-head` 澧炲姞杞婚噺宸ヤ綔鍙拌竟鐣屽拰鏍囬灞傜骇锛岃鐩栨湭鍗囩骇 hero 鐨勬ā鍧椼€?3. 鏋勫缓骞剁敤 Playwright 楠岃瘉宸℃绠＄悊椤甸《閮ㄥ伐浣滃彴瀵规瘮搴︺€佽竟鐣屽拰鍔ㄤ綔鍖烘牱寮忋€?
## Status
- 2026-05-27: 宸插畬鎴愩€俙clean-ui.css` 宸茬粺涓€鍚勬ā鍧楅《閮ㄥ伐浣滃彴鐨勬ā鍧楄壊鏍囥€佹祬鑹茶儗鏅€侀《閮ㄥ己璋冪嚎銆佹寚鏍囧崱宸︿晶鑹叉爣銆佸彸渚у姩浣滃尯鍒嗘爮銆乧ontrol 鍗″拰 quick action 鍗★紱鍚屾缁欎粛浣跨敤 `.page-head` 鐨勬櫘閫氭ā鍧楀鍔犺交閲忓伐浣滃彴杈圭晫銆?
## Verification
- `cd pms-web && npm run build` 閫氳繃锛沄ite 浠嶆彁绀烘棦鏈夌害 593KB chunk size warning銆?- `dotnet build PMS.slnx` 澶辫触锛氬綋鍓?.NET SDK/MSBuild 涓嶈瘑鍒?`.slnx` 鐨?`<Solution>` 鏍煎紡銆?- `dotnet build PMS.sln` 閫氳繃锛? warning, 0 error銆?- Playwright headless 鐧诲綍 `admin/123456` 鍚庤闂?`http://127.0.0.1:5174/inspection/plan`锛岀‘璁?`.inspection-hero` 浣跨敤宸℃妯″潡鑹叉爣銆佸彸渚у姩浣滃尯鏈夋祬鑹插垎鏍忥紝quick action 鏍囬涓烘繁鑹层€佽鏄庝负鐏拌壊锛屼笉鍐嶅嚭鐜扮櫧搴曚綆瀵规瘮銆?- Playwright headless 璁块棶 `http://127.0.0.1:5174/product/list`锛岀‘璁ゆ櫘閫?`.page-head` 鍏峰 4px 宸︿晶涓昏壊杈圭晫銆佹祬鑹茶儗鏅拰缁熶竴鍦嗚闃村奖銆?
## Goal
- Upgrade `pms-web/src/views/workhours/WorkHoursView.vue` from a plain list page into an operations-oriented work-hours service desk.

## Non-goals
- Do not change backend work-hours contracts, workflow APIs, or access-control logic in this task.
- Do not redesign unrelated operations modules in the same patch.

## Constraints
- Keep existing work-hours search, export, create, edit, submit, confirm, reject, delete, detail, and route-deep-link behavior available.
- Scope implementation to `pms-web/src/views/workhours/WorkHoursView.vue` plus memory-bank records only.
- Keep the page aligned with the current clean-ui language and avoid introducing new frontend dependencies.

## Acceptance criteria
- The work-hours page shows a clear first-screen service desk with hero signals, quick actions, insight cards, and the existing summary/table workflow below.
- Overview cards stay synchronized with search, reset, type filtering, manual refresh, route query changes, and record mutations.
- Detail deep links with `action=detail` continue to work.
- `cd pms-web && npm run build` passes, and the logged-in page is checked in browser.

## Steps
1. Add a filtered overview data load alongside the existing paged table.
2. Rebuild the first screen into a work-hours service desk with signals, quick actions, and insight cards tied to the current filter state.
3. Keep route query sync, detail deep links, and mutation refresh behavior consistent across overview and table areas.
4. Build and verify in browser, then record progress.

## Status
- 2026-06-04: Completed the `WorkHoursView` service-desk upgrade. The page now loads filtered overview data in parallel with the paged table, adds a hero work area, quick actions, review queue, personnel/hospital/type insight cards, extends the detail drawer with product and delivery fields, and keeps overview/table state synchronized across search, reset, type filtering, route deep links, and mutation refresh.
- 2026-06-04: Continued the same slice by aligning work-hours summary metrics with the current filter scope. `/api/workhours/summary` now accepts the same personnel/hospital/date/type filters and access scope as the list query, and `WorkHoursView.vue` refreshes summary, overview, and table data together so first-screen cards no longer drift from the current result set.

## Goal
- Upgrade `pms-web/src/views/handover/HandoverListView.vue` from a workflow-heavy list page into a clearer handover service desk for operations supervisors.

## Non-goals
- Do not change handover backend contracts, workflow actions, or kanban APIs in this task.
- Do not redesign unrelated operations modules in the same patch.

## Constraints
- Keep existing search, export, detail drawer, workflow actions, route deep links, and kanban behavior intact.
- Scope implementation to `pms-web/src/views/handover/HandoverListView.vue` plus memory-bank records only.
- Make first-screen insights derive from the currently filtered dataset instead of introducing a second conflicting business口径.

## Acceptance criteria
- The handover page shows a clearer first-screen service desk with insight cards for queue priority, source groups, owner load, and batch/type structure.
- Hero signals, summary metrics, and quick actions stay aligned with the current filtered result set.
- Existing detail, workflow, and kanban actions continue to work.
- The live Vite module compiles and the page is checked in browser.

## Steps
1. Reuse the existing handover full dataset load to derive a filter-scoped overview model.
2. Add first-screen insight cards without removing the current hero, summary strip, detail drawer, or kanban workflow.
3. Validate the runtime module and page behavior, then record progress.

## Status
- 2026-06-05: Completed the `HandoverListView` service-desk upgrade. The page now derives a filter-scoped overview from the existing full handover dataset, uses that scope for first-screen summary values, and adds four insight cards for priority queue, source group distribution, owner load, and batch/type structure while preserving export, detail, workflow, and kanban behavior.

## Goal
- Upgrade `pms-web/src/views/major-demand/MajorDemandView.vue` from a plain workflow list into a clearer operations-first major-demand service desk.

## Non-goals
- Do not change the major-demand backend contracts, import structure, or workflow/comment APIs in this task.
- Do not redesign unrelated operations modules in the same patch.

## Constraints
- Keep existing filter form, batch actions, table operations, detail drawer, comment flow, edit flow, and export behavior intact.
- Scope implementation to `pms-web/src/views/major-demand/MajorDemandView.vue` plus memory-bank records only.
- Reuse the current `displayRows` filtered dataset as the single source of truth for first-screen insights.

## Acceptance criteria
- The major-demand page shows a clear first-screen service desk with hero signals, quick actions, summary cards, and four insight cards.
- First-screen values stay aligned with the current filtered result set and react to quick actions and summary selection.
- Existing major-demand workflow, edit, comment, and export behaviors remain available.
- The live Vite module compiles and the page is checked in browser.

## Steps
1. Reuse `displayRows` to derive major-demand overview signals, ranking queues, and distribution buckets.
2. Add a service-desk first screen ahead of the existing filter/table workflow without breaking actions.
3. Validate the runtime module and page behavior, then record progress.

## Status
- 2026-06-05: Completed the `MajorDemandView` service-desk upgrade. The page now adds a hero workspace, quick actions, summary cards, and four insight cards for the priority queue, hospital distribution, owner load, and status structure, all derived from the existing filter-scoped `displayRows` dataset while preserving batch actions, workflow actions, detail, comment, edit, and export flows.

## Goal
- Upgrade `pms-web/src/views/alert/AlertCenterView.vue` from a summary-first list page into a clearer alert command desk for operations users.

## Non-goals
- Do not change alert-center backend contracts, alert aggregation rules, or export behavior in this task.
- Do not redesign unrelated operations modules in the same patch.

## Constraints
- Keep existing filter, export, pagination, and row jump behavior intact.
- Scope implementation to `pms-web/src/views/alert/AlertCenterView.vue` plus memory-bank records only.
- Reuse the current `/alerts/center` endpoint and derive first-screen insight cards from the same filtered dataset as the list.

## Acceptance criteria
- The alert center shows a clearer first-screen service desk with summary cards plus four insight cards for queue priority, hospital concentration, owner load, and source structure.
- First-screen values stay aligned with the current filter state and react to quick actions and summary selection.
- Existing row navigation and export behavior remain available.
- The live Vite module compiles and the page is checked in browser.

## Steps
1. Reuse the current alert-center query to load a filtered overview dataset alongside the paged table.
2. Add insight cards and move summary metrics onto the shared `SummaryMetrics` component while preserving filters and row jumps.
3. Validate the runtime module and page behavior, then record progress.

## Status
- 2026-06-05: Completed the `AlertCenterView` service-desk upgrade. The page now keeps the existing hero and filter flow, switches summary metrics onto `SummaryMetrics`, loads a filtered overview dataset from the same `/alerts/center` endpoint, and adds four insight cards for the priority queue, hospital concentration, owner load, and source structure while preserving export, pagination, and row jump behavior.

## Goal
- Upgrade `pms-web/src/views/inspection/InspectionPlanView.vue` into a clearer inspection command desk while preserving both the plan and result workflows.

## Non-goals
- Do not change inspection backend contracts, workflow actions, imports, or route semantics in this task.
- Do not redesign unrelated operations modules in the same patch.

## Constraints
- Keep the existing hero, tabs, filter forms, plan/result actions, detail drawers, deep links, and upload/export flows intact.
- Scope implementation to `pms-web/src/views/inspection/InspectionPlanView.vue` plus memory-bank records only.
- Derive the new first-screen insights from the existing filtered plan dataset and a filtered full result dataset so summary cards and insight cards stay on the same business口径.

## Acceptance criteria
- The inspection page shows four first-screen insight cards in the plan tab and four in the result tab.
- Hero signals, summary metrics, and quick actions stay aligned with the active filtered plan/result scope.
- Existing plan/result workflow actions and route-driven tab switching continue to work.
- The live Vite module compiles and the page is checked in browser.

## Steps
1. Reuse the current plan full dataset plus a filtered full result dataset to derive queue, distribution, and structure insights.
2. Add plan/result insight cards without removing the current hero, summary strip, filters, or action flows.
3. Validate the runtime module and browser behavior, then record progress.

## Status
- 2026-06-05: Completed the `InspectionPlanView` service-desk upgrade. The page now adds four plan insight cards and four result insight cards, aligns hero signals and summary cards with the filtered plan/result datasets, and expands `loadResults()` to fetch a filtered full result set for the result-side overview while preserving plan/result actions, tabs, route query behavior, uploads, and export flow.

## Goal
- Upgrade `pms-web/src/views/contract/ContractAlertView.vue` into a clearer contract-risk service desk for operations users.

## Non-goals
- Do not change contract-alert backend contracts, export format, or project drill-down behavior in this task.
- Do not redesign unrelated operations modules in the same patch.

## Constraints
- Keep the existing hero, filter form, export behavior, and project jump flow intact.
- Scope implementation to `pms-web/src/views/contract/ContractAlertView.vue` plus memory-bank records only.
- Reuse the existing `/contracts/alerts` endpoint and derive first-screen insights from the same filtered dataset as the table.

## Acceptance criteria
- The contract alert page shows a clearer first-screen service desk with four insight cards for priority queue, hospital concentration, service-group load, and validity/level structure.
- Hero signals and summary metrics stay aligned with the active filtered dataset.
- Existing filter, export, and row jump behavior remain available.
- The live Vite module compiles and the page is checked in browser.

## Steps
1. Load a filtered full contract-alert dataset alongside the paged table and use it as the single source of truth for first-screen metrics.
2. Add insight cards ahead of the existing table flow without removing current controls.
3. Validate runtime compilation and browser behavior, then record progress.

## Status
- 2026-06-05: Completed the `ContractAlertView` service-desk upgrade. The page now loads a filtered full alert dataset alongside the paged table, derives summary cards and hero signals from that shared scope, and adds four insight cards for the priority queue, hospital concentration, service-group load, and validity/level structure while preserving filter, export, and project jump behavior.

## Goal
- Upgrade `pms-web/src/views/dashboard/DashboardView.vue` into a clearer manager command desk that surfaces global risk, routing signals, and module coordination ahead of the chart layer.

## Non-goals
- Do not change dashboard backend contracts, chart drill-down behavior, or operator/supervisor/regional manager routing in this task.
- Do not redesign unrelated modules in the same patch.

## Constraints
- Keep the existing hero, metrics, operations task panel, chart sections, and drill tables intact.
- Scope implementation to `pms-web/src/views/dashboard/DashboardView.vue` plus memory-bank records only.
- Reuse the current dashboard, alert-center, project, annual-report, major-demand, and operations-task data already loaded by the page.

## Acceptance criteria
- The manager dashboard shows a clearer first-screen command layer with four insight cards for priority queue, risk-source structure, regional focus, and closure pressure.
- First-screen insight values stay aligned with the same live datasets already used by the dashboard and operations task panel.
- Existing chart drill-down and quick-action navigation remain available.
- The live Vite module compiles and the page is checked in browser.

## Steps
1. Reuse the currently loaded dashboard datasets to derive a global insight layer above the task panel and chart grid.
2. Add the insight cards and supporting click-through behavior without removing the current charts or drill tables.
3. Validate runtime compilation and browser behavior, then record progress.

## Status
- 2026-06-05: Completed the `DashboardView` command-desk upgrade. The page now adds a first-screen insight layer for the operations priority queue, risk-source structure, regional focus, and closure pressure using the same live datasets already loaded by the manager dashboard, while preserving the hero, KPI cards, operations task panel, charts, and drill tables.
