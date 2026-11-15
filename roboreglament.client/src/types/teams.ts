
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
export interface TeamCoach {
  publicId: number
  firstName: string
  lastName: string
  middleName: string | null
  isSelf: boolean
}

export interface TeamCoachLookup {
  publicId: number
  firstName: string
  lastName: string
  middleName: string | null
  alreadyInTeam: boolean
}

