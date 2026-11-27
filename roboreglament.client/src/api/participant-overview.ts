
import { apiRequest } from '@/api/http'
import type {
  ParticipantOverview,
} from '@/types/participant-overview'

export function getParticipantOverview():
  Promise<ParticipantOverview> {
  return apiRequest<ParticipantOverview>(
    '/api/participant/overview',
  )
}
