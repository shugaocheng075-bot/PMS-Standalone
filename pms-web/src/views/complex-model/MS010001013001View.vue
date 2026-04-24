<template>
  <div class="map-page" v-loading="mapLoading">
    <div class="map-hero">
      <div class="map-hero-main">
        <div class="map-hero-kicker-row">
          <span class="map-hero-kicker">Project Map</span>
          <span class="map-hero-badge">{{ mapHeroFilterLabel }}</span>
        </div>
        <h2 class="map-hero-title">待办项目地图</h2>
        <div class="map-hero-subtitle">按区域、状态与实施节点快速检索任务与项目</div>
        <div class="map-hero-signals">
          <div v-for="item in mapHeroSignals" :key="item.label" class="map-hero-signal-card">
            <span class="map-hero-signal-label">{{ item.label }}</span>
            <strong class="map-hero-signal-value">{{ item.value }}</strong>
            <span class="map-hero-signal-note">{{ item.note }}</span>
          </div>
        </div>
      </div>
      <div class="map-hero-side">
        <div class="map-control-card">
          <div class="map-control-title">地图操作台</div>
          <div class="map-control-note">{{ mapLevel === 'country' ? '当前为全国视图' : `已下钻至：${selectedProvince}` }}</div>
          <div class="map-control-actions">
            <el-button v-if="mapLevel === 'province'" size="small" @click="backToCountry" icon="Back">返回全国</el-button>
          </div>
        </div>
        <div class="map-quick-grid">
          <button v-for="action in mapQuickActions" :key="action.title" class="map-quick-action" @click="action.onClick()">
            <span class="map-quick-title">{{ action.title }}</span>
            <span class="map-quick-note">{{ action.note }}</span>
          </button>
        </div>
      </div>
    </div>

    <div class="map-layout">
      <aside class="left-panel panel">
        <div class="panel-toolbar">
          <el-segmented v-model="leftMode" :options="leftModeOptions" size="small" />
          <el-input v-model="keyword" clearable size="small" placeholder="请输入省份/项目名称" />
        </div>
        <div class="summary-text">共 {{ filteredProjects.length }} 个项目</div>

        <el-table
          v-if="leftMode === '项目'"
          :data="leftProjectRows"
          height="620"
          stripe
          border
          size="small"
          empty-text="暂无项目数据"
        >
          <el-table-column prop="province" label="地区" width="78" />
          <el-table-column prop="projectName" label="项目" min-width="160" show-overflow-tooltip />
        </el-table>

        <el-table
          v-else
          :data="leftRegionRows"
          height="620"
          stripe
          border
          size="small"
          empty-text="暂无地区数据"
          @row-click="onClickRegionRow"
        >
          <el-table-column :prop="mapLevel === 'country' ? 'province' : 'city'" :label="mapLevel === 'country' ? '地区' : '城市'" min-width="120" />
          <el-table-column prop="projectCount" label="项目" width="76" />
        </el-table>
      </aside>

      <section class="center-panel panel">
        <div class="panel-toolbar center-toolbar">
          <div class="toolbar-left">{{ mapLevel === 'country' ? '全国' : `${selectedProvince || '省级'}（城市）` }}</div>
          <div class="toolbar-right">
            <el-button v-if="mapLevel === 'province'" size="small" @click="backToCountry" icon="Back">返回全国</el-button>
            <el-select v-model="statusFilter" size="small" placeholder="项目状态" clearable style="width: 132px">
              <el-option v-for="status in statusOptions" :key="status" :label="status" :value="status" />
            </el-select>
            <el-date-picker
              v-model="dateRange"
              size="small"
              type="daterange"
              value-format="YYYY-MM-DD"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              style="width: 260px"
            />
          </div>
        </div>

        <div class="map-box" ref="mapRef"></div>

        <div class="detail-head">项目详情</div>
        <el-table :data="detailRows" stripe border size="small" max-height="520" scrollbar-always-on empty-text="暂无项目详情数据">
          <el-table-column prop="projectName" label="项目名称" min-width="180" show-overflow-tooltip />
          <el-table-column prop="productName" label="实施产品" min-width="160" show-overflow-tooltip />
          <el-table-column prop="owner" label="项目负责人" min-width="120" show-overflow-tooltip />
          <el-table-column prop="implementer" label="在场实施人员" min-width="120" show-overflow-tooltip />
        </el-table>
      </section>

      <aside class="right-panel panel">
        <div class="abn-title">异常考勤次数</div>
        <div class="abn-divider"></div>
        <el-empty description="暂无数据" :image-size="88" />
      </aside>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import type { ECharts } from '../../utils/echarts-map'
