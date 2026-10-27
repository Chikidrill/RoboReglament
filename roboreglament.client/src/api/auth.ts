
import type {
  AuthUser,
  LoginRequest,
  RegisterRequest,
} from '@/types/auth'

const API_URL = '/api/auth'

export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

async function sendRequest<T>(
  url: string,
  options: RequestInit = {},
): Promise<T> {
  const response = await fetch(url, {
    credentials: 'same-origin',
    ...options,
  })

  const text = await response.text()
  let data: unknown = null

  if (text) {
    try {
      data = JSON.parse(text)
    } catch {
      throw new Error(
        'Сервер вернул некорректный ответ. Проверьте настройки API.',
      )
    }
  }

  if (!response.ok) {
    let message = `Ошибка сервера: ${response.status}`

    if (data && typeof data === 'object') {
      if ('message' in data && typeof data.message === 'string') {
        message = data.message
      } else if (
        'errors' in data &&
        Array.isArray(data.errors)
      ) {
        message = data.errors.join(', ')
      }
    }

    throw new ApiError(message, response.status)
  }

  return data as T
}

export async function login(
  request: LoginRequest,
): Promise<void> {
  await sendRequest('/api/auth/login', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  })
}

export async function register(
  request: RegisterRequest,
): Promise<void> {
  await sendRequest('/api/auth/register', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  })
}

export async function getCurrentUser(): Promise<AuthUser> {
  return sendRequest<AuthUser>(`${API_URL}/me`)
}

export async function logout(): Promise<void> {
  await sendRequest<void>(`${API_URL}/logout`, {
    method: 'POST',
  })
}
