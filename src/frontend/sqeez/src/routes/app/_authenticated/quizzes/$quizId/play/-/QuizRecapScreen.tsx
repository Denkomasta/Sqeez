import { useTranslation } from 'react-i18next'
import { Link } from '@tanstack/react-router'
import {
  Trophy,
  Target,
  BarChart,
  ArrowRight,
  ListTodo,
  Clock,
  Info,
} from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { BadgeUnlockNotification } from '@/components/quizzes/BadgeUnlockNotification'
import type { StudentBadgeBasicDto } from '@/api/generated/model'
import { cn } from '@/lib/utils'

interface QuizRecapScreenProps {
  quizId: number | string
  quizTitle: string
  totalQuestions: number
  correctCount: number
  badges?: StudentBadgeBasicDto[]
  isPendingCorrection?: boolean
  resetQuiz: () => void
}

/**
 * Quiz completion recap.
 * Shows final score, earned badges, and navigation to the detailed results page.
 */
export function QuizRecapScreen({
  quizId,
  quizTitle,
  totalQuestions,
  correctCount,
  badges,
  isPendingCorrection = false,
  resetQuiz,
}: QuizRecapScreenProps) {
  const { t } = useTranslation()

  const scorePercentage =
    totalQuestions > 0 ? Math.round((correctCount / totalQuestions) * 100) : 0

  const isPassing = scorePercentage >= 50

  const headerThemeClasses = isPendingCorrection
    ? 'border-info/80 bg-info text-info-foreground'
    : isPassing
      ? 'border-success/80 bg-success text-success-foreground'
      : 'border-destructive/80 bg-destructive text-destructive-foreground'

  const iconColorClass = isPendingCorrection
    ? 'text-info'
    : isPassing
      ? 'text-success'
      : 'text-destructive'

  const scoreColorClass = isPendingCorrection
    ? 'text-info'
    : isPassing
      ? 'text-success'
      : 'text-destructive'

  return (
    <div className="flex min-h-[calc(100vh-4rem)] w-full animate-in flex-col items-center justify-center p-4 duration-500 zoom-in-95 fade-in md:p-6 lg:p-8">
      <div className="w-full max-w-2xl overflow-hidden rounded-[2rem] border-4 border-foreground/10 bg-card shadow-xl">
        <div
          className={cn(
            'flex flex-col items-center justify-center border-b-8 p-8 text-center text-white sm:p-10',
            headerThemeClasses,
          )}
        >
          <div className="relative mb-4">
            <div className="absolute inset-0 animate-ping rounded-full bg-white/20" />
            <div className="relative flex h-20 w-20 items-center justify-center rounded-full bg-white shadow-lg md:h-24 md:w-24">
              {isPendingCorrection ? (
                <Clock
                  className={cn('h-10 w-10 md:h-12 md:w-12', iconColorClass)}
                />
              ) : (
                <Trophy
                  className={cn('h-10 w-10 md:h-12 md:w-12', iconColorClass)}
                />
              )}
            </div>
          </div>
          <h1 className="text-3xl font-black tracking-widest drop-shadow-sm md:text-4xl">
            {isPendingCorrection
              ? t('quiz.pendingReviewTitle')
              : t('quiz.quizComplete')}
          </h1>
          <p className="mt-2 text-lg font-bold tracking-wide opacity-90 md:text-xl">
            {quizTitle}
          </p>
        </div>

        <div className="p-6 md:p-8">
          {isPendingCorrection && (
            <div className="mb-8 flex items-start gap-3 rounded-2xl border-4 border-info/20 bg-info/10 p-4 text-foreground">
              <Info className="mt-0.5 h-5 w-5 shrink-0 text-info" />
              <div className="flex flex-col">
                <span className="font-bold">{t('quiz.partialScoreTitle')}</span>
                <span className="text-sm font-medium opacity-90">
                  {t(
                    'quiz.pendingReviewMessage',
                    'Your final score is pending. Some free-text answers require manual correction by the teacher.',
                  )}
                </span>
              </div>
            </div>
          )}

          <div className="mb-8 flex flex-col items-center justify-center space-y-1">
            <span className="text-xs font-black tracking-widest text-muted-foreground uppercase md:text-sm">
              {isPendingCorrection
                ? t('quiz.currentScore')
                : t('quiz.finalScore')}
            </span>
            <div
              className={cn(
                'text-7xl font-black tracking-tighter drop-shadow-sm md:text-[5rem]',
                scoreColorClass,
              )}
            >
              {scorePercentage}%{isPendingCorrection && '*'}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4 md:gap-6">
            <div className="flex flex-col items-center justify-center gap-1 rounded-2xl border-4 border-b-8 border-muted bg-muted/10 p-4 text-center transition-transform hover:-translate-y-1 md:p-5">
              <Target className="mb-1 size-6 text-primary/60 md:size-8" />
              <span className="text-3xl font-black text-foreground md:text-4xl">
                {correctCount}
              </span>
              <span className="text-[10px] font-bold tracking-wider text-muted-foreground uppercase md:text-xs">
                {t('quiz.correctAnswers')}
              </span>
            </div>

            <div className="flex flex-col items-center justify-center gap-1 rounded-2xl border-4 border-b-8 border-muted bg-muted/10 p-4 text-center transition-transform hover:-translate-y-1 md:p-5">
              <ListTodo className="mb-1 size-6 text-primary/60 md:size-8" />
              <span className="text-3xl font-black text-foreground md:text-4xl">
                {totalQuestions}
              </span>
              <span className="text-[10px] font-bold tracking-wider text-muted-foreground uppercase md:text-xs">
                {t('quiz.totalQuestions')}
              </span>
            </div>
          </div>

          <div className="mt-8 flex flex-col gap-4 sm:flex-row sm:gap-4">
            <Button
              variant="outline"
              size="lg"
              className="h-auto w-full rounded-2xl border-4 border-b-8 border-muted bg-transparent py-3 text-lg font-black text-muted-foreground transition-all hover:-translate-y-1 hover:bg-muted/10 active:translate-y-2 active:border-b-4 sm:w-1/2 md:py-4"
              asChild
              onClick={() => resetQuiz()}
            >
              <Link to="/app/quizzes">{t('quiz.backToQuizzes')}</Link>
            </Button>

            <Button
              size="lg"
              className="h-auto w-full rounded-2xl border-b-8 border-primary-foreground/20 py-3 text-lg font-black shadow-none transition-all hover:-translate-y-1 active:translate-y-2 active:border-b-0 sm:w-1/2 md:py-4"
              asChild
              onClick={() => resetQuiz()}
            >
              <Link
                to="/app/quizzes/$quizId/results"
                params={{ quizId: quizId.toString() }}
              >
                <BarChart className="mr-2 size-5" />
                {t('quiz.viewDetails')}
                <ArrowRight className="ml-2 size-5 stroke-3" />
              </Link>
            </Button>
          </div>
        </div>
      </div>

      <BadgeUnlockNotification
        badges={badges}
        achievementText={t('quiz.achievementUnlocked')}
      />
    </div>
  )
}
