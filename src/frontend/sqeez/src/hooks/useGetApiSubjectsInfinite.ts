import {
  useInfiniteQuery,
  type InfiniteData,
  type QueryKey,
  type UseInfiniteQueryOptions,
} from '@tanstack/react-query'
import { getApiSubjects } from '@/api/generated/endpoints/subjects/subjects'
import type {
  GetApiSubjectsParams,
  PagedResponseOfSubjectDto,
} from '@/api/generated/model'

type CustomQueryOptions = Omit<
  UseInfiniteQueryOptions<
    PagedResponseOfSubjectDto, // 1. TQueryFnData: What the API returns
    Error, // 2. TError: Type of error
    InfiniteData<PagedResponseOfSubjectDto>, // 3. TData: What the hook returns (InfiniteData wrapper)
    QueryKey, // 4. TQueryKey: Type of the query key
    number // 5. TPageParam: Type of the page parameter
  >,
  'queryKey' | 'queryFn' | 'initialPageParam' | 'getNextPageParam'
>

/**
 * Infinite-query wrapper for subjects.
 * Filters are part of the query key, so changing params starts a separate cache.
 * Intended for selectors and admin flows that should not load every subject at once.
 *
 * @param params - Backend subject filters and paging options; `PageSize` defaults to 20 when omitted.
 * @param options - React Query options merged into the infinite query.
 */
export const useGetApiSubjectsInfinite = (
  params?: GetApiSubjectsParams,
  options?: CustomQueryOptions,
) => {
  const pageSize = Number(params?.PageSize) || 20

  return useInfiniteQuery({
    queryKey: ['subjects', 'infinite', params],
    initialPageParam: 1,
    queryFn: async ({ pageParam, signal }) => {
      return getApiSubjects(
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
