
import { apiRequest } from '@/api/http'

import type {
  Team,
  CreateTeamRequest,
} from '@/types/teams'

const API_URL = '/api/teams'

// Получение команд текущего тренера
export async function getMyTeams(
  signal?: AbortSignal,
): Promise<Team[]> {
  const result = await apiRequest<unknown>(
    `${API_URL}/mine`,
    { signal },
  )

  if (!Array.isArray(result)) {
    throw new Error(
      'Сервер вернул некорректный список команд.',
    )
  }

  return result as Team[]
}

// Создание новой команды
export async function createTeam(
  data: CreateTeamRequest,
): Promise<Team> {
  return apiRequest<Team>(API_URL, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  })
}