import { ElMessage } from 'element-plus'
import { fetchProjectList } from '../../api/modules/project'
import type { ProjectItem } from '../../types/project'
import { getErrorMessage } from '../../utils/error'
import { fetchHospitals } from '../../api/modules/hospital'
import { HOSPITAL_CITY_MANUAL_MAP } from '../../constants/hospitalCityMap'

type ProvinceStats = {
  province: string
  projectCount: number
  overdueCount: number
  maintenanceAmount: number
}

type CityStats = {
  city: string
  projectCount: number
  overdueCount: number
  maintenanceAmount: number
}

type LeftProjectRow = {
  id: number
  province: string
  projectName: string
}

type DetailRow = {
  id: number
  projectName: string
  productName: string
  owner: string
  implementer: string
}

const mapRef = ref<HTMLDivElement | null>(null)
const chartRef = ref<ECharts | null>(null)
const mapLoading = ref(false)

let echartsModule: typeof import('../../utils/echarts-map') | null = null
let chinaGeoJsonCache: any | null = null
const chinaGeoJsonUrl = new URL('../../assets/maps/china.geo.json', import.meta.url).href

const allProjects = ref<ProjectItem[]>([])
const selectedProvince = ref('')
const selectedCity = ref('')
const statusFilter = ref('')
const keyword = ref('')
const leftMode = ref<'地区' | '项目'>('地区')
const dateRange = ref<[string, string] | []>([])
const mapLevel = ref<'country' | 'province'>('country')
const currentMapName = ref('china')

const leftModeOptions: Array<'地区' | '项目'> = ['地区', '项目']
const statusOptions = ['合同已签署', '超期未签署', '免费维护期', '停止维护', '未知']
const municipalitySet = new Set(['北京', '上海', '天津', '重庆'])
const provinceMapCache = new Map<string, any>()
const hospitalCityMap = ref<Map<string, string>>(new Map())

const chinaFeatures = ref<Array<any>>([])

const ensureEcharts = async () => {
  if (!echartsModule) {
    echartsModule = await import('../../utils/echarts-map')
  }

  return echartsModule.mapEcharts
}

const ensureChinaGeoJson = async () => {
  if (!chinaGeoJsonCache) {
    const response = await fetch(chinaGeoJsonUrl)
    chinaGeoJsonCache = await response.json()
    chinaFeatures.value = ((chinaGeoJsonCache as any)?.features ?? []) as Array<any>
  }

  return chinaGeoJsonCache
}

const normalizeProvince = (province: string) => {
  return province
    .trim()
    .replace('维吾尔自治区', '')
    .replace('壮族自治区', '')
    .replace('回族自治区', '')
    .replace('自治区', '')
    .replace('特别行政区', '')
    .replace('省', '')
    .replace('市', '')
}

const normalizeHospitalName = (name: string) => {
  return name
    .trim()
    .toLowerCase()
    .replace(/\s+/g, '')
    .replace(/[（(].*?[)）]/g, '')
    .replace(/[·•\-—_]/g, '')
}

const normalizeCity = (city: string, province: string) => {
  const text = (city || '').trim()
  if (!text) {
    return ''
  }

  if (municipalitySet.has(province) && !text.endsWith('市')) {
    return `${text}市`
  }

  return text
}

const extractCityName = (hospitalName: string, province: string) => {
  const exact = hospitalCityMap.value.get(normalizeHospitalName(hospitalName))
  if (exact) {
    return normalizeCity(exact, province)
  }

  const text = (hospitalName || '').trim()
  const match = text.match(/([\u4e00-\u9fa5]{2,}(?:市|自治州|地区|盟))/)
  if (match && match[1]) {
    return match[1]
  }

  if (municipalitySet.has(province)) {
    return `${province}市`
  }

  return '其他'
}

