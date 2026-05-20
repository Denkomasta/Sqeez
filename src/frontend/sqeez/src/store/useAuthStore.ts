import { create } from 'zustand'
import { type UserDTO } from '@/api/generated/model'

interface AuthState {
  /** Current user returned by `/auth/me`; null means the frontend treats the session as anonymous. */
  user: UserDTO | null
  /** Convenience flag derived from `user`. */
  isAuthenticated: boolean
  /** True only for admin users. */
  isAdmin: boolean
  /** True for teachers and admins because admins can access teacher tooling. */
  isTeacher: boolean
  /** Stores a freshly loaded user and recomputes role flags. */
  setUser: (user: UserDTO | null) => void
  /** Clears local auth state after logout or refresh failure. */
  logout: () => void
}

/**
 * Stores the current authenticated user and derived role flags.
 * The user object is refreshed from `/auth/me`; cookies remain the server source of truth,
 * so this store should never be used as proof that the server session still exists.
 */
export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  isAdmin: false,
  isTeacher: false,
  setUser: (user) =>
    set({
      user,
      isAuthenticated: !!user,
      isAdmin: user?.role === 'Admin',
      isTeacher: user?.role === 'Teacher' || user?.role === 'Admin',
    }),
  logout: () =>
    set({
      user: null,
      isAuthenticated: false,
      isAdmin: false,
      isTeacher: false,
    }),
}))
