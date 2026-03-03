<template>
  <div class="map-page">
    <div class="map-head">
      <h2 class="map-title">项目地图看板</h2>
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
            <el-button v-if="mapLevel === 'province'" size="small" @click="backToCountry">返回全国</el-button>
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
import * as echarts from 'echarts'
import { ElMessage } from 'element-plus'
import { fetchProjectList } from '../../api/modules/project'
import type { ProjectItem } from '../../types/project'
import { getErrorMessage } from '../../utils/error'
import chinaGeoJson from '../../assets/maps/china.geo.json'
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
const chartRef = ref<echarts.ECharts | null>(null)

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

const chinaFeatures = ((chinaGeoJson as any)?.features ?? []) as Array<any>

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
  const hit = chinaFeatures.find((feature) => {
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
    const res = await fetchProjectList({ page: 1, size: 1000 })
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
    const res = await fetchHospitals({ page: 1, size: 1000 })
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
  await loadData()
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
  gap: 8px;
}

.map-head {
  min-height: 40px;
  display: flex;
  align-items: center;
  padding: 0 12px;
  border: 1px solid #d7e1ef;
  border-radius: 4px;
  background: linear-gradient(90deg, #eef2f8 0%, #f4f7fb 100%);
}

.map-title {
  margin: 0;
  font-size: 28px;
  font-weight: 700;
  color: #2b4b86;
}

.map-layout {
  display: grid;
  grid-template-columns: 240px 1fr 200px;
  gap: 8px;
  min-height: calc(100vh - 154px);
}

.panel {
  border: 1px solid #d7e6f3;
  border-radius: 4px;
  background: linear-gradient(180deg, #f9fbfe 0%, #f4f8fd 100%);
}

.left-panel,
.right-panel {
  padding: 8px;
}

.panel-toolbar {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 8px;
}

.summary-text {
  margin-bottom: 8px;
  color: #2f83c2;
  font-size: 12px;
  font-weight: 600;
}

.center-panel {
  display: flex;
  flex-direction: column;
  padding: 8px;
}

.center-toolbar {
  flex-direction: row;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 6px;
}

.toolbar-left {
  font-size: 30px;
  font-weight: 700;
  color: #234c7d;
}

.toolbar-right {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

.map-box {
  height: 420px;
  border: 1px solid #d8e6f4;
  border-radius: 4px;
  background: #f8fbff;
}

.detail-head {
  margin-top: 8px;
  margin-bottom: 6px;
  padding-left: 6px;
  border-left: 3px solid #3f9ad8;
  color: #294a73;
  font-weight: 700;
}

.abn-title {
  font-size: 30px;
  font-weight: 700;
  color: #233f67;
}

.abn-divider {
  height: 2px;
  margin: 8px 0 10px;
  background: #da4f5d;
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
