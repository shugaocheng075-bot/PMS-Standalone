<template>
  <div ref="pageRef" class="page-shell">
    <div class="annual-report-hero">
      <div class="annual-report-hero-main">
        <div class="annual-report-hero-kicker-row">
          <span class="annual-report-hero-kicker">Annual Report Desk</span>
          <span class="annual-report-hero-badge">{{ activeAnnualReportFilterLabel }}</span>
        </div>
        <h2 class="annual-report-hero-title">年报管理</h2>
        <div class="annual-report-hero-subtitle">
          统一查看当前筛选范围内的年报编写、提审、逾期和责任分布，直接从第一屏推进待处理报告和复核动作。
        </div>
        <div class="annual-report-hero-signals">
          <div v-for="item in heroSignals" :key="item.label" class="annual-report-signal-card">
            <span class="annual-report-signal-label">{{ item.label }}</span>
            <strong class="annual-report-signal-value">{{ item.value }}</strong>
            <span class="annual-report-signal-note">{{ item.note }}</span>
          </div>
        </div>
      </div>

      <div class="annual-report-hero-side">
        <div class="annual-report-control-card">
          <div class="annual-report-control-copy">
            <span class="annual-report-control-title">年报台动作</span>
            <span class="annual-report-control-note">先锁定状态、到期月份或责任人，再从待处理清单进入详情和流程动作。</span>
          </div>
          <div class="annual-report-control-actions no-print">
            <el-button size="small" :loading="loading" @click="refreshDesk" icon="Refresh">刷新</el-button>
            <el-button v-if="canManageAnnualReport" size="small" type="primary" @click="onOpenCreate" icon="Plus">新增年报</el-button>
            <el-button size="small" :loading="exporting" @click="onExport" icon="Download">导出</el-button>
            <el-button size="small" @click="onPrint">打印</el-button>
          </div>
        </div>

        <div class="annual-report-quick-grid">
          <button
            v-for="action in quickActions"
            :key="action.title"
            type="button"
            class="annual-report-quick-action"
            @click="action.onClick()"
          >
            <span class="annual-report-quick-title">{{ action.title }}</span>
            <span class="annual-report-quick-note">{{ action.note }}</span>
          </button>
        </div>
      </div>
    </div>

    <div class="annual-report-insight-grid">
      <section class="annual-report-insight-card">
        <div class="annual-report-insight-head">
          <div>
            <div class="annual-report-insight-title">待提交与待评审</div>
            <div class="annual-report-insight-note">优先处理仍在编写和已提交待评审的报告，避免月底集中堆积。</div>
          </div>
          <el-tag size="small" type="warning" effect="light">{{ reviewQueue.length }} 项</el-tag>
        </div>
        <div v-if="reviewQueue.length" class="annual-report-queue-list">
          <button
            v-for="item in reviewQueue"
            :key="item.id"
            type="button"
            class="annual-report-queue-item"
            @click="onOpenDetail(item)"
          >
            <div class="annual-report-queue-main">
              <strong>{{ item.hospitalName || '未填写医院' }}</strong>
              <span>{{ item.productName || '未填写产品' }} · {{ item.servicePerson || '未分配服务人员' }}</span>
            </div>
            <div class="annual-report-queue-meta">
              <el-tag size="small" :type="statusTag(item.status)">{{ item.status }}</el-tag>
              <span>{{ item.dueMonth || '未设置到期月份' }}</span>
            </div>
          </button>
        </div>
        <el-empty v-else description="当前筛选下没有待提交或待评审报告" :image-size="72" />
      </section>

      <section class="annual-report-insight-card">
        <div class="annual-report-insight-head">
          <div>
            <div class="annual-report-insight-title">服务人员分布</div>
            <div class="annual-report-insight-note">查看当前筛选范围内年报主要集中在哪些服务人员手里，便于主管做分派平衡。</div>
          </div>
          <span class="annual-report-insight-meta">{{ filteredRows.length }} 条记录</span>
        </div>
        <div v-if="topServicePersonBuckets.length" class="annual-report-chip-list">
          <div v-for="item in topServicePersonBuckets" :key="item.label" class="annual-report-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 项</span>
          </div>
        </div>
        <el-empty v-else description="暂无服务人员分布" :image-size="72" />
      </section>

      <section class="annual-report-insight-card">
        <div class="annual-report-insight-head">
          <div>
            <div class="annual-report-insight-title">服务组分布</div>
            <div class="annual-report-insight-note">按服务组看年报覆盖量，便于按组推进编写节奏和审核节奏。</div>
          </div>
          <span class="annual-report-insight-meta">{{ activeGroupCount }} 组参与</span>
        </div>
        <div v-if="topGroupBuckets.length" class="annual-report-chip-list">
          <div v-for="item in topGroupBuckets" :key="item.label" class="annual-report-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 项</span>
          </div>
        </div>
        <el-empty v-else description="暂无服务组分布" :image-size="72" />
      </section>

      <section class="annual-report-insight-card">
        <div class="annual-report-insight-head">
          <div>
            <div class="annual-report-insight-title">状态结构</div>
            <div class="annual-report-insight-note">判断当前范围内是编写堆积、评审堆积，还是已经基本闭环。</div>
          </div>
          <span class="annual-report-insight-meta">{{ overdueRows.length }} 项逾期</span>
        </div>
        <div v-if="statusBuckets.length" class="annual-report-chip-list">
          <div v-for="item in statusBuckets" :key="item.label" class="annual-report-chip">
            <strong>{{ item.label }}</strong>
            <span>{{ item.value }} 项</span>
          </div>
        </div>
        <el-empty v-else description="暂无状态分布" :image-size="72" />
      </section>
    </div>

    <div class="annual-report-summary-wrap">
      <SummaryMetrics :items="summaryCards" :columns="6" @select="onSummaryCardSelect" />
    </div>

    <AppFilterCard>
      <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
        <el-form-item label="状态">
          <el-select v-model="query.status" placeholder="全部" clearable style="width: 140px">
            <el-option v-for="status in statusOptions" :key="status" :label="status" :value="status" />
          </el-select>
        </el-form-item>
        <el-form-item label="到期月份">
          <el-date-picker
            v-model="dueMonthPicker"
            type="month"
            placeholder="全部"
            clearable
            value-format="YYYY-MM"
            style="width: 160px"
            @change="onDueMonthChange"
          />
        </el-form-item>
        <el-form-item label="服务组">
          <el-select v-model="query.groupName" clearable placeholder="全部" style="width: 160px">
            <el-option v-for="group in filteredGroupOptions" :key="group" :label="group" :value="group" />
          </el-select>
        </el-form-item>
        <el-form-item label="服务人员">
          <el-select v-model="query.servicePerson" clearable placeholder="全部" style="width: 140px">
            <el-option v-for="person in filteredServicePersonOptions" :key="person" :label="person" :value="person" />
          </el-select>
        </el-form-item>
        <el-form-item label="优先级">
          <el-select v-model="query.priority" clearable placeholder="全部" style="width: 120px">
            <el-option label="高" value="高" />
            <el-option label="中" value="中" />
            <el-option label="低" value="低" />
          </el-select>
        </el-form-item>
        <el-form-item label="评审人">
          <el-input v-model="query.reviewer" clearable placeholder="请输入评审人" style="width: 140px" />
        </el-form-item>
        <el-form-item label="医院">
          <el-input v-model="query.hospitalName" clearable placeholder="请输入医院" style="width: 180px" />
        </el-form-item>
        <el-form-item label="产品">
          <el-input v-model="query.productName" clearable placeholder="请输入产品" style="width: 180px" />
        </el-form-item>
        <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch" icon="Search">查询</el-button>
          <el-button @click="onReset" icon="Refresh">重置</el-button>
          <el-button :loading="exporting" @click="onExport" icon="Download">导出 CSV</el-button>
        </el-form-item>
      </el-form>
    </AppFilterCard>

    <AppTableCard>
      <template #header>
        <div class="annual-report-table-head">
          <div>
            <div class="annual-report-table-title">年报明细</div>
            <div class="annual-report-table-note">保留原有编辑、流程和联动能力，首屏聚焦当前筛选范围的待处理报告。</div>
          </div>
          <el-tag size="small" effect="light">{{ activeAnnualReportFilterLabel }}</el-tag>
        </div>
      </template>

      <el-table
        :data="tableData"
        v-loading="loading"
        stripe
        max-height="520"
        scrollbar-always-on
        empty-text="暂无符合条件的数据"
        :row-class-name="rowClassName"
      >
        <el-table-column prop="opportunityNumber" label="机会号" min-width="150" show-overflow-tooltip>
          <template #default="{ row }">
            <el-input
              v-if="isEditing(row.id, 'opportunityNumber')"
              v-model="row.opportunityNumber"
              size="small"
              @blur="onCellSave(row, 'opportunityNumber')"
              @keyup.enter="onCellSave(row, 'opportunityNumber')"
              v-focus
            />
            <span v-else :class="{ 'editable-cell': canEditAnnualReport(row) }" @click="onCellEdit(row.id, 'opportunityNumber')">
              {{ row.opportunityNumber || '-' }}
            </span>
          </template>
        </el-table-column>

        <el-table-column prop="hospitalName" label="医院" min-width="240" show-overflow-tooltip sortable>
          <template #default="{ row }">
            <el-input
              v-if="isEditing(row.id, 'hospitalName')"
              v-model="row.hospitalName"
              size="small"
              @blur="onCellSave(row, 'hospitalName')"
              @keyup.enter="onCellSave(row, 'hospitalName')"
              v-focus
            />
            <span v-else :class="{ 'editable-cell': canEditAnnualReport(row) }" @click="onCellEdit(row.id, 'hospitalName')">
              {{ row.hospitalName || '-' }}
            </span>
          </template>
        </el-table-column>

        <el-table-column prop="productName" label="产品" min-width="170" show-overflow-tooltip>
          <template #default="{ row }">
            <el-input
              v-if="isEditing(row.id, 'productName')"
              v-model="row.productName"
              size="small"
              @blur="onCellSave(row, 'productName')"
              @keyup.enter="onCellSave(row, 'productName')"
              v-focus
            />
            <span v-else :class="{ 'editable-cell': canEditAnnualReport(row) }" @click="onCellEdit(row.id, 'productName')">
              {{ row.productName || '-' }}
            </span>
          </template>
        </el-table-column>

        <el-table-column prop="province" label="省份" width="100" show-overflow-tooltip sortable>
          <template #default="{ row }">
            <el-input
              v-if="isEditing(row.id, 'province')"
              v-model="row.province"
              size="small"
              @blur="onCellSave(row, 'province')"
              @keyup.enter="onCellSave(row, 'province')"
              v-focus
            />
            <span v-else :class="{ 'editable-cell': canEditAnnualReport(row) }" @click="onCellEdit(row.id, 'province')">
              {{ row.province || '-' }}
            </span>
          </template>
        </el-table-column>

        <el-table-column prop="groupName" label="服务组" width="132" show-overflow-tooltip>
          <template #default="{ row }">
            <el-select
              v-if="isEditing(row.id, 'groupName')"
              v-model="row.groupName"
              size="small"
              clearable
              filterable
              @change="onCellSave(row, 'groupName')"
              @blur="onCellSave(row, 'groupName')"
              v-focus
            >
              <el-option v-for="group in groupOptions" :key="group" :label="group" :value="group" />
            </el-select>
            <span v-else :class="{ 'editable-cell': canEditAnnualReport(row) }" @click="onCellEdit(row.id, 'groupName')">
              {{ row.groupName || '-' }}
            </span>
          </template>
        </el-table-column>

        <el-table-column prop="servicePerson" label="服务人员" width="126" show-overflow-tooltip>
          <template #default="{ row }">
            <el-select
              v-if="isEditing(row.id, 'servicePerson')"
              v-model="row.servicePerson"
              size="small"
              clearable
              filterable
              @change="onCellSave(row, 'servicePerson')"
              @blur="onCellSave(row, 'servicePerson')"
              v-focus
            >
              <el-option v-for="person in servicePersonOptions" :key="person" :label="person" :value="person" />
            </el-select>
            <span v-else :class="{ 'editable-cell': canEditAnnualReport(row) }" @click="onCellEdit(row.id, 'servicePerson')">
              {{ row.servicePerson || '-' }}
            </span>
          </template>
        </el-table-column>

        <el-table-column prop="implementationStatus" label="实施状态" width="140" show-overflow-tooltip>
          <template #default="{ row }">
            <el-input
              v-if="isEditing(row.id, 'implementationStatus')"
              v-model="row.implementationStatus"
              size="small"
              @blur="onCellSave(row, 'implementationStatus')"
              @keyup.enter="onCellSave(row, 'implementationStatus')"
              v-focus
            />
            <span v-else :class="{ 'editable-cell': canEditAnnualReport(row) }" @click="onCellEdit(row.id, 'implementationStatus')">
              {{ row.implementationStatus || '-' }}
            </span>
          </template>
        </el-table-column>

        <el-table-column prop="maintenanceStartDate" label="维护开始" width="130">
          <template #default="{ row }">
            <el-date-picker
              v-if="isEditing(row.id, 'maintenanceStartDate')"
              v-model="row.maintenanceStartDate"
              type="date"
              size="small"
              value-format="YYYY-MM-DD"
              style="width: 100%"
              @change="onCellSave(row, 'maintenanceStartDate')"
              @blur="onCellSave(row, 'maintenanceStartDate')"
              v-focus
            />
            <span v-else :class="{ 'editable-cell': canEditAnnualReport(row) }" @click="onCellEdit(row.id, 'maintenanceStartDate')">
              {{ row.maintenanceStartDate ? row.maintenanceStartDate.slice(0, 10) : '-' }}
            </span>
          </template>
        </el-table-column>

        <el-table-column prop="maintenanceEndDate" label="维护结束" width="130">
          <template #default="{ row }">
            <el-date-picker
              v-if="isEditing(row.id, 'maintenanceEndDate')"
              v-model="row.maintenanceEndDate"
              type="date"
              size="small"
              value-format="YYYY-MM-DD"
              style="width: 100%"
              @change="onCellSave(row, 'maintenanceEndDate')"
              @blur="onCellSave(row, 'maintenanceEndDate')"
              v-focus
            />
            <span v-else :class="{ 'editable-cell': canEditAnnualReport(row) }" @click="onCellEdit(row.id, 'maintenanceEndDate')">
              {{ row.maintenanceEndDate ? row.maintenanceEndDate.slice(0, 10) : '-' }}
            </span>
          </template>
        </el-table-column>

        <el-table-column prop="dueMonth" label="到期月份" width="110" sortable>
          <template #default="{ row }">
            <el-tag v-if="row.dueMonth === currentMonth" type="warning" size="small">{{ row.dueMonth }}</el-tag>
            <span v-else>{{ row.dueMonth || '-' }}</span>
          </template>
        </el-table-column>

        <el-table-column prop="reportYear" label="报告年度" width="110" sortable>
          <template #default="{ row }">
            <el-input-number
              v-if="isEditing(row.id, 'reportYear')"
              v-model="row.reportYear"
              size="small"
              :min="2020"
              :max="2035"
              controls-position="right"
              style="width: 100%"
              @change="onCellSave(row, 'reportYear')"
              @blur="onCellSave(row, 'reportYear')"
              v-focus
            />
            <span v-else :class="{ 'editable-cell': canEditAnnualReport(row) }" @click="onCellEdit(row.id, 'reportYear')">
              {{ row.reportYear }}
            </span>
          </template>
        </el-table-column>

        <el-table-column prop="status" label="状态" width="110" sortable>
          <template #default="{ row }">
            <el-tag v-if="sameStatus(row.status, '已完成')" :type="statusTag(row.status)">{{ row.status }}</el-tag>
            <el-tag v-else :type="statusTag(row.status)" effect="plain">{{ row.status }}</el-tag>
          </template>
        </el-table-column>

        <el-table-column prop="priority" label="优先级" width="100" sortable>
          <template #default="{ row }">
            <el-select v-if="isEditing(row.id, 'priority')" v-model="row.priority" size="small" @change="onCellSave(row, 'priority')" v-focus>
              <el-option label="高" value="高" />
              <el-option label="中" value="中" />
              <el-option label="低" value="低" />
            </el-select>
            <el-tag
              v-else
              :type="row.priority === '高' ? 'danger' : row.priority === '中' ? 'warning' : 'info'"
              :class="{ 'editable-cell': canEditAnnualReport(row) }"
              @click="onCellEdit(row.id, 'priority')"
            >
              {{ row.priority || '-' }}
            </el-tag>
          </template>
        </el-table-column>

        <el-table-column prop="submitDate" label="提交日期" width="130">
          <template #default="{ row }">
            <span>{{ row.submitDate ? row.submitDate.slice(0, 10) : '-' }}</span>
          </template>
        </el-table-column>

        <el-table-column prop="reviewer" label="评审人" width="120" show-overflow-tooltip>
          <template #default="{ row }">
            <span>{{ row.reviewer || '-' }}</span>
          </template>
        </el-table-column>

        <el-table-column prop="reviewDate" label="评审日期" width="130">
          <template #default="{ row }">
            <span>{{ row.reviewDate ? row.reviewDate.slice(0, 10) : '-' }}</span>
          </template>
        </el-table-column>

        <el-table-column prop="remarks" label="备注" min-width="180" show-overflow-tooltip>
          <template #default="{ row }">
            <el-input
              v-if="isEditing(row.id, 'remarks')"
              v-model="row.remarks"
              size="small"
              @blur="onCellSave(row, 'remarks')"
              @keyup.enter="onCellSave(row, 'remarks')"
              v-focus
            />
            <span v-else :class="{ 'editable-cell': canEditAnnualReport(row) }" @click="onCellEdit(row.id, 'remarks')">
              {{ row.remarks || '-' }}
            </span>
          </template>
        </el-table-column>

        <el-table-column label="操作" width="388" fixed="right">
          <template #default="{ row }">
            <div class="table-action-group">
              <el-button link type="primary" @click="onOpenDetail(row)" icon="Document">详情</el-button>
              <el-button
                v-if="canStartAnnualReport(row)"
                link
                type="primary"
                :loading="isWorkflowLoading(row.id, 'start')"
                @click="onRunWorkflow(row, 'start')"
              >
                开始编写
              </el-button>
              <el-button
                v-if="canSubmitAnnualReport(row)"
                link
                type="warning"
                :loading="isWorkflowLoading(row.id, 'submit')"
                @click="onRunWorkflow(row, 'submit')"
              >
                提交评审
              </el-button>
              <el-button
                v-if="canCompleteAnnualReport(row)"
                link
                type="success"
                :loading="isWorkflowLoading(row.id, 'complete')"
                @click="onRunWorkflow(row, 'complete')"
              >
                完成评审
              </el-button>
              <el-button v-if="canReopenAnnualReport(row)" link :loading="isWorkflowLoading(row.id, 'reopen')" @click="onRunWorkflow(row, 'reopen')">
                重开
              </el-button>
              <el-button v-if="canDeleteAnnualReport(row)" link type="danger" @click="onDelete(row)" icon="Delete">删除</el-button>
              <el-button link @click="goToProjectPage(row)">项目页</el-button>
            </div>
          </template>
        </el-table-column>
      </el-table>

      <div class="pager">
        <el-pagination
          v-model:current-page="query.page"
          v-model:page-size="query.size"
          :page-sizes="[15, 30, 50, 100]"
          layout="total, sizes, prev, pager, next"
          :total="total"
          @size-change="(size: number) => { query.size = size; query.page = 1; loadData() }"
          @current-change="(page: number) => { query.page = page; loadData() }"
        />
      </div>
    </AppTableCard>

    <AppFormDialog v-model="detailVisible" title="年报详情" width="720px">
      <template v-if="detailItem">
        <el-descriptions :column="2" border size="small">
          <el-descriptions-item label="机会号">{{ detailItem.opportunityNumber }}</el-descriptions-item>
          <el-descriptions-item label="医院">{{ detailItem.hospitalName }}</el-descriptions-item>
          <el-descriptions-item label="产品">{{ detailItem.productName }}</el-descriptions-item>
          <el-descriptions-item label="省份">{{ detailItem.province }}</el-descriptions-item>
          <el-descriptions-item label="服务组">{{ detailItem.groupName }}</el-descriptions-item>
          <el-descriptions-item label="服务人员">{{ detailItem.servicePerson }}</el-descriptions-item>
          <el-descriptions-item label="实施状态">{{ detailItem.implementationStatus }}</el-descriptions-item>
          <el-descriptions-item label="维护开始">{{ detailItem.maintenanceStartDate }}</el-descriptions-item>
          <el-descriptions-item label="维护结束">{{ detailItem.maintenanceEndDate }}</el-descriptions-item>
          <el-descriptions-item label="到期月份">
            <el-tag v-if="detailItem.dueMonth === currentMonth" type="warning">{{ detailItem.dueMonth }}</el-tag>
            <span v-else>{{ detailItem.dueMonth || '-' }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="报告年度">{{ detailItem.reportYear }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="statusTag(detailItem.status)">{{ detailItem.status }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="优先级">{{ detailItem.priority || '-' }}</el-descriptions-item>
          <el-descriptions-item label="提交日期">{{ detailItem.submitDate ? detailItem.submitDate.slice(0, 10) : '-' }}</el-descriptions-item>
          <el-descriptions-item label="评审人">{{ detailItem.reviewer || '-' }}</el-descriptions-item>
          <el-descriptions-item label="评审日期">{{ detailItem.reviewDate ? detailItem.reviewDate.slice(0, 10) : '-' }}</el-descriptions-item>
          <el-descriptions-item label="备注">{{ detailItem.remarks || '-' }}</el-descriptions-item>
        </el-descriptions>

        <div class="detail-actions">
          <el-button
            v-if="detailItem && canStartAnnualReport(detailItem)"
            type="primary"
            plain
            :loading="isWorkflowLoading(detailItem.id, 'start')"
            @click="onRunWorkflow(detailItem, 'start')"
          >
            开始编写
          </el-button>
          <el-button
            v-if="detailItem && canSubmitAnnualReport(detailItem)"
            type="warning"
            plain
            :loading="isWorkflowLoading(detailItem.id, 'submit')"
            @click="onRunWorkflow(detailItem, 'submit')"
          >
            提交评审
          </el-button>
          <el-button
            v-if="detailItem && canCompleteAnnualReport(detailItem)"
            type="success"
            plain
            :loading="isWorkflowLoading(detailItem.id, 'complete')"
            @click="onRunWorkflow(detailItem, 'complete')"
          >
            完成评审
          </el-button>
          <el-button
            v-if="detailItem && canReopenAnnualReport(detailItem)"
            plain
            :loading="isWorkflowLoading(detailItem.id, 'reopen')"
            @click="onRunWorkflow(detailItem, 'reopen')"
          >
            重开
          </el-button>
          <el-button plain @click="goToProjectPage(detailItem)">去项目台账</el-button>
          <el-button plain @click="goToPersonnelPage(detailItem)">去人员权限页</el-button>
        </div>
      </template>
    </AppFormDialog>

    <AppFormDialog v-model="createVisible" title="新增年报" width="720px">
      <el-form label-width="110px">
        <el-form-item label="医院名称"><el-input v-model="createForm.hospitalName" /></el-form-item>
        <el-form-item label="产品名称"><el-input v-model="createForm.productName" /></el-form-item>
        <el-form-item label="商机编号"><el-input v-model="createForm.opportunityNumber" /></el-form-item>
        <el-form-item label="省份"><el-input v-model="createForm.province" /></el-form-item>
        <el-form-item label="服务组"><el-input v-model="createForm.groupName" /></el-form-item>
        <el-form-item label="服务人员"><el-input v-model="createForm.servicePerson" /></el-form-item>
        <el-form-item label="实施状态"><el-input v-model="createForm.implementationStatus" /></el-form-item>
        <el-form-item label="维护开始"><el-date-picker v-model="createForm.maintenanceStartDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" /></el-form-item>
        <el-form-item label="维护结束"><el-date-picker v-model="createForm.maintenanceEndDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" /></el-form-item>
        <el-form-item label="报告年度">
          <el-input-number v-model="createForm.reportYear" :min="2020" :max="2035" controls-position="right" style="width: 100%" />
        </el-form-item>
        <el-form-item label="优先级">
          <el-select v-model="createForm.priority" style="width: 100%">
            <el-option label="高" value="高" />
            <el-option label="中" value="中" />
            <el-option label="低" value="低" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注"><el-input v-model="createForm.remarks" type="textarea" :rows="2" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createVisible = false" icon="Close">取消</el-button>
        <el-button type="primary" :loading="createSubmitting" @click="onSubmitCreate" icon="Plus">创建</el-button>
      </template>
    </AppFormDialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import { usePrint } from '../../composables/usePrint'
import {
  completeAnnualReport,
  createAnnualReport,
  deleteAnnualReport,
  exportAnnualReports,
  fetchAnnualReportList,
  fetchAnnualReportSummary,
  reopenAnnualReport,
  startAnnualReport,
  submitAnnualReport,
  updateAnnualReport,
} from '../../api/modules/annual-report'
import type { AnnualReportItem, AnnualReportQuery, AnnualReportSummary, AnnualReportUpsert } from '../../types/annual-report'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { GROUP_OPTIONS, PERSON_OPTIONS } from '../../constants/filterOptions'
import AppFilterCard from '../../components/AppFilterCard.vue'
import AppTableCard from '../../components/AppTableCard.vue'
import AppFormDialog from '../../components/AppFormDialog.vue'
import SummaryMetrics from '../../components/SummaryMetrics.vue'
import { useFilterStatePersist } from '../../composables/useFilterStatePersist'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useAccessControl } from '../../composables/useAccessControl'
import { normalizeStatusText, resolveAnnualReportStatusTag } from '../../utils/statusTag'

const { printArea } = usePrint()
const pageRef = ref<HTMLElement | null>(null)

const vFocus = {
  mounted: (el: HTMLElement) => {
    const input = el.querySelector('input') || el.querySelector('textarea')
    input?.focus()
  },
}

const loading = ref(false)
const exporting = ref(false)
const total = ref(0)
const tableData = ref<AnnualReportItem[]>([])
const allRows = ref<AnnualReportItem[]>([])
const workflowLoadingKey = ref('')
const summary = ref<AnnualReportSummary>({
  notStartedCount: 0,
  writingCount: 0,
  submittedCount: 0,
  completedCount: 0,
  thisYearCount: 0,
  dueThisMonthCount: 0,
  overdueCount: 0,
  total: 0,
})

const route = useRoute()
const router = useRouter()
const access = useAccessControl()
const { runInitialLoad } = useResilientLoad()

const canManageAnnualReport = computed(() => access.canPermission('annual-report.manage'))
const workflowBusy = computed(() => workflowLoadingKey.value.length > 0)
const currentYear = computed(() => new Date().getFullYear())
const currentMonth = computed(() => {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
})

const query = reactive({
  hospitalName: '',
  productName: '',
  status: '',
  reportYear: undefined as number | undefined,
  dueMonth: '',
  groupName: '',
  servicePerson: '',
  priority: '',
  reviewer: '',
  page: 1,
  size: 15,
})

const dueMonthPicker = ref('')
const isOverdueActive = ref(false)
const detailVisible = ref(false)
const detailItem = ref<AnnualReportItem | null>(null)
const createVisible = ref(false)
const createSubmitting = ref(false)
const editingCell = ref<{ rowId: number; col: string } | null>(null)
const savingCell = ref(false)

const createForm = reactive<AnnualReportUpsert>({
  opportunityNumber: '',
  hospitalName: '',
  productName: '',
  province: '',
  groupName: '',
  servicePerson: '',
  implementationStatus: '',
  maintenanceStartDate: '',
  maintenanceEndDate: '',
  reportYear: new Date().getFullYear(),
  priority: '中',
  remarks: '',
})

type AnnualSummaryCard = {
  key: string
  title: string
  value: number
  context: string
  note: string
  color: string
  active: boolean
  action?: 'status' | 'overdue'
}

type InsightBucket = {
  label: string
  value: number
}

type AnnualReportWorkflowAction = 'start' | 'submit' | 'complete' | 'reopen'

type AnnualReportFilterState = {
  hospitalName: string
  productName: string
  status: string
  reportYear: number | undefined
  dueMonth: string
  groupName: string
  servicePerson: string
  priority: string
  reviewer: string
  page: number
  size: number
}

const readRouteQueryValue = (value: unknown): string => {
  if (typeof value === 'string') return value
  if (Array.isArray(value) && typeof value[0] === 'string') return value[0]
  return ''
}

const getRouteHospitalName = () => readRouteQueryValue(route.query.hospitalName)

const statusTag = (status: string) => resolveAnnualReportStatusTag(status)
const sameStatus = (left: string, right: string) => normalizeStatusText(left) === normalizeStatusText(right)
const isStatusActive = (status: string) => sameStatus(query.status, status)

const isClosedStatus = (status: string) => sameStatus(status, '已完成')
const isSubmittedStatus = (status: string) => sameStatus(status, '已提交')
const isWritingStatus = (status: string) => sameStatus(status, '编写中')
const isNotStartedStatus = (status: string) => sameStatus(status, '未开始')

const isDueActiveRow = (item: AnnualReportItem) =>
  Boolean(item.dueMonth) &&
  item.dueMonth <= currentMonth.value &&
  !isClosedStatus(item.status) &&
  !isSubmittedStatus(item.status)

const isDueThisMonthRow = (item: AnnualReportItem) =>
  Boolean(item.dueMonth) &&
  item.dueMonth === currentMonth.value &&
  !isClosedStatus(item.status)

const getPriorityWeight = (priority?: string | null) => {
  const normalized = normalizeStatusText(priority)
  if (normalized === '高') return 3
  if (normalized === '中') return 2
  if (normalized === '低') return 1
  return 0
}

const compareDueMonth = (left?: string | null, right?: string | null) => (left || '').localeCompare(right || '')

const matchesFilteredRow = (item: AnnualReportItem) => {
  const routeHospitalName = getRouteHospitalName()
  if (isOverdueActive.value && !isDueActiveRow(item)) return false
  if (routeHospitalName && item.hospitalName !== routeHospitalName) return false
  if (query.hospitalName && !item.hospitalName?.includes(query.hospitalName)) return false
  if (query.productName && !item.productName?.includes(query.productName)) return false
  if (query.status && !sameStatus(item.status, query.status)) return false
  if (query.dueMonth && item.dueMonth !== query.dueMonth) return false
  if (query.reportYear && item.reportYear !== query.reportYear) return false
  if (query.groupName && item.groupName !== query.groupName) return false
  if (query.servicePerson && item.servicePerson !== query.servicePerson) return false
  if (query.priority && item.priority !== query.priority) return false
  if (query.reviewer && !String(item.reviewer || '').includes(query.reviewer)) return false
  return true
}

const filteredRows = computed(() => allRows.value.filter(matchesFilteredRow))
const overdueRows = computed(() => filteredRows.value.filter(isDueActiveRow))
const reviewQueue = computed(() =>
  filteredRows.value
    .filter((item) => isWritingStatus(item.status) || isSubmittedStatus(item.status))
    .slice()
    .sort((left, right) => {
      const priorityGap = getPriorityWeight(right.priority) - getPriorityWeight(left.priority)
      if (priorityGap !== 0) return priorityGap
      return compareDueMonth(left.dueMonth, right.dueMonth)
    })
    .slice(0, 6),
)

const scopedSummary = computed<AnnualReportSummary>(() => {
  const items = filteredRows.value
  return {
    notStartedCount: items.filter((item) => isNotStartedStatus(item.status)).length,
    writingCount: items.filter((item) => isWritingStatus(item.status)).length,
    submittedCount: items.filter((item) => isSubmittedStatus(item.status)).length,
    completedCount: items.filter((item) => isClosedStatus(item.status)).length,
    thisYearCount: items.filter((item) => item.reportYear === currentYear.value).length,
    dueThisMonthCount: items.filter(isDueThisMonthRow).length,
    overdueCount: items.filter(isDueActiveRow).length,
    total: items.length,
  }
})

const buildTopBuckets = (items: AnnualReportItem[], selector: (item: AnnualReportItem) => string, limit = 6): InsightBucket[] => {
  const counter = new Map<string, number>()
  for (const item of items) {
    const key = selector(item)?.trim() || '未填写'
    counter.set(key, (counter.get(key) ?? 0) + 1)
  }
  return Array.from(counter.entries())
    .map(([label, value]) => ({ label, value }))
    .sort((left, right) => right.value - left.value || left.label.localeCompare(right.label, 'zh-CN'))
    .slice(0, limit)
}

const topServicePersonBuckets = computed(() => buildTopBuckets(filteredRows.value, (item) => item.servicePerson || '未分配'))
const topGroupBuckets = computed(() => buildTopBuckets(filteredRows.value, (item) => item.groupName || '未分组'))
const statusBuckets = computed(() => buildTopBuckets(filteredRows.value, (item) => item.status || '未知状态', 4))
const activeGroupCount = computed(() => new Set(filteredRows.value.map((item) => item.groupName).filter(Boolean)).size)

const activeAnnualReportFilterLabel = computed(() => {
  const labels: string[] = []
  const routeHospitalName = getRouteHospitalName()

  if (isOverdueActive.value) labels.push('逾期待处理')
  if (query.status) labels.push(query.status)
  if (query.dueMonth) labels.push(`到期 ${query.dueMonth}`)
  if (query.groupName) labels.push(query.groupName)
  if (query.servicePerson) labels.push(query.servicePerson)
  if (query.priority) labels.push(`${query.priority}优先`)
  if (query.reviewer) labels.push(`评审:${query.reviewer}`)
  if (query.reportYear) labels.push(`${query.reportYear} 年`)
  if (routeHospitalName) labels.push(routeHospitalName)
  if (query.productName) labels.push(query.productName)

  return labels.length ? labels.join(' / ') : '当前全部范围'
})

const heroSignals = computed(() => [
  {
    label: '当前范围',
    value: scopedSummary.value.total,
    note: '当前筛选后纳入工作台的年报总量',
  },
  {
    label: '待提交/评审',
    value: reviewQueue.value.length,
    note: '编写中与已提交待评审的报告',
  },
  {
    label: '本月到期',
    value: scopedSummary.value.dueThisMonthCount,
    note: '本月需要推进闭环的报告',
  },
  {
    label: '已逾期',
    value: scopedSummary.value.overdueCount,
    note: '已到期且仍需继续处理的报告',
  },
])

const summaryCards = computed<AnnualSummaryCard[]>(() => [
  {
    key: '未开始',
    title: '未开始',
    value: scopedSummary.value.notStartedCount,
    context: '报告状态',
    note: '查看尚未启动编写的年报',
    color: '#7c98bc',
    active: isStatusActive('未开始'),
    action: 'status',
  },
  {
    key: '编写中',
    title: '编写中',
    value: scopedSummary.value.writingCount,
    context: '报告状态',
    note: '跟进正在推进中的年报',
    color: '#c7a06c',
    active: isStatusActive('编写中'),
    action: 'status',
  },
  {
    key: '已提交',
    title: '已提交',
    value: scopedSummary.value.submittedCount,
    context: '报告状态',
    note: '查看已提交待评审的报告',
    color: '#7c98bc',
    active: isStatusActive('已提交'),
    action: 'status',
  },
  {
    key: '已完成',
    title: '已完成',
    value: scopedSummary.value.completedCount,
    context: '报告状态',
    note: '查看已归档完成的年报',
    color: '#7d9f92',
    active: isStatusActive('已完成'),
    action: 'status',
  },
  {
    key: 'overdue',
    title: '逾期待处理',
    value: scopedSummary.value.overdueCount,
    context: '风险视图',
    note: '优先处理已到期但尚未闭环的报告',
    color: scopedSummary.value.overdueCount > 0 ? '#c58a87' : '#3f4f63',
    active: isOverdueActive.value,
    action: 'overdue',
  },
  {
    key: 'all',
    title: '总数',
    value: scopedSummary.value.total,
    context: '全量视图',
    note: '返回当前权限范围内的全部年报',
    color: '#3f4f63',
    active: !normalizeStatusText(query.status) && !isOverdueActive.value,
    action: 'status',
  },
])

const quickActions = computed(() => [
  {
    title: '逾期待处理',
    note: `${scopedSummary.value.overdueCount} 项`,
    onClick: () => onOverdueClick(),
  },
  {
    title: '待评审',
    note: `${scopedSummary.value.submittedCount} 项`,
    onClick: () => onStatClick('已提交'),
  },
  {
    title: '编写中',
    note: `${scopedSummary.value.writingCount} 项`,
    onClick: () => onStatClick('编写中'),
  },
  {
    title: '本年报告',
    note: `${scopedSummary.value.thisYearCount} 项`,
    onClick: () => {
      isOverdueActive.value = false
      query.status = ''
      query.reportYear = currentYear.value
      query.page = 1
      void loadData()
    },
  },
])

const statusOptions = ref<string[]>(['未开始', '编写中', '已提交', '已完成'])
const groupOptions = ref<string[]>([...GROUP_OPTIONS])
const servicePersonOptions = ref<string[]>([...PERSON_OPTIONS])

const filteredGroupOptions = computed(() => {
  const groups = allRows.value
    .filter((item) => {
      if (getRouteHospitalName() && item.hospitalName !== getRouteHospitalName()) return false
      if (query.status && !sameStatus(item.status, query.status)) return false
      if (query.dueMonth && item.dueMonth !== query.dueMonth) return false
      if (query.reportYear && item.reportYear !== query.reportYear) return false
      if (query.servicePerson && item.servicePerson !== query.servicePerson) return false
      return true
    })
    .map((item) => item.groupName)
    .filter(Boolean)

  const unique = Array.from(new Set(groups)).sort((left, right) => left.localeCompare(right, 'zh-CN'))
  return unique.length > 0 ? unique : groupOptions.value
})

const filteredServicePersonOptions = computed(() => {
  const people = allRows.value
    .filter((item) => {
      if (getRouteHospitalName() && item.hospitalName !== getRouteHospitalName()) return false
      if (query.status && !sameStatus(item.status, query.status)) return false
      if (query.dueMonth && item.dueMonth !== query.dueMonth) return false
      if (query.reportYear && item.reportYear !== query.reportYear) return false
      if (query.groupName && item.groupName !== query.groupName) return false
      return true
    })
    .map((item) => item.servicePerson)
    .filter(Boolean)

  const unique = Array.from(new Set(people)).sort((left, right) => left.localeCompare(right, 'zh-CN'))
  return unique.length > 0 ? unique : servicePersonOptions.value
})

const buildWorkflowKey = (id: number, action: AnnualReportWorkflowAction) => `${id}:${action}`
const isWorkflowLoading = (id: number, action: AnnualReportWorkflowAction) => workflowLoadingKey.value === buildWorkflowKey(id, action)

const canEditAnnualReport = (row: AnnualReportItem) =>
  canManageAnnualReport.value && (sameStatus(row.status, '未开始') || sameStatus(row.status, '编写中'))

const canDeleteAnnualReport = (row: AnnualReportItem) => canEditAnnualReport(row)
const canStartAnnualReport = (row: AnnualReportItem) => canManageAnnualReport.value && sameStatus(row.status, '未开始')
const canSubmitAnnualReport = (row: AnnualReportItem) => canManageAnnualReport.value && sameStatus(row.status, '编写中')
const canCompleteAnnualReport = (row: AnnualReportItem) => canManageAnnualReport.value && sameStatus(row.status, '已提交')
const canReopenAnnualReport = (row: AnnualReportItem) => canManageAnnualReport.value && (sameStatus(row.status, '已提交') || sameStatus(row.status, '已完成'))
const isWorkflowControlledColumn = (col: string) => col === 'status' || col === 'submitDate' || col === 'reviewer' || col === 'reviewDate'

const isEditing = (rowId: number, col: string) => {
  if (!canManageAnnualReport.value) return false
  return editingCell.value?.rowId === rowId && editingCell.value?.col === col
}

const onCellEdit = (rowId: number, col: string) => {
  if (!canManageAnnualReport.value || workflowBusy.value || isWorkflowControlledColumn(col)) return
  const row = tableData.value.find((item) => item.id === rowId) ?? allRows.value.find((item) => item.id === rowId)
  if (!row || !canEditAnnualReport(row)) return
  editingCell.value = { rowId, col }
}

const onCellSave = async (row: AnnualReportItem, col: string) => {
  if (savingCell.value || !canEditAnnualReport(row) || isWorkflowControlledColumn(col)) return
  editingCell.value = null
  savingCell.value = true

  try {
    const payload: Record<string, unknown> = {}
    payload[col] = (row as unknown as Record<string, unknown>)[col] ?? null
    const response = await updateAnnualReport(row.id, payload)
    Object.assign(row, response.data)

    const cached = allRows.value.find((item) => item.id === row.id)
    if (cached && cached !== row) Object.assign(cached, response.data)

    ElMessage.success('已保存')
    void loadSummary()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '保存失败'))
    await loadFilterOptions()
    void loadData()
  } finally {
    savingCell.value = false
  }
}

