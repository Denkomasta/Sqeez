import { Link } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { ArrowLeft, History, FileText } from 'lucide-react'

import { Button } from '@/components/ui/Button'
import { PageLayout } from '@/components/layouting/PageLayout/PageLayout'

import { useAuthStore } from '@/store/useAuthStore'
import { useGetApiQuizzesQuizId } from '@/api/generated/endpoints/quizzes/quizzes'
import { useGetApiQuizAttemptsQuizQuizId } from '@/api/generated/endpoints/quiz-attempts/quiz-attempts'
import { AttemptsTable, type AttemptRowDto } from './AttemptsTable'
import { useState } from 'react'
import { Pagination } from '@/components/ui/Pagination'
import { isQuizActive } from '@/lib/quizHelpers'
import { useGetApiSubjectsId } from '@/api/generated/endpoints/subjects/subjects'

/**
 * Student attempt history page for one quiz.
 * Attempt links are disabled while the quiz is still active.
 */
export function QuizAttemptsPage({ quizId }: { quizId: string }) {
  const { t } = useTranslation()
  const { user, isTeacher } = useAuthStore()

  const [pageNumber, setPageNumber] = useState(1)
  const PAGE_SIZE = 15

  const { data: quiz, isLoading: isQuizLoading } = useGetApiQuizzesQuizId(
    Number(quizId),
    { studentId: user?.id },
    { query: { enabled: !!quizId } },
  )

  const { data: pastAttempts, isLoading: isAttemptsLoading } =
    useGetApiQuizAttemptsQuizQuizId(
      Number(quizId),
      { pageNumber, pageSize: PAGE_SIZE },
      { query: { enabled: !!quizId && !!user?.id } },
    )

  const { data: subject, isLoading: isSubjectLoading } = useGetApiSubjectsId(
    Number(quiz?.subjectId),
    { query: { enabled: !!quiz?.subjectId && isTeacher } },
  )

  const isLoading = isQuizLoading || isAttemptsLoading || isSubjectLoading

  const tableData: AttemptRowDto[] = (pastAttempts?.data || []).map(
    (attempt) => ({
      id: attempt.id,
      quizId: attempt.quizId,
      quizTitle: quiz?.title,
      status: String(attempt.status),
      totalScore: Number(attempt.totalScore || 0),
      studentName: attempt.studentName || '',
      studentId: attempt.studentId || undefined,
      startTime: attempt.startTime || new Date().toISOString(),
    }),
  )
  const totalPages = Number(pastAttempts?.totalPages || 1)
  const isActive = isQuizActive({
    publishDate: quiz?.publishDate,
    closingDate: quiz?.closingDate,
  })
  const isTeacherView = isTeacher && subject?.teacherId === user?.id

  return (
    <PageLayout
      containerClassName="max-w-5xl"
      isLoading={isLoading}
      title={
        <>
          <History className="size-8 text-primary" />
          {t('attempts.myResults')}
        </>
      }
      subtitle={
        <span className="flex items-center gap-2">
          <FileText className="size-4" />
          {quiz?.title}
        </span>
      }
      headerControls={
        <Button
          variant="ghost"
          size="sm"
          asChild
          className="-ml-3 w-fit text-muted-foreground hover:text-foreground"
        >
          <Link to="/app/quizzes/$quizId" params={{ quizId }}>
            <ArrowLeft className="mr-2 size-4" />
            {t('common.backToQuiz')}
          </Link>
        </Button>
      }
    >
      <div className="space-y-4">
        <AttemptsTable
          attempts={tableData}
          isTeacherView={isTeacherView}
          isLoading={isAttemptsLoading}
          isQuizActive={isActive}
        />
      </div>

      {!isLoading && totalPages > 1 && (
        <div className="mt-6 flex justify-center">
          <Pagination
            currentPage={pageNumber}
            totalPages={totalPages}
            onPageChange={setPageNumber}
          />
        </div>
      )}
    </PageLayout>
  )
}
