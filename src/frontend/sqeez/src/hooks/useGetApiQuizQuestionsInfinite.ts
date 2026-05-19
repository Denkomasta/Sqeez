import {
  useInfiniteQuery,
  type InfiniteData,
  type QueryKey,
  type UseInfiniteQueryOptions,
} from '@tanstack/react-query'

import { getApiQuizzesQuizIdQuestions } from '@/api/generated/endpoints/quizzes/quizzes'
import type {
  GetApiQuizzesQuizIdQuestionsParams,
  PagedResponseOfQuizQuestionDto,
} from '@/api/generated/model'

type CustomQueryOptions = Omit<
  UseInfiniteQueryOptions<
    PagedResponseOfQuizQuestionDto,
    Error,
    InfiniteData<PagedResponseOfQuizQuestionDto>,
    QueryKey,
    number
  >,
  'queryKey' | 'queryFn' | 'initialPageParam' | 'getNextPageParam'
>

export const getApiQuizQuestionsInfiniteQueryKey = (
  quizId: number | string,
  params?: GetApiQuizzesQuizIdQuestionsParams,
) => ['quizQuestions', 'infinite', quizId, params] as const

export const useGetApiQuizQuestionsInfinite = (
  quizId: number | string,
  params?: GetApiQuizzesQuizIdQuestionsParams,
  options?: CustomQueryOptions,
) => {
  const pageSize = Number(params?.PageSize) || 25

  return useInfiniteQuery({
    queryKey: getApiQuizQuestionsInfiniteQueryKey(quizId, params),
    initialPageParam: 1,
    queryFn: async ({ pageParam, signal }) => {
      return getApiQuizzesQuizIdQuestions(
        quizId,
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
      const currentPage = Number(lastPage.pageNumber ?? allPages.length)
      const totalPages = Number(lastPage.totalPages)

      if (Number.isFinite(totalPages) && totalPages > 0) {
        return currentPage < totalPages ? currentPage + 1 : undefined
      }

      const currentItems = lastPage.data || []

      if (currentItems.length < pageSize) {
        return undefined
      }

      return allPages.length + 1
    },
    ...options,
  })
}
