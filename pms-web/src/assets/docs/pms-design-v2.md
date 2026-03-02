# 项目管理平台（PMS）综合设计方案 v2.0

> 基于四份核心业务数据 + pms.bjgoodwill.com 参考站点重新设计  
> 数据源：年度服务报告巡检统计、实施交接项目表、王鸣总表、区域交接项目表  
> 预计总数据量：3000+ 项目记录，30+ 产品线，20+ 省份，15+ 服务组

---

## 一、业务全景分析

### 1.1 四份数据源角色定位

| 数据源 | 核心定位 | 行数 | 关键维度 |
|--------|---------|------|---------|
| 年度服务报告巡检统计 | **年度运维考核底表** — 合同状态 + 巡检计划 | ~430+ | 年度报告、合同状态、巡检日期 |
| 实施交接项目表 | **实施→运维交接流程跟踪** | ~110 | 交接批次、邮件状态、确认流程 |
| 王鸣总表 | **项目合同主数据库** — 全量项目台账 | ~1600+ | 合同金额、超期天数、客户等级、驻场 |
| 区域交接项目表 | **区域/组内项目交接明细** — 含服务需求 | ~200+ | 交接类型、项目情况简述、月需求量 |

### 1.2 业务流程全景

```
┌─────────────┐     ┌──────────────┐     ┌──────────────┐     ┌─────────────┐
│  项目签约    │────▶│  实施上线     │────▶│  交接运维     │────▶│  日常运维    │
│ (合同/SAP)  │     │ (实施部门)    │     │ (交接管理)    │     │ (服务组)     │
└─────────────┘     └──────────────┘     └──────────────┘     └─────────────┘
       │                   │                    │                     │
       ▼                   ▼                    ▼                     ▼
  合同金额/期限       验收单/确认单        交接邮件/批次确认      巡检/报告/续签
  客户等级分类        报表/模板上传        组内交接/组外交接      超期预警/驻场
  销售关联           评审评级信息         项目情况简述          年度服务报告
```

### 1.3 核心业务实体关系

```
                    ┌──────────┐
                    │  集团公司  │
                    └────┬─────┘
                         │
            ┌────────────┼────────────┐
            ▼            ▼            ▼
      ┌──────────┐ ┌──────────┐ ┌──────────┐
      │ 实施部门  │ │ 服务部门  │ │ 销售部门  │
      │(实施一组) │ │(中区服务) │ │          │
      │(实施二组) │ │(南区服务) │ │          │
      │(HIP实施) │ │(北区服务) │ │          │
      └────┬─────┘ │(西北服务) │ └────┬─────┘
           │       └────┬─────┘      │
           │            │            │
           ▼            ▼            ▼
      ┌────────────────────────────────────┐
      │           项目（核心实体）           │
      │  医院 + 产品 + 合同 + 人员 + 区域   │
      └──────────┬──────────┬──────────────┘
                 │          │
           ┌─────┘          └─────┐
           ▼                      ▼
     ┌───────────┐         ┌───────────┐
     │  交接记录   │         │  巡检记录   │
     │ 批次/邮件   │         │ 年度报告    │
     │ 确认/对接   │         │ 计划/执行   │
     └───────────┘         └───────────┘
```

---

## 二、技术架构设计

### 2.1 技术选型（与参考站点一致）

| 层次 | 技术 | 版本 | 说明 |
|------|------|------|------|
| **前端框架** | Vue 3 | 3.4+ | Composition API + Script Setup |
| **UI 组件库** | Element Plus | 2.7+ | 企业级组件，表格/表单/弹窗丰富 |
| **CSS 框架** | Tailwind CSS | 3.4+ | 原子化 CSS，快速样式开发 |
| **构建工具** | Vite | 5.x | 极速 HMR，生产环境 Rollup |
| **状态管理** | Pinia | 2.x | Vue 3 官方推荐 |
| **图表** | ECharts | 5.5+ | 数据可视化仪表盘 |
| **HTTP** | Axios | 1.7+ | 拦截器 + Token 刷新 |
| **路由** | Vue Router | 4.x | Hash/History 模式 |
| **后端框架** | .NET 8 Web API | 8.0 | 与现有 SystemAuditTool 技术栈统一 |
| **ORM** | EF Core | 8.0 | Code First + Migration |
| **数据库** | MySQL 8.0 | 8.0+ | InnoDB，支持 JSON 字段 |
| **缓存** | Redis | 7.x | 会话 + 热点数据缓存 |
| **认证** | JWT + Refresh Token | — | 双 Token 无感刷新 |
| **文件存储** | MinIO / 本地 | — | Excel 导入/导出、附件管理 |
| **Excel 处理** | EPPlus (.NET) / xlsx (前端) | — | 服务端导入导出 |

### 2.2 系统架构图

```
┌─────────────────────────────────────────────────────┐
│                    Nginx 反向代理                     │
│              (SSL/负载均衡/静态资源)                   │
└──────────────┬──────────────────┬────────────────────┘
               │                  │
    ┌──────────▼──────────┐  ┌───▼───────────────┐
    │   Vue 3 SPA 前端     │  │  .NET 8 Web API    │
    │                      │  │                    │
    │ ┌──────────────────┐ │  │ ┌────────────────┐ │
    │ │ Element Plus UI  │ │  │ │  Controller    │ │
    │ │ Tailwind CSS     │ │  │ │  Service Layer │ │
    │ │ ECharts 图表     │ │  │ │  Repository    │ │
    │ │ Pinia Store      │ │  │ │  EF Core       │ │
    │ │ Vue Router       │ │  │ │  AutoMapper    │ │
    │ └──────────────────┘ │  │ │  FluentValid   │ │
    └──────────────────────┘  │ └────────────────┘ │
                              └──┬──────────┬──────┘
                                 │          │
                          ┌──────▼───┐  ┌───▼──────┐
                          │ MySQL 8  │  │  Redis   │
                          │ 主从复制  │  │  缓存    │
                          └──────────┘  └──────────┘
```

### 2.3 前端项目结构

```
pms-frontend/
├── public/
├── src/
│   ├── api/                          # API 接口层
│   │   ├── modules/
│   │   │   ├── auth.ts               # 认证接口
│   │   │   ├── project.ts            # 项目管理
│   │   │   ├── contract.ts           # 合同管理
│   │   │   ├── handover.ts           # 交接管理
│   │   │   ├── inspection.ts         # 巡检管理
│   │   │   ├── personnel.ts          # 人员管理
│   │   │   ├── hospital.ts           # 医院/客户管理
│   │   │   ├── product.ts            # 产品管理
│   │   │   ├── report.ts             # 报告中心
│   │   │   └── system.ts             # 系统管理
│   │   ├── request.ts                # Axios 封装
│   │   └── index.ts
│   ├── assets/                       # 静态资源
│   │   ├── styles/
│   │   │   ├── tailwind.css
│   │   │   ├── element-override.scss # EP 主题覆盖
│   │   │   └── variables.scss
│   │   └── icons/
│   ├── components/                   # 通用组件
│   │   ├── Layout/
│   │   │   ├── AppSidebar.vue        # 多级菜单
│   │   │   ├── AppHeader.vue         # 顶栏+面包屑
│   │   │   ├── AppTabs.vue           # 多标签页
│   │   │   └── AppLayout.vue
│   │   ├── Table/
│   │   │   ├── ProTable.vue          # 高级表格封装
│   │   │   ├── TableFilter.vue       # 多条件筛选器
│   │   │   └── ExportButton.vue      # 导出按钮
│   │   ├── Form/
│   │   │   ├── ProForm.vue           # 动态表单
│   │   │   └── SearchForm.vue        # 搜索表单
│   │   ├── Charts/
│   │   │   ├── PieChart.vue
│   │   │   ├── BarChart.vue
│   │   │   ├── LineChart.vue
│   │   │   └── MapChart.vue          # 省份地图热力图
│   │   ├── StatusTag.vue             # 状态标签组件
│   │   ├── OverdueAlert.vue          # 超期预警组件
│   │   └── HandoverTimeline.vue      # 交接时间线组件
│   ├── views/                        # 页面视图
│   │   ├── dashboard/                # 仪表盘
│   │   │   ├── index.vue
│   │   │   ├── components/
│   │   │   │   ├── StatCards.vue      # 统计卡片
│   │   │   │   ├── ContractOverview.vue
│   │   │   │   ├── OverdueAlerts.vue  # 超期预警面板
│   │   │   │   ├── HandoverProgress.vue
│   │   │   │   ├── RegionMap.vue      # 区域分布地图
│   │   │   │   └── ProductDistribution.vue
│   │   │   └── hooks/
│   │   │       └── useDashboard.ts
│   │   ├── project/                  # 项目管理
│   │   │   ├── list.vue              # 项目台账（主数据表）
│   │   │   ├── detail.vue            # 项目详情
│   │   │   ├── import.vue            # Excel 批量导入
│   │   │   └── components/
│   │   │       ├── ProjectForm.vue
│   │   │       ├── ProjectFilter.vue
│   │   │       └── ContractTimeline.vue
│   │   ├── contract/                 # 合同管理
│   │   │   ├── list.vue              # 合同列表
│   │   │   ├── overdue.vue           # 超期合同预警
│   │   │   ├── renewal.vue           # 续签管理
│   │   │   └── components/
│   │   │       ├── ContractForm.vue
│   │   │       └── OverdueTable.vue
│   │   ├── handover/                 # 交接管理 ★新增
│   │   │   ├── implementation.vue    # 实施→运维交接
│   │   │   ├── regional.vue          # 区域/组间交接
│   │   │   ├── detail.vue            # 交接详情
│   │   │   ├── batch.vue             # 批次管理
│   │   │   └── components/
│   │   │       ├── HandoverForm.vue
│   │   │       ├── HandoverFlow.vue  # 交接流程可视化
│   │   │       ├── BatchSelector.vue
│   │   │       └── ProjectBrief.vue  # 项目情况简述
│   │   ├── inspection/               # 巡检管理
│   │   │   ├── plan.vue              # 巡检计划
│   │   │   ├── record.vue            # 巡检记录
│   │   │   ├── annual-report.vue     # 年度服务报告
│   │   │   └── components/
│   │   │       ├── InspectionForm.vue
│   │   │       └── ReportUpload.vue
│   │   ├── hospital/                 # 医院/客户管理 ★新增
│   │   │   ├── list.vue
│   │   │   ├── detail.vue
│   │   │   └── components/
│   │   │       ├── HospitalForm.vue
│   │   │       └── GradeTag.vue      # 等级标签
│   │   ├── personnel/                # 人员管理
│   │   │   ├── list.vue
│   │   │   ├── onsite.vue            # 驻场人员管理 ★新增
│   │   │   └── workload.vue          # 工作量统计
│   │   ├── product/                  # 产品管理 ★新增
│   │   │   ├── list.vue              # 产品字典
│   │   │   └── statistics.vue        # 产品部署统计
│   │   ├── report/                   # 报告中心
│   │   │   ├── overview.vue
│   │   │   ├── contract-report.vue
│   │   │   ├── handover-report.vue
│   │   │   ├── region-report.vue
│   │   │   └── export.vue
│   │   ├── rating/                   # 评级管理 ★新增
│   │   │   ├── emr-rating.vue        # 电子病历评级
│   │   │   └── interop-rating.vue    # 互联互通评级
│   │   └── system/                   # 系统管理
│   │       ├── user.vue
│   │       ├── role.vue
│   │       ├── dict.vue              # 数据字典
│   │       └── log.vue
│   ├── router/
│   │   └── index.ts
│   ├── stores/                       # Pinia 状态
│   │   ├── user.ts
│   │   ├── project.ts
│   │   ├── dict.ts                   # 字典缓存
│   │   └── app.ts
│   ├── hooks/                        # 组合式函数
│   │   ├── useTable.ts               # 通用表格逻辑
│   │   ├── useExport.ts              # 导出逻辑
│   │   ├── useDict.ts                # 字典查询
│   │   └── usePermission.ts          # 权限判断
│   ├── utils/
│   │   ├── auth.ts
│   │   ├── date.ts
│   │   ├── validator.ts
│   │   └── excel.ts                  # Excel 解析工具
│   ├── directives/
│   │   └── permission.ts             # v-permission
│   ├── App.vue
│   └── main.ts
├── .env.development
├── .env.production
├── tailwind.config.ts
├── vite.config.ts
├── tsconfig.json
└── package.json
```

