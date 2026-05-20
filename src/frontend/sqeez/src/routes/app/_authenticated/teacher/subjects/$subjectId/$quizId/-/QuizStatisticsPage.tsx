import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from '@tanstack/react-router'
import {
  ArrowLeft,
  FileSignature,
  Users,
  Target,
  Search,
  Settings,
  Clock,
  TrendingUp,
} from 'lucide-react'

import { PageLayout } from '@/components/layouting/PageLayout/PageLayout'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { Badge } from '@/components/ui/Badge/Badge'
import { Button } from '@/components/ui/Button'
import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { Pagination } from '@/components/ui/Pagination'

import { QuizAttemptsTable } from './QuizAttemptsTable'
import { useGetApiQuizzesQuizId } from '@/api/generated/endpoints/quizzes/quizzes'
import {
  useGetApiQuizzesQuizIdStatisticsQuestions,
  useGetApiQuizzesQuizIdStatisticsSummary,
} from '@/api/generated/endpoints/quiz-statistics/quiz-statistics'
import { useGetApiQuizAttemptsQuizQuizId } from '@/api/generated/endpoints/quiz-attempts/quiz-attempts'
import { QuestionAnalysis } from './QuestionAnalysis'

/**
 * Teacher quiz statistics page.
 * Combines summary, attempt table, and per-question analysis for one quiz.
 */
