
export interface ParticipantTeamItem {
  teamId: number
  teamName: string
  organization: string | null
}

export interface ParticipantApplicationItem {
  applicationId: number
  tournamentId: number
  tournamentName: string
  tournamentStatus: string
  disciplineId: number
  disciplineName: string
  teamId: number
  teamName: string
  applicationStatus: string
  isAccredited: boolean
}

export interface ParticipantMatchItem {
  matchId: number
  tournamentId: number
  disciplineId: number
  teamId: number
  name: string | null
  number: number
  scheduledAt: string | null
  status: string
}

export interface ParticipantOverview {
  teams: ParticipantTeamItem[]
  applications: ParticipantApplicationItem[]
  matches: ParticipantMatchItem[]
}
