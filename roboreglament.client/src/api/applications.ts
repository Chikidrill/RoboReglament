
import { apiRequest } from '@/api/http'

import type {
  TournamentApplication,
  CreateApplicationRequest,
} from '@/types/application'

function applicationsUrl(tournamentId: number): string {
  return `/api/tournaments/${tournamentId}/applications`
}

// Заявки на турнир, доступные текущему пользователю.
export async function getTournamentApplications(
  tournamentId: number,
  signal?: AbortSignal,
): Promise<TournamentApplication[]> {
  const result = await apiRequest<unknown>(
    applicationsUrl(tournamentId),
    { signal },
  )

  if (!Array.isArray(result)) {
    throw new Error(
      'Сервер вернул некорректный список заявок.',
    )
  }

  return result as TournamentApplication[]
}

// Подача заявки от команды тренера.
export async function createTournamentApplication(
  tournamentId: number,
  data: CreateApplicationRequest,
): Promise<TournamentApplication> {
  return apiRequest<TournamentApplication>(
    applicationsUrl(tournamentId),
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
    },
  )
}

// Обновление состава заявки из текущего состава команды.
export async function refreshApplicationParticipants(
  tournamentId: number,
  applicationId: number,
): Promise<TournamentApplication> {
  return apiRequest<TournamentApplication>(
    `${applicationsUrl(tournamentId)}/${applicationId}/participants/refresh`,
    {
      method: 'PUT',
    },
  )
}
