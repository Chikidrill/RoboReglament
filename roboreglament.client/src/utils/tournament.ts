
import type { Tournament } from '@/types/tournament'

export type StatusTone = 'blue' | 'green' | 'yellow' | 'gray'

export interface TournamentStatusInfo {
  label: string
  tone: StatusTone
}

function parseDateOnly(value: string): Date {
  const [year, month, day] = value.split('-').map(Number)

  return new Date(year!, month! - 1, day!)
}

export function formatTournamentDate(value: string): string {
  return new Intl.DateTimeFormat('ru-RU', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  }).format(parseDateOnly(value))
}

export function formatTournamentDateRange(
  start: string,
  end: string,
): string {
  if (start === end) {
    return formatTournamentDate(start)
  }

  const startDate = parseDateOnly(start)
  const endDate = parseDateOnly(end)

  if (
    startDate.getFullYear() === endDate.getFullYear() &&
    startDate.getMonth() === endDate.getMonth()
  ) {
    const monthYear = new Intl.DateTimeFormat('ru-RU', {
      month: 'long',
      year: 'numeric',
    }).format(startDate)

    return `${startDate.getDate()}–${endDate.getDate()} ${monthYear}`
  }

  return (
    `${formatTournamentDate(start)} — ` +
    formatTournamentDate(end)
  )
}

export function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat('ru-RU', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

export function getTournamentStatus(
  tournament: Tournament,
): TournamentStatusInfo {
  switch (tournament.status) {
    case 'Draft':
      return {
        label: 'Планируется',
        tone: 'gray',
      }

    case 'Registration': {
      if (tournament.registrationDeadline) {
        const deadline = Date.parse(
          tournament.registrationDeadline,
        )

        if (
          Number.isFinite(deadline) &&
          deadline < Date.now()
        ) {
          return {
            label: 'Приём заявок завершён',
            tone: 'yellow',
          }
        }
      }

      return {
        label: 'Регистрация открыта',
        tone: 'blue',
      }
    }

    case 'InProgress':
      return {
        label: 'Идёт турнир',
        tone: 'green',
      }

    case 'Completed':
      return {
        label: 'Завершён',
        tone: 'gray',
      }

    default:
      return {
        label: 'Статус неизвестен',
        tone: 'gray',
      }
  }
}
