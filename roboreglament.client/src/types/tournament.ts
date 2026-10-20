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
}

export type TournamentStatus =
  | 'Draft'
  | 'Registration'
  | 'InProgress'
  | 'Completed'
