import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle, PlayCircle, Rocket, ArrowLeft } from 'lucide-react'
import { AsyncButton, Button } from '@/components/ui/Button'
import { usePostApiQuizAttemptsStart } from '@/api/generated/endpoints/quiz-attempts/quiz-attempts'

interface QuizStartScreenProps {
  quizId: number
  quizTitle: string
  enrollmentId: number
  onAttemptStarted: (
    newAttemptId: number,
    firstQuestionId: number | null,
  ) => void
  onCancel: () => void
}

/**
 * Quiz start/resume screen.
 * Shows availability metadata before the quiz engine starts an attempt.
 */
export function QuizStartScreen({
  quizId,
  quizTitle,
  enrollmentId,
  onAttemptStarted,
  onCancel,
}: QuizStartScreenProps) {
  const { t } = useTranslation()
  const [isStarting, setIsStarting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const startMutation = usePostApiQuizAttemptsStart()

  const handleStartAttempt = async () => {
    setIsStarting(true)
    setError(null)

    try {
      const response = await startMutation.mutateAsync({
        data: {
          quizId: quizId,
          enrollmentId: enrollmentId,
        },
      })
      onAttemptStarted(
        Number(response.id),
        response?.nextQuestionId ? Number(response.nextQuestionId) : null,
      )
    } catch (err) {
      console.error('Failed to start quiz attempt:', err)
      setError(t('quiz.startError'))
    } finally {
      setIsStarting(false)
    }
  }

  return (
    <div className="flex min-h-[calc(100vh-4rem)] w-full animate-in flex-col items-center justify-center p-4 duration-500 zoom-in-95 fade-in md:p-6 lg:p-8">
      <div className="w-full max-w-2xl overflow-hidden rounded-[2rem] border-4 border-muted bg-card shadow-xl">
        <div className="flex flex-col items-center justify-center border-b-4 border-muted/50 bg-muted/10 p-8 text-center sm:p-10">
          <div className="relative mb-4">
            <div className="absolute inset-0 animate-ping rounded-full bg-primary/20" />
            <div className="relative flex h-20 w-20 items-center justify-center rounded-full bg-primary/10 shadow-sm md:h-24 md:w-24">
              <Rocket className="h-10 w-10 text-primary md:h-12 md:w-12" />
            </div>
          </div>
          <h1 className="text-3xl font-black tracking-widest text-foreground drop-shadow-sm md:text-4xl">
            {t('quiz.readyToStart')}
          </h1>
          <p className="mt-2 text-lg font-bold tracking-wide text-muted-foreground md:text-xl">
            {quizTitle}
          </p>
        </div>

        <div className="p-6 md:p-8">
          <div className="mb-8 rounded-2xl border-4 border-b-8 border-warning/30 bg-warning/10 p-6 text-warning">
            <div className="flex items-center gap-4 text-left font-bold">
              <AlertTriangle className="size-8 shrink-0 md:size-10" />
              <p className="text-lg md:text-xl">{t('quiz.startWarning')}</p>
            </div>
          </div>

          {error && (
            <div className="mb-6 rounded-2xl border-4 border-destructive/30 bg-destructive/10 p-4 text-center text-lg font-bold text-destructive">
              {error}
            </div>
          )}

          <div className="flex flex-col-reverse gap-4 sm:flex-row sm:gap-4">
            <Button
              variant="outline"
              size="lg"
              onClick={onCancel}
              disabled={isStarting}
              className="h-auto w-full rounded-2xl border-4 border-b-8 border-muted bg-transparent py-3 text-lg font-black text-muted-foreground transition-all hover:-translate-y-1 hover:bg-muted/10 active:translate-y-2 active:border-b-4 sm:w-1/2 md:py-4"
            >
              <ArrowLeft className="mr-2 size-5 stroke-3" />
              {t('common.cancel')}
            </Button>

            <AsyncButton
              size="lg"
              onClick={handleStartAttempt}
              disabled={isStarting}
              loadingText={t('quiz.beginAttempt')}
              className="h-auto w-full rounded-2xl border-b-8 border-primary-foreground/20 py-3 text-lg font-black shadow-none transition-all hover:-translate-y-1 active:translate-y-2 active:border-b-0 sm:w-1/2 md:py-4"
            >
              <PlayCircle className="mr-2 size-6 stroke-3" />
              {t('quiz.beginAttempt')}
            </AsyncButton>
          </div>
        </div>
      </div>
    </div>
  )
}
