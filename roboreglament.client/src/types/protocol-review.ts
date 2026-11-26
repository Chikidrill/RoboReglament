
import type {
  JudgeAggregationMethod,
} from '@/types/tournament'

export interface ReviewJudgeValue {
  judgePublicId: number
  protocolId: number | null
  status: 'Missing' | 'Draft' | 'Submitted'
  value: number | null
}

export interface ReviewCriterion {
  criterionId: number
  criterionName: string
  hasMismatch: boolean
  values: ReviewJudgeValue[]
}

export interface ReviewSide {
  sideId: number
  number: number
  name: string | null
  status: 'Pending' | 'Ready' | 'Mismatch'
  criteria: ReviewCriterion[]
}

export interface ProtocolReview {
  matchId: number
  aggregationMethod: JudgeAggregationMethod
  sides: ReviewSide[]
}
