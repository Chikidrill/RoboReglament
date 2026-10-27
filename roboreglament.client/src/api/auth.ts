
import type {
  AuthUser,
  LoginRequest,
  RegisterRequest,
} from '@/types/auth'

import { apiRequest } from '@/api/http'

export { ApiError } from '@/api/http'

const API_URL = '/api/auth'

export async function login(
  request: LoginRequest,
): Promise<void> {
  await apiRequest<void>(`${API_URL}/login`, {
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
  await apiRequest<void>(`${API_URL}/register`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  })
}

export async function getCurrentUser(): Promise<AuthUser> {
  return apiRequest<AuthUser>(`${API_URL}/me`)
}

export async function logout(): Promise<void> {
  await apiRequest<void>(`${API_URL}/logout`, {
    method: 'POST',
  })
}
