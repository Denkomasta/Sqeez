import { createFileRoute, Outlet, redirect } from '@tanstack/react-router'
import { queryClient } from '@/main'
import { getGetApiAuthMeQueryOptions } from '@/api/generated/endpoints/auth/auth'

/** Authenticated route shell; redirects anonymous users to login with a return URL. */
export const Route = createFileRoute('/app/_authenticated')({
  beforeLoad: async ({ location }) => {
    try {
      await queryClient.ensureQueryData({
        ...getGetApiAuthMeQueryOptions(),
        staleTime: 1000 * 60 * 5,
        retry: false,
      })
    } catch {
      throw redirect({
        to: '/login',
        search: { redirect: location.href },
      })
    }
  },
  component: () => <Outlet />,
})
