import {
  useInfiniteQuery,
  type InfiniteData,
  type QueryKey,
  type UseInfiniteQueryOptions,
} from '@tanstack/react-query'

import { getApiClasses } from '@/api/generated/endpoints/school-classes/school-classes'
import type {
  GetApiClassesParams,
  PagedResponseOfSchoolClassDto,
} from '@/api/generated/model'

type CustomQueryOptions = Omit<
  UseInfiniteQueryOptions<
    PagedResponseOfSchoolClassDto,
    Error,
    InfiniteData<PagedResponseOfSchoolClassDto>,
    QueryKey,
    number
  >,
  'queryKey' | 'queryFn' | 'initialPageParam' | 'getNextPageParam'
>

/**
 * Infinite-query wrapper for school classes.
 * It stops when a page returns fewer records than the requested page size.
 *
 * @param params - Backend class filters and paging options; `PageSize` defaults to 20 when omitted.
 * @param options - React Query options merged into the infinite query.
 */
export const useGetApiClassesInfinite = (
  params?: GetApiClassesParams,
  options?: CustomQueryOptions,
) => {
  const pageSize = Number(params?.PageSize) || 20

  return useInfiniteQuery({
    queryKey: ['classes', 'infinite', params],
    initialPageParam: 1,
    queryFn: async ({ pageParam, signal }) => {
      return getApiClasses(
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