const applyUpdatedRow = (updated: AnnualReportItem) => {
  const tableRow = tableData.value.find((item) => item.id === updated.id)
  if (tableRow) Object.assign(tableRow, updated)

  const allRow = allRows.value.find((item) => item.id === updated.id)
  if (allRow) {
    Object.assign(allRow, updated)
  } else {
    allRows.value.unshift(updated)
  }

  if (detailItem.value?.id === updated.id) {
    detailItem.value = { ...updated }
  }
}

const workflowSuccessMessage: Record<AnnualReportWorkflowAction, string> = {
  start: '已开始编写',
  submit: '已提交评审',
  complete: '已完成评审',
  reopen: '已重开为编写中',
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
  if (!readRouteQueryValue(route.query.action) && !readRouteQueryValue(route.query.id)) return
  await updateRouteQuery({ action: undefined, id: undefined })
}

const applyDrillQuery = () => {
  const status = readRouteQueryValue(route.query.status)
  const hospitalName = readRouteQueryValue(route.query.hospitalName)
  const productName = readRouteQueryValue(route.query.productName)
  const groupName = readRouteQueryValue(route.query.groupName)
  const servicePerson = readRouteQueryValue(route.query.servicePerson)
  const priority = readRouteQueryValue(route.query.priority)
  const reviewer = readRouteQueryValue(route.query.reviewer)
  const reportYear = Number(readRouteQueryValue(route.query.reportYear))
  const dueMonth = readRouteQueryValue(route.query.dueMonth)

  query.hospitalName = hospitalName || ''
  query.productName = productName || ''
  query.status = status ? normalizeStatusText(status) : ''
  query.groupName = groupName || ''
  query.servicePerson = servicePerson || ''
  query.priority = priority || ''
  query.reviewer = reviewer || ''
  if (dueMonth) {
    query.dueMonth = dueMonth
    dueMonthPicker.value = dueMonth
  } else {
    query.dueMonth = ''
    dueMonthPicker.value = ''
  }
  query.reportYear = Number.isFinite(reportYear) && reportYear > 0 ? reportYear : undefined
  if (status || groupName || servicePerson || hospitalName || productName || priority || reviewer || dueMonth || Number.isFinite(reportYear)) {
    query.page = 1
  }
}

