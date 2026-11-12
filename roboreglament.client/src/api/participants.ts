
import { apiRequest } from '@/api/http'
import type {
  TeamParticipant,
  ParticipantRequest,
} from '@/types/participant'

function participantsUrl(teamId: number): string {
  return `/api/teams/${teamId}/participants`
}

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

export async function createParticipant(
  teamId: number,
  data: ParticipantRequest,
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

export async function updateParticipant(
  teamId: number,
  participantId: number,
  data: ParticipantRequest,
): Promise<TeamParticipant> {
  return apiRequest<TeamParticipant>(
    `${participantsUrl(teamId)}/${participantId}`,
    {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
    },
  )
}

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
