export interface HandoverItem {
  id: number
  handoverNo: string
  hospitalName: string
  fromGroup: string
  fromOwner: string
  toOwner: string
  batch: string
  stage: string
  type: string
  emailSentDate?: string | null
}

export interface HandoverSummary {
  pendingCount: number
  emailSentCount: number
  inProgressCount: number
  completedCount: number
  total: number
}

export interface HandoverQuery {
  stage?: string
  batch?: string
  type?: string
  fromGroup?: string
  toOwner?: string
  page?: number
  size?: number
}

export interface HandoverKanbanColumn {
  stage: string
  count: number
  items: HandoverItem[]
}

export interface HandoverStageUpdateRequest {
  targetStage: string
}
