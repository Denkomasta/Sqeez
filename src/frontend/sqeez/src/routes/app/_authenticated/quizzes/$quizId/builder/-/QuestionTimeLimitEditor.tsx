import { useTranslation } from 'react-i18next'
import {
  getGetApiQuizzesQuizIdQuestionsQuestionIdQueryKey,
  usePatchApiQuizzesQuizIdQuestionsQuestionId,
} from '@/api/generated/endpoints/quizzes/quizzes'
import { cn } from '@/lib/utils'
import { Clock, Lock } from 'lucide-react'
import { useQueryClient } from '@tanstack/react-query'
import { handleQuizMutationError } from '@/lib/quizHelpers'
import { useQuizEditorUIStore } from '@/store/useQuizEditorUIStore'

interface QuestionTimeLimitEditorProps {
  quizId: string
  questionId: string
  currentTimeLimit: number
}

/**
 * Quiz-builder control for per-question time limit.
 * Persists changes through the question patch endpoint.
 */
export function QuestionTimeLimitEditor({
  quizId,
  questionId,
  currentTimeLimit,
}: QuestionTimeLimitEditorProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const isLocked = useQuizEditorUIStore((s) => s.isLocked)

  const { mutate: updateTime, isPending } =
    usePatchApiQuizzesQuizIdQuestionsQuestionId({
      mutation: {
        onSuccess: (updatedQuestionData) => {
          queryClient.setQueryData(
            getGetApiQuizzesQuizIdQuestionsQuestionIdQueryKey(
              quizId,
              questionId,
            ),
            updatedQuestionData,
          )
        },
        onError: (error) => handleQuizMutationError(error, t),
      },
    })

  const presets = [10, 20, 30, 60, 120]

  const handleTimeChange = (seconds: number) => {
    if (seconds === currentTimeLimit || isLocked) return

    updateTime({
      quizId,
      questionId,
      data: { timeLimit: seconds },
    })
  }

  return (
    <div className={cn('space-y-3', isLocked && 'opacity-70 grayscale-[0.2]')}>
      <div className="flex items-center gap-2 text-muted-foreground">
        <Clock className="h-4 w-4" />
        <label className="text-xs font-black tracking-widest uppercase">
          {t('editor.timeLimit')}
        </label>
        {isLocked && <Lock className="h-3 w-3" />}
      </div>

      <div className="flex flex-wrap gap-2">
        {presets.map((time) => (
          <button
            key={time}
            onClick={() => handleTimeChange(time)}
            disabled={isPending || isLocked}
            className={cn(
              'h-10 min-w-15 flex-1 rounded-lg border text-sm font-bold transition-all active:scale-95',
              currentTimeLimit === time
                ? 'border-primary bg-primary text-primary-foreground shadow-md'
                : 'border-muted bg-background text-muted-foreground',
              !isLocked && 'hover:border-primary/50',
              (isPending || isLocked) &&
                currentTimeLimit !== time &&
                'cursor-not-allowed opacity-50',
            )}
          >
            {time}s
          </button>
        ))}
      </div>
      <p className="px-1 text-[10px] text-muted-foreground italic">
        {t('editor.timeLimitHint')}
      </p>
    </div>
  )
}
