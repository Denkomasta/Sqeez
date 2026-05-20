import { useTranslation } from 'react-i18next'
import {
  getGetApiQuizzesQuizIdQuestionsQuestionIdQueryKey,
  usePatchApiQuizzesQuizIdQuestionsQuestionId,
} from '@/api/generated/endpoints/quizzes/quizzes'
import { useQueryClient } from '@tanstack/react-query'
import { cn } from '@/lib/utils'
import { CheckSquare, Lock } from 'lucide-react'
import { Switch } from '@/components/ui/Switch'
import { handleQuizMutationError } from '@/lib/quizHelpers'
import { useQuizEditorUIStore } from '@/store/useQuizEditorUIStore'

interface QuestionMultipleChoiceEditorProps {
  quizId: string
  questionId: string
  currentIsStrict: boolean
}

/**
 * Quiz-builder control for strict multiple-choice mode.
 * The toggle is disabled for locked quizzes.
 */
export function QuestionMultipleChoiceEditor({
  quizId,
  questionId,
  currentIsStrict,
}: QuestionMultipleChoiceEditorProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const isLocked = useQuizEditorUIStore((s) => s.isLocked)

  const { mutate: updateQuestion, isPending } =
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

  const handleToggle = (checked: boolean) => {
    if (isLocked || checked === currentIsStrict) return

    updateQuestion({
      quizId,
      questionId,
      data: { isStrictMultipleChoice: checked },
    })
  }

  return (
    <div
      className={cn(
        'flex items-center justify-between rounded-xl border bg-muted/5 p-6 transition-opacity',
        isLocked && 'opacity-70 grayscale-[0.2]',
      )}
    >
      <div className="flex flex-col gap-1 pr-6">
        <div className="flex items-center gap-2 font-semibold">
          <CheckSquare className="h-4 w-4 text-primary" />
          {t('editor.strictMultipleChoiceTitle')}
          {isLocked && <Lock className="h-3 w-3 text-muted-foreground" />}
        </div>
        <p className="text-sm text-muted-foreground">
          {t('editor.strictMultipleChoiceDesc')}
        </p>
      </div>
      <Switch
        checked={currentIsStrict}
        onCheckedChange={handleToggle}
        disabled={isPending || isLocked}
      />
    </div>
  )
}
