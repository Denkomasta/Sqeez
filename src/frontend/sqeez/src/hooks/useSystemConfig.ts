import { useGetApiSystemConfig } from '@/api/generated/endpoints/system-config/system-config'

/**
 * Reads global runtime limits and settings.
 * Config is cached for several minutes because it changes rarely during a session.
 *
 * @returns Normalized config query shape used by upload limits and feature flags.
 */
export function useSystemConfig() {
  const query = useGetApiSystemConfig({
    query: {
      staleTime: 1000 * 60 * 5,
      gcTime: 1000 * 60 * 60 * 24,
      refetchOnWindowFocus: false,
    },
  })

  return {
    config: query.data,
    isLoading: query.isLoading,
    isError: query.isError,
  }
}
