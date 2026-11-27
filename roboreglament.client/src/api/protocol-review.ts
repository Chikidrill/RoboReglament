
import { apiRequest } from '@/api/http'
import type { ProtocolReview } from '@/types/protocol-review'

function baseUrl(
  tournamentId: number,
  disciplineId: number,
  matchId: number,
): string {
  return (
    `/api/tournaments/${tournamentId}` +
    `/disciplines/${disciplineId}` +
    `/matches/${matchId}/protocols`
  )
}

export function getProtocolReview(
  tournamentId: number,
  disciplineId: number,
  matchId: number,
  signal?: AbortSignal,
): Promise<ProtocolReview> {
  return apiRequest<ProtocolReview>(
    `${baseUrl(tournamentId, disciplineId, matchId)}/review`,
    { signal },
  )
}

export function reopenJudgeProtocol(
  tournamentId: number,
  disciplineId: number,
  matchId: number,
  protocolId: number,
): Promise<void> {
  return apiRequest<void>(
    `${baseUrl(tournamentId, disciplineId, matchId)}` +
    `/${protocolId}/reopen`,
    { method: 'POST' },
  )
}