const { restore: restoreFilterState, clear: clearFilterState } = useFilterStatePersist<AnnualReportFilterState>({
  key: 'annual-report',
  getState: () => ({
    status: query.status,
    hospitalName: query.hospitalName,
    productName: query.productName,
    reportYear: query.reportYear,
    dueMonth: query.dueMonth,
    groupName: query.groupName,
    servicePerson: query.servicePerson,
    priority: query.priority,
    reviewer: query.reviewer,
    page: query.page,
    size: query.size,
  }),
  applyState: (state) => {
    query.status = state.status ?? ''
    query.hospitalName = state.hospitalName ?? ''
    query.productName = state.productName ?? ''
    query.reportYear = typeof state.reportYear === 'number' ? state.reportYear : undefined
    query.dueMonth = state.dueMonth ?? ''
    dueMonthPicker.value = state.dueMonth ?? ''
    query.groupName = state.groupName ?? ''
    query.servicePerson = state.servicePerson ?? ''
    query.priority = state.priority ?? ''
    query.reviewer = state.reviewer ?? ''
    query.page = typeof state.page === 'number' ? state.page : 1
    query.size = typeof state.size === 'number' ? state.size : 15
  },
})

const loadSummary = async () => {
  try {
    const response = await fetchAnnualReportSummary()
    summary.value = response.data
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载年报汇总失败'))
  }
}

