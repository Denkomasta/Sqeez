import { createFileRoute, Link, useNavigate } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import {
  FileText,
  Clock,
  AlertCircle,
  History,
  PlayCircle,
  CheckCircle,
  BookOpen,
  Info,
  ArrowLeft,
  Calendar,
} from 'lucide-react'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  CardFooter,
} from '@/components/ui/Card'
import { Badge } from '@/components/ui/Badge/Badge'
import { Button } from '@/components/ui/Button'
import { PageLayout } from '@/components/layouting/PageLayout/PageLayout'
import { useGetApiQuizzesQuizId } from '@/api/generated/endpoints/quizzes/quizzes'
import { useGetApiSubjectsId } from '@/api/generated/endpoints/subjects/subjects'
import { useAuthStore } from '@/store/useAuthStore'
import { formatDateTime } from '@/lib/dateHelpers'
import { useQuizStore } from '@/store/useQuizStore'
import { isQuizActive } from '@/lib/quizHelpers'

export const Route = createFileRoute('/app/_authenticated/quizzes/$quizId/')({
  component: QuizDetailsPage,
})

function QuizDetailsPage() {
  const { t } = useTranslation()
  const { quizId } = Route.useParams()
  const navigate = useNavigate()
  const { user } = useAuthStore()
  const { actions } = useQuizStore()

  const { data: quiz, isLoading } = useGetApiQuizzesQuizId(
    Number(quizId),
    { studentId: user?.id },
    {
      query: { enabled: !!quizId },
    },
  )

  const subjectId = quiz?.subjectId

  const { data: subject, isLoading: isLoadingSubject } = useGetApiSubjectsId(
    subjectId!,
    {
      query: { enabled: !!subjectId },
    },
  )

  if (!quiz) {
    return (
      <PageLayout
        isLoading={isLoading || isLoadingSubject}
        containerClassName="flex min-h-[60vh] max-w-5xl items-center justify-center"
      >
        <Card className="w-full max-w-md border-2 border-dashed text-center shadow-sm">
          <CardHeader>
            <div className="mx-auto mb-4 flex size-16 items-center justify-center rounded-full bg-secondary">
              <FileText className="h-8 w-8 text-muted-foreground" />
            </div>
            <CardTitle className="text-2xl">{t('common.noResults')}</CardTitle>
            <CardDescription className="mt-2 text-base">
              {t('quiz.notFoundDesc')}
            </CardDescription>
            <div className="mt-6">
              <Button
                variant="outline"
                onClick={() => navigate({ to: '/app/quizzes' })}
              >
                {t('common.goBack')}
              </Button>
            </div>
          </CardHeader>
        </Card>
      </PageLayout>
    )
  }

  const attempts = Number(quiz.quizAttempts || 0)
  const hasAttempts = attempts > 0
  const retries = Number(quiz.maxRetries)
  const maxRetriesReached = retries > 0 && attempts >= retries
  const canTakeQuiz =
    !maxRetriesReached &&
    isQuizActive({
      publishDate: quiz.publishDate,
      closingDate: quiz.closingDate,
    })

  const isPublished =
    quiz.publishDate && new Date(quiz.publishDate) <= new Date()

  return (
    <PageLayout
      containerClassName="max-w-5xl"
      isLoading={isLoading || isLoadingSubject}
      title={
        <>
          <FileText className="size-8 text-primary" />
          {quiz.title}
        </>
      }
      titleBadge={
        <Badge
          variant={hasAttempts ? 'default' : 'secondary'}
          className="px-3 py-1 text-xs tracking-wider uppercase"
        >
          {hasAttempts ? t('quiz.attempted') : t('quiz.notStarted')}
        </Badge>
      }
      subtitle={
        quiz.closingDate ? (
          <span className="flex items-center gap-1.5 font-medium text-destructive">
            <Clock className="size-4" />
            {t('quiz.due')}: {formatDateTime(quiz.closingDate)}
          </span>
        ) : undefined
      }
      headerControls={
        <Button
          variant="ghost"
          size="sm"
          onClick={() => history.back()}
          className="-ml-3 w-fit text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="mr-2 size-4" />
          {t('common.back')}
        </Button>
      }
    >
      <div className="grid grid-cols-1 gap-8 lg:grid-cols-3">
        <div className="space-y-8 lg:col-span-2">
          <Card className="border-none shadow-md">
            <CardHeader className="border-b border-border bg-muted/50 pb-4">
              <CardTitle className="flex items-center gap-2 text-xl">
                <Info className="size-5 text-primary" />
                {t('quiz.instructions')}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="prose-sm dark:prose-invert prose max-w-none text-base leading-relaxed text-muted-foreground">
                {quiz.description ? (
                  <p className="whitespace-pre-wrap">{quiz.description}</p>
                ) : (
                  <p className="italic">{t('quiz.noDescription')}</p>
                )}
              </div>
            </CardContent>
          </Card>

          {subject && (
            <Card className="overflow-hidden border-border bg-card shadow-sm">
              <CardContent className="flex flex-col gap-4 p-6 sm:flex-row sm:items-center sm:justify-between">
                <div className="space-y-1">
                  <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                    <BookOpen className="size-4" />
                    {t('subject.relatedSubject')}
                  </div>
                  <h3 className="text-lg font-semibold text-foreground">
                    {subject.name}
                  </h3>
                  {subject.code && (
                    <p className="text-sm text-muted-foreground">
                      {subject.code}
                    </p>
                  )}
                </div>
                <Button
                  variant="outline"
                  asChild
                  className="shrink-0 bg-background"
                >
                  <Link
                    to="/app/subjects/$subjectId"
                    params={{ subjectId: subject.id.toString() }}
                  >
                    {t('subject.viewSubject')}
                  </Link>
                </Button>
              </CardContent>
            </Card>
          )}
        </div>

        <div className="lg:col-span-1">
          <div className="sticky top-6 space-y-6">
            <Card className="border-2 shadow-lg">
              <CardHeader className="border-b border-border bg-muted/50 pb-4">
                <CardTitle className="text-lg">
                  {t('quiz.quizDetails')}
                </CardTitle>
              </CardHeader>

              <CardContent className="space-y-5">
                <div className="flex items-center gap-3">
                  <div className="flex size-10 items-center justify-center rounded-full bg-secondary">
                    <AlertCircle className="size-5 text-muted-foreground" />
                  </div>
                  <div>
                    <p className="text-sm font-medium text-muted-foreground">
                      {t('quiz.questions')}
                    </p>
                    <p className="text-base font-semibold">
                      {quiz.quizQuestions}
                    </p>
                  </div>
                </div>

                <div className="flex items-center gap-3">
                  <div className="flex size-10 items-center justify-center rounded-full bg-secondary">
                    <History className="size-5 text-muted-foreground" />
                  </div>
                  <div>
                    <p className="text-sm font-medium text-muted-foreground">
                      {t('quiz.attempts')}
                    </p>
                    <p className="text-base font-semibold">
                      {attempts} {retries > 0 ? `/ ${retries}` : ''}
                    </p>
                  </div>
                </div>

                <div className="flex items-center gap-3">
                  <div className="flex size-10 items-center justify-center rounded-full bg-secondary">
                    <Calendar className="size-5 text-muted-foreground" />
                  </div>
                  <div>
                    <p className="text-sm font-medium text-muted-foreground">
                      {t('quiz.publishDate')}
                    </p>
                    <p className="text-base font-semibold">
                      {formatDateTime(quiz.publishDate) || t('quiz.notSet')}
                    </p>
                  </div>
                </div>
              </CardContent>

              <CardFooter className="flex-col gap-3 border-t border-border bg-muted/40 pt-4">
                {!canTakeQuiz ? (
                  <Button className="w-full" size="lg" asChild>
                    <Link
                      to="/app/quizzes/$quizId/attempts"
                      params={{ quizId: quizId.toString() }}
                    >
                      <CheckCircle className="mr-2 size-5" />
                      {t('quiz.viewResults')}
                    </Link>
                  </Button>
                ) : isPublished ? (
                  <Button className="w-full text-base" size="lg" asChild>
                    <Link
                      to="/app/quizzes/$quizId/play"
                      params={{ quizId: quizId.toString() }}
                      search={{ attemptId: undefined }}
                      onClick={() => {
                        actions.resetQuiz()
                      }}
                    >
                      <PlayCircle className="mr-2 size-5" />
                      {hasAttempts ? t('quiz.retakeQuiz') : t('quiz.startQuiz')}
                    </Link>
                  </Button>
                ) : (
                  <Button className="w-full text-base" size="lg" disabled>
                    <PlayCircle className="mr-2 size-5" />
                    {hasAttempts ? t('quiz.retakeQuiz') : t('quiz.startQuiz')}
                  </Button>
                )}

                {hasAttempts && (
                  <Button
                    className="w-full"
                    variant="outline"
                    size="lg"
                    asChild
                  >
                    <Link
                      to="/app/quizzes/$quizId/results"
                      params={{ quizId: quizId.toString() }}
                    >
                      <History className="mr-2 size-5" />
                      {t('quiz.attemptHistory')}
                    </Link>
                  </Button>
                )}
              </CardFooter>
            </Card>
          </div>
        </div>
      </div>
    </PageLayout>
  )
}
