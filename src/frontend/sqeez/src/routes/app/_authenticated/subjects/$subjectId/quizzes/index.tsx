import { useState } from 'react'
import { createFileRoute, Link } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { ArrowLeft, FileText } from 'lucide-react'
import {
  useGetApiSubjectsId,
  useGetApiSubjectsSubjectIdQuizzes,
} from '@/api/generated/endpoints/subjects/subjects'
import { keepPreviousData } from '@tanstack/react-query'
import { QuizListView } from '../../../quizzes/-/QuizListView'
import { Button } from '@/components/ui'
import { Badge } from '@/components/ui/Badge'
import { useAuthStore } from '@/store/useAuthStore'

export const Route = createFileRoute(
  '/app/_authenticated/subjects/$subjectId/quizzes/',
)({
  component: SubjectQuizzesPage,
})

/**
 * Student subject-scoped quiz list route.
 * Combines subject metadata with paginated active quiz filtering.
 */
function SubjectQuizzesPage() {
  const { t } = useTranslation()
  const { subjectId } = Route.useParams()
  const { user } = useAuthStore()

  const [searchQuery, setSearchQuery] = useState('')
  const [pageNumber, setPageNumber] = useState(1)
  const [showActiveOnly, setShowActiveOnly] = useState(true)
  const PAGE_SIZE = 12

  const { data: subjectData, isLoading: isSubjectLoading } =
    useGetApiSubjectsId(Number(subjectId), {
      query: { enabled: !!subjectId },
    })

  const {
    data: pagedResponse,
    isLoading: isQuizzesLoading,
    isFetching,
  } = useGetApiSubjectsSubjectIdQuizzes(
    Number(subjectId),
    {
      SearchTerm: searchQuery,
      StudentId: user?.id,
      IsActive: showActiveOnly,
      PageNumber: pageNumber,
      PageSize: PAGE_SIZE,
    },
    {
      query: {
        enabled: !!subjectId,
        placeholderData: keepPreviousData,
      },
    },
  )

  const quizzes = pagedResponse?.data || []
  const totalQuizzes = Number(pagedResponse?.totalCount || 0)
  const totalPages = Number(
    pagedResponse?.totalPages || Math.ceil(totalQuizzes / PAGE_SIZE),
  )
  return (
    <QuizListView
      backButtonNode={
        <Button variant="ghost" size="sm" asChild className="-ml-3">
          <Link to="/app/subjects/$subjectId" params={{ subjectId }}>
            <ArrowLeft className="mr-2 size-4" />
            {t('subject.backToSubject')}
          </Link>
        </Button>
      }
      titleNode={
        <>
          <FileText className="size-8 text-primary" />
          <span>
            {subjectData?.name}
            <span className="ml-2 font-normal text-muted-foreground">
              | {t('quiz.subjectQuizzes')}
            </span>
          </span>
          {subjectData?.code && (
            <Badge variant="secondary" className="mb-1 ml-2 text-sm">
              {subjectData.code}
            </Badge>
          )}
        </>
      }
      quizzes={quizzes}
      totalQuizzes={totalQuizzes}
      totalPages={totalPages}
      isLoading={(isQuizzesLoading || isSubjectLoading) && !pagedResponse}
      isFetching={isFetching}
      searchQuery={searchQuery}
      setSearchQuery={setSearchQuery}
      pageNumber={pageNumber}
      setPageNumber={setPageNumber}
      subject={subjectData}
      emptyStateMessage={t('quiz.noGlobalQuizzes')}
      showActiveToggle={true}
      showActiveOnly={showActiveOnly}
      setShowActiveOnly={setShowActiveOnly}
    />
  )
}
