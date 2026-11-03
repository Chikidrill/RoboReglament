
import { apiRequest } from '@/api/http'
import type { AdminUser } from '@/types/admin'

const API_URL = '/api/admin'
const OWNER_API_URL = '/api/platform-owner'
interface UpdateUserRolesResponse {
  id: string
  email: string
  roles: string[]
}

export async function getAdminUsers(
  signal?: AbortSignal,
): Promise<AdminUser[]> {
  const result = await apiRequest<unknown>(
    `${API_URL}/users`,
    { signal },
  )

  if (!Array.isArray(result)) {
    throw new Error(
      'Сервер вернул некорректный список пользователей.',
    )
  }

  return result as AdminUser[]
}

export async function updateAdminUserRoles(
  userId: string,
  roles: string[],
): Promise<UpdateUserRolesResponse> {
  return apiRequest<UpdateUserRolesResponse>(
    `${API_URL}/users/${encodeURIComponent(userId)}/roles`,
    {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ roles }),
    },
  )
}

interface AdministratorRoleResponse {
  id: string
  email: string
  roles: string[]
}

export async function assignAdministrator(
  userId: string,
): Promise<AdministratorRoleResponse> {
  return apiRequest<AdministratorRoleResponse>(
    `${OWNER_API_URL}/administrators/${encodeURIComponent(userId)}`,
    {
      method: 'POST',
    },
  )
}

export async function revokeAdministrator(
  userId: string,
): Promise<AdministratorRoleResponse> {
  return apiRequest<AdministratorRoleResponse>(
    `${OWNER_API_URL}/administrators/${encodeURIComponent(userId)}`,
    {
      method: 'DELETE',
    },
  )
}

