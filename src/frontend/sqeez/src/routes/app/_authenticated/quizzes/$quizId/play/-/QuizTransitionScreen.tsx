import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Timer } from 'lucide-react'

interface QuestionTransitionScreenProps {
  questionNumber: number
  totalQuestions: number
  countdownSeconds?: number
  onComplete: () => void
}

/**
 * Short transition screen between quiz questions.
 * Keeps the user oriented while the next question is being fetched.
 */
export function QuestionTransitionScreen({
  questionNumber,
  totalQuestions,
  countdownSeconds = 5,
  onComplete,
}: QuestionTransitionScreenProps) {
  const { t } = useTranslation()
  const [timeLeft, setTimeLeft] = useState(countdownSeconds)

  useEffect(() => {
    if (timeLeft <= 0) {
      onComplete()
      return
    }

    const timer = setInterval(() => {
      setTimeLeft((prev) => prev - 1)
    }, 1000)

    return () => clearInterval(timer)
  }, [timeLeft, onComplete])

  return (
    <div className="flex min-h-[calc(100vh-4rem)] w-full flex-col items-center justify-center p-4">
      <div className="w-full max-w-md animate-in flex-col items-center justify-center rounded-[2.5rem] border-4 border-muted bg-card p-10 text-center shadow-xl duration-500 zoom-in-95 md:p-14">
        <div className="mx-auto mb-8 inline-flex items-center justify-center gap-2 rounded-full border-4 border-muted/50 bg-muted/10 px-6 py-2 text-sm font-bold tracking-widest text-muted-foreground uppercase md:text-base">
          <Timer className="size-5" />
          {t('quiz.questionProgress', {
            current: questionNumber,
            total: totalQuestions,
          })}
        </div>

        <h2 className="mb-10 text-4xl font-black tracking-widest text-foreground md:text-5xl">
          {t('quiz.getReady')}
        </h2>

        <div className="relative mx-auto flex h-40 w-40 items-center justify-center md:h-48 md:w-48">
          <div className="absolute inset-0 animate-ping rounded-full bg-primary/20 duration-1000" />

          <div
            key={timeLeft}
            className="relative flex h-full w-full animate-in items-center justify-center rounded-full border-8 border-b-[12px] border-primary bg-primary/10 shadow-inner duration-300 zoom-in-50"
          >
            <span className="text-7xl font-black text-primary drop-shadow-sm md:text-8xl">
              {timeLeft}
            </span>
          </div>
        </div>
      </div>
    </div>
  )
}
