
import { apiRequest } from '@/api/http'

import type {
  TeamParticipant,
  ParticipantLookupResponse,
  CreateParticipantRequest,
} from '@/types/participant'

function participantsUrl(teamId: number): string {
  return `/api/teams/${teamId}/participants`
}

// Получить текущий состав команды
export async function getTeamParticipants(
  teamId: number,
  signal?: AbortSignal,
): Promise<TeamParticipant[]> {
  const result = await apiRequest<unknown>(
    participantsUrl(teamId),
    { signal },
  )

  if (!Array.isArray(result)) {
    throw new Error(
      'Сервер вернул некорректный список участников.',
    )
  }

  return result as TeamParticipant[]
}

// Найти зарегистрированного участника по PublicId
export async function lookupParticipant(
  teamId: number,
  publicId: number,
  signal?: AbortSignal,
): Promise<ParticipantLookupResponse> {
  return apiRequest<ParticipantLookupResponse>(
    `${participantsUrl(teamId)}/lookup/${publicId}`,
    { signal },
  )
}

// Добавить зарегистрированного пользователя в команду
export async function createParticipant(
  teamId: number,
  data: CreateParticipantRequest,
): Promise<TeamParticipant> {
  return apiRequest<TeamParticipant>(
    participantsUrl(teamId),
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
    },
  )
}

// Исключить участника из команды
export async function deleteParticipant(
  teamId: number,
  participantId: number,
): Promise<void> {
  await apiRequest<void>(
    `${participantsUrl(teamId)}/${participantId}`,
    {
      method: 'DELETE',
    },
  )
}