---

## 三、数据库设计（21 张核心表）

### 3.1 ER 模型总览

```
sys_user ──┬── sys_role ── sys_permission
           │
           ├── team_department (部门: 实施一组/服务组)
           │       │
           │       ├── team_group (组别: 张茹组/何道飞组)
           │       │
           │       └── service_area (服务区域: 中区/南区/北区/西北)
           │
hospital ──┤── hospital_grade_dict
           │
           ├── project (核心) ──┬── product_deploy (项目×产品 M:N)
           │                    │
           │                    ├── contract (合同记录)
           │                    │
           │                    ├── handover_record (交接记录)
           │                    │
           │                    ├── inspection_record (巡检记录)
           │                    │
           │                    ├── annual_report (年度报告)
           │                    │
           │                    ├── project_personnel (项目人员关联)
           │                    │
           │                    └── project_brief (项目情况简述)
           │
           └── onsite_assignment (驻场安排)

product_dict ── product_category_dict

sys_dict_type ── sys_dict_data (通用字典)

operation_log (操作日志)
```

### 3.2 详细表结构

#### 3.2.1 系统基础表

```sql
-- ========================================
-- 1. 用户表
-- ========================================
CREATE TABLE sys_user (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    username        VARCHAR(50)  NOT NULL UNIQUE COMMENT '登录名',
    password_hash   VARCHAR(256) NOT NULL COMMENT '密码哈希',
    real_name       VARCHAR(50)  NOT NULL COMMENT '姓名(如: 李贝/何道飞/张茹)',
    phone           VARCHAR(20)  DEFAULT NULL,
    email           VARCHAR(100) DEFAULT NULL,
    avatar          VARCHAR(256) DEFAULT NULL,
    department_id   BIGINT       DEFAULT NULL COMMENT '所属部门',
    group_id        BIGINT       DEFAULT NULL COMMENT '所属组别',
    status          TINYINT      NOT NULL DEFAULT 1 COMMENT '1=启用 0=禁用',
    last_login_at   DATETIME     DEFAULT NULL,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_department (department_id),
    INDEX idx_group (group_id)
) COMMENT '系统用户';

-- ========================================
-- 2. 角色表
-- ========================================
CREATE TABLE sys_role (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    role_code       VARCHAR(50)  NOT NULL UNIQUE COMMENT '角色编码',
    role_name       VARCHAR(50)  NOT NULL COMMENT '角色名称',
    description     VARCHAR(256) DEFAULT NULL,
    data_scope      TINYINT      NOT NULL DEFAULT 1 COMMENT '数据权限: 1=全部 2=本部门 3=本组 4=仅本人',
    sort_order      INT          NOT NULL DEFAULT 0,
    status          TINYINT      NOT NULL DEFAULT 1,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) COMMENT '角色';

-- 预设角色:
-- admin          系统管理员 (全部数据)
-- dept_manager   部门经理 (本部门数据) — 如: 实施部经理、服务部经理
-- group_leader   组长 (本组数据) — 如: 李贝、何道飞、张茹
-- service_staff  服务人员 (仅本人负责项目)
-- impl_staff     实施人员 (仅本人负责项目)
-- viewer         只读查看

-- ========================================
-- 3. 用户角色关联
-- ========================================
CREATE TABLE sys_user_role (
    user_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    PRIMARY KEY (user_id, role_id)
) COMMENT '用户角色关联';

-- ========================================
-- 4. 权限表
-- ========================================
CREATE TABLE sys_permission (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    parent_id       BIGINT       DEFAULT 0 COMMENT '父级ID',
    perm_type       TINYINT      NOT NULL COMMENT '1=菜单 2=按钮 3=接口',
    perm_code       VARCHAR(100) NOT NULL UNIQUE COMMENT '权限编码',
    perm_name       VARCHAR(50)  NOT NULL COMMENT '权限名称',
    path            VARCHAR(200) DEFAULT NULL COMMENT '路由路径',
    component       VARCHAR(200) DEFAULT NULL COMMENT '组件路径',
    icon            VARCHAR(50)  DEFAULT NULL,
    sort_order      INT          NOT NULL DEFAULT 0,
    visible         TINYINT      NOT NULL DEFAULT 1,
    status          TINYINT      NOT NULL DEFAULT 1,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
) COMMENT '权限/菜单';

-- ========================================
-- 5. 角色权限关联
-- ========================================
CREATE TABLE sys_role_permission (
    role_id       BIGINT NOT NULL,
    permission_id BIGINT NOT NULL,
    PRIMARY KEY (role_id, permission_id)
) COMMENT '角色权限关联';

-- ========================================
-- 6. 通用数据字典
-- ========================================
CREATE TABLE sys_dict_type (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    dict_type       VARCHAR(50)  NOT NULL UNIQUE COMMENT '字典类型编码',
    dict_name       VARCHAR(100) NOT NULL COMMENT '字典类型名称',
    remark          VARCHAR(256) DEFAULT NULL,
    status          TINYINT      NOT NULL DEFAULT 1,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
) COMMENT '字典类型';

CREATE TABLE sys_dict_data (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    dict_type       VARCHAR(50)  NOT NULL COMMENT '字典类型编码',
    dict_value      VARCHAR(100) NOT NULL COMMENT '字典值',
    dict_label      VARCHAR(100) NOT NULL COMMENT '字典标签',
    sort_order      INT          NOT NULL DEFAULT 0,
    css_class       VARCHAR(50)  DEFAULT NULL COMMENT '前端样式(如: success/warning/danger)',
    remark          VARCHAR(256) DEFAULT NULL,
    status          TINYINT      NOT NULL DEFAULT 1,
    INDEX idx_dict_type (dict_type)
) COMMENT '字典数据';

-- 预设字典类型:
-- contract_status       合同状态 (免费维护期/合同已签署/超期未签署/维护超期未签署/未终验/停止维护/更换系统)
-- customer_importance   客户重要性 (A/B/C)
-- customer_relation     客户关系 (公司/部门)
-- hospital_grade        医院等级 (三甲/三级/二甲/二级/一甲/民营/医保局)
-- handover_stage        交接阶段 (未发/已发邮件/交接文档整理中/交接中/已交接/更换系统不交接/没收到邮件)
-- handover_type         交接类型 (组内/组外)
-- handover_batch        交接批次 (第一批/第二批/第三批/第四批)
-- operation_phase       运维阶段 (实施中/试运行/正式运行/停用)
-- emr_rating            电子病历评级 (三级/四级/五级/六级)
-- interop_rating        互联互通评级 (三级/四级/五级)
-- report_status         报告状态 (未开始/编写中/已提交/已完成)
-- inspection_status     巡检状态 (已计划/执行中/已完成/已取消)

-- ========================================
-- 7. 操作日志
-- ========================================
CREATE TABLE operation_log (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id         BIGINT       DEFAULT NULL,
    user_name       VARCHAR(50)  DEFAULT NULL,
    module          VARCHAR(50)  NOT NULL COMMENT '操作模块',
    action          VARCHAR(50)  NOT NULL COMMENT '操作类型',
    target_type     VARCHAR(50)  DEFAULT NULL COMMENT '目标类型',
    target_id       BIGINT       DEFAULT NULL COMMENT '目标ID',
    detail          TEXT         DEFAULT NULL COMMENT '操作详情(JSON)',
    ip_address      VARCHAR(50)  DEFAULT NULL,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_user (user_id),
    INDEX idx_module_action (module, action),
    INDEX idx_created (created_at)
) COMMENT '操作日志';
```

#### 3.2.2 组织架构表

```sql
-- ========================================
-- 8. 部门表
-- ========================================
CREATE TABLE team_department (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    dept_code       VARCHAR(50)  NOT NULL UNIQUE,
    dept_name       VARCHAR(100) NOT NULL COMMENT '部门名称',
    dept_type       VARCHAR(20)  NOT NULL COMMENT 'IMPL=实施部门 SERVICE=服务部门 SALES=销售部门',
    parent_id       BIGINT       DEFAULT 0,
    manager_id      BIGINT       DEFAULT NULL COMMENT '部门负责人',
    sort_order      INT          NOT NULL DEFAULT 0,
    status          TINYINT      NOT NULL DEFAULT 1,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) COMMENT '部门';

-- 预设数据:
-- 实施一组 (IMPL)  — 侯海亮主管
-- 实施二组 (IMPL)  — 张育明/陈宇主管
-- HIP实施部 (IMPL) — 沈岩石主管
-- 服务一部 (SERVICE) — 含中区/南区/北区/西北
-- 销售部 (SALES)

-- ========================================
-- 9. 组别/团队表
-- ========================================
CREATE TABLE team_group (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    group_code      VARCHAR(50)  NOT NULL UNIQUE,
    group_name      VARCHAR(100) NOT NULL COMMENT '组别名称(如: 张茹/何道飞/李贝/舒高成)',
    department_id   BIGINT       NOT NULL COMMENT '所属部门',
    leader_id       BIGINT       DEFAULT NULL COMMENT '组长用户ID',
    sort_order      INT          NOT NULL DEFAULT 0,
    status          TINYINT      NOT NULL DEFAULT 1,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_department (department_id)
) COMMENT '组别/团队';

-- 预设数据 (来自王鸣总表):
-- 张茹、何道飞、王可可、李贝、陈宇、舒高成
-- 张浩阳、孙强、高丹、唐广才、邵勇、姚云龙、李振

-- ========================================
-- 10. 服务区域表
-- ========================================
CREATE TABLE service_area (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    area_code       VARCHAR(50)  NOT NULL UNIQUE,
    area_name       VARCHAR(100) NOT NULL COMMENT '区域名称(中区/南区/北区/西北)',
    provinces       JSON         DEFAULT NULL COMMENT '覆盖省份列表 ["北京","天津","河北"]',
    manager_id      BIGINT       DEFAULT NULL COMMENT '区域负责人',
    department_id   BIGINT       DEFAULT NULL COMMENT '所属服务部门',
    status          TINYINT      NOT NULL DEFAULT 1,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
) COMMENT '服务区域';

-- 预设: 中区服务组, 南区服务组, 北区服务组, 西北服务组
```

