export const PROVINCE_OPTIONS = [
  '北京',
  '上海',
  '广东',
  '江苏',
  '四川',
  '湖南',
  '重庆',
  '陕西',
  '福建',
  '河南',
]

export const GROUP_OPTIONS = [
  '何道飞',
  '张茹',
  '李贝',
  '舒高成',
  '唐广才',
  '张浩阳',
  '陈宇',
  '孙强',
  '姚云龙',
]

export const HANDOVER_GROUP_OPTIONS = [
  '实施一组',
  '实施二组',
  '何道飞组',
  '张茹组',
  '李贝组',
  '舒高成组',
  'HIP实施部',
]

export const PERSON_OPTIONS = [
  '张三',
  '李四',
  '王五',
  '赵六',
  '钱七',
  '孙八',
  '周九',
  '吴十',
]

export const HANDOVER_OWNER_OPTIONS = [
  '张茹',
  '何道飞',
  '李贝',
  '舒高成',
  '唐广才',
  '张浩阳',
  '孙强',
  '姚云龙',
]

export const DEPARTMENT_OPTIONS = ['服务一部', '实施一组', '实施二组', '运维中心']

export const CITY_OPTIONS = ['北京', '上海', '广州', '南京', '成都', '长沙', '重庆', '西安', '福州', '郑州']

export const PROVINCE_CITY_MAP: Record<string, string[]> = {
  北京: ['北京'],
  上海: ['上海'],
  广东: ['广州'],
  江苏: ['南京'],
  四川: ['成都'],
  湖南: ['长沙'],
  重庆: ['重庆'],
  陕西: ['西安'],
  福建: ['福州'],
  河南: ['郑州'],
}

export const PROVINCE_GROUP_MAP: Record<string, string[]> = {
  北京: ['何道飞', '张茹'],
  上海: ['王可可'],
  广东: ['唐广才'],
  江苏: ['张茹'],
  四川: ['李贝'],
  湖南: ['舒高成'],
  重庆: ['张浩阳'],
  陕西: ['陈宇'],
  福建: ['姚云龙'],
  河南: ['孙强'],
}

export function getCitiesByProvince(province?: string) {
  if (!province) {
    return CITY_OPTIONS
  }

  return PROVINCE_CITY_MAP[province] ?? []
}

export function getGroupsByProvince(province?: string) {
  if (!province) {
    return GROUP_OPTIONS
  }

  const groups = PROVINCE_GROUP_MAP[province] ?? []
  return groups.length > 0 ? groups : GROUP_OPTIONS
}
