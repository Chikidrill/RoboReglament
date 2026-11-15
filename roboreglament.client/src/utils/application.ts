
import type { TournamentStatus } from '@/types/tournament'
import type { ApplicationStatus } from '@/types/application'

const statusLabels: Record<ApplicationStatus, string> = {
  Draft: 'Черновик',
  Pending: 'На рассмотрении',
  Approved: 'Одобрена',
  Rejected: 'Отклонена',
  Withdrawn: 'Отозвана',
}

export function getApplicationStatusLabel(
  status: ApplicationStatus,
): string {
  return statusLabels[status] ?? 'Неизвестный статус'
}

function parseUtcDate(value: string): Date {
  const hasTimeZone = /(?:Z|[+-]\d{2}:\d{2})$/i.test(value)

  return new Date(hasTimeZone ? value : `${value}Z`)
}

export function formatApplicationDate(
  value: string | null,
): string {
  if (!value) return 'Не отправлена'

  const date = parseUtcDate(value)

  if (!Number.isFinite(date.getTime())) {
    return 'Дата не указана'
  }

  return new Intl.DateTimeFormat('ru-RU', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(date)
}

export function isRegistrationOpen(
  status: TournamentStatus,
  deadline?: string | null,
): boolean {
  if (status !== 'Registration') return false
  if (!deadline) return true

  const date = parseUtcDate(deadline).getTime()

  return Number.isFinite(date) && Date.now() <= date
}
