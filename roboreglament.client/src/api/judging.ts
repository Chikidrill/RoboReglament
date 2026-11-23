
import { apiRequest } from '@/api/http'

import type { CompetitionMatch } from '@/types/match'

import type {
  JudgeMatchScorecard,
  JudgeProtocol,
  SaveJudgeProtocolRequest,
} from '@/types/judging'

const BASE_URL = '/api/judge/matches'

function protocolUrl(
  matchId: number,
  sideId: number,
): string {
  return (
    `${BASE_URL}/${matchId}` +
    `/sides/${sideId}/protocol`
  )
}

export function getJudgeMatches(
  signal?: AbortSignal,
): Promise<CompetitionMatch[]> {
  return apiRequest<CompetitionMatch[]>(
    BASE_URL,
    { signal },
  )
}

export function getJudgeScorecard(
  matchId: number,
  signal?: AbortSignal,
): Promise<JudgeMatchScorecard> {
  return apiRequest<JudgeMatchScorecard>(
    `${BASE_URL}/${matchId}/scorecard`,
    { signal },
  )
}

export function saveJudgeDraft(
  matchId: number,
  sideId: number,
  request: SaveJudgeProtocolRequest,
): Promise<JudgeProtocol> {
  return apiRequest<JudgeProtocol>(
    protocolUrl(matchId, sideId),
    {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    },
  )
}

export function submitJudgeProtocol(
  matchId: number,
  sideId: number,
): Promise<JudgeProtocol> {
  return apiRequest<JudgeProtocol>(
    `${protocolUrl(matchId, sideId)}/submit`,
    { method: 'POST' },
  )
}