const loadFilterOptions = async () => {
  try {
    const response = await fetchAnnualReportList({ page: 1, size: 100000 })
    const items = response.data.items
    allRows.value = items
    if (!items.length) return

    const groups = Array.from(new Set(items.map((item) => item.groupName).filter(Boolean))).sort((left, right) => left.localeCompare(right, 'zh-CN'))
    if (groups.length > 0) groupOptions.value = groups

    const statuses = Array.from(new Set(items.map((item) => normalizeStatusText(item.status)).filter(Boolean))).sort((left, right) => left.localeCompare(right, 'zh-CN'))
    if (statuses.length > 0) statusOptions.value = Array.from(new Set([...statusOptions.value, ...statuses]))

    const servicePeople = Array.from(new Set(items.map((item) => item.servicePerson).filter(Boolean))).sort((left, right) => left.localeCompare(right, 'zh-CN'))
    if (servicePeople.length > 0) servicePersonOptions.value = servicePeople
  } catch {
    // keep current filter options if overview prefetch fails
  }
}

const loadData = async () => {
  loading.value = true
  try {
    const routeHospitalName = getRouteHospitalName()

    if (isOverdueActive.value || routeHospitalName) {
      if (allRows.value.length === 0) {
        const response = await fetchAnnualReportList({ page: 1, size: 100000 })
        allRows.value = response.data.items
      }

      const filtered = filteredRows.value
      total.value = filtered.length
      const size = query.size <= 0 ? 15 : query.size
      const maxPage = Math.max(1, Math.ceil(filtered.length / size))
      if (query.page > maxPage) query.page = maxPage
      const page = query.page < 1 ? 1 : query.page
      const start = (page - 1) * size
      tableData.value = filtered.slice(start, start + size)
      return
    }

    const params: AnnualReportQuery = {
      page: query.page < 1 ? 1 : query.page,
      size: query.size <= 0 ? 15 : query.size,
      ...(query.hospitalName ? { hospitalName: query.hospitalName } : {}),
      ...(query.productName ? { productName: query.productName } : {}),
      ...(query.status ? { status: query.status } : {}),
      ...(query.reportYear ? { reportYear: query.reportYear } : {}),
      ...(query.dueMonth ? { dueMonth: query.dueMonth } : {}),
      ...(query.groupName ? { groupName: query.groupName } : {}),
      ...(query.servicePerson ? { servicePerson: query.servicePerson } : {}),
      ...(query.priority ? { priority: query.priority } : {}),
      ...(query.reviewer ? { reviewer: query.reviewer } : {}),
    }

    const response = await fetchAnnualReportList(params)
    tableData.value = response.data.items
    total.value = response.data.total
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载年报列表失败'))
  } finally {
    loading.value = false
  }
}

