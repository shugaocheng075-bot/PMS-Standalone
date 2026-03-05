# PMS-Web UI/UX 一致性审计报告

> **审计范围**: `src/views/` 下全部 18 个 Vue 视图文件  
> **参照基准**: `src/style.css`（686 行全局样式）  
> **审计日期**: 2026-01

---

## 目录

1. [总览矩阵](#1-总览矩阵)
2. [审计标准 1 — 页面骨架结构](#2-审计标准-1--页面骨架结构)
3. [审计标准 2 — 筛选区域](#3-审计标准-2--筛选区域)
4. [审计标准 3 — 表格样式](#4-审计标准-3--表格样式)
5. [审计标准 4 — 分页器](#5-审计标准-4--分页器)
6. [审计标准 5 — 弹窗/抽屉](#6-审计标准-5--弹窗抽屉)
7. [审计标准 6 — 按钮风格](#7-审计标准-6--按钮风格)
8. [审计标准 7 — 动画/过渡](#8-审计标准-7--动画过渡)
9. [审计标准 8 — 滚动条处理](#9-审计标准-8--滚动条处理)
10. [审计标准 9 — 统计卡片](#10-审计标准-9--统计卡片)
11. [审计标准 10 — 页面专属 CSS](#11-审计标准-10--页面专属-css)
12. [Composable 使用一致性](#12-composable-使用一致性)
13. [高优先级问题汇总](#13-高优先级问题汇总)
14. [修复建议](#14-修复建议)

---

## 1. 总览矩阵

| # | 文件 | 行数 | 类型 | 骨架 | 筛选 | 表格 | 分页 | 弹窗 | 按钮 | 统计卡 | scoped CSS |
|---|------|------|------|------|------|------|------|------|------|--------|------------|
| 1 | DashboardView | ~290 | 仪表盘 | ✅ | ❌ | ✅ | ❌ | ❌ | — | ✅ | 功能性 |
| 2 | AlertCenterView | ~190 | 列表 | ✅ | ✅ | ✅ | ✅ | ❌ | — | ⚠️ | 空 |
| 3 | ProjectListView | 727 | 列表CRUD | ✅ | ✅ | ✅ | ✅ | ✅ | ⚠️ | ❌ | 功能性 |
| 4 | ContractAlertView | ~500 | 列表 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 空 |
| 5 | HandoverListView | 503 | 列表+看板 | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ | 功能性 |
| 6 | InspectionPlanView | 740 | 双Tab列表 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 功能性 |
| 7 | AnnualReportView | ~340 | 列表 | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ | 空 |
| 8 | HospitalListView | 630 | 列表CRUD | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 空 |
| 9 | PersonnelListView | 1371 | 列表CRUD | ✅ | ✅ | ⚠️ | ⚠️ | ✅ | ❌ | ✅ | 大量 |
| 10 | ProductListView | ~480 | 列表CRUD | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 空 |
| 11 | RepairRecordView | ~500 | 列表CRUD | ⚠️ | ✅ | ✅ | ❌ | ✅ | ✅ | ⚠️ | ❌重复 |
| 12 | DataMaintenanceView | ~200 | 管理工具 | ✅ | ✅ | ❌ | ❌ | ❌ | — | ❌ | 空 |
| 13 | MajorDemandView | 573 | 列表+抽屉 | ✅ | ⚠️ | ✅ | ❌ | ✅ | ✅ | ❌ | 空 |
| 14 | WorkHoursView | ~340 | 列表CRUD | ⚠️ | ✅ | ✅ | ❌ | ✅ | ✅ | ⚠️ | ❌重复 |
| 15 | MonthlyReportView | ~280 | 列表CRUD | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ | ❌ | 最少量 |
| 16 | MS010001013001View | 658 | 地图仪表盘 | ❌ | ❌ | ✅ | ❌ | ❌ | — | ❌ | 大量 |
| 17 | LoginView | ~190 | 登录页 | ❌ | ❌ | ❌ | ❌ | ❌ | — | ❌ | 大量 |
| 18 | DesignSpecView | ~40 | 文档展示 | ✅ | ❌ | ❌ | ❌ | ❌ | — | ❌ | 功能性 |

> ✅ 符合规范 | ⚠️ 部分偏差 | ❌ 缺失/严重不一致 | — 不适用

---

## 2. 审计标准 1 — 页面骨架结构

**规范**: 根元素 `.page-shell` → `.page-head`（含 `.page-title` + `.page-subtitle`）→ 内容区

| 文件 | 根元素 class | .page-head | .page-title | .page-subtitle | 问题 |
|------|-------------|------------|-------------|----------------|------|
| DashboardView | `.page-shell.dashboard-page` | ✅ L5 | ✅ L7 | ✅ L8 | `.dashboard-page` 额外 class，可接受 |
| AlertCenterView | `.page-shell` | ✅ | ✅ | ✅ | 无 |
| ProjectListView | `.page-shell` | ✅ L4 | ✅ L7 | ✅ L8 | 无 |
| ContractAlertView | `.page-shell` | ✅ | ✅ | ✅ | 无 |
| HandoverListView | `.page-shell` | ✅ | ✅ | ✅ | 无 |
| InspectionPlanView | `.page-shell` | ✅ | ✅ | ✅ | 无 |
| AnnualReportView | `.page-shell` | ✅ | ✅ | ✅ | 无 |
| HospitalListView | `.page-shell` | ✅ | ✅ | ✅ | 无 |
| PersonnelListView | `.page-shell` | ✅ | ✅ | ✅ | 无 |
| ProductListView | `.page-shell` | ✅ | ✅ | ✅ | 无 |
| **RepairRecordView** | `.page-shell` | ✅ | ✅ | ❌ 无subtitle | **scoped CSS 重写 `.page-shell` 加 `padding:16px`、`.page-head` 改 `margin-bottom:16px`、`.page-title` 改 `font-size:20px` — 全部与全局样式冲突** (L348-351) |
| DataMaintenanceView | `.page-shell` | ✅ | ✅ | ✅ | 无 |
| MajorDemandView | `.page-shell` | ✅ | ✅ | ✅ | 无 |
| **WorkHoursView** | `.page-shell` | ✅ | ✅ | ❌ 无subtitle | **同 RepairRecordView，scoped CSS 重写骨架类** (L276-279) |
| MonthlyReportView | `.page-shell` | ✅ | ✅ | ✅ | 无 |
| **MS010001013001View** | **`.map-page`** | `.map-head` | `.map-title` | ❌ | **完全不使用标准骨架类**，使用自定义类: `.map-page`, `.map-head`, `.map-title`, `.map-layout`, `.left-panel`, `.center-panel`, `.right-panel` |
| **LoginView** | **`.login-page`** | ❌ | ❌ | ❌ | **不适用** — 登录页有独立布局，合理 |
| DesignSpecView | `.page-shell` | ✅ | ✅ | ✅ | 无 |

### 关键发现

- **RepairRecordView** (L348-363) 和 **WorkHoursView** (L276-295) 在 `<style scoped>` 中重新定义了 `.page-shell`, `.page-head`, `.page-title`, `.stats-row`, `.stat-card`, `.filter-card`, `.table-card`, `.pager` 等全局类样式，造成视觉差异：
  - `.page-shell` 加了 `padding: 16px`（全局无 padding）
  - `.page-head` 改为 `margin-bottom: 16px`（全局用 flex gap 控制间距）
  - `.page-title` 改为 `font-size: 20px; font-weight: 600`（全局为 `24px; 700`）
  - `.stat-card .v` 的色值使用硬编码值（如 `#67c23a`）而非 CSS 变量

- **MS010001013001View** 是地图仪表盘，布局完全独立，可以接受但应考虑至少让 `.map-head` 与 `.page-head` 视觉保持一致。

---

## 3. 审计标准 2 — 筛选区域

**规范**: `el-card.filter-card` → `el-form.filter-form`（含 `el-form-item`）→ `.filter-actions`（查询 + 重置按钮）

| 文件 | .filter-card | .filter-form | .filter-actions | 重置用 composable | 问题 |
|------|-------------|-------------|----------------|-------------------|------|
| DashboardView | ❌ 无筛选 | — | — | — | 正常（仪表盘无需筛选） |
| AlertCenterView | ✅ | ✅ | ✅ | ✅ useFilterStatePersist | 无 |
| ProjectListView | ✅ | ✅ | ✅ | ✅ useFilterStatePersist | 无 |
| ContractAlertView | ✅ | ✅ | ✅ | ✅ useFilterStatePersist | 无 |
| HandoverListView | ✅ | ✅ | ✅ | ✅ useFilterStatePersist | 无 |
| InspectionPlanView | ✅ ×2 (两个 tab 各一个) | ✅ | ✅ | ✅ useFilterStatePersist | 无 |
| AnnualReportView | ✅ | ✅ | ✅ | ✅ useFilterStatePersist | 无 |
| HospitalListView | ✅ | ✅ | ✅ | ✅ useFilterStatePersist | 无 |
| PersonnelListView | ✅ | ✅ | ✅ | ✅ useFilterStatePersist | 无 |
| ProductListView | ✅ | ✅ | ✅ | ✅ useFilterStatePersist | 无 |
| **RepairRecordView** | ✅ | ✅ | ✅ | **❌ 未使用 useFilterStatePersist** | 筛选状态不持久化 |
| DataMaintenanceView | ✅ ×2 | ❌ 无filter-form | ❌ | ❌ | 特殊管理工具页，可以接受 |
| **MajorDemandView** | ✅ | **❌ 使用 `el-space` 代替 `el-form.filter-form`** | ❌ | **❌ 未使用 useFilterStatePersist** | 筛选布局不标准：用 `el-space` 直接放置 `el-input`/`el-select`，无 label，与其他页面不一致 |
| **WorkHoursView** | ✅ | ✅ | ✅ | **❌ 未使用 useFilterStatePersist** | 筛选状态不持久化 |
| **MonthlyReportView** | ✅ | ✅ | ✅ | **❌ 未使用 useFilterStatePersist** | 筛选状态不持久化 |
| MS010001013001View | ❌ | — | — | — | 地图页，筛选在侧面板内 |
| LoginView | ❌ | — | — | — | 不适用 |
| DesignSpecView | ❌ | — | — | — | 无筛选 |

---

## 4. 审计标准 3 — 表格样式

**规范**: `el-card.table-card` → `el-table`（`stripe`、`scrollbar-always-on`、无 `border`、无 `size="small"`）

| 文件 | .table-card | stripe | border | size | scrollbar-always-on | highlight-current-row | 问题 |
|------|------------|--------|--------|------|--------------------|-----------------------|------|
| DashboardView | ✅ | ✅ | **✅ border** | — | ❌ | ❌ | `border` 仅在仪表盘，视觉上可接受 |
| AlertCenterView | ✅ | ✅ | ❌ | — | ❌ | ❌ | 缺少 scrollbar-always-on |
| ProjectListView | ✅ | ✅ | ❌ | — | ✅ | ✅ | **唯一使用 `highlight-current-row` 的页面** |
| ContractAlertView | ✅ | ✅ | ❌ | — | ❌ | ❌ | 缺少 scrollbar-always-on |
| HandoverListView | ✅ | ✅ | ❌ | — | ❌ | ❌ | — |
| InspectionPlanView | ✅ ×2 | ✅ | ❌ | — | ❌ | ❌ | — |
| AnnualReportView | ✅ | ✅ | ❌ | — | ❌ | ❌ | — |
| HospitalListView | ✅ | ✅ | ❌ | — | ❌ | ❌ | — |
| **PersonnelListView** | ✅ | ✅ | **✅ border** | **small** | ❌ | ❌ | **`border` + `size="small"` 使该页表格视觉上与其他页面明显不同** |
| ProductListView | ✅ | ✅ | ❌ | — | ❌ | ❌ | — |
| RepairRecordView | ✅ | ✅ | ❌ | — | ❌ | ❌ | — |
| MajorDemandView | ✅ | ✅ | ❌ | — | ❌ | ❌ | — |
| WorkHoursView | ✅ | ✅ | ❌ | — | ❌ | ❌ | — |
| MonthlyReportView | ✅ | ✅ | ❌ | — | ❌ | ❌ | — |
| MS010001013001View | ✅（内嵌小表格） | ✅ | ✅ border | **small** | ❌ | ❌ | 地图附属表格，`border`+`small` 合理 |

### `scrollbar-always-on` 使用情况

仅 **ProjectListView** 使用了 `scrollbar-always-on`。其余 16 个含表格的页面均未使用。全局 `style.css` 已有 `scrollbar-width: thin` 和自定义滚动条样式覆盖，因此实际影响不大，但建议统一。

---

## 5. 审计标准 4 — 分页器

**规范**: `.pager` → `el-pagination`（`background`, `layout="total, sizes, prev, pager, next"`, `:page-sizes="[15]"`, 默认 `pageSize=15`）

| 文件 | .pager 容器 | el-pagination | background | page-sizes | 默认 pageSize | layout | 问题 |
|------|------------|--------------|------------|------------|--------------|--------|------|
| AlertCenterView | ✅ | ✅ | **❌ 无** | `[15]` | 15 | total, sizes, prev, pager, next | 缺 `background` |
| ProjectListView | ✅ | ✅ | **❌ 无** | `[15]` | 15 | total, sizes, prev, pager, next | 缺 `background` |
| ContractAlertView | ✅ | ✅ | **❌ 无** | `[15]` | 15 | total, sizes, prev, pager, next | 缺 `background` |
| HandoverListView | ✅ | ✅ | **❌ 无** | `[15]` | 15 | total, sizes, prev, pager, next | 缺 `background` |
| InspectionPlanView | ✅ ×2 | ✅ | **❌ 无** | `[15]` | 15 | total, sizes, prev, pager, next | 缺 `background` |
| AnnualReportView | ✅ | ✅ | **❌ 无** | `[15]` | 15 | total, sizes, prev, pager, next | 缺 `background` |
| HospitalListView | ✅ | ✅ | **❌ 无** | `[15]` | 15 | total, sizes, prev, pager, next | 缺 `background` |
| **PersonnelListView** | ✅ | ✅ | **❌ 无** | `[15]` | 15 | **`sizes, prev, pager, next`** | **❌ layout 缺少 `total`**，使用独立 `.pager-total` div 显示总数 |
| ProductListView | ✅ | ✅ | **❌ 无** | `[15]` | 15 | total, sizes, prev, pager, next | 缺 `background` |
| **RepairRecordView** | ✅ | ✅ | **❌ 无** | **`[10, 20, 50, 100]`** | **20** | total, sizes, prev, pager, next | **page-sizes 和默认值不一致** |
| **WorkHoursView** | ✅ | ✅ | **❌ 无** | **`[10, 20, 50, 100]`** | **20** | total, sizes, prev, pager, next | **page-sizes 和默认值不一致** |
| **MonthlyReportView** | ✅ | ✅ | **❌ 无** | **`[10, 20, 50, 100]`** | **20** | total, sizes, prev, pager, next | **page-sizes 和默认值不一致** |
| MajorDemandView | ✅（仅文本计数） | **❌ 无 el-pagination** | — | — | — | — | 仅显示 `共 N 条` 文本 |
| DataMaintenanceView | ❌ | ❌ | — | — | — | — | 仅显示文本计数 |
| DashboardView | ❌ | ❌ | — | — | — | — | 不适用 |
| MS010001013001View | ❌ | ❌ | — | — | — | — | 不适用 |

### 关键发现

1. **所有** el-pagination 实例均缺少 `background` 属性。全局 `.pager` 样式中虽然有 `.is-background` 的选择器定义，但由于组件未传 `background` 属性，这些样式不生效。
2. **RepairRecordView** / **WorkHoursView** / **MonthlyReportView** 三个文件使用 `page-sizes=[10, 20, 50, 100]`、默认 `pageSize=20`，与其余页面的 `[15]`/`15` 不一致。
3. **PersonnelListView** 的 pagination layout 为 `"sizes, prev, pager, next"`（缺 `total`），改用独立 HTML 元素显示总数，与全局规范不一致。

---

## 6. 审计标准 5 — 弹窗/抽屉

**规范**: `el-dialog`（全局已有 `border-radius: 12px`、`header/body/footer` 样式），应使用 `v-model`、`title`、`width`、合理宽度

| 文件 | 组件类型 | 数量 | 宽度 | destroy-on-close | 问题 |
|------|---------|------|------|-----------------|------|
| ProjectListView | el-dialog | 1 | 560px | ❌ | — |
| ContractAlertView | el-dialog | 1 | 520px | ❌ | — |
| InspectionPlanView | el-dialog | 1 | 720px | ❌ | — |
| HospitalListView | el-dialog | 3 | 620px, 460px, 620px | ❌ | — |
| PersonnelListView | el-dialog | 3 | 620px, 560px, 760px | ❌ | — |
| ProductListView | el-dialog | 2 | 560px, 520px | ❌ | — |
| **RepairRecordView** | el-dialog | 1 | 600px | **✅** | — |
| **MajorDemandView** | el-dialog + **el-drawer** | 1+1 | dialog 520px, **drawer size="48%"** | **✅** dialog | el-drawer 未设 `destroy-on-close` |
| **WorkHoursView** | el-dialog | 1 | 600px | **✅** | — |
| **MonthlyReportView** | el-dialog | 1 | 640px | **✅** | — |

### 宽度分布

```
460px  ── HospitalListView (评分弹窗)
520px  ── ContractAlertView, ProductListView (详情), MajorDemandView (评论)
560px  ── ProjectListView, ProductListView, PersonnelListView (详情)
600px  ── RepairRecordView, WorkHoursView
620px  ── HospitalListView (编辑/详情), PersonnelListView
640px  ── MonthlyReportView
720px  ── InspectionPlanView
760px  ── PersonnelListView (权限)
```

### `destroy-on-close` 使用不一致

- **使用**: RepairRecordView, MajorDemandView, WorkHoursView, MonthlyReportView
- **未使用**: ProjectListView, ContractAlertView, InspectionPlanView, HospitalListView, PersonnelListView, ProductListView

这会导致表单值残留行为不一致。应统一使用 `destroy-on-close` 或在打开弹窗时手动 reset 表单。

---

## 7. 审计标准 6 — 按钮风格

**规范**: 操作列按钮应统一使用 `type="primary" link` 风格（编辑/查看）+ `type="danger" link`（删除），或 `size="small" plain`

| 文件 | 操作列按钮风格 | 示例 | 问题 |
|------|--------------|------|------|
| ProjectListView | `size="small" type="primary" plain` / `size="small" type="danger" plain` | 编辑/删除 | ⚠️ 使用 `plain` 而非 `link` |
| ContractAlertView | `type="primary" link` / `type="danger" link` | 编辑/删除 | ✅ |
| HandoverListView | `type="primary" link` / `type="danger" link` | 查看 | ✅ |
| InspectionPlanView | `type="primary" link` | 查看详情/上传 | ✅ |
| AnnualReportView | `type="primary" link` / `type="danger" link` | 修改/删除 | ✅ |
| HospitalListView | `type="primary" link` / `type="danger" link` / `type="warning" link` | 编辑/评分/详情/删除 | ✅ |
| **PersonnelListView** | **`size="small" type="primary" plain`** / `type="danger" plain` | 编辑/权限/详情/删除 | **❌ 与其他页面不一致，使用 `plain` 而非 `link`** |
| ProductListView | `type="primary" link` / `type="danger" link` | 编辑/详情/删除 | ✅ |
| RepairRecordView | `type="primary" link` / `type="danger" link` | 编辑/删除 | ✅ |
| MajorDemandView | `type="primary" link` / `type="danger" link` | 评论/编辑/删除 | ✅ |
| WorkHoursView | `type="primary" link` / `type="danger" link` | 编辑/删除 | ✅ |
| MonthlyReportView | `type="primary" link` / `type="danger" link` | 编辑/删除 | ✅ |

### 头部操作按钮

大多数页面在 `.page-head` 右侧放置 "新增" 按钮（`type="primary"`），这是一致的。

### 关键发现

- **PersonnelListView** 和部分 **ProjectListView** 操作列使用 `size="small" plain` 风格，与主流 `link` 风格不一致。
- 建议统一为 `type="primary" link` / `type="danger" link` 风格，保持视觉统一。

---

## 8. 审计标准 7 — 动画/过渡

**规范**: 全局 `style.css` 已定义：
- `.stat-card:hover` → `border-color` 过渡
- `.stat-card.clickable:active` → `transform: scale(0.97)`
- `.table-card .el-table .el-table__row` → `background-color 0.15s ease`
- `.el-dialog` → 由 Element Plus 内置过渡

| 文件 | 自定义过渡 | 问题 |
|------|----------|------|
| 大多数页面 | 无，依赖全局样式 | ✅ |
| **RepairRecordView** L353 | scoped 重写 `.stat-card { transition: border-color .2s }` | 与全局定义重复 |
| **WorkHoursView** L282 | scoped 重写 `.stat-card.clickable { transition: border-color .2s }` | 与全局定义重复 |

---

## 9. 审计标准 8 — 滚动条处理

**规范**: 全局 `style.css` 已定义：
- `* { scrollbar-width: thin }` 应用于全局
- `.table-card .el-table` 系列自定义滚动条样式（6px 宽/高、圆角、透明轨道）
- `.stats-row::-webkit-scrollbar` 水平滚动

| 文件 | 额外滚动条处理 | 问题 |
|------|--------------|------|
| 大多数页面 | 无，依赖全局 | ✅ |
| ProjectListView | `scrollbar-always-on` 在 el-table 上 | 唯一使用者，建议要么移除要么全部加上 |
| **PersonnelListView** | 动态 `tableMaxHeight` 计算 (`window.innerHeight - 340 + 'px'`) | 与全局 CSS `.table-card .el-table .el-table__body-wrapper { max-height: min(56vh, 520px) }` 可能冲突 |
| DesignSpecView | `.doc-card { max-height: calc(100vh - 170px); overflow: auto }` | 合理的页面级适配 |

---

## 10. 审计标准 9 — 统计卡片

**规范**: `.stats-row` (`el-row`) → `el-col` → `el-card.stat-card.stats-card`（双 class），内部 `.t` + `.v`

| 文件 | 卡片数 | el-col :span | .stat-card | .stats-card | .clickable | .active | 问题 |
|------|--------|-------------|------------|-------------|------------|---------|------|
| DashboardView | 5 | `:span="6"` | ✅ | ✅ | ❌ | ❌ | 5 个卡片用 span=6 不等于 24（30），但全局 CSS `.stats-row > .el-col { flex: 1 1 0 }` 会覆盖 span |
| AlertCenterView | 4 | `:xs/:sm/:md` 响应式 | ✅ | **❌ 无** | ✅ | ✅ | **唯一使用响应式 span 的页面**，缺少 `.stats-card` class |
| ContractAlertView | 4 | `:span="6"` | ✅ | ✅ | ✅ | ✅ | ✅ |
| HandoverListView | 5 | `:span="4"` | ✅ | ✅ | ✅ | ✅ | ✅ |
| InspectionPlanView (plan) | 6 | `:span="4"` | ✅ | ✅ | ✅ | ✅ | ✅ |
| InspectionPlanView (result) | 4 | `:span="6"` | ✅ | ✅ | ✅ | ✅ | ✅ |
| AnnualReportView | 6 | `:span="4"` | ✅ | ✅ | ✅ | ✅ | ✅ |
| HospitalListView | 4 | `:span="6"` | ✅ | ✅ | ✅ | ✅ | ✅ |
| PersonnelListView | 4 | `:span="6"` | ✅ | ✅ | ✅ | ✅ | ✅ |
| ProductListView | 4 | `:span="6"` | ✅ | ✅ | ✅ | ✅ | ✅ |
| **RepairRecordView** | 4 | `:span="6"` | ✅ | **❌ 无** | ❌ `.active` 直接用 | ✅ | **缺少 `.stats-card` class**，scoped 中重写了 `.stat-card` 样式 |
| **WorkHoursView** | 5 | **`:span="5"` ×4 + `:span="4"` ×1** | ✅ | **❌ 无** | ✅ | ✅ | **`:span` 合计 5×4+4=24 ✓，但 `.stats-card` 缺失**，会导致 `el-card__body` padding 不统一 |

### `.stats-card` class 的作用

全局 `style.css` 第 87-88 行定义了:
```css
.filter-card, .table-card, .stats-card {
  border-radius: 4px;
  border: 1px solid #d7e1ef;
  box-shadow: none;
}
.stats-card .el-card__body { padding: 8px 10px; }
```

缺少 `.stats-card` 的页面（AlertCenterView、RepairRecordView、WorkHoursView）卡片将使用 Element Plus 默认的较大 padding，导致卡片高度不一致。

---

## 11. 审计标准 10 — 页面专属 CSS

| 文件 | `<style scoped>` 内容 | 行数 | 问题 |
|------|---------------------|------|------|
| DashboardView | `.dashboard-page`, `.chart-grid`, `.chart-card`, `.chart-box`, `.panel-head`, `.panel-title`, `.drill-tip` | ~50行 | ✅ 功能性样式 |
| AlertCenterView | **空** | 0 | ✅ |
| ProjectListView | `.linked-highlight-row` | ~5行 | ✅ 功能性样式 |
| ContractAlertView | **空** | 0 | ✅ |
| HandoverListView | `.kanban-title`, `.kanban-col`, `.kanban-col-header`, `.kanban-items`, `.kanban-item` | ~40行 | ✅ 功能性样式 |
| InspectionPlanView | `.main-tabs`, `.risk-badge`, `.score-*`, `.result-desc` | ~30行 | ✅ 功能性样式 |
| AnnualReportView | **空** | 0 | ✅ |
| HospitalListView | **空** | 0 | ✅ |
| PersonnelListView | `.permission-groups`, `.permission-group`, `.pager`, `.pager-total`, `.template-toggle-btn`, `.hospital-scope-hint`, `.hospital-actions` | ~80行 | ⚠️ 多数功能性，但 `.pager` 重写有覆盖风险 |
| ProductListView | **空** | 0 | ✅ |
| **RepairRecordView** | 重写 12+ 全局类 | ~20行 | **❌ 严重：重写全局骨架/卡片/分页器样式** |
| DataMaintenanceView | **空** | 0 | ✅ |
| MajorDemandView | **空** | 0 | ✅ |
| **WorkHoursView** | 重写 12+ 全局类 | ~25行 | **❌ 严重：重写全局骨架/卡片/分页器样式** |
| MonthlyReportView | `.pager` | ~3行 | ⚠️ 重写 `.pager { margin-top:12px; display:flex; justify-content:flex-end }` |
| MS010001013001View | 完全自定义布局 | ~80行 | ✅ 独立页面合理 |
| LoginView | 完全自定义布局 | ~70行 | ✅ 登录页合理 |
| DesignSpecView | `.doc-card`, `.doc-pre` | ~15行 | ✅ 功能性样式 |

---

## 12. Composable 使用一致性

| Composable | 用途 | 应使用的页面 | 未使用的页面 |
|-----------|------|------------|------------|
| `useFilterStatePersist` | 筛选条件持久化（浏览器刷新后恢复） | 所有有筛选的列表页 | **RepairRecordView, MajorDemandView, WorkHoursView, MonthlyReportView** |
| `useLinkedRealtimeRefresh` | 关联数据实时刷新 | 所有 CRUD 列表页 | **RepairRecordView, WorkHoursView, MonthlyReportView** |
| `useResilientLoad` | 容错加载（首次加载失败自动重试） | 所有有数据加载的页面 | **RepairRecordView, WorkHoursView, MonthlyReportView, MajorDemandView** |
| `useAccessControl` | 权限控制 | 需要权限控制的页面 | 部分页面未使用（取决于业务是否需要） |
| `useMultiSelectActions` | 批量操作 | 有批量选择的页面 | — |

### 关键发现

**RepairRecordView, WorkHoursView, MonthlyReportView** 三个文件明显是较早期或独立开发的页面，未采纳项目后期引入的标准 composable 模式。这三个文件表现出高度相似的"旧模式"特征：

- ❌ 不使用 `useFilterStatePersist`
- ❌ 不使用 `useLinkedRealtimeRefresh`
- ❌ 不使用 `useResilientLoad`
- ❌ `page-sizes=[10,20,50,100]` / `pageSize=20`
- ❌ scoped CSS 重写全局样式（Repair/WorkHours）
- ❌ `destroy-on-close` 用法与其他页面不同

---

## 13. 高优先级问题汇总

按严重性排序：

### P0 — 视觉破坏（需立即修复）

| # | 文件 | 问题 | 影响 |
|---|------|------|------|
| 1 | RepairRecordView L348-363 | scoped CSS 重写 `.page-shell` padding、`.page-title` font-size、`.stat-card` 颜色等 12+ 全局类 | 该页面视觉与所有其他页面不一致：标题更小、额外内边距、间距异常 |
| 2 | WorkHoursView L276-295 | 同上，scoped CSS 重写全局类 | 同上 |

### P1 — 功能不一致（建议尽快修复）

| # | 文件 | 问题 | 影响 |
|---|------|------|------|
| 3 | RepairRecordView, WorkHoursView, MonthlyReportView | `page-sizes=[10,20,50,100]`, `pageSize=20` | 分页行为与其他页面不一致，用户体验混乱 |
| 4 | RepairRecordView, WorkHoursView, MonthlyReportView | 未使用 `useFilterStatePersist` | 刷新后筛选条件丢失 |
| 5 | RepairRecordView, WorkHoursView, MonthlyReportView | 未使用 `useResilientLoad` | 首次加载失败无自动重试 |
| 6 | PersonnelListView | pagination layout 缺少 `total`，用独立 div 显示 | 样式/位置与其他页面不同 |
| 7 | PersonnelListView | 操作列按钮使用 `size="small" plain` 而非 `link` | 按钮视觉不一致 |

### P2 — 样式细节（建议修复）

| # | 文件 | 问题 | 影响 |
|---|------|------|------|
| 8 | AlertCenterView, RepairRecordView, WorkHoursView | `el-card.stat-card` 缺少 `.stats-card` class | 卡片 padding 不统一（缺少全局 `.stats-card` 的 `padding: 8px 10px`） |
| 9 | 所有使用 el-pagination 的页面 | 缺少 `background` 属性 | 全局 `.pager .el-pagination.is-background` 样式不生效（但当前全局样式也能正常工作） |
| 10 | destroy-on-close 使用不统一 | 部分弹窗用、部分不用 | 表单数据可能残留 |

### P3 — 建议优化

| # | 文件 | 问题 |
|---|------|------|
| 11 | MajorDemandView | 筛选区使用 `el-space` 代替标准 `el-form.filter-form`，缺少 label |
| 12 | ProjectListView | 唯一使用 `highlight-current-row` + `scrollbar-always-on`，建议统一规范 |
| 13 | MS010001013001View | `.map-head` 与全局 `.page-head` 视觉风格已类似，但 class 名不同 |

---

## 14. 修复建议

### 一、RepairRecordView & WorkHoursView — 删除重复的 scoped 样式

删除 RepairRecordView L348-363 和 WorkHoursView L276-295 中的 scoped 样式，仅保留页面特有样式（如有）。这些全局类已在 `style.css` 中定义，scoped 重写会导致与全局风格不一致。

### 二、统一分页配置

将 RepairRecordView / WorkHoursView / MonthlyReportView 的分页参数改为：
```vue
:page-sizes="[15]"
```
默认 `pageSize` 改为 `15`。

### 三、补充标准 Composable

为 RepairRecordView / WorkHoursView / MonthlyReportView / MajorDemandView 添加：
```typescript
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useResilientLoad } from '../../composables/useResilientLoad'
```

### 四、补充 `.stats-card` class

在 AlertCenterView / RepairRecordView / WorkHoursView 的 stat-card el-card 组件上添加 `class="stat-card stats-card"`。

### 五、统一操作按钮风格

将 PersonnelListView 操作列按钮从 `size="small" type="primary" plain` 改为 `type="primary" link`，保持与 90% 页面一致。

### 六、统一 destroy-on-close

要么全部 el-dialog 加 `destroy-on-close`，要么全部不加（改用手动 reset）。推荐全部加 `destroy-on-close` 以简化代码。

### 七、PersonnelListView pagination layout

将 `layout="sizes, prev, pager, next"` 改为 `layout="total, sizes, prev, pager, next"`，删除独立的 `.pager-total` div。

---

> **审计结论**: 18 个视图中有 **12 个** 完全或高度符合统一规范，**3 个**（RepairRecordView、WorkHoursView、MonthlyReportView）属于"旧模式"代码需要整体对齐，**3 个**（MS010001013001View、LoginView、DataMaintenanceView）为特殊用途页面可以保留独立风格。PersonnelListView 虽整体符合规范，但有若干细节偏差需修正。
