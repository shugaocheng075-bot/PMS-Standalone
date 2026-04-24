import { use } from 'echarts/core'
import type { ECharts } from 'echarts/core'
import { init } from 'echarts/core'
import { PieChart, BarChart, LineChart, GaugeChart } from 'echarts/charts'
import {
  TooltipComponent,
  LegendComponent,
  GridComponent,
  TitleComponent,
  GraphicComponent,
} from 'echarts/components'
import { CanvasRenderer } from 'echarts/renderers'

use([
  PieChart,
  BarChart,
  LineChart,
  GaugeChart,
  TooltipComponent,
  LegendComponent,
  GridComponent,
  TitleComponent,
  GraphicComponent,
  CanvasRenderer,
])

export const basicEcharts = {
  init,
}

export type { ECharts }
