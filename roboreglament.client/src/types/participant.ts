
export interface TeamParticipant {
  id: number
  teamId: number
  firstName: string
  lastName: string
  middleName: string | null
  birthDate: string | null
}

export interface ParticipantRequest {
  firstName: string
  lastName: string
  middleName: string | null
  birthDate: string | null
}
