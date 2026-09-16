const tokenKey = 'shop_token'
const userKey = 'shop_user'

export function getToken() {
  return localStorage.getItem(tokenKey)
}

export function getUserName() {
  return localStorage.getItem(userKey)
}

export function setSession(token: string, userName: string) {
  localStorage.setItem(tokenKey, token)
  localStorage.setItem(userKey, userName)
}

export function clearSession() {
  localStorage.removeItem(tokenKey)
  localStorage.removeItem(userKey)
}

export function isLoggedIn() {
  return Boolean(getToken())
}
