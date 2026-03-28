import { use } from 'echarts/core'
import type { ECharts } from 'echarts/core'
import { init, registerMap } from 'echarts/core'
import { MapChart } from 'echarts/charts'
import {
  TooltipComponent,
  LegendComponent,
  VisualMapComponent,
  TitleComponent,
} from 'echarts/components'
import { CanvasRenderer } from 'echarts/renderers'

use([
  MapChart,
  TooltipComponent,
  LegendComponent,
  VisualMapComponent,
  TitleComponent,
  CanvasRenderer,
])

export const mapEcharts = {
  init,
  registerMap,
}

export type { ECharts }
