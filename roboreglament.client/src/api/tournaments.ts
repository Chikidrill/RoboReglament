
import type { Tournament } from '@/types/tournament'

const API_URL = '/api/tournaments'

async function getJson<T>(
  url: string,
  signal?: AbortSignal,
): Promise<T> {
  const response = await fetch(url, {
    method: 'GET',
    credentials: 'same-origin',
    signal,
  })

  if (!response.ok) {
    throw new Error(
      `Ошибка при обращении к API: ${response.status}`,
    )
  }

  return (await response.json()) as T
}

export async function getTournaments(
  signal?: AbortSignal,
): Promise<Tournament[]> {
  const result = await getJson<unknown>(API_URL, signal)

  if (!Array.isArray(result)) {
    throw new Error('API вернул некорректный список турниров.')
  }

  return result as Tournament[]
}

export async function getTournamentById(
  id: number,
  signal?: AbortSignal,
): Promise<Tournament> {
  return getJson<Tournament>(`${API_URL}/${id}`, signal)
}
