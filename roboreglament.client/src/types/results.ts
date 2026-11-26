
import type {
  CompetitionFormat,
  ScoringMode,
  JudgeAggregationMethod,
  RankingMethod,
  TournamentStatus,
} from '@/types/tournament'

export interface ResultTeam {
  teamId: number
  name: string
}

export interface ResultSide {
  sideId: number
  number: number
  name: string | null
  totalScore: number
  judgeCount: number
  isWinner: boolean
  teams: ResultTeam[]
}

export interface ResultMatch {
  matchId: number
  number: number
  name: string | null
  scheduledAt: string | null
  isDraw: boolean
  sides: ResultSide[]
}

export interface TeamRanking {
  position: number
  teamId: number
  teamName: string
  played: number
  rankingValue: number
  totalScore: number
  wins: number
  draws: number
  losses: number
}

export interface DisciplineResults {
  disciplineId: number
  name: string
  format: CompetitionFormat
  scoringMode: ScoringMode
  aggregationMethod: JudgeAggregationMethod
  rankingMethod: RankingMethod

  winPoints: number
  drawPoints: number
  lossPoints: number

  matches: ResultMatch[]
  standings: TeamRanking[]
}

export interface TournamentResults {
  tournamentId: number
  tournamentName: string
  status: TournamentStatus
  disciplines: DisciplineResults[]
}
