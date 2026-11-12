
export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  firstName: string
  lastName: string
  middleName: string | null
  email: string
  password: string
}

export interface AuthUser {
  id: string
  publicId: number
  email: string
  firstName: string
  lastName: string
  middleName: string | null
  roles: string[]
}
