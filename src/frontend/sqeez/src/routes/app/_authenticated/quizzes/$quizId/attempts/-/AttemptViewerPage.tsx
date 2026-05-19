import { useTranslation } from 'react-i18next'
import { Clock, CheckCircle, GraduationCap, ArrowLeft } from 'lucide-react'
import { Link } from '@tanstack/react-router'

import { useGetApiQuizAttemptsId } from '@/api/generated/endpoints/quiz-attempts/quiz-attempts'
import { useAuthStore } from '@/store/useAuthStore'
import { Button } from '@/components/ui/Button'
import { Card, CardContent } from '@/components/ui/Card'
import { PageLayout } from '@/components/layouting/PageLayout/PageLayout'
import { QuestionResultCard } from './QuestionResultCard'
import { useGetApiQuizzesQuizId } from '@/api/generated/endpoints/quizzes/quizzes'
import { useGetApiSubjectsId } from '@/api/generated/endpoints/subjects/subjects'
import { getAttemptStatusLabel } from '@/lib/attemptStatusHelpers'

/**
 * Detailed attempt review page for students and teachers.
 * Teacher grading is enabled only when the current teacher owns the subject.
 */
export function AttemptViewerPage({
  attemptId,
  quizId,
}: {
  attemptId: string
  quizId: string
}) {
  const { t } = useTranslation()
  const { user, isTeacher } = useAuthStore()

  const { data: attempt, isLoading: isAttemptLoading } =
    useGetApiQuizAttemptsId(Number(attemptId))

  const { data: quiz, isLoading: isQuizLoading } = useGetApiQuizzesQuizId(
    Number(quizId),
    {},
    { query: { enabled: !!quizId } },
  )

  const { data: subject, isLoading: isSubjectLoading } = useGetApiSubjectsId(
    Number(quiz?.subjectId),
    { query: { enabled: !!quiz?.subjectId } },
  )

  if (!attempt || !quiz || !subject) {
    return (
      <PageLayout
        isLoading={isAttemptLoading || isQuizLoading || isSubjectLoading}
        containerClassName="max-w-4xl text-center text-destructive"
      >
        {t('common.error')}
      </PageLayout>
    )
  }

  const canGrade = isTeacher && subject.teacherId === user?.id
  const startTime = new Date(attempt.startTime || '')
  const endTime = new Date(attempt.endTime || '')
  const timeTakenMins = Math.round(
    (endTime.getTime() - startTime.getTime()) / 60000,
  )

  return (
    <PageLayout
      containerClassName="max-w-4xl"
      isLoading={isAttemptLoading || isQuizLoading || isSubjectLoading}
      title={quiz.title}
      subtitle={t('attempts.review')}
      headerActions={
        <Link to="..">
          <Button variant="ghost" size="icon">
            <ArrowLeft className="h-5 w-5" />
          </Button>
        </Link>
      }
    >
      <Card className="border-primary/20 bg-primary/5 shadow-sm">
        <CardContent className="flex flex-wrap items-center justify-between gap-6 p-6">
          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
              <CheckCircle className="h-6 w-6 text-primary" />
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground">
                {t('attempts.status')}
              </p>
              <p className="text-lg font-bold text-foreground">
                {getAttemptStatusLabel(t, attempt.status)}
              </p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-info/10">
              <Clock className="h-6 w-6 text-info" />
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground">
                {t('attempts.timeTaken')}
              </p>
              <p className="text-lg font-bold text-foreground">
                {timeTakenMins} {t('common.minutes')}
              </p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-success/10">
              <GraduationCap className="h-6 w-6 text-success" />
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground">
                {t('attempts.totalScore')}
              </p>
              <p className="text-lg font-bold text-foreground">
                {attempt.totalScore} {t('common.points')}
                {attempt.mark && ` (Mark: ${attempt.mark})`}
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      <div className="space-y-6 py-4">
        <h2 className="text-xl font-semibold tracking-tight">
          {t('attempts.responses')}
        </h2>

        {attempt.responses.map((response) => (
          <QuestionResultCard
            key={response.id}
            quizId={attempt.quizId}
            attemptId={attemptId}
            studentResponse={response}
            isTeacher={canGrade}
          />
        ))}
      </div>
    </PageLayout>
  )
}
