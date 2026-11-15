
export type ApplicationStatus =
  | 'Pending'
  | 'Approved'
  | 'Rejected'

export interface ApplicationParticipant {
  id: number
  firstName: string
  lastName: string
  middleName: string | null
  birthDate: string | null
}

export interface TournamentApplication {
  id: number
  tournamentId: number
  teamId: number
  teamName: string
  status: ApplicationStatus
  submittedAt: string
  comment: string | null

  isAccredited: boolean
  accreditedAt: string | null
  accreditationComment: string | null

  participants: ApplicationParticipant[]
}

export interface CreateApplicationRequest {
  teamId: number
}
