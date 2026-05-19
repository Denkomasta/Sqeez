import { redirect, type ParsedLocation } from '@tanstack/react-router'
import { queryClient } from '@/main'
import { getGetApiAuthMeQueryOptions } from '@/api/generated/endpoints/auth/auth'

interface GuardArgs {
  location: ParsedLocation
}

/**
 * Creates a TanStack Router guard that requires an authenticated user role.
 * Unauthenticated users are sent to login with a redirect back to the target URL.
 */
export function createRoleGuard(allowedRoles: string[]) {
  return async ({ location }: GuardArgs) => {
    let user
    try {
      user = await queryClient.ensureQueryData({
        ...getGetApiAuthMeQueryOptions(),
        staleTime: 1000 * 60 * 5,
        retry: false,
      })
    } catch {
      throw redirect({ to: '/login', search: { redirect: location.href } })
    }

    if (!allowedRoles.includes(user.role)) {
      throw redirect({ to: '/app' })
    }

    return { user }
  }
}

export const teacherRouteGuard = createRoleGuard(['Teacher', 'Admin'])
export const adminRouteGuard = createRoleGuard(['Admin'])
