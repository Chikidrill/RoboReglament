
import type { TournamentStatus } from '@/types/tournament'

export type ApplicationStatus =
  | 'Draft'
  | 'Pending'
  | 'Approved'
  | 'Rejected'
  | 'Withdrawn'

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
  submittedAt: string | null
  comment: string | null
  isAccredited: boolean
  accreditedAt: string | null
  accreditationComment: string | null
  participants: ApplicationParticipant[]
  disciplineId: number
  disciplineName: string
}

export interface CreateApplicationRequest {
  teamId: number
  disciplineId: number
  saveAsDraft?: boolean
}

export interface CoachApplicationItem {
  tournamentName: string
  tournamentStatus: TournamentStatus
  registrationDeadline: string | null
  application: TournamentApplication
}
