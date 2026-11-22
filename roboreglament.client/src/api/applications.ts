
import { apiRequest } from '@/api/http'

import type {
  TournamentApplication,
  CreateApplicationRequest,
  CoachApplicationItem,
} from '@/types/application'

function applicationsUrl(tournamentId: number): string {
  return `/api/tournaments/${tournamentId}/applications`
}

// Общие заявки конкретного турнира.
export async function getTournamentApplications(
  tournamentId: number,
  signal?: AbortSignal,
): Promise<TournamentApplication[]> {
  return apiRequest<TournamentApplication[]>(
    applicationsUrl(tournamentId),
    { signal },
  )
}

// Собственные заявки тренера.
export async function getMyApplications(
  tournamentId?: number,
  signal?: AbortSignal,
): Promise<CoachApplicationItem[]> {
  const query = tournamentId === undefined
    ? ''
    : `?tournamentId=${tournamentId}`

  return apiRequest<CoachApplicationItem[]>(
    `/api/applications/mine${query}`,
    { signal },
  )
}

// Создание заявки или черновика.
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

// Отправка черновика или отозванной заявки.
export async function submitTournamentApplication(
  tournamentId: number,
  applicationId: number,
): Promise<TournamentApplication> {
  return apiRequest<TournamentApplication>(
    `${applicationsUrl(tournamentId)}/${applicationId}/submit`,
    { method: 'POST' },
  )
}

// Отзыв заявки.
export async function withdrawTournamentApplication(
  tournamentId: number,
  applicationId: number,
): Promise<TournamentApplication> {
  return apiRequest<TournamentApplication>(
    `${applicationsUrl(tournamentId)}/${applicationId}/withdraw`,
    { method: 'POST' },
  )
}

// Удаление черновика.
export async function deleteApplicationDraft(
  tournamentId: number,
  applicationId: number,
): Promise<void> {
  await apiRequest<void>(
    `${applicationsUrl(tournamentId)}/${applicationId}`,
    { method: 'DELETE' },
  )
}

// Обновление заявленного состава.
export async function refreshApplicationParticipants(
  tournamentId: number,
  applicationId: number,
): Promise<TournamentApplication> {
  return apiRequest<TournamentApplication>(
    `${applicationsUrl(tournamentId)}/${applicationId}/participants/refresh`,
    { method: 'PUT' },
  )
}


export function reviewTournamentApplication(
  tournamentId: number,
  applicationId: number,
  status: 'Approved' | 'Rejected',
  comment: string | null,
): Promise<TournamentApplication> {
  return apiRequest<TournamentApplication>(
    `${applicationsUrl(tournamentId)}/${applicationId}/status`,
    {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ status, comment }),
    },
  )
}

export function accreditTournamentApplication(
  tournamentId: number,
  applicationId: number,
  comment: string | null,
): Promise<TournamentApplication> {
  return apiRequest<TournamentApplication>(
    `${applicationsUrl(tournamentId)}/${applicationId}/accreditation`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ comment }),
    },
  )
}
