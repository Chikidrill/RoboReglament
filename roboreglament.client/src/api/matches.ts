
import { apiRequest } from '@/api/http'

import type {
  CompetitionMatch,
  EligibleMatchTeam,
  MatchJudge,
  MatchStatus,
  SaveMatchRequest,
  DrawMatchesRequest,
} from '@/types/match'

function baseUrl(
  tournamentId: number,
  disciplineId: number,
): string {
  return (
    `/api/tournaments/${tournamentId}` +
    `/disciplines/${disciplineId}/matches`
  )
}

function json(method: string, data: unknown): RequestInit {
  return {
    method,
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  }
}

export function getMatches(
  tournamentId: number,
  disciplineId: number,
  signal?: AbortSignal,
): Promise<CompetitionMatch[]> {
  return apiRequest<CompetitionMatch[]>(
    baseUrl(tournamentId, disciplineId),
    { signal },
  )
}

export function getEligibleMatchTeams(
  tournamentId: number,
  disciplineId: number,
  signal?: AbortSignal,
): Promise<EligibleMatchTeam[]> {
  return apiRequest<EligibleMatchTeam[]>(
    `${baseUrl(tournamentId, disciplineId)}/eligible-teams`,
    { signal },
  )
}

export function createMatch(
  tournamentId: number,
  disciplineId: number,
  request: SaveMatchRequest,
): Promise<CompetitionMatch> {
  return apiRequest<CompetitionMatch>(
    baseUrl(tournamentId, disciplineId),
    json('POST', request),
  )
}

export function updateMatch(
  tournamentId: number,
  disciplineId: number,
  matchId: number,
  request: SaveMatchRequest,
): Promise<CompetitionMatch> {
  return apiRequest<CompetitionMatch>(
    `${baseUrl(tournamentId, disciplineId)}/${matchId}`,
    json('PUT', request),
  )
}

export function deleteMatch(
  tournamentId: number,
  disciplineId: number,
  matchId: number,
): Promise<void> {
  return apiRequest<void>(
    `${baseUrl(tournamentId, disciplineId)}/${matchId}`,
    { method: 'DELETE' },
  )
}

export function drawMatches(
  tournamentId: number,
  disciplineId: number,
  request: DrawMatchesRequest,
): Promise<CompetitionMatch[]> {
  return apiRequest<CompetitionMatch[]>(
    `${baseUrl(tournamentId, disciplineId)}/draw`,
    json('POST', request),
  )
}

export function assignMatchJudge(
  tournamentId: number,
  disciplineId: number,
  matchId: number,
  publicId: number,
): Promise<MatchJudge> {
  return apiRequest<MatchJudge>(
    `${baseUrl(tournamentId, disciplineId)}/${matchId}/judges`,
    json('POST', { publicId }),
  )
}

export function removeMatchJudge(
  tournamentId: number,
  disciplineId: number,
  matchId: number,
  publicId: number,
): Promise<void> {
  return apiRequest<void>(
    `${baseUrl(tournamentId, disciplineId)}` +
    `/${matchId}/judges/${publicId}`,
    { method: 'DELETE' },
  )
}

export function updateMatchStatus(
  tournamentId: number,
  disciplineId: number,
  matchId: number,
  status: MatchStatus,
): Promise<CompetitionMatch> {
  return apiRequest<CompetitionMatch>(
    `${baseUrl(tournamentId, disciplineId)}/${matchId}/status`,
    json('PUT', { status }),
  )
}