#### 3.2.3 医院/客户表

```sql
-- ========================================
-- 11. 医院/客户表 ★核心
-- ========================================
CREATE TABLE hospital (
    id                  BIGINT PRIMARY KEY AUTO_INCREMENT,
    hospital_name       VARCHAR(200) NOT NULL COMMENT '医院名称',
    customer_code       VARCHAR(50)  DEFAULT NULL COMMENT '客户编码(SAP)',
    province            VARCHAR(20)  NOT NULL COMMENT '省份',
    city                VARCHAR(50)  DEFAULT NULL COMMENT '城市',
    hospital_grade      VARCHAR(20)  DEFAULT NULL COMMENT '医院等级: 三甲/三级/二甲/二级/一甲/民营',
    importance          CHAR(1)      DEFAULT NULL COMMENT '客户重要性: A/B/C',
    customer_relation   VARCHAR(10)  DEFAULT NULL COMMENT '客户关系: 公司/部门',
    
    -- 评级信息 (来自实施交接表)
    emr_rating_year     YEAR         DEFAULT NULL COMMENT '电子病历评级年份',
    emr_rating_level    VARCHAR(10)  DEFAULT NULL COMMENT '电子病历评级: 三级/四级/五级/六级',
    interop_rating_year YEAR         DEFAULT NULL COMMENT '互联互通评级年份',
    interop_rating_level VARCHAR(10) DEFAULT NULL COMMENT '互联互通评级: 三级/四级/五级',
    
    -- 联系信息
    contact_name        VARCHAR(50)  DEFAULT NULL COMMENT 'IT联系人',
    contact_phone       VARCHAR(20)  DEFAULT NULL,
    contact_ability     VARCHAR(20)  DEFAULT NULL COMMENT 'IT对接能力评估: 强/一般/弱',
    communication_note  TEXT         DEFAULT NULL COMMENT '沟通备注',
    
    remark              TEXT         DEFAULT NULL,
    status              TINYINT      NOT NULL DEFAULT 1,
    created_at          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    UNIQUE INDEX uk_name_province (hospital_name, province),
    INDEX idx_province (province),
    INDEX idx_grade (hospital_grade),
    INDEX idx_importance (importance)
) COMMENT '医院/客户';
```

#### 3.2.4 产品表

```sql
-- ========================================
-- 12. 产品类别字典
-- ========================================
CREATE TABLE product_category (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    category_code   VARCHAR(50)  NOT NULL UNIQUE,
    category_name   VARCHAR(100) NOT NULL COMMENT '产品大类: EMR/护理/集成/AI/管理/移动',
    sort_order      INT          NOT NULL DEFAULT 0,
    status          TINYINT      NOT NULL DEFAULT 1
) COMMENT '产品类别';

-- ========================================
-- 13. 产品字典表 ★
-- ========================================
CREATE TABLE product (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    product_code    VARCHAR(50)  NOT NULL UNIQUE,
    product_name    VARCHAR(100) NOT NULL COMMENT '产品名称',
    product_version VARCHAR(20)  DEFAULT NULL COMMENT '版本号',
    category_id     BIGINT       DEFAULT NULL COMMENT '所属类别',
    description     TEXT         DEFAULT NULL,
    sort_order      INT          NOT NULL DEFAULT 0,
    status          TINYINT      NOT NULL DEFAULT 1,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_category (category_id)
) COMMENT '产品字典';

-- 预设产品 (合并四份表):
-- EMR类:       住院电子病历V6, 门诊电子病历V6, 护理电子病历V6, 产科病历
-- 临床辅助:     临床路径V5/V6, CDSS, 单病种, VTE
-- 重症/手术:    手术麻醉管理系统V3.5, ICU/重症监护(2.0/2.5/3.0)
-- AI/质控:     AI内涵质控, 病历质控, 病案归档, 病案管理, HQMS数据上报
-- DRG:         DRG/DIP
-- 集成平台:     医疗信息平台V2.0/V2.1/V3.0, 集成服务, 互联互通, 主索引系统
-- 移动/护理:    移动护理, 护理管理, 移睿医生, 移睿云医生
-- 管理类:       医务管理, 不良事件系统, 随访系统, 疾病管理平台
-- 急诊:         急诊信息系统/院内急诊
-- 其他:         智慧康养云, 互联网医院, 心电信息管理系统, 专病中心系统
```

#### 3.2.5 项目核心表

```sql
-- ========================================
-- 14. 项目表 ★核心主表
-- ========================================
CREATE TABLE project (
    id                  BIGINT PRIMARY KEY AUTO_INCREMENT,
    project_no          VARCHAR(50)  DEFAULT NULL COMMENT '项目编号(自动生成)',
    hospital_id         BIGINT       NOT NULL COMMENT '医院ID',
    opportunity_no      VARCHAR(50)  DEFAULT NULL COMMENT '机会号(CRM)',
    doc_platform_no     VARCHAR(50)  DEFAULT NULL COMMENT '文档平台序号',
    sap_no              VARCHAR(50)  DEFAULT NULL COMMENT 'SAP号',
    
    -- 组织归属
    group_id            BIGINT       DEFAULT NULL COMMENT '当前负责组别',
    service_area_id     BIGINT       DEFAULT NULL COMMENT '服务区域',
    
    -- 销售信息
    dept_sales_id       BIGINT       DEFAULT NULL COMMENT '部门销售',
    company_sales_id    BIGINT       DEFAULT NULL COMMENT '公司销售',
    
    -- 合同金额 (来自王鸣总表)
    sales_amount        DECIMAL(14,2) DEFAULT NULL COMMENT '销售合同额(万元)',
    maintenance_amount  DECIMAL(14,2) DEFAULT NULL COMMENT '维护合同额(万元)',
    is_main_contract    TINYINT      DEFAULT NULL COMMENT '是否主合同: 1=是 0=否',
    
    -- 合同状态与期限
    contract_status     VARCHAR(30)  DEFAULT NULL COMMENT '合同状态: 免费维护期/合同已签署/超期未签署/...',
    maintenance_start   DATE         DEFAULT NULL COMMENT '维护开始日期',
    maintenance_end     DATE         DEFAULT NULL COMMENT '维护结束日期',
    maintenance_months  INT          DEFAULT NULL COMMENT '维护期限(月)',
    is_overdue          TINYINT      DEFAULT 0 COMMENT '是否超期: 1=是 0=否',
    overdue_days        INT          DEFAULT 0 COMMENT '超期天数',
    is_free_maintenance TINYINT      DEFAULT NULL COMMENT '是否免费维护: 1=是 0=否',
    
    -- 运维阶段
    operation_phase     VARCHAR(20)  DEFAULT NULL COMMENT '运维阶段: 实施中/试运行/正式运行',
    
    -- 验收信息 (来自实施交接表)
    acceptance_form     VARCHAR(50)  DEFAULT NULL COMMENT '验收单状态: 有/无/部分',
    service_confirm     VARCHAR(50)  DEFAULT NULL COMMENT '售后服务确认单: 有/无',
    report_uploaded     TINYINT      DEFAULT 0 COMMENT '报表是否上传',
    template_uploaded   TINYINT      DEFAULT 0 COMMENT '模板是否上传',
    
    -- 服务需求 (来自区域交接表-项目情况简述)
    monthly_demand      INT          DEFAULT NULL COMMENT '月需求量',
    clinical_groups     INT          DEFAULT NULL COMMENT '使用临床科室数',
    demand_description  TEXT         DEFAULT NULL COMMENT '需求特征描述',
    
    -- 年度报告相关 (来自巡检统计)
    annual_report_status VARCHAR(20) DEFAULT NULL COMMENT '年度报告状态',
    
    remark              TEXT         DEFAULT NULL,
    created_by          BIGINT       DEFAULT NULL,
    created_at          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX idx_hospital (hospital_id),
    INDEX idx_group (group_id),
    INDEX idx_area (service_area_id),
    INDEX idx_contract_status (contract_status),
    INDEX idx_overdue (is_overdue),
    INDEX idx_maintenance_end (maintenance_end)
) COMMENT '项目主表';

-- ========================================
-- 15. 项目-产品部署关联表 (M:N)
-- ========================================
CREATE TABLE product_deploy (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    project_id      BIGINT       NOT NULL,
    product_id      BIGINT       NOT NULL,
    deploy_version  VARCHAR(20)  DEFAULT NULL COMMENT '部署版本',
    deploy_date     DATE         DEFAULT NULL COMMENT '上线日期',
    status          VARCHAR(20)  DEFAULT 'ACTIVE' COMMENT 'ACTIVE/DECOMMISSIONED',
    remark          VARCHAR(256) DEFAULT NULL,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_project (project_id),
    INDEX idx_product (product_id),
    UNIQUE INDEX uk_project_product (project_id, product_id)
) COMMENT '项目产品部署';

-- ========================================
-- 16. 项目人员关联表
-- ========================================
CREATE TABLE project_personnel (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    project_id      BIGINT       NOT NULL,
    user_id         BIGINT       DEFAULT NULL COMMENT '人员ID',
    personnel_name  VARCHAR(50)  NOT NULL COMMENT '人员姓名(兼容未注册人员)',
    role_type       VARCHAR(20)  NOT NULL COMMENT '角色: SERVICE=服务人员 IMPL=实施人员 ONSITE=驻场 SALES=销售',
    is_primary      TINYINT      NOT NULL DEFAULT 0 COMMENT '是否主要负责人',
    start_date      DATE         DEFAULT NULL,
    end_date        DATE         DEFAULT NULL,
    status          TINYINT      NOT NULL DEFAULT 1,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_project (project_id),
    INDEX idx_user (user_id),
    INDEX idx_role (role_type)
) COMMENT '项目人员关联';
```

#### 3.2.6 交接管理表 ★新增核心模块