const getProvinceAdcode = (province: string) => {
  const hit = chinaFeatures.value.find((feature) => {
    const name = normalizeProvince(String(feature?.properties?.name ?? ''))
    return name === province
  })
  return String(hit?.properties?.adcode ?? '')
}

const dateInRange = (dateText: string) => {
  if (!dateRange.value.length) {
    return true
  }

  const date = Date.parse(dateText)
  if (Number.isNaN(date)) {
    return true
  }

  const [from, to] = dateRange.value
  const fromTime = Date.parse(from)
  const toTime = Date.parse(to)
  if (Number.isNaN(fromTime) || Number.isNaN(toTime)) {
    return true
  }

  return date >= fromTime && date <= toTime
}

const filteredProjects = computed(() => {
  const text = keyword.value.trim().toLowerCase()
  return allProjects.value.filter((item) => {
    const province = normalizeProvince(item.province)
    const statusMatch = !statusFilter.value || item.contractStatus === statusFilter.value
    const keywordMatch = !text
      || item.hospitalName.toLowerCase().includes(text)
      || item.productName.toLowerCase().includes(text)
      || province.toLowerCase().includes(text)
    const dateMatch = dateInRange(item.afterSalesEndDate)
    return statusMatch && keywordMatch && dateMatch
  })
})

const provinceStats = computed<ProvinceStats[]>(() => {
  const map = new Map<string, ProvinceStats>()
  for (const item of filteredProjects.value) {
    const province = normalizeProvince(item.province)
    if (!province) {
      continue
    }

    if (!map.has(province)) {
      map.set(province, {
        province,
        projectCount: 0,
        overdueCount: 0,
        maintenanceAmount: 0,
      })
    }

    const entry = map.get(province)!
    entry.projectCount += 1
    entry.overdueCount += item.overdueDays > 0 ? 1 : 0
    entry.maintenanceAmount += Number(item.maintenanceAmount || 0)
  }

  return Array.from(map.values()).sort((a, b) => b.projectCount - a.projectCount)
})

const cityStats = computed<CityStats[]>(() => {
  if (!selectedProvince.value) {
    return []
  }

  const map = new Map<string, CityStats>()
  const source = filteredProjects.value.filter((item) => normalizeProvince(item.province) === selectedProvince.value)

  for (const item of source) {
    const city = extractCityName(item.hospitalName, selectedProvince.value)
    if (!map.has(city)) {
      map.set(city, {
        city,
        projectCount: 0,
        overdueCount: 0,
        maintenanceAmount: 0,
      })
    }

    const entry = map.get(city)!
    entry.projectCount += 1
    entry.overdueCount += item.overdueDays > 0 ? 1 : 0
    entry.maintenanceAmount += Number(item.maintenanceAmount || 0)
  }

  return Array.from(map.values()).sort((a, b) => b.projectCount - a.projectCount)
})

const leftRegionRows = computed(() => {
  return mapLevel.value === 'country' ? provinceStats.value : cityStats.value
})

const leftProjectRows = computed<LeftProjectRow[]>(() => {
  return filteredProjects.value
    .slice()
    .sort((a, b) => normalizeProvince(a.province).localeCompare(normalizeProvince(b.province), 'zh-CN'))
    .map((item) => ({
      id: item.id,
      province: normalizeProvince(item.province),
      projectName: item.hospitalName,
    }))
})

const detailRows = computed<DetailRow[]>(() => {
  let source = filteredProjects.value

  if (mapLevel.value === 'province' && selectedProvince.value) {
    source = source.filter((item) => normalizeProvince(item.province) === selectedProvince.value)
  }

  if (mapLevel.value === 'province' && selectedCity.value) {
    source = source.filter((item) => extractCityName(item.hospitalName, selectedProvince.value) === selectedCity.value)
  }

  return source.slice(0, 12).map((item) => ({
    id: item.id,
    projectName: item.hospitalName,
    productName: item.productName,
    owner: item.salesName || '--',
    implementer: item.maintenancePersonName || '--',
  }))
})

const mapHeroFilterLabel = computed(() => {
  if (statusFilter.value) return `状态：${statusFilter.value}`
  if (keyword.value) return `搜索：${keyword.value}`
  if (mapLevel.value === 'province') return `省级：${selectedProvince.value}`
  return '全国项目概览'
})

