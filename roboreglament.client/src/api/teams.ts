
import { apiRequest } from '@/api/http'

import type {
  Team,
  CreateTeamRequest,
  TeamCoach,
  TeamCoachLookup,
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


// Обновление данных команды
export async function updateTeam(
  teamId: number,
  data: CreateTeamRequest,
): Promise<Team> {
  return apiRequest<Team>(
    `${API_URL}/${teamId}`,
    {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
    },
  )
}


// Удаление команды
export async function deleteTeam(
  teamId: number,
): Promise<void> {
  await apiRequest<void>(`${API_URL}/${teamId}`, {
    method: 'DELETE',
  })
}

// Получение тренеров команды
export async function getTeamCoaches(
  teamId: number,
  signal?: AbortSignal,
): Promise<TeamCoach[]> {
  return apiRequest<TeamCoach[]>(
    `${API_URL}/${teamId}/coaches`,
    { signal },
  )
}

// Поиск тренера по публичному ID
export async function lookupTeamCoach(
  teamId: number,
  publicId: number,
): Promise<TeamCoachLookup> {
  return apiRequest<TeamCoachLookup>(
    `${API_URL}/${teamId}/coaches/lookup/${publicId}`,
  )
}

// Добавление тренера
export async function addTeamCoach(
  teamId: number,
  publicId: number,
): Promise<void> {
  await apiRequest<void>(
    `${API_URL}/${teamId}/coaches`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ publicId }),
    },
  )
}

// Исключение тренера
export async function removeTeamCoach(
  teamId: number,
  publicId: number,
): Promise<void> {
  await apiRequest<void>(
    `${API_URL}/${teamId}/coaches/${publicId}`,
    {
      method: 'DELETE',
    },
  )
}