```sql
-- ========================================
-- 17. 交接记录表 ★
-- ========================================
CREATE TABLE handover_record (
    id                  BIGINT PRIMARY KEY AUTO_INCREMENT,
    handover_no         VARCHAR(50)  NOT NULL UNIQUE COMMENT '交接单号(自动生成)',
    project_id          BIGINT       NOT NULL COMMENT '项目ID',
    
    -- 交接分类
    handover_type       VARCHAR(10)  NOT NULL COMMENT '交接类型: IMPL=实施→运维 REGION=区域/组间',
    handover_category   VARCHAR(10)  DEFAULT NULL COMMENT '组内/组外: INNER/OUTER',
    handover_batch      VARCHAR(20)  DEFAULT NULL COMMENT '交接批次: 第一批~第四批',
    
    -- 交接双方
    from_user_id        BIGINT       DEFAULT NULL COMMENT '交出人ID',
    from_user_name      VARCHAR(50)  NOT NULL COMMENT '交出人(提出交接人)',
    from_group_id       BIGINT       DEFAULT NULL COMMENT '原组别',
    from_supervisor_id  BIGINT       DEFAULT NULL COMMENT '原主管',
    from_supervisor_name VARCHAR(50) DEFAULT NULL COMMENT '原实施主管姓名',
    
    to_user_id          BIGINT       DEFAULT NULL COMMENT '对接人ID',
    to_user_name        VARCHAR(50)  DEFAULT NULL COMMENT '交接对接人',
    to_supervisor_id    BIGINT       DEFAULT NULL COMMENT '对接人主管ID',
    to_supervisor_name  VARCHAR(50)  DEFAULT NULL COMMENT '交接对接人主管',
    to_service_area     VARCHAR(50)  DEFAULT NULL COMMENT '接收方服务区域',
    
    -- 流程状态
    stage               VARCHAR(30)  NOT NULL DEFAULT '未发' COMMENT '交接现阶段',
    -- 可选值: 未发/已发邮件/没收到邮件/交接文档整理中/交接中/已交接/更换系统不交接
    can_handover        VARCHAR(10)  DEFAULT NULL COMMENT '是否可以交接: 是/否/待确认',
    
    -- 邮件与确认
    email_sent_date     DATE         DEFAULT NULL COMMENT '交接邮件发送日期',
    confirm_user_id     BIGINT       DEFAULT NULL,
    confirm_user_name   VARCHAR(50)  DEFAULT NULL COMMENT '确认人',
    confirm_date        DATE         DEFAULT NULL COMMENT '确认日期',
    
    -- 项目情况简述 (来自区域交接表)
    project_brief       TEXT         DEFAULT NULL COMMENT '项目情况简述(月需求/科室/IT能力/沟通/大需求)',
    
    remark              TEXT         DEFAULT NULL,
    created_by          BIGINT       DEFAULT NULL,
    created_at          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX idx_project (project_id),
    INDEX idx_stage (stage),
    INDEX idx_type (handover_type),
    INDEX idx_batch (handover_batch),
    INDEX idx_from_user (from_user_id),
    INDEX idx_to_user (to_user_id)
) COMMENT '交接记录';

-- ========================================
-- 18. 交接流程日志
-- ========================================
CREATE TABLE handover_flow_log (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    handover_id     BIGINT       NOT NULL COMMENT '交接记录ID',
    from_stage      VARCHAR(30)  NOT NULL COMMENT '原阶段',
    to_stage        VARCHAR(30)  NOT NULL COMMENT '新阶段',
    operator_id     BIGINT       DEFAULT NULL,
    operator_name   VARCHAR(50)  DEFAULT NULL,
    remark          VARCHAR(500) DEFAULT NULL,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_handover (handover_id)
) COMMENT '交接状态流转日志';
```

#### 3.2.7 巡检与报告表

```sql
-- ========================================
-- 19. 巡检记录表
-- ========================================
CREATE TABLE inspection_record (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    project_id      BIGINT       NOT NULL,
    hospital_id     BIGINT       NOT NULL,
    inspector_id    BIGINT       DEFAULT NULL COMMENT '巡检人',
    inspector_name  VARCHAR(50)  DEFAULT NULL,
    
    plan_date       DATE         DEFAULT NULL COMMENT '计划巡检日期',
    actual_date     DATE         DEFAULT NULL COMMENT '实际巡检日期',
    inspection_type VARCHAR(20)  DEFAULT NULL COMMENT '巡检方式: 现场/远程',
    status          VARCHAR(20)  NOT NULL DEFAULT '已计划' COMMENT '已计划/执行中/已完成/已取消',
    
    -- 巡检结果
    result_summary  TEXT         DEFAULT NULL COMMENT '巡检结果摘要',
    issues_found    INT          DEFAULT 0 COMMENT '发现问题数',
    issues_resolved INT          DEFAULT 0 COMMENT '已解决问题数',
    
    remark          TEXT         DEFAULT NULL,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX idx_project (project_id),
    INDEX idx_hospital (hospital_id),
    INDEX idx_plan_date (plan_date),
    INDEX idx_status (status)
) COMMENT '巡检记录';

-- ========================================
-- 20. 年度服务报告
-- ========================================
CREATE TABLE annual_report (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    project_id      BIGINT       NOT NULL,
    hospital_id     BIGINT       NOT NULL,
    report_year     YEAR         NOT NULL COMMENT '报告年度',
    
    group_leader    VARCHAR(50)  DEFAULT NULL COMMENT '组长',
    service_person  VARCHAR(50)  DEFAULT NULL COMMENT '服务人员',
    
    -- 报告状态
    status          VARCHAR(20)  NOT NULL DEFAULT '未开始' COMMENT '未开始/编写中/已提交/已审核/已完成',
    submit_date     DATE         DEFAULT NULL,
    
    -- 关联文件
    report_file_url VARCHAR(500) DEFAULT NULL COMMENT '报告文件路径',
    
    remark          TEXT         DEFAULT NULL,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    UNIQUE INDEX uk_project_year (project_id, report_year),
    INDEX idx_hospital (hospital_id),
    INDEX idx_year (report_year),
    INDEX idx_status (status)
) COMMENT '年度服务报告';
```

#### 3.2.8 驻场管理

```sql
-- ========================================
-- 21. 驻场安排表 ★新增
-- ========================================
CREATE TABLE onsite_assignment (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    project_id      BIGINT       NOT NULL COMMENT '项目ID',
    hospital_id     BIGINT       NOT NULL COMMENT '医院ID',
    user_id         BIGINT       DEFAULT NULL COMMENT '驻场人员ID',
    personnel_name  VARCHAR(50)  NOT NULL COMMENT '驻场人员姓名',
    onsite_location VARCHAR(100) DEFAULT NULL COMMENT '驻地',
    headcount       INT          NOT NULL DEFAULT 1 COMMENT '驻场人数',
    start_date      DATE         DEFAULT NULL,
    end_date        DATE         DEFAULT NULL,
    status          TINYINT      NOT NULL DEFAULT 1 COMMENT '1=在岗 0=已撤离',
    remark          VARCHAR(256) DEFAULT NULL,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_project (project_id),
    INDEX idx_hospital (hospital_id),
    INDEX idx_user (user_id)
) COMMENT '驻场安排';
```

### 3.3 表关系索引图

```
                        sys_user (用户)
                       ╱    │    ╲
                      ╱     │     ╲
            sys_user_role   │   project_personnel
                  │         │         │
             sys_role       │      project ◄────── product_deploy ──── product
                  │         │      │  │  │                              │
         sys_role_permission│      │  │  └── handover_record      product_category
                  │         │      │  │          │
          sys_permission    │      │  │    handover_flow_log
                            │      │  │
              team_department      │  ├── inspection_record
                    │              │  │
               team_group          │  ├── annual_report
                                   │  │
               service_area        │  └── onsite_assignment
                                   │
                              hospital
```

---

## 四、核心模块设计（10 大模块）

### 4.1 模块总览

| # | 模块 | 路由前缀 | 核心功能 | 数据来源 |
|---|------|---------|---------|---------|
| 1 | 驾驶舱/仪表盘 | /dashboard | 全局概览、多维统计、预警 | 全部4表 |
| 2 | 项目管理 | /project | 项目台账 CRUD、批量导入 | 王鸣总表 + 巡检统计 |
| 3 | 合同管理 | /contract | 合同全生命周期、超期预警 | 王鸣总表 |
| 4 | 交接管理 ★ | /handover | 实施/区域交接流程 + 审批 | 实施交接表 + 区域交接表 |
| 5 | 巡检管理 | /inspection | 巡检计划排程、执行、报告 | 巡检统计表 |
| 6 | 医院/客户管理 ★ | /hospital | 客户档案、等级、评级 | 全部4表 |
| 7 | 人员管理 | /personnel | 组织架构、驻场、工作量 | 全部4表 |
| 8 | 产品管理 ★ | /product | 产品字典、部署统计 | 全部4表 |
| 9 | 报告中心 | /report | 多维度分析报表、导出 | 全部4表 |
| 10 | 系统管理 | /system | 用户/角色/权限/字典/日志 | — |

### 4.2 驾驶舱（Dashboard）★重点

