export const normalizeStatusText = (status: string | null | undefined): string => {
  return String(status ?? '')
    .replace(/[\u3000\s]+/g, '')
    .trim()
}

export const resolveMajorDemandStatusTag = (status: string): string => {
  const normalized = normalizeStatusText(status)
  if (normalized === '已完成') return 'success'
  if (normalized === '已关闭') return 'info'
  if (normalized === '待验证') return 'warning'
  if (normalized === '处理中' || normalized === '待处理' || normalized === '待评估') return 'primary'
  return 'danger'
}

export const resolveRepairStatusTag = (status: string): string => {
  const normalized = normalizeStatusText(status)
  if (normalized === '已完成') return 'success'
  if (normalized === '处理中') return 'warning'
  if (normalized === '待处理') return 'danger'
  return 'info'
}

export const resolveAnnualReportStatusTag = (status: string): string => {
  const normalized = normalizeStatusText(status)
  if (normalized === '已完成') return 'success'
  if (normalized === '编写中') return 'warning'
  if (normalized === '已提交') return 'info'
  if (normalized === '待处理' || normalized === '处理中') return 'primary'
  return 'danger'
}

export const resolveInspectionStatusTag = (status: string): string => {
  const normalized = normalizeStatusText(status)
  if (normalized === '已完成') return 'success'
  if (normalized === '执行中' || normalized === '处理中') return 'warning'
  if (normalized === '已取消') return 'danger'
  if (normalized === '待处理') return 'primary'
  return 'info'
}

export const resolveMonthlyReportStatusTag = (status: string): string => {
  const normalized = normalizeStatusText(status)
  if (normalized === 'draft' || normalized === '草稿') return 'info'
  if (normalized === 'submitted' || normalized === '已提交') return 'warning'
  if (normalized === 'approved' || normalized === '已审核' || normalized === '已完成') return 'success'
  return 'info'
}

export const resolveProductStatusTag = (status: string): string => {
  const normalized = normalizeStatusText(status)
  if (normalized === '运行中') return 'success'
  if (normalized === '试运行') return 'warning'
  return 'info'
}