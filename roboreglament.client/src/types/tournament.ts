
export type TournamentStatus =
  | 'Draft'
  | 'Registration'
  | 'InProgress'
  | 'Completed'

export type CompetitionFormat = 'Run' | 'Match'
export type ScoringMode = 'PerTeam' | 'PerAlliance'

export interface Discipline {
  id: number
  tournamentId: number
  name: string
  description: string | null

  format: CompetitionFormat
  scoringMode: ScoringMode

  sidesPerEvent: number
  minTeamsPerSide: number
  maxTeamsPerSide: number
}


export interface DisciplineRequest {
  name: string
  description: string | null
}

export interface Tournament {
  id: number
  name: string
  description: string | null
  location: string | null
  startDate: string
  endDate: string
  registrationDeadline?: string | null
  status: TournamentStatus
  createdAt?: string
  disciplines: Discipline[]
}

export interface TournamentRequest {
  name: string
  description: string | null
  location: string | null
  startDate: string
  endDate: string
  registrationDeadline: string | null
}

export interface TournamentOrganizer {
  id: string
  publicId: number
  email: string
  firstName: string
  lastName: string
  middleName: string | null
}

export interface TournamentOrganizerLookup {
  id: string
  publicId: number
  firstName: string
  lastName: string
  middleName: string | null
  alreadyOrganizer: boolean
}
