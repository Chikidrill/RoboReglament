
export interface TeamParticipant {
  id: number
  teamId: number
  publicId: number
  firstName: string
  lastName: string
  middleName: string | null
  birthDate: string | null
}

export interface ParticipantLookupResponse {
  publicId: number
  firstName: string
  lastName: string
  middleName: string | null
  alreadyInTeam: boolean
}

export interface CreateParticipantRequest {
  publicId: number
}
