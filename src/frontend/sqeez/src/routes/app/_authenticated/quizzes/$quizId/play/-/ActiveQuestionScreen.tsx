import { useTranslation } from 'react-i18next'
import { CheckCircle2 } from 'lucide-react'
import { AsyncButton } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'
import { QuestionCard } from '@/components/quizzes/QuestionCard'

import type { DetailedQuizQuestionDto } from '@/api/generated/model'
import { LiveTimer } from './LiveTimer'

interface ActiveQuestionScreenProps {
  question: DetailedQuizQuestionDto
  isLoading: boolean
  currentNumber: number
  totalQuestions: number
  selectedOptionIds: (number | string)[]
  freeTextValue: string
  hasSelection: boolean
  onSelectOption: (optId: number | string) => void
  onChangeFreeText: (text: string) => void
  onSubmit: (isTimeout: boolean) => Promise<void>
  renderMediaAsset?: (
    assetId: number | string,
    isOption?: boolean,
  ) => React.ReactNode
}

/**
 * Active quiz-play screen for the current question.
 * Owns local answer selection state until the student submits or time expires.
 */
export function ActiveQuestionScreen({
  question,
  isLoading,
  currentNumber,
  totalQuestions,
  selectedOptionIds,
  freeTextValue,
  hasSelection,
  onSelectOption,
  onChangeFreeText,
  onSubmit,
  renderMediaAsset,
}: ActiveQuestionScreenProps) {
  const { t } = useTranslation()

  if (isLoading) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4">
        <Spinner size="lg" />
      </div>
    )
  }

  const timeLimit = Number(question.timeLimit || 0)

  return (
    <div className="flex min-h-[calc(100vh-4rem)] w-full animate-in flex-col justify-center p-4 duration-500 fade-in md:p-6 md:px-12 lg:p-8 lg:px-16">
      <div className="mb-6 space-y-2">
        <div className="flex justify-between text-sm font-medium text-muted-foreground">
          <span>
            {t('quiz.questionProgress', {
              current: currentNumber,
              total: totalQuestions,
            })}
          </span>
          <LiveTimer
            key={question.id}
            timeLimit={timeLimit}
            onTimeUp={() => onSubmit(true)}
          />
        </div>

        <div className="h-2.5 w-full overflow-hidden rounded-full bg-secondary">
          <div
            className="h-full bg-primary transition-all duration-300 ease-in-out"
            style={{
              width: `${((currentNumber - 1) / totalQuestions) * 100}%`,
            }}
          />
        </div>
      </div>

      <QuestionCard
        question={question}
        selectedOptionIds={selectedOptionIds}
        onSelectOption={onSelectOption}
        freeTextValue={freeTextValue}
        onChangeFreeText={onChangeFreeText}
        renderMediaAsset={renderMediaAsset}
      />

      <div className="mt-8 flex justify-end">
        <AsyncButton
          size="lg"
          onClick={() => onSubmit(false)}
          disabled={!hasSelection && timeLimit === 0}
          className="w-full shadow-md sm:w-auto"
          loadingText={t('common.submitting')}
        >
          <CheckCircle2 className="mr-2 h-5 w-5" />
          {t('quiz.submitAnswer')}
        </AsyncButton>
      </div>
    </div>
  )
}
