const PLACEHOLDER_VALUES = new Set([
  '-',
  '--',
  '/',
  'n/a',
  'na',
  'none',
  'null',
  'undefined',
  '无',
  '暂无',
  '未填',
  '未填写',
  '未设置',
  '空',
  'xxxx',
  'xxx',
])

const normalize = (value: unknown): string => String(value ?? '').trim()

const normalizeKey = (value: string): string => {
  return value
    .trim()
    .toLowerCase()
    .replace(/[\s_\-—:：/／()[\]（）【】]/g, '')
}

export const normalizeMajorDemandValue = (value: unknown): string => {
  const text = normalize(value)
  if (!text) {
    return ''
  }

  const compact = text.replace(/\s+/g, '').toLowerCase()
  if (PLACEHOLDER_VALUES.has(compact) || /^x{3,}$/i.test(compact)) {
    return ''
  }

  return text
}

export const resolveMajorDemandRowId = (row: Record<string, string>, fallbackIndex?: number): string => {
  const candidates = ['_RowId', 'rowId', 'RowId', '行ID', 'ID', 'id']
  for (const key of candidates) {
    const value = normalizeMajorDemandValue(row[key])
    if (value) {
      return value
    }
  }

  return typeof fallbackIndex === 'number' ? `MD-${String(fallbackIndex).padStart(5, '0')}` : ''
}

const pickByExactHeaders = (row: Record<string, string>, headers: string[]): string => {
  const headerSet = new Set(headers.map(normalizeKey))
  for (const [key, value] of Object.entries(row)) {
    if (headerSet.has(normalizeKey(key))) {
      const normalized = normalizeMajorDemandValue(value)
      if (normalized) {
        return normalized
      }
    }
  }

  return ''
}

const pickByKeywords = (row: Record<string, string>, keywords: string[]): string => {
  const normalizedKeywords = keywords.map(normalizeKey)
  for (const [key, value] of Object.entries(row)) {
    const normalizedKey = normalizeKey(key)
    if (!normalizedKeywords.some((keyword) => normalizedKey.includes(keyword))) {
      continue
    }

    const normalized = normalizeMajorDemandValue(value)
    if (normalized) {
      return normalized
    }
  }

  return ''
}

export const resolveMajorDemandHospital = (row: Record<string, string>): string => {
  return pickByExactHeaders(row, [
    '医院名称',
    '最终医院',
    '最终用户',
    '最终客户',
    '客户名称',
    '医院',
    '客户',
    '项目名称',
  ]) || pickByKeywords(row, ['医院', '客户'])
}

export const resolveMajorDemandOwner = (row: Record<string, string>): string => {
  return pickByExactHeaders(row, [
    '负责人',
    '责任人',
    '服务人员',
    '实施人员',
    '运维人员',
    '维护人员',
    '售后人员',
    '对接人',
    'owner',
  ]) || pickByKeywords(row, ['负责人', '责任人', '服务人员', '实施人员', '运维人员', '维护人员', '售后'])
}

export const resolveMajorDemandDueDate = (row: Record<string, string>): string => {
  return pickByExactHeaders(row, [
    '计划完成',
    '计划完成日期',
    '计划结束',
    '计划结束日期',
    '截止日期',
    '结束日期',
    '验收时间',
    '计划验收时间',
  ]) || pickByKeywords(row, ['计划完成', '计划结束', '截止', '结束日期', '验收时间'])
}

export const resolveMajorDemandStatus = (row: Record<string, string>): string => {
  const status = pickByExactHeaders(row, ['当前状态', '状态', '实施状态', '项目状态'])
    || pickByKeywords(row, ['当前状态', '状态'])
  if (status) {
    return status
  }

  const accepted = pickByExactHeaders(row, ['是否验收'])
  if (['是', '已验收', '完成', '已完成', 'true', 'yes', 'y'].includes(accepted.toLowerCase())) {
    return '已完成'
  }

  return ''
}

export const resolveMajorDemandTitle = (row: Record<string, string>, rowId: string): string => {
  const demandNo = pickByExactHeaders(row, [
    '重大需求编号',
    '需求编号',
    '需求单号',
    '编号',
  ])
  const demandName = pickByExactHeaders(row, [
    '重大合同名称',
    '重大需求名称',
    '需求名称',
    '合同名称',
    '标题',
    '事项',
    '需求',
  ]) || pickByKeywords(row, ['合同名称', '需求名称', '标题', '事项'])

  if (demandNo && demandName) {
    return `${demandNo} · ${demandName}`
  }

  return demandName || demandNo || `需求-${rowId}`
}
