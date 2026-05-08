import { Link } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import {
  Clock,
  ChevronRight,
  PlayCircle,
  AlertCircle,
  Search,
  History,
  BookOpen,
  Edit,
  BarChart,
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
import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { Pagination } from '@/components/ui/Pagination'
import { PageLayout } from '@/components/layouting/PageLayout/PageLayout'
import type { QuizDto, SubjectDto, UserRole } from '@/api/generated/model'
import { formatDate } from '@/lib/dateHelpers'

interface QuizListViewProps {
  titleNode: React.ReactNode
  backButtonNode?: React.ReactNode
  headerActions?: React.ReactNode
  quizzes: QuizDto[]
  totalQuizzes: number
  totalPages: number
  isLoading: boolean
  isFetching: boolean
  searchQuery: string
  setSearchQuery: (query: string) => void
  pageNumber: number
  setPageNumber: (page: number) => void
  emptyStateMessage: string
  subject?: SubjectDto
  role?: UserRole
  showActiveToggle?: boolean
  showActiveOnly?: boolean
  setShowActiveOnly?: (active: boolean) => void
}

export function QuizListView({
  titleNode,
  backButtonNode,
  headerActions,
  quizzes,
  totalQuizzes,
  totalPages,
  isLoading,
  isFetching,
  searchQuery,
  setSearchQuery,
  pageNumber,
  setPageNumber,
  emptyStateMessage,
  subject,
  role = 'Student',
  showActiveToggle = false,
  showActiveOnly = true,
  setShowActiveOnly,
}: QuizListViewProps) {
  const { t } = useTranslation()

  return (
    <PageLayout
      containerClassName="max-w-7xl"
      isLoading={isLoading}
      title={titleNode}
      headerActions={headerActions}
      titleBadge={
        <span className="font-medium text-muted-foreground">
          {t('quiz.totalCount', { count: totalQuizzes })}
        </span>
      }
      headerControls={
        <div className="flex flex-col gap-4">
          {backButtonNode}
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <DebouncedInput
              id="quiz-search"
              value={searchQuery}
              onChange={(newQuery) => {
                setSearchQuery(newQuery)
                setPageNumber(1)
              }}
              placeholder={t('quiz.search')}
              icon={<Search className="h-4 w-4" />}
              className="sm:max-w-xs"
              hideErrors
            />

            {showActiveToggle && setShowActiveOnly && (
              <div className="flex rounded-lg bg-muted p-1">
                <button
                  onClick={() => {
                    setShowActiveOnly(true)
                    setPageNumber(1)
                  }}
                  className={`flex items-center justify-center rounded-md px-4 py-1.5 text-sm font-medium transition-all ${
                    showActiveOnly
                      ? 'bg-background text-foreground shadow-sm'
                      : 'text-muted-foreground hover:text-foreground'
                  }`}
                >
                  {t('quiz.active')}
                </button>
                <button
                  onClick={() => {
                    setShowActiveOnly(false)
                    setPageNumber(1)
                  }}
                  className={`flex items-center justify-center rounded-md px-4 py-1.5 text-sm font-medium transition-all ${
                    !showActiveOnly
                      ? 'bg-background text-foreground shadow-sm'
                      : 'text-muted-foreground hover:text-foreground'
                  }`}
                >
                  {t('quiz.pastInactive')}
                </button>
              </div>
            )}
          </div>
        </div>
      }
    >
      <div
        className={`transition-opacity duration-200 ${
          isFetching && !isLoading ? 'opacity-50' : 'opacity-100'
        }`}
      >
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {quizzes.length > 0 ? (
            quizzes.map((quiz) => {
              const hasAttempts = Number(quiz.quizAttempts) > 0

              const targetSubjectId = subject?.id || 0

              return (
                <Card
                  key={quiz.id}
                  className="flex flex-col transition-shadow hover:shadow-md"
                >
                  <CardHeader className="pb-3">
                    <div className="flex items-start justify-between gap-4">
                      <CardTitle className="line-clamp-2 text-lg">
                        {quiz.title}
                      </CardTitle>

                      <Badge
                        variant={hasAttempts ? 'default' : 'secondary'}
                        className="shrink-0"
                      >
                        {role === 'Teacher'
                          ? quiz.publishDate
                            ? t('quiz.published')
                            : t('quiz.draft')
                          : hasAttempts
                            ? t('quiz.completed')
                            : t('quiz.pending')}
                      </Badge>
                    </div>
                    <CardDescription className="mt-1 line-clamp-2">
                      {quiz.description || t('quiz.noDescription')}
                    </CardDescription>
                  </CardHeader>

                  <CardContent className="flex-1 pb-3">
                    <div className="flex flex-col gap-2 text-sm text-muted-foreground">
                      {subject && (
                        <div className="flex items-center gap-2 font-medium text-foreground">
                          <BookOpen className="h-4 w-4 text-primary" />
                          <span className="line-clamp-1">{subject.name}</span>
                        </div>
                      )}

                      {quiz.closingDate && (
                        <div className="flex items-center gap-2">
                          <Clock className="h-4 w-4" />
                          <span>
                            {t('quiz.due')}: {formatDate(quiz.closingDate)}
                          </span>
                        </div>
                      )}

                      {quiz.quizQuestions !== undefined && (
                        <div className="flex items-center gap-2">
                          <AlertCircle className="h-4 w-4" />
                          <span>
                            {quiz.quizQuestions} {t('quiz.questions')}
                          </span>
                        </div>
                      )}

                      {quiz.quizAttempts !== undefined && (
                        <div className="flex items-center gap-2">
                          <History className="h-4 w-4" />
                          <span>
                            {quiz.quizAttempts}
                            {role === 'Student' && quiz.maxRetries
                              ? ` / ${quiz.maxRetries}`
                              : ''}{' '}
                            {role === 'Teacher'
                              ? t('dashboard.totalAttempts')
                              : t('quiz.attempts')}
                          </span>
                        </div>
                      )}
                    </div>
                  </CardContent>

                  <CardFooter className="border-t bg-muted/10 pt-3">
                    {role === 'Teacher' ? (
                      <div className="flex w-full gap-2">
                        <Link
                          to="/app/quizzes/$quizId/builder"
                          params={{ quizId: quiz.id.toString() }}
                          className="group flex flex-1 items-center justify-center gap-2 rounded-md bg-secondary px-4 py-2 text-sm font-medium text-secondary-foreground transition-colors hover:bg-secondary/80"
                        >
                          <Edit className="h-4 w-4" />
                          <span className="hidden sm:inline">
                            {t('dashboard.editQuiz')}
                          </span>
                        </Link>

                        <Link
                          to="/app/teacher/subjects/$subjectId/$quizId"
                          params={{
                            subjectId: String(targetSubjectId),
                            quizId: quiz.id.toString(),
                          }}
                          className="group flex flex-1 items-center justify-center gap-2 rounded-md bg-nav-purple px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-nav-purple/90"
                        >
                          <BarChart className="h-4 w-4" />
                          <span className="hidden sm:inline">
                            {t('quiz.viewStats')}
                          </span>
                        </Link>
                      </div>
                    ) : (
                      <Link
                        to="/app/quizzes/$quizId"
                        params={{ quizId: quiz.id.toString() }}
                        className="group flex w-full items-center justify-center gap-2 rounded-md bg-secondary px-4 py-2 text-sm font-medium text-secondary-foreground transition-colors hover:bg-secondary/80"
                      >
                        <PlayCircle className="h-4 w-4" />
                        {hasAttempts
                          ? quiz.maxRetries &&
                            quiz.quizAttempts >= quiz.maxRetries
                            ? t('quiz.viewResults')
                            : t('quiz.retakeQuiz')
                          : t('quiz.startQuiz')}
                        <ChevronRight className="h-4 w-4 transition-transform group-hover:translate-x-1" />
                      </Link>
                    )}
                  </CardFooter>
                </Card>
              )
            })
          ) : (
            <div className="col-span-full py-12 text-center text-muted-foreground">
              {searchQuery ? t('quiz.noSearchResults') : emptyStateMessage}
            </div>
          )}
        </div>

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
    </PageLayout>
  )
}
