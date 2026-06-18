<template>
  <div class="page-shell">
    <div class="inspection-hero">
      <div class="inspection-hero-main">
        <div class="inspection-hero-kicker-row">
          <span class="inspection-hero-kicker">Inspection Operations Hub</span>
          <span class="inspection-hero-badge">{{ inspectionFilterLabel }}</span>
        </div>
        <h2 class="inspection-hero-title">巡检管理</h2>
        <div class="inspection-hero-subtitle">管理巡检排期、执行状态与实际巡检结果，先在首屏确认计划负载和执行压力，再切入计划与结果双视图继续处理。</div>

        <div class="inspection-hero-signals">
          <div v-for="item in activeInspectionHeroSignals" :key="item.label" class="inspection-signal-card">
            <span class="inspection-signal-label">{{ item.label }}</span>
            <strong class="inspection-signal-value">{{ item.value }}</strong>
            <span class="inspection-signal-note">{{ item.note }}</span>
          </div>
        </div>
      </div>

      <div class="inspection-hero-side">
        <div class="inspection-control-card">
          <div class="inspection-control-copy">
            <span class="inspection-control-title">巡检动作</span>
            <span class="inspection-control-note">当前计划 {{ total }} 条，结果 {{ resultTotal }} 条，可直接切换视图或创建计划。</span>
          </div>
          <div class="inspection-control-actions">
            <el-button size="small" @click="onTabChange('plan')">计划视图</el-button>
            <el-button size="small" @click="onTabChange('results')">结果视图</el-button>
            <el-button v-if="canManageInspection && activeTab === 'plan'" size="small" type="primary" @click="onOpenPlanCreate" icon="Plus">新增巡检计划</el-button>
          </div>
        </div>

        <div class="inspection-quick-grid">
          <button v-for="action in activeInspectionQuickActions" :key="action.title" type="button" class="inspection-quick-action" @click="action.onClick()">
            <span class="inspection-quick-title">{{ action.title }}</span>
            <span class="inspection-quick-note">{{ action.note }}</span>
          </button>
        </div>
      </div>
    </div>

    <el-tabs v-model="activeTab" class="main-tabs" @tab-change="onTabChange">
      <!-- ===================== 巡检计划 Tab ===================== -->
      <el-tab-pane label="巡检计划" name="plan">
        <SummaryMetrics :items="planSummaryCards" :columns="6" @select="onPlanSummaryCardSelect" />

        <div class="inspection-insight-grid">
          <section class="inspection-insight-card">
            <div class="inspection-insight-head">
              <div>
                <div class="inspection-insight-title">优先推进计划</div>
                <div class="inspection-insight-note">先看已逾期、执行中和高优先级计划，直接进入详情或编辑继续推进。</div>
              </div>
              <el-tag size="small" type="warning" effect="light">{{ planPriorityQueue.length }} 项</el-tag>
            </div>
            <div v-if="planPriorityQueue.length" class="inspection-queue-list">
              <button
                v-for="item in planPriorityQueue"
                :key="item.id"
                type="button"
                class="inspection-queue-item"
                @click="onOpenPlanDetail(item)"
              >
                <div class="inspection-queue-main">
                  <strong>{{ item.hospitalName }}</strong>
                  <span>{{ item.productName || '未填写产品' }} · {{ item.inspector || '未分配巡检人' }}</span>
                </div>
                <div class="inspection-queue-meta">
                  <el-tag size="small" :type="statusTag(item.status)">{{ item.status }}</el-tag>
                  <span>{{ formatPlanSlaText(item) }}</span>
                </div>
              </button>
            </div>
            <el-empty v-else description="当前筛选下没有需要优先推进的巡检计划" :image-size="72" />
          </section>

          <section class="inspection-insight-card">
            <div class="inspection-insight-head">
              <div>
                <div class="inspection-insight-title">医院关注</div>
                <div class="inspection-insight-note">查看计划最集中的医院，便于主管按医院维度安排巡检节奏。</div>
              </div>
              <span class="inspection-insight-meta">{{ filteredPlanRows.length }} 条计划</span>
            </div>
            <div v-if="topPlanHospitalBuckets.length" class="inspection-chip-list">
              <div v-for="item in topPlanHospitalBuckets" :key="item.label" class="inspection-chip">
                <strong>{{ item.label }}</strong>
                <span>{{ item.value }} 条</span>
              </div>
            </div>
            <el-empty v-else description="暂无医院分布" :image-size="72" />
          </section>

          <section class="inspection-insight-card">
            <div class="inspection-insight-head">
              <div>
                <div class="inspection-insight-title">巡检人负载</div>
                <div class="inspection-insight-note">按未闭环计划看巡检人当前负载，帮助主管判断是否需要分流。</div>
              </div>
              <span class="inspection-insight-meta">{{ openPlanCount }} 项未闭环</span>
            </div>
            <div v-if="topInspectorBuckets.length" class="inspection-chip-list">
              <div v-for="item in topInspectorBuckets" :key="item.label" class="inspection-chip">
                <strong>{{ item.label }}</strong>
                <span>{{ item.value }} 条</span>
              </div>
            </div>
            <el-empty v-else description="暂无巡检人负载" :image-size="72" />
          </section>

          <section class="inspection-insight-card">
            <div class="inspection-insight-head">
              <div>
                <div class="inspection-insight-title">方式与优先级结构</div>
                <div class="inspection-insight-note">快速判断当前是现场还是远程为主，以及高优先级计划是否在积压。</div>
              </div>
              <span class="inspection-insight-meta">{{ overduePlanCount }} 项逾期</span>
            </div>
            <div v-if="planStructureBuckets.length" class="inspection-chip-list">
              <div v-for="item in planStructureBuckets" :key="item.label" class="inspection-chip inspection-chip--soft">
                <strong>{{ item.label }}</strong>
                <span>{{ item.value }} 条</span>
              </div>
            </div>
            <el-empty v-else description="暂无方式与优先级结构" :image-size="72" />
          </section>
        </div>

  

        <ProTable
      title="明细数据"
      :data="tableData"
      :loading="loading"
      :total="total"
      v-model:page="query.page"
      v-model:size="query.size"
      @refresh="loadData"
      @pagination-change="loadData"
      stripe
      row-key="id"
      empty-text="暂无符合条件的数据"
      @row-dblclick="onPlanRowDoubleClick"
    >
      <template #toolbar>
        <el-button :loading="exporting" @click="onExport" icon="Download">导出CSV</el-button>
      </template>

      <template #search>
      <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
            <el-form-item label="医院">
              <el-input v-model="query.hospitalName" clearable style="width: 200px" placeholder="请输入医院名称" />
            </el-form-item>
            <el-form-item label="状态">
              <el-select v-model="query.status" clearable style="width: 140px" placeholder="全部">
                <el-option v-for="status in statusOptions" :key="status" :label="status" :value="status" />
              </el-select>
            </el-form-item>
            <el-form-item label="省份">
              <el-select v-model="query.province" clearable style="width: 140px" placeholder="全部">
                <el-option v-for="province in provinceOptions" :key="province" :label="province" :value="province" />
              </el-select>
            </el-form-item>
            <el-form-item label="产品">
              <el-select
                v-model="query.productName"
                clearable
                filterable
                default-first-option
                style="width: 180px"
                placeholder="全部"
              >
                <el-option v-for="product in productOptions" :key="product" :label="product" :value="product" />
              </el-select>
            </el-form-item>
            <el-form-item label="组别">
              <el-select v-model="query.groupName" clearable style="width: 160px" placeholder="全部">
                <el-option v-for="group in filteredGroupOptions" :key="group" :label="group" :value="group" />
              </el-select>
            </el-form-item>
            <el-form-item label="巡检人">
              <el-select v-model="query.inspector" clearable style="width: 140px" placeholder="全部">
                <el-option v-for="person in inspectorOptions" :key="person" :label="person" :value="person" />
              </el-select>
            </el-form-item>
            <el-form-item label="方式">
              <el-select v-model="query.inspectionType" clearable style="width: 120px" placeholder="全部">
                <el-option label="现场" value="现场" />
                <el-option label="远程" value="远程" />
              </el-select>
            </el-form-item>
            <el-form-item label="优先级">
              <el-select v-model="query.priority" clearable style="width: 120px" placeholder="全部">
                <el-option label="高" value="高" />
                <el-option label="中" value="中" />
                <el-option label="低" value="低" />
              </el-select>
            </el-form-item>
            <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch" icon="Search">查询</el-button>
          <el-button @click="onReset" icon="Refresh">重置</el-button>
        </el-form-item>
          </el-form>
    </template>

    
    
            <el-table-column prop="hospitalName" label="医院" min-width="220" show-overflow-tooltip sortable />
            <el-table-column prop="productName" label="产品" min-width="180" show-overflow-tooltip sortable />
            <el-table-column prop="province" label="省份" width="100" show-overflow-tooltip sortable />
            <el-table-column prop="hospitalLevel" label="医院级别" width="110" show-overflow-tooltip />
            <el-table-column prop="groupName" label="组别" width="140" show-overflow-tooltip />
            <el-table-column prop="inspector" label="巡检人" width="120" show-overflow-tooltip />
            <el-table-column prop="inspectionType" label="方式" width="90" />
            <el-table-column prop="priority" label="优先级" width="90">
              <template #default="scope">
                <el-tag :type="scope.row.priority === '高' ? 'danger' : scope.row.priority === '中' ? 'warning' : 'info'">{{ scope.row.priority || '-' }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="remarks" label="备注" min-width="220" show-overflow-tooltip />
            <el-table-column prop="planDate" label="计划日期" width="120" sortable>
              <template #default="scope">{{ formatDate(scope.row.planDate) }}</template>
            </el-table-column>
            <el-table-column prop="slaDueAt" label="时效" width="190">
              <template #default="scope">
                <div class="deadline-cell">
                  <span>{{ scope.row.slaDueAt ? formatDateTime(scope.row.slaDueAt) : '-' }}</span>
                  <el-tag v-if="scope.row.slaDueAt" size="small" :type="planSlaTagType(scope.row)">
                    {{ formatPlanSlaText(scope.row) }}
                  </el-tag>
                </div>
              </template>
            </el-table-column>
            <el-table-column prop="actualDate" label="实际日期" width="120">
              <template #default="scope">{{ scope.row.actualDate ? formatDate(scope.row.actualDate) : '-' }}</template>
            </el-table-column>
            <el-table-column prop="status" label="状态" width="100" sortable>
              <template #default="scope">
                <el-tag :type="statusTag(scope.row.status)">{{ scope.row.status }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="460" fixed="right">
              <template #default="scope">
                <div class="table-action-group">
                  <el-button v-if="canManageInspection && canStartPlan(scope.row.status)" link type="primary" :loading="isPlanActionLoading(scope.row.id, 'start')" :disabled="isPlanRowBusy(scope.row.id)" @click="onStartPlan(scope.row)">开始执行</el-button>
                  <el-button v-if="canManageInspection && canCompletePlan(scope.row.status)" link type="success" :loading="isPlanActionLoading(scope.row.id, 'complete')" :disabled="isPlanRowBusy(scope.row.id)" @click="onCompletePlan(scope.row)">完成巡检</el-button>
                  <el-button v-if="canManageInspection && canReopenPlan(scope.row.status)" link type="warning" :loading="isPlanActionLoading(scope.row.id, 'reopen')" :disabled="isPlanRowBusy(scope.row.id)" @click="onReopenPlan(scope.row)">重开计划</el-button>
                  <el-button v-if="canManageInspection" link type="primary" @click="onOpenPlanEdit(scope.row)" icon="Edit">编辑</el-button>
                  <el-button v-if="canManageInspection" link type="danger" @click="onDeletePlan(scope.row)" icon="Delete">删除</el-button>
                  <el-button link type="primary" @click="onOpenPlanDetail(scope.row)" icon="Document">详情</el-button>
                  <el-button v-if="isSameStatus(scope.row.status, '已完成')" link @click="goToResultTab(scope.row)" icon="View">查看结果</el-button>
                </div>
              </template>
            </el-table-column>
    


    
    </ProTable>
      </el-tab-pane>

      <!-- ===================== 巡检结果 Tab ===================== -->
      <el-tab-pane label="巡检结果" name="results">
        <!-- 结果统计概览 -->
        <SummaryMetrics :items="resultSummaryCards" :columns="4" @select="onResultSummaryCardSelect" />

        <div class="inspection-insight-grid">
          <section class="inspection-insight-card">
            <div class="inspection-insight-head">
              <div>
                <div class="inspection-insight-title">重点结果清单</div>
                <div class="inspection-insight-note">先看严重和警告结果，再进入详情核对风险项和环境状态。</div>
              </div>
              <el-tag size="small" type="danger" effect="light">{{ resultPriorityQueue.length }} 项</el-tag>
            </div>
            <div v-if="resultPriorityQueue.length" class="inspection-queue-list">
              <button
                v-for="item in resultPriorityQueue"
                :key="item.id"
                type="button"
                class="inspection-queue-item"
                @click="showResultDetail(item)"
              >
                <div class="inspection-queue-main">
                  <strong>{{ item.hospitalName }}</strong>
                  <span>{{ item.productName || '未填写产品' }} · {{ item.inspector || '未填写巡检人' }}</span>
                </div>
                <div class="inspection-queue-meta">
                  <el-tag size="small" :type="healthTag(item.healthLevel)">{{ item.healthLevel }}</el-tag>
                  <span>{{ item.warningCount + item.criticalCount }} 项风险</span>
                </div>
              </button>
            </div>
            <el-empty v-else description="当前筛选下没有重点巡检结果" :image-size="72" />
          </section>

          <section class="inspection-insight-card">
            <div class="inspection-insight-head">
              <div>
                <div class="inspection-insight-title">医院分布</div>
                <div class="inspection-insight-note">看当前结果最集中的医院，便于按医院安排复核和整改跟进。</div>
              </div>
              <span class="inspection-insight-meta">{{ filteredResultRows.length }} 条结果</span>
            </div>
            <div v-if="topResultHospitalBuckets.length" class="inspection-chip-list">
              <div v-for="item in topResultHospitalBuckets" :key="item.label" class="inspection-chip">
                <strong>{{ item.label }}</strong>
                <span>{{ item.value }} 条</span>
              </div>
            </div>
            <el-empty v-else description="暂无医院分布" :image-size="72" />
          </section>

          <section class="inspection-insight-card">
            <div class="inspection-insight-head">
              <div>
                <div class="inspection-insight-title">巡检人结果负载</div>
                <div class="inspection-insight-note">查看当前结果集中在哪些巡检人，便于主管复核执行质量。</div>
              </div>
              <span class="inspection-insight-meta">{{ activeResultInspectorCount }} 个巡检人口径</span>
            </div>
            <div v-if="topResultInspectorBuckets.length" class="inspection-chip-list">
              <div v-for="item in topResultInspectorBuckets" :key="item.label" class="inspection-chip">
                <strong>{{ item.label }}</strong>
                <span>{{ item.value }} 条</span>
              </div>
            </div>
            <el-empty v-else description="暂无巡检人结果分布" :image-size="72" />
          </section>

          <section class="inspection-insight-card">
            <div class="inspection-insight-head">
              <div>
                <div class="inspection-insight-title">健康等级结构</div>
                <div class="inspection-insight-note">快速判断当前范围内是良好结果主导，还是警告、严重结果更集中。</div>
              </div>
              <span class="inspection-insight-meta">{{ highRiskResultCount }} 条高风险</span>
            </div>
            <div v-if="resultHealthBuckets.length" class="inspection-chip-list">
              <div v-for="item in resultHealthBuckets" :key="item.label" class="inspection-chip inspection-chip--soft">
                <strong>{{ item.label }}</strong>
                <span>{{ item.value }} 条</span>
              </div>
            </div>
            <el-empty v-else description="暂无健康等级结构" :image-size="72" />
          </section>
        </div>

        <!-- 结果筛选 -->
        <AppFilterCard>
          <el-form :model="resultQuery" inline class="filter-form" @submit.prevent="onResultSearch">
            <el-form-item label="医院">
              <el-select v-model="resultQuery.hospitalName" clearable filterable style="width: 200px" placeholder="全部">
                <el-option v-for="h in resultHospitalOptions" :key="h" :label="h" :value="h" />
              </el-select>
            </el-form-item>
            <el-form-item label="产品">
              <el-select v-model="resultQuery.productName" clearable filterable style="width: 180px" placeholder="全部">
                <el-option v-for="p in resultProductOptions" :key="p" :label="p" :value="p" />
              </el-select>
            </el-form-item>
            <el-form-item label="健康等级">
              <el-select v-model="resultQuery.healthLevel" clearable style="width: 120px" placeholder="全部">
                <el-option label="良好" value="良好" />
                <el-option label="警告" value="警告" />
                <el-option label="严重" value="严重" />
              </el-select>
            </el-form-item>
            <el-form-item label="巡检人">
              <el-select v-model="resultQuery.inspector" clearable style="width: 140px" placeholder="全部">
                <el-option v-for="person in resultInspectorOptions" :key="person" :label="person" :value="person" />
              </el-select>
            </el-form-item>
            <el-form-item class="filter-actions">
              <el-button type="primary" @click="onResultSearch" icon="Search">查询</el-button>
              <el-button @click="onResultReset" icon="Refresh">重置</el-button>
              <el-button type="success" :loading="resultUploadLoading" @click="triggerResultUpload" icon="Upload">上传JSON</el-button>
              <input ref="resultUploadInput" type="file" accept=".json,application/json" style="display: none" @change="onResultFileChange" />
            </el-form-item>
          </el-form>
        </AppFilterCard>

        <!-- 结果表格 -->
        <AppTableCard>
          <el-table :data="resultTableData" v-loading="resultLoading" stripe max-height="520" scrollbar-always-on empty-text="暂无巡检结果数据（SystemAuditTool 推送后将在此显示）" @row-dblclick="onResultRowDoubleClick">
            <el-table-column prop="hospitalName" label="医院" min-width="220" show-overflow-tooltip sortable />
            <el-table-column prop="productName" label="产品" min-width="180" show-overflow-tooltip sortable />
            <el-table-column prop="inspectedAt" label="巡检时间" width="170" sortable>
              <template #default="scope">{{ formatDateTime(scope.row.inspectedAt) }}</template>
            </el-table-column>
            <el-table-column prop="inspector" label="巡检人" width="120" show-overflow-tooltip />
            <el-table-column prop="healthLevel" label="健康等级" width="110" sortable>
              <template #default="scope">
                <el-tag :type="healthTag(scope.row.healthLevel)" effect="dark">{{ scope.row.healthLevel }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="overallScore" label="评分" width="90" sortable>
              <template #default="scope">
                <span :class="scoreClass(scope.row.overallScore)">{{ scope.row.overallScore }}</span>
              </template>
            </el-table-column>
            <el-table-column label="风险统计" width="170">
              <template #default="scope">
                <span v-if="scope.row.criticalCount > 0" class="risk-badge critical">严重 {{ scope.row.criticalCount }}</span>
                <span v-if="scope.row.warningCount > 0" class="risk-badge warning">警告 {{ scope.row.warningCount }}</span>
                <span v-if="scope.row.criticalCount === 0 && scope.row.warningCount === 0" class="risk-badge good">无风险</span>
              </template>
            </el-table-column>
            <el-table-column prop="databaseVersion" label="数据库版本" width="150" show-overflow-tooltip />
            <el-table-column label="存储" width="90">
              <template #default="scope">
                <span v-if="scope.row.storageUsedPercent != null" :class="storageClass(scope.row.storageUsedPercent)">
                  {{ scope.row.storageUsedPercent }}%
                </span>
                <span v-else>-</span>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="200" fixed="right">
              <template #default="scope">
                <div class="table-action-group">
                  <el-button link type="primary" size="small" @click="showResultDetail(scope.row)" icon="Document">详情</el-button>
                  <el-button link size="small" @click="goToProjectPage(scope.row)">项目页</el-button>
                </div>
              </template>
            </el-table-column>
          </el-table>

          <div class="pager">
            <el-pagination
              v-model:current-page="resultQuery.page"
              v-model:page-size="resultQuery.size"
              :page-sizes="[15, 30, 50, 100]"
              layout="total, sizes, prev, pager, next"
              :total="resultTotal"
              @size-change="(size: number) => { resultQuery.size = size; resultQuery.page = 1; loadResults() }"
              @current-change="(page: number) => { resultQuery.page = page; loadResults() }"
            />
          </div>
        </AppTableCard>

        <!-- 详情弹窗 -->
        <ProDrawer v-model="detailVisible" title="巡检结果详情" width="720px">
          <template v-if="detailRow">
            <el-descriptions :column="2" border size="small" class="result-desc">
              <el-descriptions-item label="医院">{{ detailRow.hospitalName }}</el-descriptions-item>
              <el-descriptions-item label="产品">{{ detailRow.productName }}</el-descriptions-item>
              <el-descriptions-item label="巡检时间">{{ formatDateTime(detailRow.inspectedAt) }}</el-descriptions-item>
              <el-descriptions-item label="巡检人">{{ detailRow.inspector }}</el-descriptions-item>
              <el-descriptions-item label="健康等级">
                <el-tag :type="healthTag(detailRow.healthLevel)" effect="dark">{{ detailRow.healthLevel }}</el-tag>
              </el-descriptions-item>
              <el-descriptions-item label="综合评分">
                <span :class="scoreClass(detailRow.overallScore)" style="font-weight: 700; font-size: 16px;">{{ detailRow.overallScore }}</span>
              </el-descriptions-item>
              <el-descriptions-item label="耗时">{{ detailRow.durationSeconds }}秒</el-descriptions-item>
              <el-descriptions-item label="成功">{{ detailRow.success ? '是' : '否' }}</el-descriptions-item>
              <el-descriptions-item label="数据库版本" :span="2">{{ detailRow.databaseVersion ?? '-' }}</el-descriptions-item>
              <el-descriptions-item label="存储使用率">{{ detailRow.storageUsedPercent != null ? detailRow.storageUsedPercent + '%' : '-' }}</el-descriptions-item>
              <el-descriptions-item label="表空间使用率">{{ detailRow.tablespaceUsedPercent != null ? detailRow.tablespaceUsedPercent + '%' : '-' }}</el-descriptions-item>
              <el-descriptions-item label="备份状态">{{ detailRow.backupStatus ?? '-' }}</el-descriptions-item>
              <el-descriptions-item label="预计存储满天数">{{ detailRow.daysToFull != null ? detailRow.daysToFull + '天' : '-' }}</el-descriptions-item>
            </el-descriptions>

            <div v-if="detailRow.errorMessage" style="margin-top: 12px;">
              <el-alert type="error" :closable="false" :title="'错误信息：' + detailRow.errorMessage" />
            </div>

            <div v-if="detailRow.topRisks && detailRow.topRisks.length > 0" style="margin-top: 16px;">
              <h4 style="margin-bottom: 8px;">风险项 ({{ detailRow.topRisks.length }})</h4>
              <el-table :data="detailRow.topRisks" stripe size="small" max-height="300" scrollbar-always-on>
                <el-table-column prop="level" label="级别" width="80">
                  <template #default="scope">
                    <el-tag :type="riskLevelTag(scope.row.level)" size="small">{{ scope.row.level }}</el-tag>
                  </template>
                </el-table-column>
                <el-table-column prop="category" label="分类" width="120" show-overflow-tooltip />
                <el-table-column prop="title" label="标题" min-width="180" show-overflow-tooltip />
                <el-table-column prop="currentValue" label="当前值" width="120" show-overflow-tooltip />
                <el-table-column prop="thresholdValue" label="阈值" width="120" show-overflow-tooltip />
              </el-table>
            </div>
          </template>
        </ProDrawer>

      </el-tab-pane>
    </el-tabs>

    <ProDrawer v-model="planDetailVisible" title="巡检计划详情" width="680px">
      <template v-if="planDetailRow">
        <el-descriptions :column="2" border size="small" class="result-desc">
          <el-descriptions-item label="医院">{{ planDetailRow.hospitalName }}</el-descriptions-item>
          <el-descriptions-item label="产品">{{ planDetailRow.productName }}</el-descriptions-item>
          <el-descriptions-item label="省份">{{ planDetailRow.province }}</el-descriptions-item>
          <el-descriptions-item label="医院级别">{{ planDetailRow.hospitalLevel || '-' }}</el-descriptions-item>
          <el-descriptions-item label="组别">{{ planDetailRow.groupName }}</el-descriptions-item>
          <el-descriptions-item label="巡检人">{{ planDetailRow.inspector }}</el-descriptions-item>
          <el-descriptions-item label="方式">{{ planDetailRow.inspectionType }}</el-descriptions-item>
          <el-descriptions-item label="优先级">{{ planDetailRow.priority || '-' }}</el-descriptions-item>
          <el-descriptions-item label="计划日期">{{ formatDate(planDetailRow.planDate) }}</el-descriptions-item>
          <el-descriptions-item label="SLA截止">{{ planDetailRow.slaDueAt ? formatDateTime(planDetailRow.slaDueAt) : '-' }}</el-descriptions-item>
          <el-descriptions-item label="实际日期">{{ planDetailRow.actualDate ? formatDate(planDetailRow.actualDate) : '-' }}</el-descriptions-item>
          <el-descriptions-item label="开始执行">{{ planDetailRow.startedAt ? formatDateTime(planDetailRow.startedAt) : '-' }}</el-descriptions-item>
          <el-descriptions-item label="完成时间">{{ planDetailRow.completedAt ? formatDateTime(planDetailRow.completedAt) : '-' }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="statusTag(planDetailRow.status)">{{ planDetailRow.status }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="备注" :span="2">{{ planDetailRow.remarks || '-' }}</el-descriptions-item>
        </el-descriptions>

        <div class="detail-actions">
          <el-button v-if="canManageInspection && canStartPlan(planDetailRow.status)" plain type="primary" :loading="isPlanActionLoading(planDetailRow.id, 'start')" :disabled="isPlanRowBusy(planDetailRow.id)" @click="onStartPlan(planDetailRow)">开始执行</el-button>
          <el-button v-if="canManageInspection && canCompletePlan(planDetailRow.status)" plain type="success" :loading="isPlanActionLoading(planDetailRow.id, 'complete')" :disabled="isPlanRowBusy(planDetailRow.id)" @click="onCompletePlan(planDetailRow)">完成巡检</el-button>
          <el-button v-if="canManageInspection && canReopenPlan(planDetailRow.status)" plain type="warning" :loading="isPlanActionLoading(planDetailRow.id, 'reopen')" :disabled="isPlanRowBusy(planDetailRow.id)" @click="onReopenPlan(planDetailRow)">重开计划</el-button>
          <el-button v-if="canManageInspection" plain type="primary" @click="onOpenPlanEdit(planDetailRow)" icon="Edit">编辑计划</el-button>
          <el-button v-if="canManageInspection" plain type="danger" @click="onDeletePlan(planDetailRow)" icon="Delete">删除计划</el-button>
          <el-button plain @click="goToProjectPage(planDetailRow)">去项目台账</el-button>
          <el-button plain @click="goToResultTab(planDetailRow)">去巡检结果</el-button>
        </div>
      </template>
    </ProDrawer>

    <ProDrawer v-model="planEditVisible" :title="editingPlanId ? '编辑巡检计划' : '新增巡检计划'" width="620px">
      <el-form label-width="100px">
        <el-form-item label="医院">
          <el-input v-model="planEditForm.hospitalName" clearable />
        </el-form-item>
        <el-form-item label="产品">
          <el-input v-model="planEditForm.productName" clearable />
        </el-form-item>
        <el-form-item label="省份">
          <el-select v-model="planEditForm.province" clearable filterable style="width: 100%">
            <el-option v-for="province in provinceOptions" :key="`edit-province-${province}`" :label="province" :value="province" />
          </el-select>
        </el-form-item>
        <el-form-item label="医院级别">
          <el-input v-model="planEditForm.hospitalLevel" clearable />
        </el-form-item>
        <el-form-item label="组别">
          <el-select v-model="planEditForm.groupName" clearable filterable style="width: 100%">
            <el-option v-for="group in filteredGroupOptions" :key="`edit-${group}`" :label="group" :value="group" />
          </el-select>
        </el-form-item>
        <el-form-item label="巡检人">
          <el-select v-model="planEditForm.inspector" clearable filterable style="width: 100%">
            <el-option v-for="person in inspectorOptions" :key="`edit-person-${person}`" :label="person" :value="person" />
          </el-select>
        </el-form-item>
        <el-form-item label="计划日期">
          <el-date-picker v-model="planEditForm.planDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
        </el-form-item>
        <el-form-item label="实际日期">
          <el-date-picker v-model="planEditForm.actualDate" type="date" value-format="YYYY-MM-DD" clearable style="width: 100%" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="planEditForm.status" style="width: 100%">
            <el-option v-for="status in statusOptions" :key="`edit-status-${status}`" :label="status" :value="status" />
          </el-select>
        </el-form-item>
        <el-form-item label="方式">
          <el-select v-model="planEditForm.inspectionType" style="width: 100%">
            <el-option label="现场" value="现场" />
            <el-option label="远程" value="远程" />
          </el-select>
        </el-form-item>
        <el-form-item label="优先级">
          <el-select v-model="planEditForm.priority" clearable style="width: 100%">
            <el-option label="高" value="高" />
            <el-option label="中" value="中" />
            <el-option label="低" value="低" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="planEditForm.remarks" type="textarea" :rows="3" clearable />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="planEditVisible = false" icon="Close">取消</el-button>
        <el-button type="primary" :loading="planSubmitting" @click="submitPlanEdit" icon="Check">保存</el-button>
      </template>
    </ProDrawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import {
  completeInspection,
  createInspection,
  deleteInspection,
  exportInspections,
  fetchInspectionResults,
  fetchInspections,
  fetchInspectionSummary,
  reopenInspection,
  startInspection,
  submitInspectionResults,
  updateInspection,
  type InspectionResultSubmitItem,
} from '../../api/modules/inspection'
import type { InspectionPlanItem, InspectionPlanUpsert, InspectionResult, InspectionSummary } from '../../types/inspection'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { GROUP_OPTIONS, PERSON_OPTIONS, PROVINCE_OPTIONS } from '../../constants/filterOptions'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useAccessControl } from '../../composables/useAccessControl'
import { normalizeStatusText, resolveInspectionStatusTag } from '../../utils/statusTag'
import ProTable from '../../components/ProTable.vue'
import ProDrawer from '../../components/ProDrawer.vue'
import SummaryMetrics from '../../components/SummaryMetrics.vue'


// ---- Tab 切换 ----
const activeTab = ref('plan')
const router = useRouter()
const access = useAccessControl()
const canManageInspection = computed(() => (access.isManager() || access.isRegionalManager()) && access.canPermission('inspection.manage'))

const loading = ref(false)
const exporting = ref(false)
const total = ref(0)
const tableData = ref<InspectionPlanItem[]>([])
const allPlanRows = ref<InspectionPlanItem[]>([])
const planActionLoadingKey = ref('')
const nowTick = ref(Date.now())
let planTickTimer: number | null = null
const summary = ref<InspectionSummary>({
  plannedCount: 0,
  inProgressCount: 0,
  completedCount: 0,
  cancelledCount: 0,
  thisMonthCount: 0,
  total: 0,
})

type InsightBucket = {
  label: string
  value: number
}

const query = reactive({
  hospitalName: '',
  status: '',
  province: '',
  productName: '',
  groupName: '',
  inspector: '',
  inspectionType: '',
  priority: '',
  page: 1,
  size: 15,
})

type InspectionSummaryCard = {
  key: string
  title: string
  value: number
  context: string
  note: string
  color: string
  active?: boolean
  clickable?: boolean
}

const buildBuckets = <T>(items: T[], resolveLabel: (item: T) => string, limit = 5): InsightBucket[] => {
  const counts = new Map<string, number>()
  items.forEach((item) => {
    const label = resolveLabel(item).trim() || '未设置'
    counts.set(label, (counts.get(label) ?? 0) + 1)
  })

  return Array.from(counts.entries())
    .map(([label, value]) => ({ label, value }))
    .sort((a, b) => (b.value - a.value) || a.label.localeCompare(b.label, 'zh-CN'))
    .slice(0, limit)
}

const parsePlanDateValue = (value?: string | null) => {
  if (!value) {
    return null
  }

  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? null : date
}

const filteredPlanRows = computed(() => allPlanRows.value
  .filter((item) => !query.hospitalName || item.hospitalName.includes(query.hospitalName))
  .filter((item) => !getRoutePlanHospitalName() || item.hospitalName === getRoutePlanHospitalName())
  .filter((item) => !query.status || isSameStatus(item.status, query.status))
  .filter((item) => !query.province || item.province === query.province)
  .filter((item) => !query.productName || item.productName === query.productName)
  .filter((item) => !query.groupName || item.groupName === query.groupName)
  .filter((item) => !query.inspector || item.inspector === query.inspector)
  .filter((item) => !query.inspectionType || item.inspectionType === query.inspectionType)
  .filter((item) => !query.priority || item.priority === query.priority))

const isClosedPlan = (status: string) => isSameStatus(status, '已完成') || isSameStatus(status, '已取消')
const isOverduePlan = (item: InspectionPlanItem) => {
  if (isClosedPlan(item.status)) {
    return false
  }

  const dueAt = parsePlanDateValue(item.slaDueAt)
  return Boolean(dueAt) && dueAt!.getTime() <= nowTick.value
}

const openPlanCount = computed(() => filteredPlanRows.value.filter((item) => !isClosedPlan(item.status)).length)
const overduePlanCount = computed(() => filteredPlanRows.value.filter((item) => isOverduePlan(item)).length)
const plannedPlanCount = computed(() => filteredPlanRows.value.filter((item) => isSameStatus(item.status, '已计划')).length)
const inProgressPlanCount = computed(() => filteredPlanRows.value.filter((item) => isSameStatus(item.status, '执行中')).length)
const completedPlanCount = computed(() => filteredPlanRows.value.filter((item) => isSameStatus(item.status, '已完成')).length)
const cancelledPlanCount = computed(() => filteredPlanRows.value.filter((item) => isSameStatus(item.status, '已取消')).length)
const thisMonthPlanCount = computed(() => {
  const now = new Date(nowTick.value)
  return filteredPlanRows.value.filter((item) => {
    const date = parsePlanDateValue(item.planDate)
    return Boolean(date) && date!.getFullYear() === now.getFullYear() && date!.getMonth() === now.getMonth()
  }).length
})

const planPriorityQueue = computed(() => filteredPlanRows.value
  .filter((item) => !isClosedPlan(item.status))
  .slice()
  .sort((left, right) => {
    const overdueDiff = Number(isOverduePlan(right)) - Number(isOverduePlan(left))
    if (overdueDiff !== 0) {
      return overdueDiff
    }

    const leftPriority = left.priority === '高' ? 3 : left.priority === '中' ? 2 : 1
    const rightPriority = right.priority === '高' ? 3 : right.priority === '中' ? 2 : 1
    if (rightPriority !== leftPriority) {
      return rightPriority - leftPriority
    }

    return String(left.planDate || '').localeCompare(String(right.planDate || ''), 'zh-CN')
  })
  .slice(0, 5))

const topPlanHospitalBuckets = computed(() => buildBuckets(filteredPlanRows.value, (item) => item.hospitalName || '未填写医院'))
const topInspectorBuckets = computed(() => buildBuckets(
  filteredPlanRows.value.filter((item) => !isClosedPlan(item.status)),
  (item) => item.inspector || '未分配巡检人',
))
const planStructureBuckets = computed(() => buildBuckets(
  filteredPlanRows.value,
  (item) => `${item.inspectionType || '未设置方式'} · ${item.priority || '未设置优先级'}`,
))

const planSummaryCards = computed<InspectionSummaryCard[]>(() => [
  {
    key: '已计划',
    title: '已计划',
    value: plannedPlanCount.value,
    context: '计划状态',
    note: '查看已排期待执行的巡检任务',
    color: '#7c98bc',
    active: query.status === '已计划',
  },
  {
    key: '执行中',
    title: '执行中',
    value: inProgressPlanCount.value,
    context: '计划状态',
    note: '跟进当前执行中的巡检任务',
    color: '#c7a06c',
    active: query.status === '执行中',
  },
  {
    key: '已完成',
    title: '已完成',
    value: completedPlanCount.value,
    context: '计划状态',
    note: '查看已完成的巡检计划闭环情况',
    color: '#7d9f92',
    active: query.status === '已完成',
  },
  {
    key: '已取消',
    title: '已取消',
    value: cancelledPlanCount.value,
    context: '计划状态',
    note: '核查已取消计划的原因与记录',
    color: '#c58a87',
    active: query.status === '已取消',
  },
  {
    key: 'month',
    title: '本月计划',
    value: thisMonthPlanCount.value,
    context: '节奏概览',
    note: '本月周期内已纳入排期的巡检数量',
    color: '#3f4f63',
    clickable: false,
  },
  {
    key: 'all',
    title: '总数',
    value: filteredPlanRows.value.length,
    context: '全量视图',
    note: '查看全部巡检计划与状态分布',
    color: '#3f4f63',
    active: query.status === '',
  },
])

const inspectionFilterLabel = computed(() => {
  if (activeTab.value === 'results' && resultQuery.healthLevel) {
    return `结果：${resultQuery.healthLevel}`
  }

  if (activeTab.value === 'plan' && query.status) {
    return `计划：${query.status}`
  }

  if (activeTab.value === 'plan' && query.province) {
    return `省份：${query.province}`
  }

  if (activeTab.value === 'plan' && query.productName) {
    return `产品：${query.productName}`
  }

  if (activeTab.value === 'results' && resultQuery.hospitalName) {
    return `医院：${resultQuery.hospitalName}`
  }

  return activeTab.value === 'results' ? '巡检结果视图' : '巡检计划视图'
})

const inspectionHeroSignals = computed(() => {
  const highRiskResults = resultTableData.value.filter((item) => item.healthLevel === '严重' || item.healthLevel === '警告').length
  const planHospitalCount = new Set(tableData.value.map((item) => item.hospitalName).filter(Boolean)).size

  return [
    {
      label: '计划总数',
      value: String(summary.value.total),
      note: '当前口径下全部巡检计划数量',
    },
    {
      label: '执行中',
      value: String(summary.value.inProgressCount),
      note: '仍在推进中的巡检任务',
    },
    {
      label: '本月排期',
      value: String(summary.value.thisMonthCount),
      note: '当前月度已排入巡检节奏的任务',
    },
    {
      label: '结果风险',
      value: String(highRiskResults),
      note: `当前结果页高风险记录，覆盖 ${planHospitalCount} 家医院`,
    },
  ]
})

const inspectionQuickActions = computed(() => [
  {
    title: '执行中计划',
    note: '聚焦正在执行的巡检任务',
    onClick: () => {
      activeTab.value = 'plan'
      query.status = '执行中'
      query.page = 1
      void loadData()
    },
  },
  {
    title: '已完成计划',
    note: '复查已完成巡检的闭环质量',
    onClick: () => {
      activeTab.value = 'plan'
      query.status = '已完成'
      query.page = 1
      void loadData()
    },
  },
  {
    title: '严重结果',
    note: '切到结果视图查看严重健康等级',
    onClick: () => {
      activeTab.value = 'results'
      resultQuery.healthLevel = '严重'
      resultQuery.page = 1
      void loadResults()
    },
  },
  {
    title: '结果重置',
    note: '清空结果筛选回到全量巡检数据',
    onClick: () => {
      activeTab.value = 'results'
      onResultReset()
    },
  },
])

const activeInspectionHeroSignals = computed(() => {
  const planHospitalCount = new Set(filteredPlanRows.value.map((item) => item.hospitalName).filter(Boolean)).size
  return [
    {
      label: '计划总数',
      value: String(filteredPlanRows.value.length),
      note: '当前口径下全部巡检计划数量',
    },
    {
      label: '执行中',
      value: String(inProgressPlanCount.value),
      note: '仍在推进中的巡检任务',
    },
    {
      label: '本月排期',
      value: String(thisMonthPlanCount.value),
      note: '当前月度已排入巡检节奏的任务',
    },
    {
      label: '结果风险',
      value: String(highRiskResultCount.value),
      note: `当前结果页高风险记录，覆盖 ${planHospitalCount} 家医院`,
    },
  ]
})

const activeInspectionQuickActions = computed(() => inspectionQuickActions.value)

const route = useRoute()
const planDetailVisible = ref(false)
const planDetailRow = ref<InspectionPlanItem | null>(null)
const planEditVisible = ref(false)
const planSubmitting = ref(false)
const editingPlanId = ref<number | null>(null)
const planEditForm = reactive<InspectionPlanUpsert>({
  hospitalName: '',
  productName: '',
  province: '',
  hospitalLevel: '',
  groupName: '',
  inspector: '',
  planDate: '',
  actualDate: null,
  status: '已计划',
  inspectionType: '远程',
  priority: '中',
  remarks: '',
})

const readRouteQueryValue = (value: unknown): string => {
  if (typeof value === 'string') {
    return value
  }

  if (Array.isArray(value) && typeof value[0] === 'string') {
    return value[0]
  }

  return ''
}

const updateRouteQuery = async (patch: Record<string, string | undefined>) => {
  const nextQuery = { ...route.query }
  Object.entries(patch).forEach(([key, value]) => {
    if (value) {
      nextQuery[key] = value
      return
    }

    delete nextQuery[key]
  })

  await router.replace({ path: route.path, query: nextQuery })
}

const clearRouteActionQuery = async () => {
  if (!readRouteQueryValue(route.query.action) && !readRouteQueryValue(route.query.id) && !readRouteQueryValue(route.query.resultId)) {
    return
  }

  await updateRouteQuery({ action: undefined, id: undefined, resultId: undefined })
}

const applyDrillQuery = () => {
  const tab = readRouteQueryValue(route.query.tab)
  activeTab.value = tab === 'results' ? 'results' : 'plan'

  const status = readRouteQueryValue(route.query.status)
  const province = readRouteQueryValue(route.query.province)
  const productName = readRouteQueryValue(route.query.productName)
  const groupName = readRouteQueryValue(route.query.groupName)
  const inspector = readRouteQueryValue(route.query.inspector)
  const hospitalName = readRouteQueryValue(route.query.hospitalName)
  const inspectionType = readRouteQueryValue(route.query.inspectionType)
  const priority = readRouteQueryValue(route.query.priority)
  const healthLevel = readRouteQueryValue(route.query.healthLevel)

  if (activeTab.value === 'results') {
    if (hospitalName) resultQuery.hospitalName = hospitalName
    if (productName) resultQuery.productName = productName
    if (inspector) resultQuery.inspector = inspector
    if (healthLevel) resultQuery.healthLevel = healthLevel
    resultQuery.page = 1
    return
  }

  if (hospitalName) query.hospitalName = hospitalName
  if (status) query.status = status
  if (province) query.province = province
  if (productName) query.productName = productName
  if (groupName) query.groupName = groupName
  if (inspector) query.inspector = inspector
  if (inspectionType) query.inspectionType = inspectionType
  if (priority) query.priority = priority
  if (status || province || productName || groupName || inspector || hospitalName || inspectionType || priority) {
    query.page = 1
  }
}

const getRoutePlanHospitalName = () => readRouteQueryValue(route.query.hospitalName)

type InspectionFilterState = {
  hospitalName: string
  status: string
  province: string
  productName: string
  groupName: string
  inspector: string
  inspectionType: string
  priority: string
  page: number
  size: number
}

const statusOptions = ref<string[]>(['已计划', '执行中', '已完成', '已取消'])
const provinceOptions = ref<string[]>([...PROVINCE_OPTIONS])
const productOptions = ref<string[]>([])
const groupOptionsByProvince = ref<Record<string, string[]>>({})
const inspectorOptions = ref<string[]>([...PERSON_OPTIONS])

const allGroupOptions = computed(() => {
  const groups = Object.values(groupOptionsByProvince.value).flat()
  return groups.length > 0 ? Array.from(new Set(groups)) : GROUP_OPTIONS
})

const filteredGroupOptions = computed(() => {
  if (!query.province) {
    return allGroupOptions.value
  }

  const groups = groupOptionsByProvince.value[query.province]
  return groups && groups.length > 0 ? groups : allGroupOptions.value
})

watch(
  () => query.province,
  () => {
    if (query.groupName && !filteredGroupOptions.value.includes(query.groupName)) {
      query.groupName = ''
    }
  },
)

const loadFilterOptions = async () => {
  try {
    const res = await fetchInspections({ page: 1, size: 100000 })
    const items = res.data.items
    allPlanRows.value = items

    if (!items.length) {
      return
    }

    const provinces = Array.from(new Set(items.map((item) => item.province).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (provinces.length > 0) {
      provinceOptions.value = provinces
    }

    const statuses = Array.from(new Set(items.map((item) => item.status).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (statuses.length > 0) {
      statusOptions.value = Array.from(new Set([...statusOptions.value, ...statuses]))
    }

    const map: Record<string, string[]> = {}
    for (const item of items) {
      if (!item.province || !item.groupName) continue

      const province = item.province
      const groupName = item.groupName
      const groups = map[province] ?? (map[province] = [])

      if (!groups.includes(groupName)) {
        groups.push(groupName)
      }
    }

    groupOptionsByProvince.value = Object.fromEntries(
      Object.entries(map).map(([province, groups]) => [province, groups.sort((a, b) => a.localeCompare(b, 'zh-CN'))]),
    )

    const products = Array.from(new Set(items.map((item) => item.productName).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (products.length > 0) {
      productOptions.value = products
    }

    const inspectors = Array.from(new Set(items.map((item) => item.inspector).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (inspectors.length > 0) {
      inspectorOptions.value = inspectors
    }
  } catch {
  }
}
const { runInitialLoad } = useResilientLoad()

const formatDate = (value: string) => value.slice(0, 10)
const isSameStatus = (left: string, right: string) => normalizeStatusText(left) === normalizeStatusText(right)

const statusTag = (status: string) => resolveInspectionStatusTag(status)

const parsePlanSlaDate = (value?: string | null) => {
  if (!value) {
    return null
  }

  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? null : date
}

const formatPlanSlaText = (item: InspectionPlanItem) => {
  const dueAt = parsePlanSlaDate(item.slaDueAt)
  if (!dueAt) {
    return '未设置'
  }

  if (isSameStatus(item.status, '已完成')) {
    return '已闭环'
  }

  const diff = dueAt.getTime() - nowTick.value
  const hours = Math.round(diff / 3600000)
  if (hours < 0) {
    return `逾期 ${Math.abs(hours)}h`
  }
  if (hours < 24) {
    return `${hours}h 内`
  }
  return `${Math.ceil(hours / 24)} 天内`
}

const planSlaTagType = (item: InspectionPlanItem) => {
  if (isSameStatus(item.status, '已完成')) {
    return 'success'
  }

  const dueAt = parsePlanSlaDate(item.slaDueAt)
  if (!dueAt) {
    return 'info'
  }

  const diff = dueAt.getTime() - nowTick.value
  if (diff <= 0) {
    return 'danger'
  }
  if (diff <= 24 * 3600000) {
    return 'warning'
  }
  return 'success'
}

const canStartPlan = (status: string) => isSameStatus(status, '已计划')
const canCompletePlan = (status: string) => isSameStatus(status, '执行中')
const canReopenPlan = (status: string) => isSameStatus(status, '已完成') || isSameStatus(status, '已取消')

const planActionKey = (id: number, action: string) => `${id}:${action}`
const isPlanActionLoading = (id: number, action: string) => planActionLoadingKey.value === planActionKey(id, action)
const isPlanRowBusy = (id: number) => planActionLoadingKey.value.startsWith(`${id}:`)

const loadSummary = async () => {
  try {
    const res = await fetchInspectionSummary()
    summary.value = res.data
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载巡检汇总失败，请稍后重试'))
  }
}

const loadData = async () => {
  loading.value = true
  try {
    if (getRoutePlanHospitalName()) {
      const source = allPlanRows.value.length > 0 ? allPlanRows.value : (await fetchInspections({ page: 1, size: 100000 })).data.items
      if (allPlanRows.value.length === 0) {
        allPlanRows.value = source
      }

      const filtered = source
        .filter((item) => !query.hospitalName || item.hospitalName.includes(query.hospitalName))
        .filter((item) => !getRoutePlanHospitalName() || item.hospitalName === getRoutePlanHospitalName())
        .filter((item) => !query.status || isSameStatus(item.status, query.status))
        .filter((item) => !query.province || item.province === query.province)
        .filter((item) => !query.productName || item.productName === query.productName)
        .filter((item) => !query.groupName || item.groupName === query.groupName)
        .filter((item) => !query.inspector || item.inspector === query.inspector)
        .filter((item) => !query.inspectionType || item.inspectionType === query.inspectionType)
        .filter((item) => !query.priority || item.priority === query.priority)

      total.value = filtered.length
      const start = (query.page - 1) * query.size
      tableData.value = filtered.slice(start, start + query.size)
      return
    }

    const res = await fetchInspections(query)
    tableData.value = res.data.items
    total.value = res.data.total
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载巡检列表失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

const onStatClick = (status: string) => {
  query.hospitalName = ''
  query.province = ''
  query.productName = ''
  query.groupName = ''
  query.inspector = ''
  query.inspectionType = ''
  query.priority = ''
  query.status = status
  query.page = 1
  void updateRouteQuery({
    hospitalName: undefined,
    status: undefined,
    province: undefined,
    productName: undefined,
    groupName: undefined,
    inspector: undefined,
    inspectionType: undefined,
    priority: undefined,
    action: undefined,
    id: undefined,
    tab: 'plan',
  })
  loadData()
}

const onPlanSummaryCardSelect = (card: { key?: string | number; clickable?: boolean }) => {
  if (card.clickable === false || typeof card.key !== 'string') {
    return
  }

  onStatClick(card.key === 'all' ? '' : card.key)
}

const onSearch = () => {
  query.page = 1
  loadData()
}

const onReset = () => {
  query.hospitalName = ''
  query.status = ''
  query.province = ''
  query.productName = ''
  query.groupName = ''
  query.inspector = ''
  query.inspectionType = ''
  query.priority = ''
  query.page = 1
  query.size = 15
  clearFilterState()
  void updateRouteQuery({
    hospitalName: undefined,
    status: undefined,
    province: undefined,
    productName: undefined,
    groupName: undefined,
    inspector: undefined,
    inspectionType: undefined,
    priority: undefined,
    action: undefined,
    id: undefined,
    tab: 'plan',
  })
  loadData()
}

const onExport = async () => {
  exporting.value = true
  try {
    const blob = await exportInspections(query)
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `巡检计划-${Date.now()}.csv`
    link.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导出失败'))
  } finally {
    exporting.value = false
  }
}

const { restore: restoreFilterState, clear: clearFilterState } = useFilterStatePersist<InspectionFilterState>({
  key: 'inspection-plan',
  getState: () => ({
    hospitalName: query.hospitalName,
    status: query.status,
    province: query.province,
    productName: query.productName,
    groupName: query.groupName,
    inspector: query.inspector,
    inspectionType: query.inspectionType,
    priority: query.priority,
    page: query.page,
    size: query.size,
  }),
  applyState: (state) => {
    query.hospitalName = state.hospitalName ?? ''
    query.status = state.status ?? ''
    query.province = state.province ?? ''
    query.productName = state.productName ?? ''
    query.groupName = state.groupName ?? ''
    query.inspector = state.inspector ?? ''
    query.inspectionType = state.inspectionType ?? ''
    query.priority = state.priority ?? ''
    query.page = typeof state.page === 'number' ? state.page : 1
    query.size = typeof state.size === 'number' ? state.size : 15
  },
})

const refreshLinkedData = async () => {
  await Promise.allSettled([loadSummary(), loadFilterOptions(), loadData()])
}

useLinkedRealtimeRefresh({
  refresh: refreshLinkedData,
  scope: 'inspection',
  intervalMs: 60000,
})

// ===================== 巡检结果 Tab =====================

const resultLoading = ref(false)
const resultTotal = ref(0)
const resultTableData = ref<InspectionResult[]>([])
const allResultRows = ref<InspectionResult[]>([])
const detailVisible = ref(false)
const detailRow = ref<InspectionResult | null>(null)

const resultQuery = reactive({
  hospitalName: '',
  productName: '',
  inspector: '',
  healthLevel: '',
  page: 1,
  size: 15,
})

const resultHospitalOptions = ref<string[]>([])
const resultProductOptions = ref<string[]>([])
const resultInspectorOptions = ref<string[]>([])
const resultUploadLoading = ref(false)
const resultUploadInput = ref<HTMLInputElement | null>(null)

const resultHealthCounts = reactive({ good: 0, warning: 0, critical: 0 })
const filteredResultRows = computed(() => allResultRows.value)
const highRiskResultCount = computed(() => filteredResultRows.value.filter((item) => item.healthLevel === '严重' || item.healthLevel === '警告').length)
const resultPriorityQueue = computed(() => filteredResultRows.value
  .filter((item) => item.healthLevel === '严重' || item.healthLevel === '警告')
  .slice()
  .sort((left, right) => (right.criticalCount + right.warningCount) - (left.criticalCount + left.warningCount))
  .slice(0, 5))
const topResultHospitalBuckets = computed(() => buildBuckets(filteredResultRows.value, (item) => item.hospitalName || '未填写医院'))
const topResultInspectorBuckets = computed(() => buildBuckets(filteredResultRows.value, (item) => item.inspector || '未填写巡检人'))
const resultHealthBuckets = computed(() => buildBuckets(filteredResultRows.value, (item) => item.healthLevel || '未设置等级', 4))
const activeResultInspectorCount = computed(() => new Set(filteredResultRows.value.map((item) => (item.inspector || '未填写巡检人').trim() || '未填写巡检人')).size)

const resultSummaryCards = computed<InspectionSummaryCard[]>(() => [
  {
    key: 'all',
    title: '全部结果',
    value: filteredResultRows.value.length,
    context: '结果视图',
    note: '查看全部巡检结果与健康分布',
    color: '#3f4f63',
    active: resultQuery.healthLevel === '',
  },
  {
    key: '良好',
    title: '良好',
    value: filteredResultRows.value.filter((item) => item.healthLevel === '良好').length,
    context: '健康等级',
    note: '健康状态正常的巡检结果',
    color: '#7d9f92',
    active: resultQuery.healthLevel === '良好',
  },
  {
    key: '警告',
    title: '警告',
    value: filteredResultRows.value.filter((item) => item.healthLevel === '警告').length,
    context: '健康等级',
    note: '需要跟进但未到严重级别的结果',
    color: '#c7a06c',
    active: resultQuery.healthLevel === '警告',
  },
  {
    key: '严重',
    title: '严重',
    value: filteredResultRows.value.filter((item) => item.healthLevel === '严重').length,
    context: '健康等级',
    note: '优先处理的严重风险巡检结果',
    color: '#c58a87',
    active: resultQuery.healthLevel === '严重',
  },
])

const loadResults = async () => {
  resultLoading.value = true
  try {
    const [res, allRes] = await Promise.all([
      fetchInspectionResults(resultQuery),
      fetchInspectionResults({ ...resultQuery, page: 1, size: 100000 }),
    ])
    resultTableData.value = res.data.items
    resultTotal.value = res.data.total
    allResultRows.value = allRes.data.items
  } catch (error) {
    resultTableData.value = []
    resultTotal.value = 0
    allResultRows.value = []
    ElMessage.error(getErrorMessage(error, '加载巡检结果失败'))
  } finally {
    resultLoading.value = false
  }
}

const loadResultFilterOptions = async () => {
  try {
    const res = await fetchInspectionResults({ page: 1, size: 100000 })
    const items = res.data.items
    if (!items.length) return

    resultHospitalOptions.value = Array.from(new Set(items.map((i) => i.hospitalName).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    resultProductOptions.value = Array.from(new Set(items.map((i) => i.productName).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    resultInspectorOptions.value = Array.from(new Set(items.map((i) => i.inspector).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))

    resultHealthCounts.good = items.filter((i) => i.healthLevel === '良好').length
    resultHealthCounts.warning = items.filter((i) => i.healthLevel === '警告').length
    resultHealthCounts.critical = items.filter((i) => i.healthLevel === '严重').length
  } catch {
    // silent
  }
}

let resultsLoaded = false

const onTabChange = (tab: string) => {
  void updateRouteQuery({ tab })
  if (tab === 'results' && !resultsLoaded) {
    resultsLoaded = true
    loadResultFilterOptions()
    loadResults()
  }
}

const goToProjectPage = (row: Pick<InspectionPlanItem, 'hospitalName' | 'productName'> | Pick<InspectionResult, 'hospitalName' | 'productName'>) => {
  void router.push({
    path: '/project/list',
    query: {
      hospitalName: row.hospitalName,
      productName: row.productName,
      action: 'edit',
    },
  })
}

const onOpenPlanDetail = (row: InspectionPlanItem, syncRoute = true) => {
  planDetailRow.value = row
  planDetailVisible.value = true
  if (syncRoute) {
    void updateRouteQuery({ tab: 'plan', action: 'detail', id: String(row.id), resultId: undefined })
  }
}

const onOpenPlanEdit = (row: InspectionPlanItem, syncRoute = true) => {
  editingPlanId.value = row.id
  planEditForm.groupName = row.groupName
  planEditForm.inspector = row.inspector
  planEditForm.hospitalName = row.hospitalName
  planEditForm.productName = row.productName
  planEditForm.province = row.province
  planEditForm.hospitalLevel = row.hospitalLevel
  planEditForm.planDate = row.planDate
  planEditForm.actualDate = row.actualDate ?? null
  planEditForm.status = row.status
  planEditForm.inspectionType = row.inspectionType
  planEditForm.priority = row.priority
  planEditForm.remarks = row.remarks
  planEditVisible.value = true
  if (syncRoute) {
    void updateRouteQuery({ tab: 'plan', action: 'edit', id: String(row.id), resultId: undefined })
  }
}

const resetPlanEditForm = () => {
  planEditForm.hospitalName = ''
  planEditForm.productName = ''
  planEditForm.province = ''
  planEditForm.hospitalLevel = ''
  planEditForm.groupName = ''
  planEditForm.inspector = ''
  planEditForm.planDate = new Date().toISOString().slice(0, 10)
  planEditForm.actualDate = null
  planEditForm.status = '已计划'
  planEditForm.inspectionType = '远程'
  planEditForm.priority = '中'
  planEditForm.remarks = ''
}

const onOpenPlanCreate = () => {
  editingPlanId.value = null
  resetPlanEditForm()
  planEditVisible.value = true
  void updateRouteQuery({ tab: 'plan', action: 'create', id: undefined, resultId: undefined })
}

const onPlanRowDoubleClick = (row: InspectionPlanItem) => {
  if (canManageInspection.value) {
    onOpenPlanEdit(row)
    return
  }

  onOpenPlanDetail(row)
}

const submitPlanEdit = async () => {
  planSubmitting.value = true
  try {
    if (editingPlanId.value) {
      const res = await updateInspection(editingPlanId.value, planEditForm)
      ElMessage.success('巡检计划更新成功')
      planDetailRow.value = res.data
    } else {
      const res = await createInspection(planEditForm)
      ElMessage.success('巡检计划创建成功')
      planDetailRow.value = res.data
    }

    planEditVisible.value = false
    await Promise.allSettled([loadSummary(), loadFilterOptions(), loadData()])
  } catch (error) {
    ElMessage.error(getErrorMessage(error, editingPlanId.value ? '更新巡检计划失败' : '创建巡检计划失败'))
  } finally {
    planSubmitting.value = false
  }
}

const onDeletePlan = async (row: InspectionPlanItem) => {
  try {
    await ElMessageBox.confirm(`确认删除巡检计划 #${row.id} 吗？`, '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消',
    })
  } catch {
    return
  }

  try {
    await deleteInspection(row.id)
    ElMessage.success('巡检计划删除成功')
    planDetailVisible.value = false
    planEditVisible.value = false
    await Promise.allSettled([loadSummary(), loadFilterOptions(), loadData()])
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '删除巡检计划失败'))
  }
}

const goToResultTab = (row: Pick<InspectionPlanItem, 'hospitalName' | 'productName'>) => {
  activeTab.value = 'results'
  resultsLoaded = true
  resultQuery.hospitalName = row.hospitalName
  resultQuery.productName = row.productName
  resultQuery.page = 1
  void updateRouteQuery({
    tab: 'results',
    hospitalName: row.hospitalName,
    productName: row.productName,
    action: undefined,
    id: undefined,
  })
  void Promise.allSettled([loadResultFilterOptions(), loadResults()])
}

const syncPlanDetailSnapshot = (id: number) => {
  const latest = tableData.value.find((item) => item.id === id) ?? allPlanRows.value.find((item) => item.id === id)
  if (latest) {
    planDetailRow.value = latest
  }
}

const runPlanWorkflowAction = async (
  row: InspectionPlanItem,
  action: 'start' | 'complete' | 'reopen',
  runner: () => Promise<InspectionPlanItem>,
  successMessage: string,
) => {
  planActionLoadingKey.value = planActionKey(row.id, action)
  try {
    const updated = await runner()
    planDetailRow.value = updated
    ElMessage.success(successMessage)
    await loadSummary()
    await loadFilterOptions()
    await loadData()
    syncPlanDetailSnapshot(row.id)
  } catch (error) {
    ElMessage.error(getErrorMessage(error, `${successMessage}失败`))
  } finally {
    planActionLoadingKey.value = ''
  }
}

const onStartPlan = async (row: InspectionPlanItem) => {
  if (!canManageInspection.value) {
    ElMessage.warning('当前账号无巡检管理权限')
    return
  }

  await runPlanWorkflowAction(row, 'start', async () => {
    const res = await startInspection(row.id, row.inspector ? { inspector: row.inspector } : undefined)
    return res.data
  }, '巡检已开始执行')
}

const onCompletePlan = async (row: InspectionPlanItem) => {
  if (!canManageInspection.value) {
    ElMessage.warning('当前账号无巡检管理权限')
    return
  }

  try {
    const { value } = await ElMessageBox.prompt('可选填写本次巡检结论。', '完成巡检', {
      inputPlaceholder: '巡检结论',
      confirmButtonText: '完成',
      cancelButtonText: '取消',
    })

    await runPlanWorkflowAction(row, 'complete', async () => {
      const res = await completeInspection(row.id, value?.trim() ? { remarks: value.trim() } : undefined)
      return res.data
    }, '巡检已完成')
  } catch (error) {
    if (error === 'cancel' || error === 'close') {
      return
    }
    ElMessage.error(getErrorMessage(error, '完成巡检失败'))
  }
}

const onReopenPlan = async (row: InspectionPlanItem) => {
  if (!canManageInspection.value) {
    ElMessage.warning('当前账号无巡检管理权限')
    return
  }

  try {
    const { value } = await ElMessageBox.prompt('可选填写重开原因。', '重开巡检计划', {
      inputPlaceholder: '重开原因',
      confirmButtonText: '重开',
      cancelButtonText: '取消',
    })

    await runPlanWorkflowAction(row, 'reopen', async () => {
      const res = await reopenInspection(row.id, value?.trim() ? { reason: value.trim() } : undefined)
      return res.data
    }, '巡检计划已重开')
  } catch (error) {
    if (error === 'cancel' || error === 'close') {
      return
    }
    ElMessage.error(getErrorMessage(error, '重开巡检计划失败'))
  }
}

const onResultHealthClick = (level: string) => {
  resultQuery.hospitalName = ''
  resultQuery.productName = ''
  resultQuery.inspector = ''
  resultQuery.healthLevel = level
  resultQuery.page = 1
  loadResults()
}

const onResultSummaryCardSelect = (card: { key?: string | number }) => {
  if (typeof card.key !== 'string') {
    return
  }

  onResultHealthClick(card.key === 'all' ? '' : card.key)
}

const onResultSearch = () => {
  resultQuery.page = 1
  loadResults()
}

const onResultReset = () => {
  resultQuery.hospitalName = ''
  resultQuery.productName = ''
  resultQuery.inspector = ''
  resultQuery.healthLevel = ''
  resultQuery.page = 1
  resultQuery.size = 15
  void updateRouteQuery({ hospitalName: undefined, productName: undefined, inspector: undefined, healthLevel: undefined, action: undefined, resultId: undefined, tab: 'results' })
  loadResults()
}

const triggerResultUpload = () => {
  if (resultUploadLoading.value) return
  resultUploadInput.value?.click()
}

const onResultFileChange = async (event: Event) => {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return

  resultUploadLoading.value = true
  try {
    const text = await file.text()
    const parsed = JSON.parse(text)
    const payload = (Array.isArray(parsed) ? parsed : [parsed]) as InspectionResultSubmitItem[]

    if (payload.length === 0) {
      ElMessage.warning('上传文件中没有巡检结果数据')
      return
    }

    const hasRequiredFields = payload.every((item) => item && item.hospitalName && item.productName)
    if (!hasRequiredFields) {
      ElMessage.error('JSON格式不正确：缺少 hospitalName 或 productName 字段')
      return
    }

    const res = await submitInspectionResults(payload)
    ElMessage.success(res.message || `上传成功（${payload.length} 条）`)
    resultQuery.page = 1
    await Promise.all([loadResultFilterOptions(), loadResults()])
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '上传失败，请检查JSON格式或服务状态'))
  } finally {
    input.value = ''
    resultUploadLoading.value = false
  }
}

const showResultDetail = (row: InspectionResult) => {
  detailRow.value = row
  detailVisible.value = true
  void updateRouteQuery({ tab: 'results', action: 'result-detail', resultId: String(row.id), id: undefined })
}

const onResultRowDoubleClick = (row: InspectionResult) => {
  showResultDetail(row)
}

const syncPlanDetailFromRoute = async () => {
  const action = readRouteQueryValue(route.query.action)
  if (activeTab.value !== 'plan' || (action !== 'detail' && action !== 'edit' && action !== 'create')) {
    return
  }

  if (action === 'create') {
    if (canManageInspection.value && !planEditVisible.value) {
      onOpenPlanCreate()
    }
    return
  }

  const id = Number(readRouteQueryValue(route.query.id))
  if (!Number.isFinite(id) || id <= 0) {
    return
  }

  const matched = tableData.value.find((item) => item.id === id)
  if (matched) {
    if (action === 'edit' && canManageInspection.value) {
      onOpenPlanEdit(matched, false)
      return
    }

    onOpenPlanDetail(matched, false)
    return
  }

  try {
    const res = await fetchInspections({ ...query, page: 1, size: 100000 })
    const found = res.data.items.find((item) => item.id === id)
    if (found) {
      if (action === 'edit' && canManageInspection.value) {
        onOpenPlanEdit(found, false)
        return
      }

      onOpenPlanDetail(found, false)
    }
  } catch {
  }
}

const syncSinglePlanActionFromFilters = async () => {
  const action = readRouteQueryValue(route.query.action)
  const id = Number(readRouteQueryValue(route.query.id))
  if (activeTab.value !== 'plan' || (action !== 'detail' && action !== 'edit') || (Number.isFinite(id) && id > 0) || planDetailVisible.value || planEditVisible.value) {
    return
  }

  const source = allPlanRows.value.length > 0 ? allPlanRows.value : tableData.value
  const filtered = source
    .filter((item) => !getRoutePlanHospitalName() || item.hospitalName === getRoutePlanHospitalName())
    .filter((item) => !query.hospitalName || item.hospitalName.includes(query.hospitalName))
    .filter((item) => !query.status || isSameStatus(item.status, query.status))
    .filter((item) => !query.province || item.province === query.province)
    .filter((item) => !query.productName || item.productName === query.productName)
    .filter((item) => !query.groupName || item.groupName === query.groupName)
    .filter((item) => !query.inspector || item.inspector === query.inspector)
    .filter((item) => !query.inspectionType || item.inspectionType === query.inspectionType)
    .filter((item) => !query.priority || item.priority === query.priority)

  if (filtered.length !== 1) {
    return
  }

  const matched = filtered[0]
  if (!matched) {
    return
  }

  if (action === 'edit' && canManageInspection.value) {
    onOpenPlanEdit(matched, false)
    return
  }

  onOpenPlanDetail(matched, false)
}

const syncResultDetailFromRoute = async () => {
  const action = readRouteQueryValue(route.query.action)
  if (activeTab.value !== 'results' || action !== 'result-detail') {
    return
  }

  const resultId = Number(readRouteQueryValue(route.query.resultId))
  if (!Number.isFinite(resultId) || resultId <= 0) {
    return
  }

  const matched = resultTableData.value.find((item) => item.id === resultId)
  if (matched) {
    detailRow.value = matched
    detailVisible.value = true
    return
  }

  try {
    const res = await fetchInspectionResults({ ...resultQuery, page: 1, size: 100000 })
    const found = res.data.items.find((item) => item.id === resultId)
    if (found) {
      detailRow.value = found
      detailVisible.value = true
    }
  } catch {
  }
}

const formatDateTime = (value: string) => {
  if (!value) return '-'
  return value.replace('T', ' ').slice(0, 19)
}

const healthTag = (level: string) => {
  if (level === '良好') return 'success'
  if (level === '警告') return 'warning'
  if (level === '严重') return 'danger'
  return 'info'
}

const riskLevelTag = (level: string) => {
  if (level === '严重' || level === 'Critical') return 'danger'
  if (level === '警告' || level === 'Warning') return 'warning'
  return 'info'
}

const scoreClass = (score: number) => {
  if (score >= 80) return 'score-good'
  if (score >= 50) return 'score-warning'
  return 'score-danger'
}

const storageClass = (pct: number) => {
  if (pct >= 90) return 'score-danger'
  if (pct >= 70) return 'score-warning'
  return 'score-good'
}

onMounted(async () => {
  planTickTimer = window.setInterval(() => {
    nowTick.value = Date.now()
  }, 60000)

  await access.ensureAccessProfileLoaded()
  restoreFilterState()
  applyDrillQuery()

  if (activeTab.value === 'results') {
    activeTab.value = 'results'
    resultsLoaded = true
    void loadResultFilterOptions()
    void loadResults()
  }

  await runInitialLoad({
    tasks: [loadSummary, loadFilterOptions, loadData],
    retryChecks: [
      {
        when: () => summary.value.total > 0 && total.value === 0,
        task: loadData,
      },
    ],
  })
  await syncPlanDetailFromRoute()
  await syncResultDetailFromRoute()
})

onBeforeUnmount(() => {
  if (planTickTimer !== null) {
    window.clearInterval(planTickTimer)
    planTickTimer = null
  }
})

watch(planDetailVisible, (visible) => {
  if (!visible && !detailVisible.value) {
    void clearRouteActionQuery()
  }
})

watch(planEditVisible, (visible) => {
  if (!visible && !planDetailVisible.value && !detailVisible.value) {
    void clearRouteActionQuery()
  }
})

watch(detailVisible, (visible) => {
  if (!visible && !planDetailVisible.value) {
    void clearRouteActionQuery()
  }
})

watch(() => route.fullPath, async () => {
  applyDrillQuery()
  if (activeTab.value === 'results') {
    resultsLoaded = true
    await Promise.allSettled([loadResultFilterOptions(), loadResults()])
    await syncResultDetailFromRoute()
    return
  }

  await loadData()
  await syncPlanDetailFromRoute()
  await syncSinglePlanActionFromFilters()
})
</script>

<style scoped>
.inspection-hero {
  display: grid;
  grid-template-columns: minmax(0, 1.18fr) minmax(320px, 0.82fr);
  gap: 16px;
  padding: 24px;
  border-radius: 28px;
  color: #ffffff;
  background:
    radial-gradient(circle at 14% 18%, rgba(171, 214, 255, 0.18), transparent 22%),
    radial-gradient(circle at 100% 0, rgba(159, 236, 211, 0.15), transparent 24%),
    linear-gradient(145deg, #24405d 0%, #2e5572 48%, #2f7267 100%);
  box-shadow: 0 26px 44px rgba(28, 62, 86, 0.18);
  margin-bottom: 12px;
}

.inspection-hero-main {
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-width: 0;
}

.inspection-hero-kicker-row {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.inspection-hero-kicker,
.inspection-hero-badge {
  display: inline-flex;
  align-items: center;
  padding: 7px 12px;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 700;
}

.inspection-hero-kicker {
  border: 1px solid rgba(255, 255, 255, 0.18);
  background: rgba(255, 255, 255, 0.08);
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.inspection-hero-badge {
  background: rgba(255, 255, 255, 0.14);
  color: #eef9ff;
}

.inspection-hero-title {
  margin: 0;
  font-size: 34px;
  line-height: 1.12;
  font-weight: 700;
}

.inspection-hero-subtitle {
  max-width: 700px;
  font-size: 14px;
  line-height: 1.85;
  color: rgba(232, 245, 247, 0.84);
}

.inspection-hero-signals {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
  margin-top: 4px;
}

.inspection-signal-card {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 16px;
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.11), rgba(255, 255, 255, 0.05));
}

.inspection-signal-label {
  font-size: 12px;
  color: rgba(227, 245, 247, 0.76);
}

.inspection-signal-value {
  font-size: 28px;
  line-height: 1;
  font-weight: 700;
}

.inspection-signal-note {
  font-size: 12px;
  line-height: 1.7;
  color: rgba(229, 244, 246, 0.76);
}

.inspection-hero-side {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.inspection-control-card,
.inspection-quick-action {
  border: 1px solid rgba(255, 255, 255, 0.14);
  border-radius: 20px;
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.14), rgba(255, 255, 255, 0.08));
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.12);
}

.inspection-control-card {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 18px;
}

.inspection-control-copy {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.inspection-control-title {
  font-size: 16px;
  font-weight: 700;
}

.inspection-control-note {
  font-size: 12px;
  line-height: 1.7;
  color: rgba(232, 244, 246, 0.74);
}

.inspection-control-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.inspection-control-actions :deep(.el-button) {
  min-height: 40px;
}

.inspection-quick-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.inspection-quick-action {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 16px;
  color: #ffffff;
  text-align: left;
  cursor: pointer;
  transition: transform 0.2s ease, border-color 0.2s ease, background 0.2s ease;
}

.inspection-quick-action:hover {
  transform: translateY(-1px);
  border-color: rgba(255, 255, 255, 0.24);
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.18), rgba(255, 255, 255, 0.09));
}

.inspection-quick-title {
  font-size: 14px;
  font-weight: 700;
}

.inspection-quick-note {
  font-size: 12px;
  line-height: 1.65;
  color: rgba(232, 245, 247, 0.76);
}

.inspection-insight-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
  margin: 16px 0;
}

.inspection-insight-card {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 18px 20px;
  border-radius: 20px;
  background: #ffffff;
  border: 1px solid rgba(148, 163, 184, 0.16);
  box-shadow: 0 12px 28px rgba(15, 23, 42, 0.06);
}

.inspection-insight-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.inspection-insight-title {
  font-size: 15px;
  font-weight: 700;
  color: #111827;
}

.inspection-insight-note {
  margin-top: 4px;
  font-size: 12px;
  line-height: 1.6;
  color: #64748b;
}

.inspection-insight-meta {
  font-size: 12px;
  font-weight: 600;
  color: #475569;
  white-space: nowrap;
}

.inspection-queue-list,
.inspection-chip-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.inspection-queue-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  width: 100%;
  padding: 12px 14px;
  border-radius: 16px;
  border: 1px solid rgba(148, 163, 184, 0.16);
  background: #f8fafc;
  cursor: pointer;
  text-align: left;
  transition: transform 0.15s ease, box-shadow 0.15s ease, border-color 0.15s ease;
}

.inspection-queue-item:hover {
  transform: translateY(-1px);
  border-color: rgba(59, 130, 246, 0.22);
  box-shadow: 0 12px 24px rgba(15, 23, 42, 0.08);
}

.inspection-queue-main,
.inspection-queue-meta {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.inspection-queue-main strong,
.inspection-chip strong {
  font-size: 13px;
  font-weight: 700;
  color: #111827;
}

.inspection-queue-main span,
.inspection-queue-meta span,
.inspection-chip span {
  font-size: 12px;
  line-height: 1.5;
  color: #64748b;
}

.inspection-queue-meta {
  align-items: flex-end;
}

.inspection-chip-list {
  gap: 8px;
}

.inspection-chip {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 11px 14px;
  border-radius: 14px;
  background: #f8fafc;
  border: 1px solid rgba(148, 163, 184, 0.14);
}

.inspection-chip--soft {
  background: #f5f7fb;
}

.main-tabs {
  margin-bottom: 0;
}
.main-tabs :deep(.el-tabs__header) {
  margin-bottom: 16px;
}

.risk-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 10px;
  font-size: 12px;
  margin-right: 4px;
  line-height: 18px;
}
.risk-badge.critical {
  background: var(--el-color-danger-light-9);
  color: var(--el-color-danger);
}
.risk-badge.warning {
  background: var(--el-color-warning-light-9);
  color: var(--el-color-warning-dark-2);
}
.risk-badge.good {
  background: var(--el-color-success-light-9);
  color: var(--el-color-success);
}

.score-good { color: var(--el-color-success); font-weight: 600; }
.score-warning { color: var(--el-color-warning-dark-2); font-weight: 600; }
.score-danger { color: var(--el-color-danger); font-weight: 600; }

.result-desc {
  margin-bottom: 8px;
}

.deadline-cell {
  display: flex;
  align-items: center;
  gap: 6px;
  white-space: nowrap;
}

.detail-actions {
  margin-top: 16px;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

:deep(.table-action-group) {
  flex-wrap: nowrap;
  white-space: nowrap;
}

@media (max-width: 1280px) {
  .inspection-hero {
    grid-template-columns: 1fr;
  }

  .inspection-insight-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 768px) {
  .inspection-hero {
    padding: 18px;
  }

  .inspection-hero-title {
    font-size: 28px;
  }

  .inspection-hero-signals,
  .inspection-quick-grid {
    grid-template-columns: 1fr;
  }

  .inspection-control-actions {
    flex-direction: column;
    align-items: stretch;
  }

  .inspection-queue-item,
  .inspection-chip,
  .inspection-insight-head {
    align-items: flex-start;
    flex-direction: column;
  }

  .inspection-queue-meta {
    align-items: flex-start;
  }
}
</style>
