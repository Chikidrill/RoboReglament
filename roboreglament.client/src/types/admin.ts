export interface AdminUser {
  id: string
  email: string
  firstName: string
  lastName: string
  middleName: string | null
  roles: string[]
}
