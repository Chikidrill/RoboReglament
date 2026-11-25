
import type {
  CompetitionFormat,
  ScoringMode,
} from '@/types/tournament'

import type { MatchStatus } from '@/types/match'

export type JudgeProtocolStatus = 'Draft' | 'Submitted'

export interface JudgeCriterion {
  id: number
  name: string
  description: string | null
  minValue: number
  maxValue: number | null
  weight: number
  isPenalty: boolean
  order: number
}

export interface JudgeScore {
  criterionId: number
  value: number
}

export interface JudgeProtocol {
  id: number
  sideId: number
  status: JudgeProtocolStatus
  comment: string | null
  submittedAt: string | null
  total: number | null
  scores: JudgeScore[]
}

export interface JudgeSideScorecard {
  id: number
  number: number
  name: string | null
  teams: string[]
  protocol: JudgeProtocol | null
}

export interface JudgeMatchScorecard {
  matchId: number
  matchName: string | null
  status: MatchStatus
  format: CompetitionFormat
  scoringMode: ScoringMode
  templateName: string
  criteria: JudgeCriterion[]
  sides: JudgeSideScorecard[]
}

export interface SaveJudgeProtocolRequest {
  scores: JudgeScore[]
  comment: string | null
}
