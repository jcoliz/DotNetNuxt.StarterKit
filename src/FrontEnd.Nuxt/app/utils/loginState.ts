import type { RouteLocationRaw } from 'vue-router'

export interface LoginStateModel {
  isLoggedIn: boolean
  name?: string
  photo?: string
  profileRoute: RouteLocationRaw
  onLogin: () => void
  onLogout: () => void
}