const refreshDesk = async () => {
  await Promise.allSettled([loadSummary(), loadFilterOptions(), loadData()])
}

const onRunWorkflow = async (row: AnnualReportItem, action: AnnualReportWorkflowAction) => {
  const key = buildWorkflowKey(row.id, action)
  if (workflowLoadingKey.value) return

  workflowLoadingKey.value = key
  try {
    const response = await (action === 'start'
      ? startAnnualReport(row.id)
      : action === 'submit'
        ? submitAnnualReport(row.id)
        : action === 'complete'
          ? completeAnnualReport(row.id)
          : reopenAnnualReport(row.id))

    applyUpdatedRow(response.data)
    ElMessage.success(workflowSuccessMessage[action])
    await refreshDesk()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '流程操作失败'))
  } finally {
    workflowLoadingKey.value = ''
  }
}

const onDueMonthChange = (value: string | null) => {
  isOverdueActive.value = false
  query.dueMonth = value || ''
  dueMonthPicker.value = value || ''
  query.page = 1
  void loadData()
}

const onOverdueClick = () => {
  if (isOverdueActive.value) {
    isOverdueActive.value = false
  } else {
    isOverdueActive.value = true
    query.status = ''
    query.reportYear = undefined
    query.dueMonth = ''
    dueMonthPicker.value = ''
    query.groupName = ''
    query.servicePerson = ''
  }
  query.page = 1
  void updateRouteQuery({ hospitalName: undefined, reportYear: undefined })
  void loadData()
}

