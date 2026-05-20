import { useState } from 'react'
import { useNavigate, useRouter } from '@tanstack/react-router'
import { useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '@/store/useAuthStore'
import { postApiAuthLogout } from '@/api/generated/endpoints/auth/auth'

/**
 * Logs the user out locally even if the server logout request fails.
 * This keeps stale authenticated UI from surviving a broken network request.
 *
 * @returns Logout handler and pending state for buttons/menus.
 */
export function useLogout() {
  const navigate = useNavigate()
  const router = useRouter()
  const queryClient = useQueryClient()
  const clearAuthStore = useAuthStore((s) => s.logout)

  const [isPending, setIsPending] = useState(false)

  const performLogout = async () => {
    setIsPending(true)

    try {
      await postApiAuthLogout()
    } catch (error) {
      console.error('Server logout failed', error)
    } finally {
      clearAuthStore()
      queryClient.clear()
      await router.invalidate()

      setIsPending(false)
      navigate({ to: '/', replace: true })
    }
  }

  return { performLogout, isPending }
}