```
┌─────────────────────────────────────────────────────────────────┐
│ 项目管理平台 — 运营驾驶舱                                        │
├────────┬────────┬────────┬────────┬────────┬────────┬───────────┤
│项目总数 │合同总额 │超期合同 │进行中交接│本月巡检 │驻场人数  │ 待处理    │
│ 1,628  │ ¥2.3亿 │  87   │  23    │  15    │  42    │  12      │
│ +5.2%  │ +12%   │ ⚠警告  │ 进度62%│ 完成率73│         │          │
├────────┴────────┴────────┴────────┴────────┴────────┴───────────┤
│                                                                  │
│  ┌──────────────────────┐  ┌──────────────────────────────────┐  │
│  │  合同状态分布 (饼图)   │  │  超期合同预警 TOP 10 (表格)       │  │
│  │                      │  │                                  │  │
│  │  ● 合同已签署  42%   │  │  医院名称   超期天数  金额  负责人  │  │
│  │  ● 免费维护期  28%   │  │  ─────────────────────────────── │  │
│  │  ● 超期未签署  15%   │  │  XX医院    245天   120万  张三    │  │
│  │  ● 维护超期    8%    │  │  YY医院    180天   85万   李四    │  │
│  │  ● 未终验      5%    │  │  ...                             │  │
│  │  ● 停止维护    2%    │  │                                  │  │
│  └──────────────────────┘  └──────────────────────────────────┘  │
│                                                                  │
│  ┌──────────────────────┐  ┌──────────────────────────────────┐  │
│  │  省份分布热力地图      │  │  交接进度看板                     │  │
│  │                      │  │                                  │  │
│  │  [中国地图着色]       │  │  未发邮件 ████░░░░  12            │  │
│  │  颜色深度=项目数量    │  │  已发邮件 ██████░░  18            │  │
│  │                      │  │  交接中   ████████  24            │  │
│  │  北京: 235 ▪        │  │  已交接   ██████████████  56       │  │
│  │  江苏: 180 ▪        │  │                                  │  │
│  │  四川: 120 ▪        │  │                                  │  │
│  └──────────────────────┘  └──────────────────────────────────┘  │
│                                                                  │
│  ┌──────────────────────┐  ┌──────────────────────────────────┐  │
│  │  组别项目数排名(柱状图)│  │  产品部署 TOP 10 (横向柱状图)     │  │
│  │                      │  │                                  │  │
│  │  张茹   ████████ 280 │  │  电子病历V6  ████████████ 420    │  │
│  │  何道飞 ██████   210 │  │  临床路径V6  ████████    280     │  │
│  │  李贝   █████    185 │  │  CDSS       ██████      195     │  │
│  │  舒高成 ████     150 │  │  病案归档    █████       170     │  │
│  │  ...                 │  │  AI质控     ████        130      │  │
│  └──────────────────────┘  └──────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### 4.3 项目管理模块

#### 4.3.1 项目台账（主列表）

**核心概念**：一条项目记录 = 一家医院 × 一个主合同/SAP 号

| 列表字段 | 说明 | 筛选 | 排序 |
|----------|------|------|------|
| 项目编号 | 自动生成 | — | ✓ |
| 组别 | 张茹/何道飞等 | 下拉多选 | ✓ |
| 省份/城市 | — | 下拉联动 | ✓ |
| 医院名称 | — | 模糊搜索 | ✓ |
| 医院等级 | 三甲/三级/二甲/二级/民营 | 下拉多选 | ✓ |
| 客户等级 | A/B/C | 下拉 | ✓ |
| 上线产品 | 多产品逗号分隔 | 下拉多选 | — |
| 合同状态 | 带色标签 | 下拉多选 | ✓ |
| 维护起止 | — | 日期范围 | ✓ |
| 超期天数 | 红色高亮>90天 | 范围筛选 | ✓ |
| 销售合同额 | — | 范围筛选 | ✓ |
| 维护合同额 | — | 范围筛选 | ✓ |
| 服务人员 | — | 模糊搜索 | — |
| 驻场 | 是/否标签 | 下拉 | — |
| 操作 | 查看/编辑/交接/巡检 | — | — |

**筛选器设计**（折叠式高级筛选）：

```
┌─────────────────────────────────────────────────────────────────┐
│ 🔍 基础筛选: [组别 ▼] [省份 ▼] [医院名称____] [合同状态 ▼]      │
│                                                                  │
│ ▽ 高级筛选                                                       │
│ ┌─────────────────────────────────────────────────────────────┐  │
│ │ 医院等级: [全部▼]  客户等级: [全部▼]  产品: [全部▼]          │  │
│ │ 维护时间: [____] 至 [____]  超期: [全部/是/否▼]              │  │
│ │ 合同金额: [____] 至 [____]  驻场: [全部/是/否▼]              │  │
│ │                                    [搜索] [重置] [导出Excel]  │  │
│ └─────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

#### 4.3.2 项目详情页（Tab 布局）

```
┌─────────────────────────────────────────────────────────────┐
│ ← 返回  项目详情: XX人民医院                                  │
├─────────────────────────────────────────────────────────────┤
│ [基本信息] [合同与产品] [交接记录] [巡检/报告] [驻场] [操作日志] │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Tab 1: 基本信息                                             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ 医院名称: XX人民医院      省份/城市: 北京/海淀          │   │
│  │ 医院等级: 三甲 [蓝标签]    客户等级: A [红标签]         │   │
│  │ 客户编码: 10086            机会号: OPP-2024-001       │   │
│  │ 组别: 何道飞               服务区域: 北区服务组         │   │
│  │ 服务人员: 张三、李四        驻场: 是(2人)              │   │
│  │ 部门销售: 王五              公司销售: 赵六             │   │
│  │ IT联系人: 刘主任            对接能力: 强               │   │
│  │ 月均需求: 15条/月           科室数: 32                 │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  Tab 2: 合同与产品                                           │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ 合同状态: 合同已签署 [绿色]                            │   │
│  │ 销售合同额: ¥120万          维护合同额: ¥18万/年       │   │
│  │ 维护期限: 2024-01-01 ~ 2024-12-31 (12个月)           │   │
│  │ 超期: 否                                              │   │
│  │ 验收单: 有 ✓    售后确认单: 有 ✓                       │   │
│  │                                                       │   │
│  │ 部署产品列表:                                          │   │
│  │ ┌─────────────────────────────────────────────────┐   │   │
│  │ │ 产品名称         版本    上线日期    状态         │   │   │
│  │ │ 住院电子病历      V6     2023-06    运行中       │   │   │
│  │ │ 临床路径          V6     2023-08    运行中       │   │   │
│  │ │ CDSS             V2     2023-10    运行中       │   │   │
│  │ │ AI内涵质控        V1     2024-03    试运行       │   │   │
│  │ └─────────────────────────────────────────────────┘   │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  Tab 3: 交接记录 (时间线视图)                                │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ ○ 2024-03-15 第一批 实施→运维 侯海亮→张茹 已交接 ✓   │   │
│  │ │                                                     │   │
│  │ ○ 2024-06-20 组内交接 张茹→何道飞 交接中 ●            │   │
│  │   └ 交接邮件已发 2024-06-18                           │   │
│  │   └ 项目简述: 月需求15条，IT能力强，主要为模板调整     │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 4.4 合同管理模块

#### 4.4.1 合同状态流转

```
  ┌──────────┐    签署     ┌──────────────┐
  │  未终验   │───────────▶│  免费维护期    │
  └──────────┘            └──────┬───────┘
                                 │ 到期
                                 ▼
                          ┌──────────────┐    签署    ┌────────────────┐
                          │  超期未签署    │──────────▶│  合同已签署      │
                          └──────┬───────┘           └───────┬────────┘
                                 │ 续签到期                    │ 到期
                                 ▼                            ▼
                          ┌──────────────┐           ┌────────────────┐
                          │  维护超期未签署 │           │  维护合同已签署  │
                          └──────┬───────┘           └────────────────┘
                                 │ 长期不签
                                 ▼
                          ┌──────────────┐
                          │  停止维护      │
                          └──────────────┘
                                or
                          ┌──────────────┐
                          │  更换系统      │
                          └──────────────┘
```

#### 4.4.2 超期预警规则

| 预警级别 | 条件 | 颜色 | 动作 |
|---------|------|------|------|
| 🟡 提醒 | 合同到期前 30 天 | 黄色 | 站内提醒 |
| 🟠 警告 | 合同已超期 1-90 天 | 橙色 | 站内+邮件 |
| 🔴 严重 | 合同超期 > 90 天 | 红色 | 站内+邮件+仪表盘高亮 |
| ⬛ 停止 | 停止维护 | 灰色 | 归档 |

#### 4.4.3 超期合同看板

```
┌─────────────────────────────────────────────────────────────┐
│  超期合同预警                                    [导出] [刷新] │
├──────────┬──────────┬──────────┬──────────────────────────── │
│ 提醒(30) │ 警告(87) │ 严重(23) │ 全部(140)                   │
├──────────┴──────────┴──────────┴──────────────────────────── │
│                                                              │
│  医院名称      组别    产品     超期天数  维护金额    负责人    │
│  ─────────────────────────────────────────────────────────── │
│  XX人民医院    张茹    EMR V6   245     ¥18万/年   张三      │
│  YY中心医院    何道飞  临床路径  180     ¥12万/年   李四      │
│  ...                                                         │
│                                                              │
│  [批量催签] [批量分配跟进人] [导出催签清单]                    │
└─────────────────────────────────────────────────────────────┘
```

### 4.5 交接管理模块 ★核心新增

#### 4.5.1 交接类型

| 类型 | 说明 | 流程 | 数据来源 |
|------|------|------|---------|
| **实施→运维交接** | 实施部门完成上线后移交服务部门 | 提交→发邮件→对接→确认 | 实施交接项目表 |
| **区域/组间交接** | 服务组之间调整负责项目 | 提出→审批→移交→确认 | 区域交接项目表 |
| **组内交接** | 同组内人员变更 | 提出→交接→确认 | 区域交接项目表 |

#### 4.5.2 交接流程状态机

```
实施→运维交接:
  未发 ──▶ 已发邮件 ──▶ 交接文档整理中 ──▶ 交接中 ──▶ 已交接
   │                                                    
   └── 更换系统不交接（终态）
   └── 没收到邮件（异常态，需重发）

区域/组间交接:
  提出交接 ──▶ 审批中 ──▶ 交接中 ──▶ 已交接
```

#### 4.5.3 交接管理页面

```
┌─────────────────────────────────────────────────────────────────┐
│  交接管理                                                        │
├────────────────┬────────────────────────────────────────────────┤
│ [实施→运维交接] │ [区域/组间交接]                                  │
├────────────────┴────────────────────────────────────────────────┤
│                                                                  │
│  筛选: [批次▼] [交接阶段▼] [原组别▼] [对接人▼] [搜索________]    │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 交接单号  医院     原组别    原主管    对接人   批次   阶段  │   │
│  │─────────────────────────────────────────────────────────-│   │
│  │ HO-001  XX医院   实施一组  侯海亮   张茹    第一批 已交接 │   │
│  │ HO-002  YY医院   实施二组  张育明   何道飞  第二批 已发邮件│   │
│  │ HO-003  ZZ医院   HIP实施  沈岩石   李贝    第三批 未发    │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  交接看板 (拖拽式 Kanban):                                       │
│  ┌────────┐ ┌─────────┐ ┌──────────┐ ┌────────┐ ┌────────┐    │
│  │  未发   │ │ 已发邮件 │ │ 文档整理中│ │ 交接中  │ │ 已交接  │    │
│  │  (12)  │ │  (18)   │ │   (5)    │ │  (8)   │ │  (67)  │    │
│  │        │ │         │ │          │ │        │ │        │    │
│  │ ┌────┐│ │ ┌──── ┐│ │ ┌──────┐│ │ ┌────┐│ │ ┌────┐│    │
│  │ │XX院││ │ │YY院 ││ │ │ZZ院  ││ │ │AA院││ │ │BB院││    │
│  │ └────┘│ │ └─────┘│ │ └──────┘│ │ └────┘│ │ └────┘│    │
│  │ ┌────┐│ │ ┌─────┐│ │          │ │ ┌────┐│ │ ┌────┐│    │
│  │ │CC院││ │ │DD院 ││ │          │ │ │EE院││ │ │FF院││    │
│  │ └────┘│ │ └─────┘│ │          │ │ └────┘│ │ └────┘│    │
│  └────────┘ └─────────┘ └──────────┘ └────────┘ └────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

#### 4.5.4 交接详情页