const onStatClick = (status: string) => {
  isOverdueActive.value = false
  query.hospitalName = ''
  query.productName = ''
  query.status = normalizeStatusText(status)
  query.reportYear = undefined
  query.dueMonth = ''
  dueMonthPicker.value = ''
  query.groupName = ''
  query.servicePerson = ''
  query.priority = ''
  query.reviewer = ''
  query.page = 1
  void updateRouteQuery({ hospitalName: undefined, reportYear: undefined })
  void loadData()
}

const onSummaryCardSelect = (card: { key?: string | number; action?: string }) => {
  if (card.action === 'overdue') {
    onOverdueClick()
    return
  }
  if (typeof card.key !== 'string') return
  onStatClick(card.key === 'all' ? '' : card.key)
}

const goToProjectPage = (row: AnnualReportItem) => {
  void router.push({
    path: '/project/list',
    query: { groupName: row.groupName, maintenancePersonName: row.servicePerson, action: 'edit' },
  })
}

const goToPersonnelPage = (row: AnnualReportItem) => {
  void router.push({
    path: '/permission/manage',
    query: { name: row.servicePerson, groupName: row.groupName },
  })
}

const onOpenDetail = (row: AnnualReportItem, syncRoute = true) => {
  detailItem.value = row
  detailVisible.value = true
  if (syncRoute) void updateRouteQuery({ action: 'detail', id: String(row.id) })
}