const mapHeroSignals = computed(() => {
  const overdueTotal = filteredProjects.value.filter((p) => p.overdueDays > 0).length
  const totalAmount = filteredProjects.value.reduce((sum, p) => sum + Number(p.maintenanceAmount || 0), 0)
  return [
    { label: '覆盖省份', value: provinceStats.value.length, note: '个地区' },
    { label: '当前项目', value: filteredProjects.value.length, note: '个项目' },
    { label: '超期项目', value: overdueTotal, note: '需关注' },
    { label: '合同总额', value: `${(totalAmount / 10000).toFixed(0)}万`, note: '年度维保' },
  ]
})

const mapQuickActions = computed(() => [
  {
    title: '超期合同',
    note: `${filteredProjects.value.filter((p) => p.overdueDays > 0).length} 个`,
    onClick: () => { statusFilter.value = '超期未签署'; keyword.value = '' },
  },
  {
    title: '免费维护',
    note: `${allProjects.value.filter((p) => p.contractStatus === '免费维护期').length} 个`,
    onClick: () => { statusFilter.value = '免费维护期'; keyword.value = '' },
  },
  {
    title: '已签署',
    note: `${allProjects.value.filter((p) => p.contractStatus === '合同已签署').length} 个`,
    onClick: () => { statusFilter.value = '合同已签署'; keyword.value = '' },
  },
  {
    title: '全部重置',
    note: '清除所有筛选',
    onClick: () => { statusFilter.value = ''; keyword.value = ''; dateRange.value = []; if (mapLevel.value === 'province') backToCountry() },
  },
])

const updateMap = () => {
  if (!chartRef.value) {
    return
  }

  const isCountry = mapLevel.value === 'country'
  const stats = isCountry ? provinceStats.value : cityStats.value
  const maxValue = Math.max(...stats.map((item) => item.projectCount), 1)
  const seriesData = isCountry
    ? provinceStats.value.map((item) => ({
      name: item.province,
      value: item.projectCount,
      overdueCount: item.overdueCount,
      maintenanceAmount: item.maintenanceAmount,
    }))
    : cityStats.value.map((item) => ({
      name: item.city,
      value: item.projectCount,
      overdueCount: item.overdueCount,
      maintenanceAmount: item.maintenanceAmount,
    }))

  chartRef.value.setOption({
    tooltip: {
      trigger: 'item',
      formatter: (params: any) => {
        const data = params.data
        if (!data) {
          return `${params.name}<br/>项目数：0`
        }
        return [
          `${params.name}`,
          `项目数：${data.value}`,
          `超期项目：${data.overdueCount}`,
          `合同额：${(Number(data.maintenanceAmount || 0) / 10000).toFixed(1)} 万`,
        ].join('<br/>')
      },
    },
    visualMap: {
      min: 0,
      max: maxValue,
      text: ['高', '低'],
      orient: 'vertical',
      right: 8,
      bottom: 8,
      itemWidth: 10,
      itemHeight: 76,
      inRange: {
        color: ['#d7efff', '#5ab3eb', '#1f78be'],
      },
      textStyle: {
        color: '#4c6283',
      },
    },
    series: [
      {
        name: '项目分布',
        type: 'map',
        map: currentMapName.value,
        roam: true,
        zoom: isCountry ? 1.08 : 1,
        label: {
          show: true,
          fontSize: 10,
          color: '#33506f',
        },
        emphasis: {
          label: {
            color: '#0d3f6d',
          },
          itemStyle: {
            areaColor: '#65bbef',
          },
        },
        itemStyle: {
          borderColor: '#97b8d4',
          borderWidth: 1,
          areaColor: '#eef5fc',
        },
        data: seriesData,
      },
    ],
  })
}