```
┌─────────────────────────────────────────────────────────────┐
│ ← 返回  交接详情: HO-002 YY中心医院                          │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  交接信息:                                                    │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ 交接类型: 实施→运维    批次: 第二批    阶段: 已发邮件   │   │
│  │ 交出人: 张育明(实施二组)   原主管: 陈宇               │   │
│  │ 对接人: 何道飞            对接主管: 何道飞            │   │
│  │ 邮件日期: 2024-06-15     确认人: —    确认日期: —     │   │
│  │ 是否可交接: 是                                        │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  项目情况简述:                                                │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ 月需求量: 约15条/月                                    │   │
│  │ 需求类型: 主要为模板调整和报表修改                      │   │
│  │ 临床科室: 32个科室使用                                  │   │
│  │ IT对接: 信息科李主任，技术能力强，沟通顺畅              │   │
│  │ 大需求: 无重大需求，日常运维为主                        │   │
│  │ 特殊说明: 院方要求每两周一次远程巡检                    │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  部署产品:                                                    │
│  [电子病历V6] [门诊病历] [CDSS] [临床路径V6] [AI内涵质控]     │
│                                                              │
│  交接时间线:                                                  │
│  ○──── 2024-06-10 提出交接 (张育明)                          │
│  │                                                           │
│  ●──── 2024-06-15 发送交接邮件                               │
│  │                                                           │
│  ◌──── 待确认...                                             │
│                                                              │
│  [确认交接] [退回] [标记异常]                                  │
└─────────────────────────────────────────────────────────────┘
```

### 4.6 医院/客户管理模块 ★新增

```
┌─────────────────────────────────────────────────────────────┐
│  医院/客户管理                                                │
├─────────────────────────────────────────────────────────────┤
│  筛选: [省份▼] [等级▼] [客户等级▼] [医院名称____] [搜索]     │
│                                                              │
│  医院名称    省份  城市  等级   客户等级 项目数 合同额  状态   │
│  ──────────────────────────────────────────────────────────  │
│  XX人民医院  北京  海淀  三甲    A      5    ¥320万  正常   │
│  YY中心医院  江苏  南京  三甲    B      3    ¥180万  正常   │
│  ZZ医院      四川  成都  二甲    C      2    ¥60万   超期   │
│                                                              │
│  统计卡片:                                                    │
│  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐                │
│  │三甲:120 │ │三级:85  │ │二甲:65  │ │其他:50  │                │
│  │ A:45    │ │ A:20   │ │ A:8    │ │ A:3    │                │
│  │ B:50    │ │ B:40   │ │ B:30   │ │ B:20   │                │
│  │ C:25    │ │ C:25   │ │ C:27   │ │ C:27   │                │
│  └────────┘ └────────┘ └────────┘ └────────┘                │
└─────────────────────────────────────────────────────────────┘
```

### 4.7 人员管理模块（增强）

新增功能：
- **驻场人员管理**：驻场/撤离、驻场地点、驻场人数统计
- **服务人员 → 新服务人员映射**（王鸣总表的人员变更跟踪）
- **工作量看板**：按人员统计负责项目数、超期数、待交接数

```
┌─────────────────────────────────────────────────────────────┐
│  人员工作量看板                                               │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  姓名    组别    负责项目  超期合同  待交接  驻场项目  满意度   │
│  ────────────────────────────────────────────────────────── │
│  张三    何道飞   28       3       1       2       95%     │
│  李四    张茹     22       5       0       0       88%     │
│  王五    李贝     35       2       3       1       92%     │
│                                                              │
│  ┌─────────────────────┐  ┌─────────────────────────────┐  │
│  │ 组别人员分布(树状图)  │  │ 驻场地图(地图标点)            │  │
│  │                     │  │                              │  │
│  │ 张茹组              │  │  [中国地图 + 驻场标点]         │  │
│  │  ├── 张三(28)       │  │  ● 北京(12人)               │  │
│  │  ├── 李四(22)       │  │  ● 南京(5人)                │  │
│  │  └── 王五(15)       │  │  ● 成都(3人)                │  │
│  │ 何道飞组            │  │                              │  │
│  │  ├── 赵六(20)       │  │                              │  │
│  │  └── 钱七(18)       │  │                              │  │
│  └─────────────────────┘  └─────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### 4.8 评级管理模块 ★新增

跟踪医院的电子病历评级与互联互通评级：

```
┌─────────────────────────────────────────────────────────────┐
│  评级管理                                                    │
├──────────────┬──────────────────────────────────────────────┤
│ [电子病历评级] │ [互联互通评级]                                │
├──────────────┴──────────────────────────────────────────────┤
│                                                              │
│  今年参评医院: 45   目标通过: 38   当前通过: 22               │
│                                                              │
│  医院名称    省份  目标级别  当前状态  负责人  上线产品        │
│  ──────────────────────────────────────────────────────────  │
│  XX医院      北京  五级     准备中    张三    EMR V6/CDSS   │
│  YY医院      江苏  四级     已通过 ✓  李四    EMR V6        │
│                                                              │
│  评级通过率趋势 (折线图)                                      │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  100%                                                 │   │
│  │   80%                              ●                  │   │
│  │   60%                    ●                            │   │
│  │   40%          ●                                      │   │
│  │   20%  ●                                              │   │
│  │    0%                                                 │   │
│  │       2021   2022   2023   2024                       │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## 五、RESTful API 设计

### 5.1 API 总览

| 模块 | 前缀 | 端点数 | 说明 |
|------|------|--------|------|
| 认证 | /api/auth | 4 | 登录/登出/刷新/修改密码 |
| 仪表盘 | /api/dashboard | 6 | 统计/预警/看板数据 |
| 项目 | /api/projects | 8 | CRUD + 导入导出 |
| 合同 | /api/contracts | 7 | 管理 + 超期预警 |
| 交接 | /api/handovers | 9 | CRUD + 流程操作 |
| 巡检 | /api/inspections | 6 | 计划 + 执行 |
| 年度报告 | /api/annual-reports | 5 | 报告管理 |
| 医院 | /api/hospitals | 6 | 客户档案管理 |
| 人员 | /api/personnel | 5 | 人员 + 驻场 |
| 产品 | /api/products | 4 | 产品字典 |
| 评级 | /api/ratings | 4 | 评级跟踪 |
| 系统 | /api/system | 8 | 用户/角色/字典/日志 |
| **合计** | | **72** | |

### 5.2 核心 API 详细设计

#### 5.2.1 认证模块

```
POST   /api/auth/login              登录 → { token, refreshToken, userInfo }
POST   /api/auth/logout             登出
POST   /api/auth/refresh            刷新Token
PUT    /api/auth/password           修改密码
```

#### 5.2.2 仪表盘

```
GET    /api/dashboard/stats                 全局统计数据
GET    /api/dashboard/contract-overview     合同状态分布
GET    /api/dashboard/overdue-alerts        超期合同预警 TOP N
GET    /api/dashboard/handover-progress     交接进度统计
GET    /api/dashboard/region-distribution   省份/区域分布
GET    /api/dashboard/product-ranking       产品部署排名
```

#### 5.2.3 项目管理

```
GET    /api/projects                        项目列表(分页+多条件筛选)
  ?page=1&size=20
  &groupId=      // 组别
  &province=     // 省份
  &hospitalName= // 医院名称模糊
  &hospitalGrade=// 医院等级
  &importance=   // 客户等级
  &contractStatus=// 合同状态
  &isOverdue=    // 是否超期
  &productId=    // 产品
  &sortBy=overdueD days&sortDir=desc

GET    /api/projects/:id                    项目详情
POST   /api/projects                        新增项目
PUT    /api/projects/:id                    编辑项目
DELETE /api/projects/:id                    删除项目
POST   /api/projects/import                 Excel批量导入
GET    /api/projects/export                 Excel导出
GET    /api/projects/:id/timeline           项目时间线(合同+交接+巡检)
```

#### 5.2.4 合同管理

```
GET    /api/contracts                       合同列表
GET    /api/contracts/:id                   合同详情
POST   /api/contracts                       新增
PUT    /api/contracts/:id                   编辑
PUT    /api/contracts/:id/status            更新合同状态
GET    /api/contracts/overdue               超期合同列表
GET    /api/contracts/expiring              即将到期合同(30天内)
```

#### 5.2.5 交接管理 ★

```
GET    /api/handovers                       交接列表(分页+筛选)
  ?type=IMPL|REGION
  &stage=
  &batch=
  &fromGroup=
  &toUser=

GET    /api/handovers/:id                   交接详情
POST   /api/handovers                       新建交接
PUT    /api/handovers/:id                   编辑交接
PUT    /api/handovers/:id/stage             推进交接阶段
POST   /api/handovers/:id/send-email        发送交接邮件
PUT    /api/handovers/:id/confirm           确认交接
GET    /api/handovers/:id/flow-log          阶段流转日志
GET    /api/handovers/kanban                看板视图数据
```

#### 5.2.6 巡检管理

```
GET    /api/inspections                     巡检列表
GET    /api/inspections/:id                 巡检详情
POST   /api/inspections                     新增巡检计划
PUT    /api/inspections/:id                 编辑
PUT    /api/inspections/:id/execute         执行巡检(填写结果)
GET    /api/inspections/calendar            巡检日历视图
```

#### 5.2.7 医院/客户

```
GET    /api/hospitals                       医院列表
GET    /api/hospitals/:id                   医院详情(含关联项目)
POST   /api/hospitals                       新增
PUT    /api/hospitals/:id                   编辑
PUT    /api/hospitals/:id/rating            更新评级信息
GET    /api/hospitals/statistics            医院统计(等级/省份分布)
```

#### 5.2.8 人员管理

```
GET    /api/personnel                       人员列表
GET    /api/personnel/:id/projects          人员负责项目
GET    /api/personnel/workload              工作量统计看板
GET    /api/personnel/onsite                驻场人员列表
PUT    /api/personnel/onsite/:id            驻场调整
```

#### 5.2.9 系统管理

```
GET    /api/system/users                    用户管理-列表
POST   /api/system/users                    新增用户
PUT    /api/system/users/:id                编辑用户
GET    /api/system/roles                    角色列表
POST   /api/system/roles                    新增角色
PUT    /api/system/roles/:id/permissions    分配权限
GET    /api/system/dicts/:type              字典数据查询
POST   /api/system/dicts                    新增字典
GET    /api/system/logs                     操作日志查询
```

### 5.3 通用响应格式

```typescript
// 成功响应
{
  "code": 200,
  "message": "success",
  "data": { ... },
  "timestamp": "2024-07-24T10:00:00Z"
}

// 分页响应
{
  "code": 200,
  "message": "success",
  "data": {
    "list": [ ... ],
    "pagination": {
      "page": 1,
      "size": 20,
      "total": 1628,
      "pages": 82
    }
  }
}

// 错误响应
{
  "code": 400,         // 400/401/403/404/500
  "message": "参数校验失败",
  "errors": [
    { "field": "hospitalName", "message": "不能为空" }
  ]
}
```

