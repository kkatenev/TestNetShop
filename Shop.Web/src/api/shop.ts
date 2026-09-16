import type { ActivityLog, Order, Product } from '../types'
import { clearSession, getToken } from '../auth'

export const apiUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:5007'

export class ApiError extends Error {
  status: number
  body: string

  constructor(status: number, path: string, body = '') {
    super(body || `API ${status}: ${path}`)
    this.status = status
    this.body = body
  }
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const token = getToken()
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options?.headers as Record<string, string> | undefined),
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  const response = await fetch(`${apiUrl}${path}`, {
    ...options,
    headers,
  })

  if (response.status === 401) {
    clearSession()
    throw new ApiError(401, path)
  }

  if (!response.ok) {
    const body = await response.text()
    throw new ApiError(response.status, path, body.replace(/^"|"$/g, ''))
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}

export type LoginResult = {
  accessToken: string
  userName: string
  expiresAt: string
}

export function login(userName: string, password: string) {
  return request<LoginResult>('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ userName, password }),
  })
}

export function register(userName: string, password: string) {
  return request<LoginResult>('/auth/register', {
    method: 'POST',
    body: JSON.stringify({ userName, password }),
  })
}

export function getProducts() {
  return request<Product[]>('/products')
}

export function getOrders() {
  return request<Order[]>('/orders')
}

export function createOrder(items: { productId: number; quantity: number }[]) {
  return request<Order>('/orders', {
    method: 'POST',
    body: JSON.stringify({ items }),
  })
}

export function getActivityLogs() {
  return request<ActivityLog[]>('/activity-logs')
}