const enterProvince = async (province: string) => {
  const echarts = await ensureEcharts()
  const adcode = getProvinceAdcode(province)
  if (!adcode) {
    ElMessage.warning(`未找到 ${province} 的地图编码，无法下钻`)
    return
  }

  const mapName = `province-${adcode}`
  if (!provinceMapCache.has(mapName)) {
    try {
      const response = await fetch(`https://geo.datav.aliyun.com/areas_v3/bound/${adcode}_full.json`)
      const geoJson = await response.json()
      provinceMapCache.set(mapName, geoJson)
      echarts.registerMap(mapName, geoJson)
    } catch {
      ElMessage.error(`加载 ${province} 城市地图失败`)
      return
    }
  }

  selectedProvince.value = province
  selectedCity.value = ''
  mapLevel.value = 'province'
  currentMapName.value = mapName
  updateMap()
}

const backToCountry = () => {
  mapLevel.value = 'country'
  selectedCity.value = ''
  currentMapName.value = 'china'
  updateMap()
}

const onClickRegionRow = (row: ProvinceStats | CityStats) => {
  if (mapLevel.value === 'country') {
    void enterProvince((row as ProvinceStats).province)
    return
  }

  selectedCity.value = (row as CityStats).city
}

const loadData = async () => {
  try {
    const res = await fetchProjectList({ page: 1, size: 100000 })
    allProjects.value = res.data.items
    if (!selectedProvince.value && provinceStats.value.length > 0) {
      selectedProvince.value = provinceStats.value[0]!.province
    }
    updateMap()
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '加载项目地图数据失败，请稍后重试'))
  }
}

const loadHospitalCityMap = async () => {
  const merged = new Map<string, string>()

  for (const [hospitalName, city] of Object.entries(HOSPITAL_CITY_MANUAL_MAP)) {
    merged.set(normalizeHospitalName(hospitalName), city)
  }

  try {
    const res = await fetchHospitals({ page: 1, size: 100000 })
    for (const item of res.data.items) {
      if (!item.hospitalName || !item.city) {
        continue
      }

      merged.set(normalizeHospitalName(item.hospitalName), item.city)
    }
  } catch {
  }

  hospitalCityMap.value = merged
}

const resizeMap = () => {
  chartRef.value?.resize()
}

onMounted(async () => {
  await nextTick()
  const echarts = await ensureEcharts()
  const chinaGeoJson = await ensureChinaGeoJson()

  if (mapRef.value) {
    echarts.registerMap('china', chinaGeoJson as any)
    chartRef.value = echarts.init(mapRef.value)
    chartRef.value.on('click', (params: any) => {
      if (typeof params.name !== 'string' || !params.name) {
        return
      }

      if (mapLevel.value === 'country') {
        void enterProvince(normalizeProvince(params.name))
        return
      }

      selectedCity.value = params.name
    })
  }

  await loadHospitalCityMap()
  mapLoading.value = true
  await loadData()
  mapLoading.value = false
  window.addEventListener('resize', resizeMap)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', resizeMap)
  chartRef.value?.dispose()
})

watch([provinceStats, cityStats, statusFilter, dateRange, keyword, mapLevel], () => {
  updateMap()
})
</script>

<style scoped>
.map-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

/* ===== Map Hero ===== */
.map-hero {
  display: grid;
  grid-template-columns: minmax(0, 1.18fr) minmax(300px, 0.82fr);
  gap: 16px;
  padding: 24px;
  border-radius: 28px;
  color: #ffffff;
  background:
    radial-gradient(ellipse 80% 60% at 18% 30%, rgba(47, 142, 130, 0.24) 0%, transparent 70%),
    linear-gradient(135deg, #152b45 0%, #1d4060 48%, #1f6058 100%);
  box-shadow: 0 26px 44px rgba(21, 43, 69, 0.38), inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

.map-hero-kicker-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 10px;
}

.map-hero-kicker {
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  opacity: 0.65;
}

.map-hero-badge {
  padding: 2px 10px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.14);
  font-size: 11px;
  font-weight: 600;
  opacity: 0.9;
}

.map-hero-title {
  margin: 0 0 6px;
  font-size: 28px;
  font-weight: 800;
  line-height: 1.2;
  letter-spacing: -0.5px;
}

.map-hero-subtitle {
  font-size: 13px;
  opacity: 0.7;
  margin-bottom: 18px;
}

.map-hero-signals {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 10px;
}

.map-hero-signal-card {
  display: flex;
  flex-direction: column;
  gap: 3px;
  padding: 10px 12px;
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(4px);
  border: 1px solid rgba(255, 255, 255, 0.12);
  transition: background 0.2s;
}