---

## 六、后端项目结构（.NET 8）

```
PMS.WebAPI/
├── PMS.sln
├── src/
│   ├── PMS.API/                            # API 入口层
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── DashboardController.cs
│   │   │   ├── ProjectController.cs
│   │   │   ├── ContractController.cs
│   │   │   ├── HandoverController.cs       ★
│   │   │   ├── InspectionController.cs
│   │   │   ├── HospitalController.cs       ★
│   │   │   ├── PersonnelController.cs
│   │   │   ├── ProductController.cs        ★
│   │   │   ├── RatingController.cs         ★
│   │   │   ├── AnnualReportController.cs
│   │   │   └── SystemController.cs
│   │   ├── Filters/
│   │   │   ├── GlobalExceptionFilter.cs
│   │   │   └── OperationLogFilter.cs
│   │   ├── Middlewares/
│   │   │   ├── JwtMiddleware.cs
│   │   │   └── RequestLoggingMiddleware.cs
│   │   ├── appsettings.json
│   │   └── Program.cs
│   │
│   ├── PMS.Application/                    # 应用层 (业务逻辑)
│   │   ├── Services/
│   │   │   ├── AuthService.cs
│   │   │   ├── DashboardService.cs
│   │   │   ├── ProjectService.cs
│   │   │   ├── ContractService.cs
│   │   │   ├── HandoverService.cs          ★
│   │   │   ├── InspectionService.cs
│   │   │   ├── HospitalService.cs          ★
│   │   │   ├── PersonnelService.cs
│   │   │   ├── ProductService.cs           ★
│   │   │   ├── RatingService.cs            ★
│   │   │   ├── ImportExportService.cs      # Excel 导入导出
│   │   │   ├── NotificationService.cs      # 预警通知
│   │   │   └── SystemService.cs
│   │   ├── DTOs/
│   │   │   ├── Project/
│   │   │   │   ├── ProjectListDto.cs
│   │   │   │   ├── ProjectDetailDto.cs
│   │   │   │   ├── ProjectCreateDto.cs
│   │   │   │   └── ProjectQueryDto.cs
│   │   │   ├── Handover/
│   │   │   │   ├── HandoverListDto.cs
│   │   │   │   ├── HandoverDetailDto.cs
│   │   │   │   ├── HandoverCreateDto.cs
│   │   │   │   ├── HandoverStageDto.cs
│   │   │   │   └── HandoverKanbanDto.cs
│   │   │   ├── Contract/
│   │   │   ├── Hospital/
│   │   │   ├── Dashboard/
│   │   │   └── Common/
│   │   │       ├── PagedResult.cs
│   │   │       └── ApiResponse.cs
│   │   ├── Mappings/
│   │   │   └── AutoMapperProfile.cs
│   │   └── Validators/
│   │       ├── ProjectValidator.cs
│   │       └── HandoverValidator.cs
│   │
│   ├── PMS.Domain/                         # 领域层 (实体+接口)
│   │   ├── Entities/
│   │   │   ├── SysUser.cs
│   │   │   ├── SysRole.cs
│   │   │   ├── SysPermission.cs
│   │   │   ├── TeamDepartment.cs
│   │   │   ├── TeamGroup.cs
│   │   │   ├── ServiceArea.cs
│   │   │   ├── Hospital.cs
│   │   │   ├── Product.cs
│   │   │   ├── ProductCategory.cs
│   │   │   ├── Project.cs                  ★
│   │   │   ├── ProductDeploy.cs
│   │   │   ├── ProjectPersonnel.cs
│   │   │   ├── HandoverRecord.cs           ★
│   │   │   ├── HandoverFlowLog.cs
│   │   │   ├── InspectionRecord.cs
│   │   │   ├── AnnualReport.cs
│   │   │   ├── OnsiteAssignment.cs         ★
│   │   │   ├── SysDictType.cs
│   │   │   ├── SysDictData.cs
│   │   │   └── OperationLog.cs
│   │   ├── Interfaces/
│   │   │   ├── IProjectRepository.cs
│   │   │   ├── IHandoverRepository.cs
│   │   │   ├── IHospitalRepository.cs
│   │   │   └── IGenericRepository.cs
│   │   └── Enums/
│   │       ├── ContractStatus.cs
│   │       ├── HandoverStage.cs
│   │       ├── HandoverType.cs
│   │       └── HospitalGrade.cs
│   │
│   └── PMS.Infrastructure/                 # 基础设施层
│       ├── Data/
│       │   ├── PmsDbContext.cs
│       │   ├── Configurations/             # EF Fluent 配置
│       │   │   ├── ProjectConfiguration.cs
│       │   │   ├── HandoverConfiguration.cs
│       │   │   └── HospitalConfiguration.cs
│       │   └── Migrations/
│       ├── Repositories/
│       │   ├── ProjectRepository.cs
│       │   ├── HandoverRepository.cs
│       │   ├── HospitalRepository.cs
│       │   └── GenericRepository.cs
│       ├── Caching/
│       │   └── RedisCacheService.cs
│       └── External/
│           └── EmailService.cs
│
├── tests/
│   ├── PMS.UnitTests/
│   └── PMS.IntegrationTests/
└── docker-compose.yml
```

---

## 七、核心业务规则

### 7.1 合同超期自动计算

```csharp
// 每日定时任务 (Hangfire / BackgroundService)
public class ContractOverdueJob : IHostedService
{
    // 每天凌晨 2:00 执行
    public async Task ExecuteAsync()
    {
        var projects = await _repo.GetProjectsWithMaintenanceEnd();
        foreach (var p in projects)
        {
            if (p.MaintenanceEnd < DateTime.Today && 
                p.ContractStatus != "合同已签署" &&
                p.ContractStatus != "维护合同已签署" &&
                p.ContractStatus != "停止维护")
            {
                p.IsOverdue = true;
                p.OverdueDays = (DateTime.Today - p.MaintenanceEnd.Value).Days;
                
                // 自动更新合同状态
                if (p.ContractStatus == "免费维护期")
                    p.ContractStatus = "超期未签署";
                else if (p.ContractStatus == "合同已签署")
                    p.ContractStatus = "维护超期未签署";
            }
        }
        await _repo.SaveChangesAsync();
        
        // 发送预警通知
        await _notifier.SendOverdueAlerts(projects.Where(p => p.IsOverdue));
    }
}
```

### 7.2 交接流程控制

```csharp
// 交接阶段流转规则
public static readonly Dictionary<string, string[]> HandoverStageTransitions = new()
{
    ["未发"]           = new[] { "已发邮件", "更换系统不交接" },
    ["已发邮件"]       = new[] { "交接文档整理中", "交接中", "没收到邮件" },
    ["没收到邮件"]     = new[] { "已发邮件" },  // 重新发送
    ["交接文档整理中"] = new[] { "交接中" },
    ["交接中"]         = new[] { "已交接" },
    ["已交接"]         = Array.Empty<string>(),  // 终态
    ["更换系统不交接"] = Array.Empty<string>(),  // 终态
};

// 交接完成后自动更新项目归属
public async Task CompleteHandover(long handoverId)
{
    var handover = await _repo.GetById(handoverId);
    handover.Stage = "已交接";
    handover.ConfirmDate = DateTime.Today;
    
    // 更新项目负责人/组别
    var project = await _projectRepo.GetById(handover.ProjectId);
    project.GroupId = handover.ToGroupId;
    
    // 更新人员关联
    await _personnelService.TransferProject(
        handover.ProjectId, 
        handover.FromUserId, 
        handover.ToUserId
    );
    
    // 记录流转日志
    await _flowLogRepo.Add(new HandoverFlowLog
    {
        HandoverId = handoverId,
        FromStage = "交接中",
        ToStage = "已交接",
        OperatorId = _currentUser.Id
    });
}
```

### 7.3 数据权限控制

```csharp
// 数据权限过滤 (EF Core Global Query Filter)
public class DataScopeFilter
{
    // data_scope: 
    // 1=全部(admin) 
    // 2=本部门(dept_manager 看本部门所有组) 
    // 3=本组(group_leader 看本组)
    // 4=仅本人(service_staff 只看自己负责的)
    
    public IQueryable<Project> ApplyScope(IQueryable<Project> query, UserContext user)
    {
        return user.DataScope switch
        {
            1 => query,  // 全部
            2 => query.Where(p => p.Group.DepartmentId == user.DepartmentId),
            3 => query.Where(p => p.GroupId == user.GroupId),
            4 => query.Where(p => p.ProjectPersonnel
                        .Any(pp => pp.UserId == user.Id)),
            _ => query.Where(p => false)
        };
    }
}
```

### 7.4 Excel 批量导入逻辑

```csharp
public class ImportExportService
{
    /// <summary>
    /// 导入王鸣总表格式的 Excel
    /// 自动识别列映射、匹配/创建医院、关联产品
    /// </summary>
    public async Task<ImportResult> ImportMasterList(Stream excelStream)
    {
        var result = new ImportResult();
        using var package = new ExcelPackage(excelStream);
        var ws = package.Workbook.Worksheets[0];
        
        for (int row = 3; row <= ws.Dimension.End.Row; row++)
        {
            try
            {
                // 1. 查找或创建医院
                var hospitalName = ws.Cells[row, 7].Text.Trim();
                var province = ws.Cells[row, 3].Text.Trim();
                var hospital = await FindOrCreateHospital(hospitalName, province, ws, row);
                
                // 2. 查找或创建项目
                var groupName = ws.Cells[row, 2].Text.Trim();
                var productName = ws.Cells[row, 12].Text.Trim();  // 项目类别=产品
                var project = await FindOrCreateProject(hospital.Id, groupName, ws, row);
                
                // 3. 关联产品
                var product = await FindOrCreateProduct(productName);
                await LinkProductDeploy(project.Id, product.Id);
                
                // 4. 更新合同信息
                project.ContractStatus = ws.Cells[row, 15].Text.Trim();
                project.SalesAmount = ParseDecimal(ws.Cells[row, 21].Text);
                project.MaintenanceAmount = ParseDecimal(ws.Cells[row, 22].Text);
                project.OverdueDays = ParseInt(ws.Cells[row, 20].Text);
                
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportError(row, ex.Message));
            }
        }
        return result;
    }
}
```

---

## 八、关键页面交互设计

### 8.1 全局导航菜单

