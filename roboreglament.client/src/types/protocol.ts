
export interface ProtocolCriterion {
  id: number
  name: string
  description: string | null
  minValue: number
  maxValue: number | null
  weight: number
  isPenalty: boolean
  order: number
}

export interface ProtocolTemplate {
  id: number
  tournamentId: number
  disciplineId: number
  name: string
  description: string | null
  isActive: boolean
  isLocked: boolean
  createdAt: string
  criteria: ProtocolCriterion[]
}

export interface SaveProtocolCriterionRequest {
  name: string
  description: string | null
  minValue: number
  maxValue: number
  weight: number
  isPenalty: boolean
}

export interface SaveProtocolTemplateRequest {
  name: string
  description: string | null
  isActive: boolean
  criteria: SaveProtocolCriterionRequest[]
}

export interface PreviewScoreRequest {
  criterionId: number
  value: number
}

export interface PreviewCriterionResponse {
  criterionId: number
  name: string
  value: number
  weight: number
  isPenalty: boolean
  contribution: number
}

export interface PreviewProtocolResponse {
  templateId: number
  total: number
  maximumPossible: number
  criteria: PreviewCriterionResponse[]
}
