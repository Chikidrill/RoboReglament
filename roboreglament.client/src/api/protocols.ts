
import { apiRequest } from '@/api/http'

import type {
  ProtocolTemplate,
  SaveProtocolTemplateRequest,
  PreviewProtocolResponse,
  PreviewScoreRequest,
} from '@/types/protocol'

function url(
  tournamentId: number,
  disciplineId: number,
): string {
  return (
    `/api/tournaments/${tournamentId}` +
    `/disciplines/${disciplineId}` +
    '/protocol-templates'
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

export function getProtocolTemplates(
  tournamentId: number,
  disciplineId: number,
  signal?: AbortSignal,
): Promise<ProtocolTemplate[]> {
  return apiRequest<ProtocolTemplate[]>(
    url(tournamentId, disciplineId),
    { signal },
  )
}

export function getProtocolTemplate(
  tournamentId: number,
  disciplineId: number,
  templateId: number,
  signal?: AbortSignal,
): Promise<ProtocolTemplate> {
  return apiRequest<ProtocolTemplate>(
    `${url(tournamentId, disciplineId)}/${templateId}`,
    { signal },
  )
}

export function createProtocolTemplate(
  tournamentId: number,
  disciplineId: number,
  request: SaveProtocolTemplateRequest,
): Promise<ProtocolTemplate> {
  return apiRequest<ProtocolTemplate>(
    url(tournamentId, disciplineId),
    json('POST', request),
  )
}

export function updateProtocolTemplate(
  tournamentId: number,
  disciplineId: number,
  templateId: number,
  request: SaveProtocolTemplateRequest,
): Promise<ProtocolTemplate> {
  return apiRequest<ProtocolTemplate>(
    `${url(tournamentId, disciplineId)}/${templateId}`,
    json('PUT', request),
  )
}

export function deleteProtocolTemplate(
  tournamentId: number,
  disciplineId: number,
  templateId: number,
): Promise<void> {
  return apiRequest<void>(
    `${url(tournamentId, disciplineId)}/${templateId}`,
    { method: 'DELETE' },
  )
}

export function previewProtocol(
  tournamentId: number,
  disciplineId: number,
  templateId: number,
  scores: PreviewScoreRequest[],
): Promise<PreviewProtocolResponse> {
  return apiRequest<PreviewProtocolResponse>(
    `${url(tournamentId, disciplineId)}/${templateId}/preview`,
    json('POST', { scores }),
  )
}
