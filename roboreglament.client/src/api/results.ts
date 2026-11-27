
import { apiRequest } from '@/api/http'
import type { TournamentResults } from '@/types/results'

export function getTournamentResults(
  tournamentId: number,
  signal?: AbortSignal,
): Promise<TournamentResults> {
  return apiRequest<TournamentResults>(
    `/api/tournaments/${tournamentId}/results`,
    { signal },
  )
}
