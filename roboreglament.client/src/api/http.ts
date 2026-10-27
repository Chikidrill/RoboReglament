
export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

async function getCsrfToken(): Promise<string> {
  const response = await fetch('/api/security/csrf', {
    method: 'GET',
    credentials: 'same-origin',
    cache: 'no-store',
  })

  if (!response.ok) {
    throw new ApiError(
      'Не удалось получить CSRF-токен.',
      response.status,
    )
  }

  const data: unknown = await response.json()

  if (
    !data ||
    typeof data !== 'object' ||
    !('token' in data) ||
    typeof data.token !== 'string' ||
    !data.token
  ) {
    throw new Error('Сервер вернул некорректный CSRF-токен.')
  }

  return data.token
}

export async function apiRequest<T>(
  url: string,
  options: RequestInit = {},
): Promise<T> {
  const method = (options.method ?? 'GET').toUpperCase()
  const headers = new Headers(options.headers)

  const safeMethods = ['GET', 'HEAD', 'OPTIONS', 'TRACE']

  if (!safeMethods.includes(method)) {
    const csrfToken = await getCsrfToken()
    headers.set('X-CSRF-TOKEN', csrfToken)
  }

  const response = await fetch(url, {
    ...options,
    credentials: 'same-origin',
    headers,
  })

  const responseText = await response.text()
  let data: unknown = null

  if (responseText) {
    try {
      data = JSON.parse(responseText)
    } catch {
      if (response.ok) {
        throw new Error('Сервер вернул некорректный JSON.')
      }
    }
  }

  if (!response.ok) {
    let message = `Ошибка запроса: ${response.status}`

    if (data && typeof data === 'object') {
      if (
        'message' in data &&
        typeof data.message === 'string'
      ) {
        message = data.message
      } else if (
        'errors' in data &&
        Array.isArray(data.errors)
      ) {
        message = data.errors.join(', ')
      } else if (
        'title' in data &&
        typeof data.title === 'string'
      ) {
        message = data.title
      }
    }

    throw new ApiError(message, response.status)
  }

  return data as T
}