const syncDetailFromRoute = () => {
  const action = readRouteQueryValue(route.query.action)
  if (action !== 'detail') return
  const id = Number(readRouteQueryValue(route.query.id))
  if (!Number.isFinite(id) || id <= 0) return
  const matched = tableData.value.find((item) => item.id === id) ?? allRows.value.find((item) => item.id === id)
  if (matched) onOpenDetail(matched, false)
}

const rowClassName = ({ row }: { row: AnnualReportItem }) => (isDueActiveRow(row) ? 'due-row' : '')

const onSearch = () => {
  query.page = 1
  void loadData()
}

const onReset = () => {
  isOverdueActive.value = false
  query.hospitalName = ''
  query.productName = ''
  query.status = ''
  query.reportYear = undefined
  query.dueMonth = ''
  dueMonthPicker.value = ''
  query.groupName = ''
  query.servicePerson = ''
  query.priority = ''
  query.reviewer = ''
  query.page = 1
  query.size = 15
  clearFilterState()
  void updateRouteQuery({
    status: undefined,
    groupName: undefined,
    servicePerson: undefined,
    hospitalName: undefined,
    reportYear: undefined,
    dueMonth: undefined,
    action: undefined,
    id: undefined,
  })
  void loadData()
}

const onExport = async () => {
  exporting.value = true
  try {
    const blob = await exportAnnualReports(query)
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `年度报告-${Date.now()}.csv`
    link.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '导出失败'))
  } finally {
    exporting.value = false
  }
}

const resetCreateForm = () => {
  createForm.opportunityNumber = ''
  createForm.hospitalName = ''
  createForm.productName = ''
  createForm.province = ''
  createForm.groupName = ''
  createForm.servicePerson = ''
  createForm.implementationStatus = ''
  createForm.maintenanceStartDate = ''
  createForm.maintenanceEndDate = ''
  createForm.reportYear = new Date().getFullYear()
  createForm.priority = '中'
  createForm.remarks = ''
}

const onOpenCreate = () => {
  resetCreateForm()
  createVisible.value = true
}

const onSubmitCreate = async () => {
  if (!createForm.hospitalName?.trim() || !createForm.productName?.trim()) {
    ElMessage.warning('请至少填写医院名称和产品名称')
    return
  }

  createSubmitting.value = true
  try {
    await createAnnualReport(createForm)
    ElMessage.success('新增成功')
    createVisible.value = false
    await refreshDesk()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '新增失败'))
  } finally {
    createSubmitting.value = false
  }
}

