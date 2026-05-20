import { useNavigate, useRouter } from '@tanstack/react-router'
import { queryClient } from '@/main'
import { useAuthStore } from '@/store/useAuthStore'
import { getGetApiAuthMeQueryOptions } from '@/api/generated/endpoints/auth/auth'

/**
 * Finalizes a successful login/register flow.
 * The session is considered valid only after `/auth/me` returns the current user.
 *
 * @returns Async handler that stores the user, invalidates router state, and navigates to the redirect target.
 */
export function useAuthSuccess() {
  const navigate = useNavigate()
  const router = useRouter()
  const setUser = useAuthStore((state) => state.setUser)

  const handleSuccess = async (redirectPath?: string) => {
    try {
      const user = await queryClient.fetchQuery({
        ...getGetApiAuthMeQueryOptions(),
        staleTime: 0,
      })

      setUser(user)

      await router.invalidate()

      navigate({
        to: redirectPath || '/app',
        replace: true,
      })
    } catch (error) {
      console.error('Failed to fetch user session after auth', error)
      setUser(null)
      throw error
    }
  }

  return handleSuccess
}
