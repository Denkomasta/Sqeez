import { useState } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { keepPreviousData } from '@tanstack/react-query'
import { FileText } from 'lucide-react'
import { useGetApiQuizzes } from '@/api/generated/endpoints/quizzes/quizzes'
import { QuizListView } from './-/QuizListView'
import { useAuthStore } from '@/store/useAuthStore'

export const Route = createFileRoute('/app/_authenticated/quizzes/')({
  component: AllQuizzesPage,
})

/**
 * Student global quiz list route.
 * Owns search, active-only filtering, and pagination state.
 */
function AllQuizzesPage() {
  const { t } = useTranslation()
  const { user } = useAuthStore()

  const [searchQuery, setSearchQuery] = useState('')
  const [pageNumber, setPageNumber] = useState(1)
  const [showActiveOnly, setShowActiveOnly] = useState(true)
  const PAGE_SIZE = 12

  const {
    data: pagedResponse,
    isLoading,
    isFetching,
  } = useGetApiQuizzes(
    {
      SearchTerm: searchQuery,
      StudentId: user?.id,
      IsActive: showActiveOnly,
      PageNumber: pageNumber,
      PageSize: PAGE_SIZE,
    },
    {
      query: {
        placeholderData: keepPreviousData,
      },
    },
  )

  const quizzes = pagedResponse?.data || []
  const totalQuizzes = Number(pagedResponse?.totalCount || 0)
  const totalPages = Number(
    pagedResponse?.totalPages || Math.ceil(totalQuizzes / PAGE_SIZE),
  )

  const titleNode = (
    <>
      <FileText className="h-8 w-8 text-primary" />
      {t('quiz.allQuizzes')}
    </>
  )

  return (
    <QuizListView
      titleNode={titleNode}
      quizzes={quizzes}
      totalQuizzes={totalQuizzes}
      totalPages={totalPages}
      isLoading={isLoading && !pagedResponse}
      isFetching={isFetching}
      searchQuery={searchQuery}
      setSearchQuery={setSearchQuery}
      pageNumber={pageNumber}
      setPageNumber={setPageNumber}
      emptyStateMessage={t('quiz.noGlobalQuizzes')}
      showActiveOnly={showActiveOnly}
      setShowActiveOnly={setShowActiveOnly}
    />
  )
}