const onDelete = async (row: AnnualReportItem) => {
  if (!canDeleteAnnualReport(row)) {
    ElMessage.warning('仅未开始或编写中的年度报告可删除')
    return
  }

  try {
    await ElMessageBox.confirm(`确认删除年度报告 #${row.id} 吗？`, '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消',
    })
  } catch {
    return
  }

  try {
    await deleteAnnualReport(row.id)
    ElMessage.success('删除成功')
    await refreshDesk()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '删除失败'))
  }
}

const refreshLinkedData = async () => {
  await refreshDesk()
}

useLinkedRealtimeRefresh({ refresh: refreshLinkedData, scope: 'annual-report', intervalMs: 60000 })

watch(
  () => query.groupName,
  () => {
    if (query.servicePerson && !filteredServicePersonOptions.value.includes(query.servicePerson)) {
      query.servicePerson = ''
    }
  },
)

watch(
  () => query.servicePerson,
  () => {
    if (query.groupName && !filteredGroupOptions.value.includes(query.groupName)) {
      query.groupName = ''
    }
  },
)

watch(
  () => [query.status, query.reportYear, query.dueMonth],
  () => {
    if (query.groupName && !filteredGroupOptions.value.includes(query.groupName)) query.groupName = ''
    if (query.servicePerson && !filteredServicePersonOptions.value.includes(query.servicePerson)) query.servicePerson = ''
  },
)

watch(detailVisible, (visible) => {
  if (!visible) void clearRouteActionQuery()
})

watch(
  () => route.fullPath,
  async () => {
    applyDrillQuery()
    await loadData()
    syncDetailFromRoute()
  },
)

const onPrint = () => printArea(pageRef.value, '年度报告')

onMounted(async () => {
  restoreFilterState()
  applyDrillQuery()
  await runInitialLoad({
    tasks: [loadSummary, loadFilterOptions, loadData],
    retryChecks: [{ when: () => summary.value.total > 0 && total.value === 0, task: loadData }],
  })
  syncDetailFromRoute()
})
</script>

<style scoped>
.annual-report-hero {
  display: grid;
  grid-template-columns: minmax(0, 1.65fr) minmax(300px, 0.95fr);
  gap: 18px;
  margin-bottom: 18px;
}

.annual-report-hero-main,
.annual-report-control-card,
.annual-report-insight-card,
.annual-report-quick-action,
.annual-report-summary-wrap {
  border: 1px solid var(--pms-border, #e5e7eb);
  border-radius: 8px;
  background: #fff;
  box-shadow: var(--pms-shadow-card, 0 1px 2px rgba(15, 23, 42, 0.04));
}

.annual-report-hero-main {
  padding: 22px 24px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.annual-report-hero-kicker-row,
.annual-report-control-copy,
.annual-report-insight-head,
.annual-report-table-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.annual-report-hero-kicker {
  font-size: 12px;
  line-height: 18px;
  font-weight: 700;
  color: #2563eb;
  text-transform: uppercase;
}

.annual-report-hero-badge {
  display: inline-flex;
  align-items: center;
  min-height: 24px;
  padding: 0 10px;
  border-radius: 999px;
  background: #eff6ff;
  color: #1d4ed8;
  font-size: 12px;
  line-height: 18px;
}

.annual-report-hero-title {
  margin: 0;
  font-size: 30px;
  line-height: 1.2;
  font-weight: 700;
  color: #0f172a;
}

.annual-report-hero-subtitle {
  font-size: 14px;
  line-height: 1.7;
  color: #475569;
  max-width: 780px;
}

.annual-report-hero-signals {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
}

.annual-report-signal-card {
  min-height: 112px;
  padding: 14px 16px;
  border-radius: 8px;
  background: linear-gradient(180deg, #f8fbff 0%, #ffffff 100%);
  border: 1px solid #e2e8f0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.annual-report-signal-label {
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
}

.annual-report-signal-value {
  font-size: 30px;
  line-height: 1.1;
  font-weight: 700;
  color: #0f172a;
}

.annual-report-signal-note {
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
}

.annual-report-hero-side {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.annual-report-control-card {
  padding: 18px;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.annual-report-control-copy {
  flex-direction: column;
  align-items: flex-start;
  justify-content: flex-start;
  gap: 6px;
}

.annual-report-control-title,
.annual-report-insight-title,
.annual-report-table-title {
  font-size: 16px;
  line-height: 24px;
  font-weight: 700;
  color: #0f172a;
}

.annual-report-control-note,
.annual-report-insight-note,
.annual-report-table-note {
  font-size: 13px;
  line-height: 20px;
  color: #64748b;
}

.annual-report-control-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.annual-report-quick-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.annual-report-quick-action {
  appearance: none;
  padding: 16px;
  text-align: left;
  cursor: pointer;
  transition: border-color 0.2s ease, box-shadow 0.2s ease, transform 0.2s ease;
}

.annual-report-quick-action:hover {
  border-color: #bfdbfe;
  box-shadow: 0 8px 20px rgba(37, 99, 235, 0.08);
  transform: translateY(-1px);
}

.annual-report-quick-title {
  display: block;
  font-size: 14px;
  line-height: 20px;
  font-weight: 700;
  color: #0f172a;
}

.annual-report-quick-note {
  display: block;
  margin-top: 6px;
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
}

.annual-report-insight-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 16px;
  margin-bottom: 18px;
}

.annual-report-insight-card {
  padding: 18px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-height: 236px;
}

.annual-report-insight-meta {
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
  white-space: nowrap;
}

.annual-report-queue-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.annual-report-queue-item {
  appearance: none;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  background: #f8fafc;
  padding: 12px;
  display: flex;
  justify-content: space-between;
  gap: 12px;
  text-align: left;
  cursor: pointer;
  transition: border-color 0.2s ease, background-color 0.2s ease;
}

.annual-report-queue-item:hover {
  border-color: #bfdbfe;
  background: #f8fbff;
}

.annual-report-queue-main,
.annual-report-queue-meta {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.annual-report-queue-main strong,
.annual-report-chip strong {
  font-size: 13px;
  line-height: 20px;
  font-weight: 700;
  color: #0f172a;
}

.annual-report-queue-main span,
.annual-report-queue-meta span,
.annual-report-chip span {
  font-size: 12px;
  line-height: 18px;
  color: #64748b;
}

.annual-report-chip-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.annual-report-chip {
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 12px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  background: #fff;
}

.annual-report-summary-wrap {
  padding: 16px;
  margin-bottom: 18px;
}

.annual-report-table-head {
  margin-bottom: 6px;
}

.editable-cell {
  cursor: pointer;
  padding: 2px 4px;
  border-radius: 4px;
  min-height: 22px;
  display: inline-block;
  min-width: 30px;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  transition: background-color 0.2s ease;
}

.editable-cell:hover {
  background-color: var(--el-fill-color-light);
}

.detail-actions {
  margin-top: 16px;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

:deep(.table-action-group) {
  flex-wrap: nowrap;
}

:deep(.due-row) {
  background-color: #fdf6ec !important;
}

:deep(.due-row:hover > td) {
  background-color: #faecd8 !important;
}

@media (max-width: 1440px) {
  .annual-report-hero {
    grid-template-columns: 1fr;
  }

  .annual-report-insight-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 992px) {
  .annual-report-hero-main,
  .annual-report-control-card,
  .annual-report-insight-card,
  .annual-report-summary-wrap {
    padding: 16px;
  }

  .annual-report-hero-signals,
  .annual-report-insight-grid {
    grid-template-columns: 1fr;
  }

  .annual-report-quick-grid {
    grid-template-columns: 1fr;
  }

  .annual-report-hero-kicker-row,
  .annual-report-insight-head,
  .annual-report-table-head {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
