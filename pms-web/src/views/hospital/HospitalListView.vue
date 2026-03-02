<template>
  <div class="page-shell">
    <div class="page-head">
      <div>
        <h2 class="page-title">医院管理</h2>
        <div class="page-subtitle">医院与客户分级管理、区域筛选与联系信息维护</div>
      </div>
      <el-button type="primary" @click="onOpenCreate">新增医院</el-button>
    </div>

    <el-row :gutter="16" class="stats-row">
      <el-col :span="6"><el-card shadow="never" class="stat-card stats-card"><div class="t">全部医院</div><div class="v">{{ summary.total }}</div></el-card></el-col>
      <el-col :span="6"><el-card shadow="never" class="stat-card stats-card"><div class="t">三级医院</div><div class="v success">{{ summary.threeTierCount }}</div></el-card></el-col>
      <el-col :span="6"><el-card shadow="never" class="stat-card stats-card"><div class="t">二级医院</div><div class="v warning">{{ summary.twoTierCount }}</div></el-card></el-col>
      <el-col :span="6"><el-card shadow="never" class="stat-card stats-card"><div class="t">一级医院</div><div class="v danger">{{ summary.oneTierCount }}</div></el-card></el-col>
    </el-row>

    <el-card shadow="never" class="filter-card">
      <el-form :model="query" inline>
        <el-form-item label="医院名称"><el-input v-model="query.hospitalName" clearable /></el-form-item>
        <el-form-item label="医院等级">
          <el-select v-model="query.tier" clearable style="width: 140px" placeholder="全部">
            <el-option label="三级" value="三级" />
            <el-option label="二级" value="二级" />
            <el-option label="一级" value="一级" />
          </el-select>
        </el-form-item>
        <el-form-item label="省份">
          <el-select v-model="query.province" clearable style="width: 140px" placeholder="全部">
            <el-option v-for="province in provinceOptions" :key="province" :label="province" :value="province" />
          </el-select>
        </el-form-item>
        <el-form-item label="城市">
          <el-select v-model="query.city" clearable style="width: 140px" placeholder="全部">
            <el-option v-for="city in filteredCityOptions" :key="city" :label="city" :value="city" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="onSearch">查询</el-button>
          <el-button @click="onReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" class="table-card">
      <el-table :data="tableData" v-loading="loading" stripe>
        <el-table-column prop="hospitalName" label="医院名称" min-width="220" show-overflow-tooltip />
        <el-table-column prop="tier" label="等级" width="100">
          <template #default="scope">
            <el-tag :type="tierTag(scope.row.tier)">{{ scope.row.tier }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="province" label="省份" width="100" show-overflow-tooltip />
        <el-table-column prop="city" label="城市" width="100" show-overflow-tooltip />
        <el-table-column prop="address" label="地址" min-width="220" show-overflow-tooltip />
        <el-table-column prop="contactPerson" label="联系人" width="100" show-overflow-tooltip />
        <el-table-column prop="contactPhone" label="联系电话" width="140" show-overflow-tooltip />
        <el-table-column prop="departmentCount" label="科室数量" width="100" align="right" />
        <el-table-column label="评级" min-width="180">
          <template #default="scope">
            <span>EMR: {{ scope.row.emrRatingLevel || '-' }}</span>
            <span style="margin-left: 10px">互联互通: {{ scope.row.interopRatingLevel || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="210" fixed="right">
          <template #default="scope">
            <el-button
              type="primary"
              link
              :loading="detailLoadingId === scope.row.id"
              :disabled="submitLoading || deletingId === scope.row.id || ratingLoading"
              @click="onOpenDetail(scope.row.id)"
            >详情</el-button>
            <el-button
              type="primary"
              link
              :disabled="submitLoading || deletingId === scope.row.id || ratingLoading"
              @click="onOpenEdit(scope.row)"
            >编辑</el-button>
            <el-button
              type="primary"
              link
              :disabled="submitLoading || deletingId === scope.row.id || ratingLoading"
              @click="onOpenRating(scope.row)"
            >评级</el-button>
            <el-button
              type="danger"
              link
              :loading="deletingId === scope.row.id"
              :disabled="submitLoading || deletingId === scope.row.id || ratingLoading"
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

    <el-dialog v-model="editVisible" :title="editMode === 'create' ? '新增医院' : '编辑医院'" width="620px">
      <el-form ref="editFormRef" :model="editForm" :rules="editRules" label-width="90px">
        <el-form-item label="医院名称" prop="hospitalName"><el-input v-model="editForm.hospitalName" /></el-form-item>
        <el-form-item label="医院等级">
          <el-select v-model="editForm.tier" style="width: 160px">
            <el-option label="三级" value="三级" />
            <el-option label="二级" value="二级" />
            <el-option label="一级" value="一级" />
          </el-select>
        </el-form-item>
        <el-form-item label="省份" prop="province"><el-input v-model="editForm.province" /></el-form-item>
        <el-form-item label="城市" prop="city"><el-input v-model="editForm.city" /></el-form-item>
        <el-form-item label="地址" prop="address"><el-input v-model="editForm.address" /></el-form-item>
        <el-form-item label="联系人" prop="contactPerson"><el-input v-model="editForm.contactPerson" /></el-form-item>
        <el-form-item label="联系电话" prop="contactPhone"><el-input v-model="editForm.contactPhone" /></el-form-item>
        <el-form-item label="科室数量" prop="departmentCount"><el-input v-model="editForm.departmentCount" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button :disabled="submitLoading" @click="editVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" :disabled="submitLoading" @click="onSaveEdit">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="ratingVisible" title="更新评级" width="460px">
      <el-form :model="ratingForm" label-width="95px">
        <el-form-item label="EMR评级"><el-input v-model="ratingForm.emrRatingLevel" /></el-form-item>
        <el-form-item label="互联互通评级"><el-input v-model="ratingForm.interopRatingLevel" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button :disabled="ratingLoading" @click="ratingVisible = false">取消</el-button>
        <el-button type="primary" :loading="ratingLoading" :disabled="ratingLoading" @click="onSaveRating">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="detailVisible" title="医院详情" width="620px">
      <el-descriptions v-if="detailItem" :column="2" border>
        <el-descriptions-item label="医院名称">{{ detailItem.hospitalName }}</el-descriptions-item>
        <el-descriptions-item label="等级">{{ detailItem.tier }}</el-descriptions-item>
        <el-descriptions-item label="省份">{{ detailItem.province }}</el-descriptions-item>
        <el-descriptions-item label="城市">{{ detailItem.city }}</el-descriptions-item>
        <el-descriptions-item label="地址" :span="2">{{ detailItem.address }}</el-descriptions-item>
        <el-descriptions-item label="联系人">{{ detailItem.contactPerson }}</el-descriptions-item>
        <el-descriptions-item label="联系电话">{{ detailItem.contactPhone }}</el-descriptions-item>
        <el-descriptions-item label="科室数量">{{ detailItem.departmentCount }}</el-descriptions-item>
        <el-descriptions-item label="EMR评级">{{ detailItem.emrRatingLevel || '-' }}</el-descriptions-item>
        <el-descriptions-item label="互联互通评级">{{ detailItem.interopRatingLevel || '-' }}</el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button type="primary" @click="detailVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  createHospital,
  deleteHospital,
  fetchHospitalById,
  fetchHospitals,
  fetchHospitalSummary,
  updateHospital,
  updateHospitalRating,
} from '../../api/modules/hospital'
import type { HospitalItem, HospitalRating, HospitalSummary, HospitalUpsert } from '../../types/hospital'
import type { FormInstance, FormRules } from 'element-plus'
import { useResilientLoad } from '../../composables/useResilientLoad'
import { getErrorMessage } from '../../utils/error'
import { CITY_OPTIONS, PROVINCE_OPTIONS } from '../../constants/filterOptions'
import { useLinkedRealtimeRefresh } from '../../composables/useLinkedRealtimeRefresh'

const loading = ref(false)
const total = ref(0)
const tableData = ref<HospitalItem[]>([])
const summary = ref<HospitalSummary>({
  total: 0,
  threeTierCount: 0,
  twoTierCount: 0,
  oneTierCount: 0,
  regionCounts: {},
})

const query = reactive({
  hospitalName: '',
  tier: '',
  province: '',
  city: '',
  page: 1,
  size: 10,
})

const provinceOptions = ref<string[]>([...PROVINCE_OPTIONS])
const cityOptionsByProvince = ref<Record<string, string[]>>({})

const allCityOptions = computed(() => {
  const cities = Object.values(cityOptionsByProvince.value).flat()
  return cities.length > 0 ? Array.from(new Set(cities)) : CITY_OPTIONS
})

const filteredCityOptions = computed(() => {
  if (!query.province) {
    return allCityOptions.value
  }

  const cities = cityOptionsByProvince.value[query.province]
  return cities && cities.length > 0 ? cities : allCityOptions.value
})

watch(
  () => query.province,
  () => {
    if (query.city && !filteredCityOptions.value.includes(query.city)) {
      query.city = ''
    }
  },
)

const loadFilterOptions = async () => {
  try {
    const res = await fetchHospitals({ page: 1, size: 200 })
    const items = res.data.items

    if (!items.length) {
      return
    }

    const provinces = Array.from(new Set(items.map((item) => item.province).filter(Boolean)))
    if (provinces.length > 0) {
      provinceOptions.value = provinces
    }

    const cityMap: Record<string, string[]> = {}
    for (const item of items) {
      if (!item.province || !item.city) {
        continue
      }

      const province = item.province
      const city = item.city
      const cities = cityMap[province] ?? (cityMap[province] = [])

      if (!cities.includes(city)) {
        cities.push(city)
      }
    }

    cityOptionsByProvince.value = cityMap
  } catch {
  }
}

const editVisible = ref(false)
const ratingVisible = ref(false)
const detailVisible = ref(false)
const editMode = ref<'create' | 'edit'>('create')
const activeId = ref<number | null>(null)
const detailItem = ref<HospitalItem | null>(null)
const editFormRef = ref<FormInstance>()
const submitLoading = ref(false)
const ratingLoading = ref(false)
const deletingId = ref<number | null>(null)
const detailLoadingId = ref<number | null>(null)

const editForm = reactive<HospitalUpsert>({
  hospitalName: '',
  tier: '三级',
  province: '',
  city: '',
  address: '',
  contactPerson: '',
  contactPhone: '',
  departmentCount: '',
})

const ratingForm = reactive<HospitalRating>({
  emrRatingLevel: '',
  interopRatingLevel: '',
})
const { runInitialLoad } = useResilientLoad()
const { notifyDataChanged } = useLinkedRealtimeRefresh({
  refresh: async () => {
    await Promise.allSettled([loadSummary(), loadFilterOptions(), loadData()])
  },
  scope: 'hospital',
  intervalMs: 10000,
})

const editRules: FormRules<HospitalUpsert> = {
  hospitalName: [{ required: true, message: '请输入医院名称', trigger: 'blur' }],
  province: [{ required: true, message: '请输入省份', trigger: 'blur' }],
  city: [{ required: true, message: '请输入城市', trigger: 'blur' }],
  address: [{ required: true, message: '请输入地址', trigger: 'blur' }],
  contactPerson: [{ required: true, message: '请输入联系人', trigger: 'blur' }],
  contactPhone: [
    { required: true, message: '请输入联系电话', trigger: 'blur' },
    { pattern: /^(\d{3,4}-?)?\d{7,11}$/, message: '联系电话格式不正确', trigger: 'blur' },
  ],
  departmentCount: [{ required: true, message: '请输入科室数量', trigger: 'blur' }],
}

const tierTag = (tier: string) => {
  if (tier === '三级') return 'success'
  if (tier === '二级') return 'warning'
  return 'info'
}

const loadSummary = async () => {
  try {
    const res = await fetchHospitalSummary()
    summary.value = res.data
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载医院汇总失败，请稍后重试'))
  }
}

const loadData = async () => {
  loading.value = true
  try {
    const res = await fetchHospitals(query)
    tableData.value = res.data.items
    total.value = res.data.total
  } catch (error) {
    tableData.value = []
    total.value = 0
    ElMessage.error(getErrorMessage(error, '加载医院列表失败，请稍后重试'))
  } finally {
    loading.value = false
  }
}

const onSearch = () => {
  query.page = 1
  loadData()
}

const onReset = () => {
  query.hospitalName = ''
  query.tier = ''
  query.province = ''
  query.city = ''
  query.page = 1
  query.size = 10
  loadData()
}

const resetEditForm = () => {
  editForm.hospitalName = ''
  editForm.tier = '三级'
  editForm.province = ''
  editForm.city = ''
  editForm.address = ''
  editForm.contactPerson = ''
  editForm.contactPhone = ''
  editForm.departmentCount = ''
}

const onOpenCreate = () => {
  editMode.value = 'create'
  activeId.value = null
  resetEditForm()
  editVisible.value = true
}

const onOpenEdit = (row: HospitalItem) => {
  editMode.value = 'edit'
  activeId.value = row.id
  editForm.hospitalName = row.hospitalName
  editForm.tier = row.tier
  editForm.province = row.province
  editForm.city = row.city
  editForm.address = row.address
  editForm.contactPerson = row.contactPerson
  editForm.contactPhone = row.contactPhone
  editForm.departmentCount = row.departmentCount
  editVisible.value = true
}

const onOpenRating = (row: HospitalItem) => {
  activeId.value = row.id
  ratingForm.emrRatingLevel = row.emrRatingLevel || ''
  ratingForm.interopRatingLevel = row.interopRatingLevel || ''
  ratingVisible.value = true
}

const onOpenDetail = async (id: number) => {
  if (detailLoadingId.value === id) return
  detailLoadingId.value = id
  try {
    const res = await fetchHospitalById(id)
    detailItem.value = res.data
    detailVisible.value = true
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载医院详情失败，请稍后重试'))
  } finally {
    detailLoadingId.value = null
  }
}

const onSaveEdit = async () => {
  if (submitLoading.value) {
    return
  }

  if (!editFormRef.value) {
    return
  }

  const valid = await editFormRef.value.validate().catch(() => false)
  if (!valid) return

  submitLoading.value = true
  try {
    if (editMode.value === 'create') {
      await createHospital(editForm)
      ElMessage.success('新增成功')
    } else if (activeId.value) {
      await updateHospital(activeId.value, editForm)
      ElMessage.success('更新成功')
    }

    editVisible.value = false
    await Promise.all([loadSummary(), loadData()])
    notifyDataChanged('global')
  } catch (error) {
    ElMessage.error(
      getErrorMessage(
        error,
        editMode.value === 'create' ? '新增医院失败，请稍后重试' : '更新医院失败，请稍后重试',
      ),
    )
  } finally {
    submitLoading.value = false
  }
}

const onDelete = async (row: HospitalItem) => {
  if (submitLoading.value || ratingLoading.value || deletingId.value === row.id) {
    return
  }

  deletingId.value = row.id
  try {
    await ElMessageBox.confirm(`确认删除医院“${row.hospitalName}”吗？`, '提示', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消',
    })

    await deleteHospital(row.id)
    ElMessage.success('删除成功')
    await Promise.all([loadSummary(), loadData()])
    notifyDataChanged('global')
  } catch (error) {
    if (error === 'cancel' || error === 'close') {
      ElMessage.info('已取消删除')
    } else {
      ElMessage.error(getErrorMessage(error, '删除医院失败，请稍后重试'))
    }
  } finally {
    deletingId.value = null
  }
}

const onSaveRating = async () => {
  if (ratingLoading.value) {
    return
  }

  if (!activeId.value) {
    return
  }

  ratingLoading.value = true
  try {
    await updateHospitalRating(activeId.value, ratingForm)
    ElMessage.success('评级已更新')
    ratingVisible.value = false
    await loadData()
    notifyDataChanged('global')
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '更新评级失败，请稍后重试'))
  } finally {
    ratingLoading.value = false
  }
}

onMounted(async () => {
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
