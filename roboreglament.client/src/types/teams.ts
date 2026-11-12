
export interface Team {
  id: number
  name: string
  organization: string | null
  city: string | null
  participantsCount: number
}

export interface CreateTeamRequest {
  name: string
  organization: string | null
  city: string | null
}