```
📊 驾驶舱
📁 项目管理
   ├── 项目台账
   ├── 项目导入
   └── 项目详情
📋 合同管理
   ├── 合同列表
   ├── 超期预警
   └── 续签管理
🔄 交接管理 ★
   ├── 实施→运维交接
   ├── 区域/组间交接
   └── 批次管理
🔍 巡检管理
   ├── 巡检计划
   ├── 巡检记录
   └── 年度服务报告
🏥 医院管理 ★
   ├── 医院档案
   └── 评级管理
👥 人员管理
   ├── 人员列表
   ├── 驻场管理 ★
   └── 工作量看板
📦 产品管理 ★
   ├── 产品字典
   └── 部署统计
📈 报告中心
   ├── 综合分析
   ├── 合同分析
   ├── 交接分析
   └── 区域分析
⚙️ 系统管理
   ├── 用户管理
   ├── 角色管理
   ├── 数据字典
   └── 操作日志
```

### 8.2 多 Tab 标签页

参考 pms.bjgoodwill.com 样式，支持：
- 多标签页打开（最多 20 个）
- 右键菜单（关闭当前/关闭其他/关闭全部）
- 标签页拖拽排序
- 重新加载当前页

### 8.3 Element Plus 组件使用策略

| 场景 | 组件 | 配置 |
|------|------|------|
| 数据表格 | ElTable + ElPagination | 虚拟滚动(1000+行)、列拖拽 |
| 筛选表单 | ElForm + ElInput/ElSelect/ElDatePicker | 折叠展开、记住筛选 |
| 状态标签 | ElTag + 自定义 StatusTag | 6 种颜色映射合同状态 |
| 交接看板 | vuedraggable + ElCard | 拖拽式 Kanban |
| 统计卡片 | ElCard + 自定义 StatCard | 数字动画 + 趋势箭头 |
| 弹窗表单 | ElDialog + ElForm | 新增/编辑共用 |
| 时间线 | ElTimeline | 交接流程/项目历史 |
| 树形组织 | ElTree | 部门-组别-人员 |
| 文件上传 | ElUpload | Excel/附件上传 |
| 日历视图 | ElCalendar + 自定义 | 巡检计划日历 |

---

## 九、数据迁移方案

### 9.1 Excel → 数据库迁移步骤

```
Step 1: 基础数据初始化
        ├── 产品字典 (30+ 产品) → product + product_category
        ├── 组别/部门 → team_department + team_group
        ├── 服务区域 → service_area
        └── 数据字典 → sys_dict_type + sys_dict_data

Step 2: 医院主数据导入
        └── 合并四表的医院数据 → hospital (去重合并)
            去重键: hospital_name + province

Step 3: 项目主数据导入 (王鸣总表为主)
        └── ~1600 条 → project + product_deploy
            关联: hospital_id, group_id, product_id

Step 4: 补充年度报告/巡检数据 (巡检统计表)
        └── ~430 条 → 更新 project + annual_report + inspection_record

Step 5: 导入交接记录 (实施交接表 + 区域交接表)
        └── ~310 条 → handover_record

Step 6: 驻场信息导入 (王鸣总表)
        └── 筛选 is_onsite=是 → onsite_assignment

Step 7: 人员关联补充
        └── 服务人员/新服务人员 → project_personnel
```

### 9.2 数据清洗规则

| 问题 | 处理方式 |
|------|---------|
| 医院名称不一致 | 模糊匹配 + 人工确认映射表 |
| 产品名称版本混杂 | 正则提取产品名+版本号 |
| 多产品在同一单元格 | 分割符: 逗号/顿号/换行 → 拆分为多条 product_deploy |
| 金额格式不一致 | 统一为万元(decimal)，去除"万"/"元"字符 |
| 日期格式混杂 | Excel 序列号 + 文本日期双重解析 |
| 人员姓名去重 | 建立人员映射字典，统一到 sys_user |
| 空行/合并单元格 | 跳过空序号行，向上填充合并单元格值 |

---

## 十、项目实施计划（12-15 周）

### 10.1 分阶段排期

```
第1周    ┃ 项目启动 + 环境搭建
         ┃ ├── 需求确认 & 原型评审
         ┃ ├── 前端: Vite + Vue3 + EP + Tailwind 脚手架
         ┃ ├── 后端: .NET 8 解决方案 + EF Core + MySQL
         ┃ └── CI/CD: Docker + Nginx 配置

第2-3周  ┃ 基础架构 + 系统管理
         ┃ ├── 认证(JWT)+权限(RBAC)+数据权限
         ┃ ├── 通用组件: ProTable/ProForm/StatusTag
         ┃ ├── 布局框架: 侧边栏+多标签页+面包屑
         ┃ ├── 数据字典管理
         ┃ └── 用户/角色管理

第4-5周  ┃ 医院 + 产品 + 项目管理(核心)
         ┃ ├── 医院档案 CRUD + 评级管理
         ┃ ├── 产品字典管理
         ┃ ├── 项目台账 CRUD(主表)
         ┃ ├── 项目详情(多Tab)
         ┃ ├── 项目-产品关联
         ┃ └── Excel 批量导入 ★

第6-7周  ┃ 合同管理 + 超期预警
         ┃ ├── 合同生命周期管理
         ┃ ├── 超期自动计算定时任务
         ┃ ├── 超期预警看板
         ┃ ├── 续签跟踪
         ┃ └── 合同状态流转

第8-9周  ┃ 交接管理 ★核心
         ┃ ├── 实施→运维交接
         ┃ ├── 区域/组间交接
         ┃ ├── 交接看板(Kanban)
         ┃ ├── 交接流程状态机
         ┃ ├── 批次管理
         ┃ └── 项目情况简述表单

第10-11周┃ 巡检 + 人员 + 报告
         ┃ ├── 巡检计划日历
         ┃ ├── 巡检记录管理
         ┃ ├── 年度服务报告管理
         ┃ ├── 人员管理 + 驻场管理
         ┃ └── 工作量看板

第12-13周┃ 仪表盘 + 报告中心
         ┃ ├── 驾驶舱统计大屏(ECharts)
         ┃ ├── 省份地图热力图
         ┃ ├── 多维度分析报表
         ┃ ├── Excel 导出
         ┃ └── 通知/预警中心

第14-15周┃ 数据迁移 + 测试 + 上线
         ┃ ├── 历史数据清洗 & 迁移脚本
         ┃ ├── 四份 Excel 数据导入验证
         ┃ ├── UAT 用户验收测试
         ┃ ├── 性能优化(分页/缓存/索引)
         ┃ └── 生产环境部署
```

### 10.2 里程碑

| 里程碑 | 时间 | 交付物 |
|--------|------|--------|
| M1 - 框架就绪 | 第 3 周末 | 可登录+基础布局+系统管理 |
| M2 - 核心数据 | 第 5 周末 | 项目/医院/产品 CRUD + 导入 |
| M3 - 合同管控 | 第 7 周末 | 合同管理 + 超期预警 |
| M4 - 交接上线 | 第 9 周末 | 完整交接流程 + 看板 |
| M5 - 功能完整 | 第 13 周末 | 仪表盘 + 报表 + 全功能 |
| M6 - 正式上线 | 第 15 周末 | 数据迁移完成 + 生产部署 |

### 10.3 团队配置建议

| 角色 | 人数 | 职责 |
|------|------|------|
| 前端开发 | 2 | Vue 3 + Element Plus 页面开发 |
| 后端开发 | 2 | .NET 8 API + 业务逻辑 + 迁移脚本 |
| 全栈/架构 | 1 | 技术选型 + 核心框架 + Code Review |
| 测试 | 1 | 功能测试 + 数据验证 |
| 产品/业务 | 0.5 | 需求确认 + 数据清洗配合 |

---

## 十一、部署架构

### 11.1 生产环境

```yaml
# docker-compose.yml
version: '3.8'
services:
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./dist:/usr/share/nginx/html    # Vue 前端
      - ./ssl:/etc/nginx/ssl

  api:
    build: ./src/PMS.API
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__Default=Server=mysql;Database=pms;Uid=root;Pwd=${DB_PWD}
      - Redis__Connection=redis:6379
    depends_on:
      - mysql
      - redis

  mysql:
    image: mysql:8.0
    ports:
      - "3306:3306"
    environment:
      MYSQL_ROOT_PASSWORD: ${DB_PWD}
      MYSQL_DATABASE: pms
    volumes:
      - mysql_data:/var/lib/mysql
      - ./init.sql:/docker-entrypoint-initdb.d/init.sql

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

volumes:
  mysql_data:
  redis_data:
```

### 11.2 Nginx 配置

```nginx
server {
    listen 80;
    server_name pms.example.com;

    # Vue SPA
    location / {
        root /usr/share/nginx/html;
        try_files $uri $uri/ /index.html;
        gzip on;
        gzip_types text/css application/javascript application/json;
    }

    # API 反向代理
    location /api/ {
        proxy_pass http://api:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }

    # WebSocket (如需实时通知)
    location /ws/ {
        proxy_pass http://api:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
```

---

## 十二、与原始设计方案的对比

| 维度 | v1.0 (原方案) | v2.0 (综合方案) | 变化 |
|------|-------------|---------------|------|
| 数据源 | 1 份 Excel | 4 份 Excel | +3 新数据源 |
| 数据库表 | 7 张 | 21 张 | +14 张 |
| 功能模块 | 7 个 | 10 个 | +交接/医院/产品 |
| API 端点 | ~18 个 | ~72 个 | 4x |
| 核心实体 | 项目+合同+巡检 | +交接+医院+产品+驻场+评级 | 5 个新实体 |
| 项目数据 | ~430 行 | ~3000+ 行 | 7x |
| 产品种类 | ~10 种 | ~35 种 | 3.5x |
| 合同状态 | 简单 | 7 种状态+超期计算 | 完整生命周期 |
| 交接管理 | 无 | 完整流程+看板+简述 | **全新模块** |
| 客户分级 | 无 | A/B/C + 公司/部门 | **全新维度** |
| 驻场管理 | 无 | 人员+地点+人数 | **全新功能** |
| 评级管理 | 无 | 电子病历+互联互通 | **全新功能** |
| 实施周期 | 8-11 周 | 12-15 周 | +4 周 |

---

## 十三、风险与应对

| 风险 | 等级 | 应对策略 |
|------|------|---------|
| 四表数据医院名称不一致 | 高 | 建立医院映射字典，开发模糊匹配工具，人工确认 |
| 数据量大导致查询慢 | 中 | 分页+索引+Redis缓存+异步导出 |
| 业务规则复杂交接流程 | 中 | 状态机模式 + 流转日志 + 可视化 |
| 人员角色权限复杂 | 中 | RBAC + 数据权限(全部/部门/组/个人) |
| Excel 格式多变 | 中 | 模板下载 + 导入预览 + 错误报告 |
| 需求变更频繁 | 低 | 字典驱动 + 配置化 + 模块解耦 |

---

> **总结**：v2.0 方案完整覆盖了"项目签约→实施上线→运维交接→日常运维→合同续签"全生命周期管理，
> 新增交接管理、医院/客户分级、驻场管理、评级跟踪四大核心模块，数据库从 7 表扩展到 21 表，
> API 72 个端点，支撑 3000+ 项目记录的高效管理。