export function QuizStatisticsPage({
  subjectId,
  quizId,
}: {
  subjectId: string
  quizId: string
}) {
  const { t } = useTranslation()
  const [searchQuery, setSearchQuery] = useState('')
  const [pageNumber, setPageNumber] = useState(1)
  const pageSize = 15

  const { data: quizData, isLoading: isLoadingQuiz } = useGetApiQuizzesQuizId(
    Number(quizId),
  )

  const { data: quizStatisticsData, isLoading: isLoadingStatistics } =
    useGetApiQuizzesQuizIdStatisticsSummary(Number(quizId), {
      query: { enabled: !!quizId },
    })

  const {
    data: attemptsResponse,
    isLoading: isLoadingAttempts,
    isFetching,
  } = useGetApiQuizAttemptsQuizQuizId(
    Number(quizId),
    {
      pageNumber,
      pageSize,
    },
    { query: { enabled: !!quizId } },
  )

  const { data: questionStats } = useGetApiQuizzesQuizIdStatisticsQuestions(
    Number(quizId),
    { query: { enabled: !!quizId } },
  )

  const attempts = attemptsResponse?.data || []
  const totalTableCount = Number(attemptsResponse?.totalCount || 0)
  const totalPages = Number(attemptsResponse?.totalPages || 1)

  const isLoading =
    isLoadingQuiz ||
    isLoadingStatistics ||
    (isLoadingAttempts && !attemptsResponse)

  const statsTotalAttempts = Number(quizStatisticsData?.totalAttempts || 0)
  const statsCompletedAttempts = Number(
    quizStatisticsData?.completedAttempts || 0,
  )
  const averageScore = Number(quizStatisticsData?.averageScore || 0)
  const highestScore = Number(quizStatisticsData?.highestScore || 0)
  const lowestScore = Number(quizStatisticsData?.lowestScore || 0)
  const avgTimeMinutes = Number(
    quizStatisticsData?.averageCompletionTimeMinutes || 0,
  )

  return (
    <PageLayout
      variant="app"
      containerClassName="max-w-7xl"
      isLoading={isLoading}
      title={
        <div className="flex flex-col items-start gap-4">
          <Link
            to="/app/teacher/quizzes"
            search={{
              subjectId: subjectId,
              activeOnly: true,
            }}
            className="flex w-fit items-center gap-2 text-sm font-normal text-muted-foreground transition-colors hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" />
            {t('quiz.backToQuizzes')}
          </Link>
          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-nav-purple/10 text-nav-purple">
              <FileSignature className="h-6 w-6" />
            </div>
            <span>{quizData?.title}</span>
          </div>
        </div>
      }
      titleBadge={
        <Badge
          variant={quizData?.publishDate ? 'default' : 'secondary'}
          className="mt-8 shadow-sm"
        >
          {quizData?.publishDate ? t('quiz.published') : t('quiz.draft')}
        </Badge>
      }
      headerActions={
        <Link to="/app/quizzes/$quizId/builder" params={{ quizId }}>
          <Button variant="outline" className="mt-8 gap-2 shadow-sm">
            <Settings className="h-4 w-4" />
            {t('quiz.editQuiz')}
          </Button>
        </Link>
      }
      headerControls={
        <div className="flex w-full flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <DebouncedInput
            id="attempts-search"
            value={searchQuery}
            onChange={(val) => {
              setSearchQuery(val)
              setPageNumber(1)
            }}
            placeholder={t('quiz.searchStudents')}
            icon={<Search className="h-4 w-4" />}
            className="w-full bg-background sm:max-w-xs"
            hideErrors
          />
          <div className="text-sm font-medium whitespace-nowrap text-muted-foreground">
            {t('common.totalCount', {
              count: totalTableCount,
              defaultValue: `Total: ${totalTableCount}`,
            })}
          </div>
        </div>
      }
    >
      <div className="flex flex-col gap-8">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <Card className="border-border shadow-sm transition-shadow hover:shadow-md">
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                {t('quiz.totalAttempts')}
              </CardTitle>
              <Users className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-3xl font-bold">{statsTotalAttempts}</div>
              <p className="mt-1 text-xs text-muted-foreground">
                {statsCompletedAttempts} {t('quiz.completed')}
              </p>
            </CardContent>
          </Card>

          <Card className="border-border shadow-sm transition-shadow hover:shadow-md">
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                {t('quiz.averageScore')}
              </CardTitle>
              <Target className="h-4 w-4 text-info" />
            </CardHeader>
            <CardContent>
              <div className="text-3xl font-bold text-info">
                {averageScore.toFixed(1)}
              </div>
            </CardContent>
          </Card>

          <Card className="border-border shadow-sm transition-shadow hover:shadow-md">
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                {t('quiz.highLowScore')}
              </CardTitle>
              <TrendingUp className="h-4 w-4 text-success" />
            </CardHeader>
            <CardContent>
              <div className="text-3xl font-bold text-success">
                {highestScore}{' '}
                <span className="text-xl font-medium text-muted-foreground/50">
                  /
                </span>{' '}
                {lowestScore}
              </div>
            </CardContent>
          </Card>

          <Card className="border-border shadow-sm transition-shadow hover:shadow-md">
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                {t('quiz.avgCompletionTime')}
              </CardTitle>
              <Clock className="h-4 w-4 text-warning" />
            </CardHeader>
            <CardContent>
              <div className="text-3xl font-bold text-warning">
                {avgTimeMinutes.toFixed(1)}{' '}
                <span className="text-sm font-normal">min</span>
              </div>
            </CardContent>
          </Card>
        </div>

        <QuestionAnalysis questions={questionStats ?? []} />

        <div
          className={`transition-opacity duration-200 ${isFetching && !isLoadingAttempts ? 'opacity-50' : 'opacity-100'}`}
        >
          <div className="mb-4">
            <h2 className="text-xl font-semibold tracking-tight">
              {t('quiz.attemptHistory')}
            </h2>
          </div>

          <QuizAttemptsTable
            attempts={attempts}
            isLoading={isFetching || isLoadingAttempts}
          />

          {totalPages > 1 && (
            <div className="mt-8 flex justify-center">
              <Pagination
                currentPage={pageNumber}
                totalPages={totalPages}
                onPageChange={setPageNumber}
              />
            </div>
          )}
        </div>
      </div>
    </PageLayout>
  )
}