.map-hero-signal-card:hover {
  background: rgba(255, 255, 255, 0.16);
}

.map-hero-signal-label {
  font-size: 11px;
  font-weight: 600;
  opacity: 0.7;
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.map-hero-signal-value {
  font-size: 22px;
  font-weight: 800;
  line-height: 1.15;
}

.map-hero-signal-note {
  font-size: 11px;
  opacity: 0.6;
}

.map-hero-side {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.map-control-card {
  padding: 16px 18px;
  border-radius: 20px;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(8px);
  border: 1px solid rgba(255, 255, 255, 0.14);
}

.map-control-title {
  font-size: 15px;
  font-weight: 700;
  color: #fff;
  margin-bottom: 4px;
}

.map-control-note {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.65);
  margin-bottom: 12px;
}

.map-control-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.map-control-actions :deep(.el-button) {
  background: rgba(255, 255, 255, 0.14);
  border-color: rgba(255, 255, 255, 0.25);
  color: #fff;
}

.map-control-actions :deep(.el-button:hover) {
  background: rgba(255, 255, 255, 0.24);
}

.map-quick-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
  flex: 1;
}

.map-quick-action {
  display: flex;
  flex-direction: column;
  gap: 3px;
  padding: 10px 12px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.14);
  cursor: pointer;
  text-align: left;
  color: #fff;
  transition: background 0.2s, transform 0.15s;
}

.map-quick-action:hover {
  background: rgba(255, 255, 255, 0.18);
  transform: translateY(-1px);
}

.map-quick-title {
  font-size: 13px;
  font-weight: 700;
}

.map-quick-note {
  font-size: 11px;
  opacity: 0.65;
}

@media (max-width: 1400px) {
  .map-hero {
    grid-template-columns: 1fr;
  }

  .map-hero-signals {
    grid-template-columns: repeat(4, 1fr);
  }

  .map-quick-grid {
    grid-template-columns: repeat(4, 1fr);
  }
}

@media (max-width: 768px) {
  .map-hero {
    padding: 16px;
  }

  .map-hero-signals {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 1400px) {
  .map-hero {
    grid-template-columns: 1fr;
  }

  .map-hero-signals {
    grid-template-columns: repeat(4, 1fr);
  }

  .map-quick-grid {
    grid-template-columns: repeat(4, 1fr);
  }
}

@media (max-width: 768px) {
  .map-hero {
    padding: 16px;
  }

  .map-hero-signals {
    grid-template-columns: repeat(2, 1fr);
  }
}

.map-layout {
  display: grid;
  grid-template-columns: 240px 1fr 200px;
  gap: 16px;
  min-height: calc(100vh - 154px);
}

.panel {
  border: 1px solid #e7ebf0;
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.96);
  box-shadow: 0 12px 28px rgba(15, 23, 42, 0.04);
}

.left-panel,
.right-panel {
  padding: 16px;
}

.panel-toolbar {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-bottom: 12px;
}

.summary-text {
  margin-bottom: 12px;
  color: #64748b;
  font-size: 12px;
  font-weight: 600;
}

.center-panel {
  display: flex;
  flex-direction: column;
  padding: 16px;
}

.center-toolbar {
  flex-direction: row;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 6px;
}

.toolbar-left {
  font-size: 22px;
  font-weight: 700;
  color: #1f2937;
}

.toolbar-right {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

.map-box {
  height: 420px;
  border: 1px solid #edf2f6;
  border-radius: 14px;
  background: #f8fafc;
}

.detail-head {
  margin-top: 14px;
  margin-bottom: 10px;
  color: #334155;
  font-size: 15px;
  font-weight: 700;
}

.abn-title {
  font-size: 22px;
  font-weight: 700;
  color: #1f2937;
}

.abn-divider {
  height: 1px;
  margin: 10px 0 14px;
  background: #edf2f6;
}

@media (max-width: 1400px) {
  .map-layout {
    grid-template-columns: 220px 1fr 180px;
  }

  .toolbar-left,
  .abn-title {
    font-size: 24px;
  }
}

@media (max-width: 1200px) {
  .map-layout {
    grid-template-columns: 1fr;
  }

  .map-box {
    height: 360px;
  }
}
</style>
