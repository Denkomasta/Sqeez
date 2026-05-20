import {
  useInfiniteQuery,
  type InfiniteData,
  type QueryKey,
  type UseInfiniteQueryOptions,
} from '@tanstack/react-query'

import { getApiUsers } from '@/api/generated/endpoints/user/user'
import type {
  GetApiUsersParams,
  PagedResponseOfStudentDto,
} from '@/api/generated/model'

type CustomQueryOptions = Omit<
  UseInfiniteQueryOptions<
    PagedResponseOfStudentDto,
    Error,
    InfiniteData<PagedResponseOfStudentDto>,
    QueryKey,
    number
  >,
  'queryKey' | 'queryFn' | 'initialPageParam' | 'getNextPageParam'
>

/**
 * Infinite-query wrapper for users.
 * It advances pages by item count, so callers must keep PageSize stable per query.
 *
 * @param params - Backend user filters and paging options; `PageSize` defaults to 20 when omitted.
 * @param options - React Query options merged into the infinite query.
 */
export const useGetApiUsersInfinite = (
  params?: GetApiUsersParams,
  options?: CustomQueryOptions,
) => {
  const pageSize = Number(params?.PageSize) || 20

  return useInfiniteQuery({
    queryKey: ['users', 'infinite', params],
    initialPageParam: 1,
    queryFn: async ({ pageParam, signal }) => {
      return getApiUsers(
        {
          ...params,
          PageNumber: pageParam,
          PageSize: pageSize,
        },
        undefined,
        signal,
      )
    },
    getNextPageParam: (lastPage, allPages) => {
      const currentItems = lastPage?.data || []

      if (currentItems.length < pageSize) {
        return undefined
      }

      return allPages.length + 1
    },
    ...options,
  })
}
