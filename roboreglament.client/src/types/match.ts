
import type {
  CompetitionFormat,
  ScoringMode,
} from '@/types/tournament'

export type MatchStatus =
  | 'Scheduled'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled'

export interface EligibleMatchTeam {
  id: number
  name: string
}

export interface MatchTeam {
  id: number
  teamId: number
  teamName: string
  position: number | null
}

export interface MatchSide {
  id: number
  number: number
  name: string | null
  teams: MatchTeam[]
}

export interface MatchJudge {
  id: string
  publicId: number
  firstName: string
  lastName: string
  middleName: string | null
  assignedAt: string
}

export interface CompetitionMatch {
  id: number
  tournamentId: number
  disciplineId: number

  format: CompetitionFormat
  scoringMode: ScoringMode

  number: number
  name: string | null
  scheduledAt: string | null
  status: MatchStatus

  protocolTemplateId: number | null
  protocolTemplateName: string | null

  sides: MatchSide[]
  judges: MatchJudge[]
}

export interface SaveMatchSideRequest {
  name: string | null
  teamIds: number[]
}

export interface SaveMatchRequest {
  name: string | null
  scheduledAt: string | null
  protocolTemplateId: number | null
  sides: SaveMatchSideRequest[]
}

export interface DrawMatchesRequest {
  teamsPerSide: number
  protocolTemplateId: number | null
  namePrefix: string | null
}
