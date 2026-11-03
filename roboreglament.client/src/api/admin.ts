
import { apiRequest } from '@/api/http'
import type { AdminUser } from '@/types/admin'

const API_URL = '/api/admin'

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
