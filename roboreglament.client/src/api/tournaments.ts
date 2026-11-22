
import { apiRequest } from '@/api/http'

import type {
  Tournament,
  TournamentRequest,
  TournamentStatus,
  Discipline,
  DisciplineRequest,
  TournamentOrganizer,
  TournamentOrganizerLookup,
} from '@/types/tournament'

const API_URL = '/api/tournaments'

function json(method: string, data: unknown): RequestInit {
  return {
    method,
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  }
}

// Публичные турниры.

export function getTournaments(
  signal?: AbortSignal,
): Promise<Tournament[]> {
  return apiRequest<Tournament[]>(API_URL, { signal })
}

export function getTournamentById(
  id: number,
  signal?: AbortSignal,
): Promise<Tournament> {
  return apiRequest<Tournament>(
    `${API_URL}/${id}`,
    { signal },
  )
}

// Управление турнирами.

export function getMyTournaments(
  signal?: AbortSignal,
): Promise<Tournament[]> {
  return apiRequest<Tournament[]>(
    `${API_URL}/mine`,
    { signal },
  )
}

export function getManagedTournament(
  id: number,
  signal?: AbortSignal,
): Promise<Tournament> {
  return apiRequest<Tournament>(
    `${API_URL}/${id}/manage`,
    { signal },
  )
}

export function createTournament(
  data: TournamentRequest,
): Promise<Tournament> {
  return apiRequest<Tournament>(
    API_URL,
    json('POST', data),
  )
}

export function updateTournament(
  id: number,
  data: TournamentRequest,
): Promise<Tournament> {
  return apiRequest<Tournament>(
    `${API_URL}/${id}`,
    json('PUT', data),
  )
}

export function deleteTournament(
  id: number,
): Promise<void> {
  return apiRequest<void>(
    `${API_URL}/${id}`,
    { method: 'DELETE' },
  )
}

export function updateTournamentStatus(
  id: number,
  status: TournamentStatus,
): Promise<Tournament> {
  return apiRequest<Tournament>(
    `${API_URL}/${id}/status`,
    json('PUT', { status }),
  )
}

// Дисциплины.

export function createDiscipline(
  tournamentId: number,
  data: DisciplineRequest,
): Promise<Discipline> {
  return apiRequest<Discipline>(
    `${API_URL}/${tournamentId}/disciplines`,
    json('POST', data),
  )
}

export function updateDiscipline(
  tournamentId: number,
  disciplineId: number,
  data: DisciplineRequest,
): Promise<Discipline> {
  return apiRequest<Discipline>(
    `${API_URL}/${tournamentId}/disciplines/${disciplineId}`,
    json('PUT', data),
  )
}

export function deleteDiscipline(
  tournamentId: number,
  disciplineId: number,
): Promise<void> {
  return apiRequest<void>(
    `${API_URL}/${tournamentId}/disciplines/${disciplineId}`,
    { method: 'DELETE' },
  )
}

// Организаторы.

export function getTournamentOrganizers(
  tournamentId: number,
  signal?: AbortSignal,
): Promise<TournamentOrganizer[]> {
  return apiRequest<TournamentOrganizer[]>(
    `${API_URL}/${tournamentId}/organizers`,
    { signal },
  )
}

export function lookupTournamentOrganizer(
  tournamentId: number,
  publicId: number,
): Promise<TournamentOrganizerLookup> {
  return apiRequest<TournamentOrganizerLookup>(
    `${API_URL}/${tournamentId}/organizers/lookup/${publicId}`,
  )
}

export function addTournamentOrganizer(
  tournamentId: number,
  userId: string,
): Promise<void> {
  return apiRequest<void>(
    `${API_URL}/${tournamentId}/organizers/${encodeURIComponent(userId)}`,
    { method: 'POST' },
  )
}

export function removeTournamentOrganizer(
  tournamentId: number,
  userId: string,
): Promise<void> {
  return apiRequest<void>(
    `${API_URL}/${tournamentId}/organizers/${encodeURIComponent(userId)}`,
    { method: 'DELETE' },
  )
}
