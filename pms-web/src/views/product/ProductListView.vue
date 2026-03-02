<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">产品管理</h2>
        <div class="page-subtitle">产品字典维护与部署统计管理</div>
      </div>
      <el-button v-if="canManageProduct" type="primary" @click="onOpenCreate">新增产品</el-button>
    </div>

    <el-row :gutter="16" class="stats-row">
      <el-col :span="6"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.status === '' }" @click="onStatClick('')"><div class="t">产品总数</div><div class="v">{{ summary.total }}</div></el-card></el-col>
      <el-col :span="6"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.status === '运行中' }" @click="onStatClick('运行中')"><div class="t">运行中</div><div class="v success">{{ summary.activeCount }}</div></el-card></el-col>
      <el-col :span="6"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.status === '试运行' }" @click="onStatClick('试运行')"><div class="t">试运行</div><div class="v warning">{{ summary.pilotCount }}</div></el-card></el-col>
      <el-col :span="6"><el-card shadow="never" class="stat-card stats-card clickable" :class="{ active: query.status === '已停用' }" @click="onStatClick('已停用')"><div class="t">已停用</div><div class="v danger">{{ summary.retiredCount }}</div></el-card></el-col>
    </el-row>

    <el-card shadow="never" class="filter-card">
      <el-form :model="query" inline class="filter-form" @submit.prevent="onSearch">
        <el-form-item label="产品名称"><el-input v-model="query.productName" clearable @keyup.enter="onSearch" /></el-form-item>
        <el-form-item label="分类">
          <el-select v-model="query.category" clearable style="width: 140px" placeholder="全部">
            <el-option v-for="category in categoryOptions" :key="category" :label="category" :value="category" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="query.status" clearable style="width: 140px" placeholder="全部">
            <el-option v-for="status in statusOptions" :key="status" :label="status" :value="status" />
          </el-select>
        </el-form-item>
        <el-form-item class="filter-actions">
          <el-button type="primary" @click="onSearch">查询</el-button>
          <el-button @click="onReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" class="table-card">
      <el-table :data="tableData" v-loading="loading" stripe empty-text="暂无符合条件的数据">
        <el-table-column prop="productName" label="产品名称" min-width="220" show-overflow-tooltip sortable />
        <el-table-column prop="version" label="版本" width="100" sortable />
        <el-table-column prop="category" label="分类" width="120" show-overflow-tooltip sortable />
        <el-table-column prop="status" label="状态" width="110" sortable>
          <template #default="scope">
            <el-tag :type="statusTag(scope.row.status)">{{ scope.row.status }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="deployHospitalCount" label="部署医院数" width="120" align="right" sortable />
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="scope">
            <el-button
              type="primary"
              link
              :loading="detailLoadingId === scope.row.id"
              :disabled="submitLoading || deletingId === scope.row.id"
              @click="onOpenDetail(scope.row.id)"
            >详情</el-button>
            <el-button
              v-if="canManageProduct"
              type="primary"
              link
              :disabled="submitLoading || deletingId === scope.row.id"
              @click="onOpenEdit(scope.row)"
            >编辑</el-button>
            <el-button
              v-if="canManageProduct"
              type="danger"
              link
              :loading="deletingId === scope.row.id"
              :disabled="submitLoading || deletingId === scope.row.id"
              @click="onDelete(scope.row)"
            >删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pager">
        <el-pagination
          v-model:current-page="query.page"
          v-model:page-size="query.size"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next"
          :total="total"
          @size-change="loadData"
          @current-change="loadData"
        />
      </div>
    </el-card>

    <el-dialog v-model="editVisible" :title="editMode === 'create' ? '新增产品' : '编辑产品'" width="560px">
      <el-form ref="editFormRef" :model="editForm" :rules="editRules" label-width="90px">
        <el-form-item label="产品名称" prop="productName"><el-input v-model="editForm.productName" /></el-form-item>
        <el-form-item label="版本" prop="version"><el-input v-model="editForm.version" /></el-form-item>
        <el-form-item label="分类" prop="category"><el-input v-model="editForm.category" /></el-form-item>
        <el-form-item label="状态" prop="status">
          <el-select v-model="editForm.status" style="width: 160px">
            <el-option label="运行中" value="运行中" />
            <el-option label="试运行" value="试运行" />
            <el-option label="已停用" value="已停用" />
          </el-select>
        </el-form-item>
        <el-form-item label="部署医院" prop="deployHospitalCount">
          <el-input-number v-model="editForm.deployHospitalCount" :min="0" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button :disabled="submitLoading" @click="editVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" :disabled="submitLoading" @click="onSaveEdit">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="detailVisible" title="产品详情" width="520px">
      <el-descriptions v-if="detailItem" :column="2" border>
        <el-descriptions-item label="产品名称">{{ detailItem.productName }}</el-descriptions-item>
        <el-descriptions-item label="版本">{{ detailItem.version }}</el-descriptions-item>
        <el-descriptions-item label="分类">{{ detailItem.category }}</el-descriptions-item>
        <el-descriptions-item label="状态">{{ detailItem.status }}</el-descriptions-item>
        <el-descriptions-item label="部署医院数">{{ detailItem.deployHospitalCount }}</el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button type="primary" @click="detailVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { useRoute } from 'vue-router'
import {
  createProduct,
  deleteProduct,
  fetchProductById,
  fetchProducts,
  fetchProductSummary,
  updateProduct,
} from '../../api/modules/product'
import type { ProductItem, ProductSummary, ProductUpsert } from '../../types/product'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'
import { useAccessControl } from '../../composables/useAccessControl'

const loading = ref(false)
const total = ref(0)
const tableData = ref<ProductItem[]>([])
const summary = ref<ProductSummary>({ total: 0, activeCount: 0, pilotCount: 0, retiredCount: 0 })

const query = reactive({
  productName: '',
  category: '',
  status: '',
  page: 1,
  size: 10,
})
const categoryOptions = ref<string[]>(['EMR', '临床辅助', '管理', 'AI', '移动'])
const statusOptions = ref<string[]>(['运行中', '试运行', '已停用'])
const route = useRoute()

const readRouteQueryValue = (value: unknown): string => {
  if (typeof value === 'string') {
    return value
  }

  if (Array.isArray(value) && typeof value[0] === 'string') {
    return value[0]
  }

  return ''
}

const applyDrillQuery = () => {
  const status = readRouteQueryValue(route.query.status)
  if (!status) {
    return
  }

  query.status = status
  query.page = 1
}

const editVisible = ref(false)
const detailVisible = ref(false)
const editMode = ref<'create' | 'edit'>('create')
const activeId = ref<number | null>(null)
const detailItem = ref<ProductItem | null>(null)
const editFormRef = ref<FormInstance>()
const submitLoading = ref(false)
const deletingId = ref<number | null>(null)
const detailLoadingId = ref<number | null>(null)

const access = useAccessControl()
const canManageProduct = computed(() => access.canPermission('product.manage'))

const editForm = reactive<ProductUpsert>({
  productName: '',
  version: '',
  category: '',
  status: '运行中',
  deployHospitalCount: 0,
})
const { runInitialLoad } = useResilientLoad()
const { notifyDataChanged } = useLinkedRealtimeRefresh({
  refresh: async () => {
    await Promise.allSettled([loadSummary(), loadData()])
  },
  scope: 'product',
  intervalMs: 10000,
})

const editRules: FormRules<ProductUpsert> = {
  productName: [{ required: true, message: '请输入产品名称', trigger: 'blur' }],
  version: [{ required: true, message: '请输入版本', trigger: 'blur' }],
  category: [{ required: true, message: '请输入分类', trigger: 'blur' }],
  status: [{ required: true, message: '请选择状态', trigger: 'change' }],
}

const statusTag = (status: string) => {
  if (status === '运行中') return 'success'
  if (status === '试运行') return 'warning'
  return 'info'
}

const loadSummary = async () => {
  try {
    const res = await fetchProductSummary()
    summary.value = res.data
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载产品汇总失败，请稍后重试'))
  }
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await fetchProducts(query)
    tableData.value = res.data.items
    total.value = res.data.total
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载产品列表失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

const loadFilterOptions = async () => {
  try {
    const res = await fetchProducts({ page: 1, size: 5000 })
    const items = res.data.items

    if (!items.length) {
      return
    }

    const categories = Array.from(new Set(items.map((item) => item.category).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (categories.length > 0) {
      categoryOptions.value = Array.from(new Set([...categoryOptions.value, ...categories]))
    }

    const statuses = Array.from(new Set(items.map((item) => item.status).filter(Boolean))).sort((a, b) => a.localeCompare(b, 'zh-CN'))
    if (statuses.length > 0) {
      statusOptions.value = Array.from(new Set([...statusOptions.value, ...statuses]))
    }
  } catch {
  }
}

const onStatClick = (status: string) => {
  query.status = status
  query.page = 1
  loadData()
}

const onSearch = () => {
  query.page = 1
  loadData()
}

const onReset = () => {
  query.productName = ''
  query.category = ''
  query.status = ''
  query.page = 1
  query.size = 10
  loadData()
}

const resetEditForm = () => {
  editForm.productName = ''
  editForm.version = ''
  editForm.category = ''
  editForm.status = '运行中'
  editForm.deployHospitalCount = 0
}

const onOpenCreate = () => {
  if (!canManageProduct.value) {
    ElMessage.warning('当前账号无新增产品权限')
    return
  }

  editMode.value = 'create'
  activeId.value = null
  resetEditForm()
  editVisible.value = true
}

const onOpenEdit = (row: ProductItem) => {
  if (!canManageProduct.value) {
    ElMessage.warning('当前账号无编辑产品权限')
    return
  }

  editMode.value = 'edit'
  activeId.value = row.id
  editForm.productName = row.productName
  editForm.version = row.version
  editForm.category = row.category
  editForm.status = row.status
  editForm.deployHospitalCount = row.deployHospitalCount
  editVisible.value = true
}

const onOpenDetail = async (id: number) => {
  if (detailLoadingId.value === id) return
  detailLoadingId.value = id
  try {
    const res = await fetchProductById(id)
    detailItem.value = res.data
    detailVisible.value = true
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载产品详情失败，请稍后重试'))
  } finally {
    detailLoadingId.value = null
  }
}

const onSaveEdit = async () => {
  if (!canManageProduct.value) {
    ElMessage.warning('当前账号无产品维护权限')
    return
  }

  if (submitLoading.value) return
  if (!editFormRef.value) return
  const valid = await editFormRef.value.validate().catch(() => false)
  if (!valid) return

  submitLoading.value = true
  try {
    if (editMode.value === 'create') {
      await createProduct(editForm)
      ElMessage.success('新增成功')
    } else if (activeId.value) {
      await updateProduct(activeId.value, editForm)
      ElMessage.success('更新成功')
    }

    editVisible.value = false
    await Promise.all([loadSummary(), loadData()])
    notifyDataChanged('global')
  } catch (error) {
    ElMessage.error(
      getErrorMessage(
        error,
        editMode.value === 'create' ? '新增产品失败，请稍后重试' : '更新产品失败，请稍后重试',
      ),
    )
  } finally {
    submitLoading.value = false
  }
}

const onDelete = async (row: ProductItem) => {
  if (!canManageProduct.value) {
    ElMessage.warning('当前账号无删除产品权限')
    return
  }

  if (submitLoading.value || deletingId.value === row.id) return
  deletingId.value = row.id
  try {
    await ElMessageBox.confirm(`确认删除产品“${row.productName}”吗？`, '提示', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消',
    })

    await deleteProduct(row.id)
    ElMessage.success('删除成功')
    await Promise.all([loadSummary(), loadData()])
    notifyDataChanged('global')
  } catch (error) {
    if (error === 'cancel' || error === 'close') {
      ElMessage.info('已取消删除')
    } else {
      ElMessage.error(getErrorMessage(error, '删除产品失败，请稍后重试'))
    }
  } finally {
    deletingId.value = null
  }
}

onMounted(async () => {
  await access.ensureAccessProfileLoaded()
  applyDrillQuery()
  await runInitialLoad({
    tasks: [loadSummary, loadFilterOptions, loadData],
    retryChecks: [
      {
        when: () => summary.value.total > 0 && total.value === 0,
        task: loadData,
      },
    ],
  })
})
</script>

<style scoped>
</style>
