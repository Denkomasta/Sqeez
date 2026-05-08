import { useTranslation } from 'react-i18next'
import {
  usePatchApiQuizzesQuizIdQuestionsQuestionId,
  getGetApiQuizzesQuizIdQuestionsQuestionIdQueryKey,
} from '@/api/generated/endpoints/quizzes/quizzes'
import { useQueryClient } from '@tanstack/react-query'
import { cn } from '@/lib/utils'
import { Lock, Scale } from 'lucide-react'
import { handleQuizMutationError } from '@/lib/quizHelpers'
import { useQuizEditorUIStore } from '@/store/useQuizEditorUIStore'

interface QuestionDifficultyEditorProps {
  quizId: string
  questionId: string
  currentDifficulty: number
  hasPenalty: boolean
  calculatedPenalty: number
}

export function QuestionDifficultyEditor({
  quizId,
  questionId,
  currentDifficulty,
  hasPenalty,
  calculatedPenalty,
}: QuestionDifficultyEditorProps) {
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

  const levels = [1, 2, 3, 4, 5]

  const handleDifficultyChange = (level: number) => {
    if (level === currentDifficulty || isLocked) return

    updateQuestion({
      quizId,
      questionId,
      data: { difficulty: level },
    })
  }

  const handleTogglePenalty = () => {
    if (isLocked) return

    updateQuestion({
      quizId,
      questionId,
      data: { hasPenalty: !hasPenalty },
    })
  }

  return (
    <div
      className={cn(
        'flex flex-col gap-2',
        isLocked && 'opacity-70 grayscale-[0.2]',
      )}
    >
      <div className="flex items-center gap-2 text-xs font-black tracking-widest text-muted-foreground uppercase">
        {t('editor.difficulty')}
        {isLocked && <Lock className="h-3 w-3" />}
      </div>

      <div className="flex gap-2">
        {levels.map((level) => (
          <button
            key={level}
            onClick={() => handleDifficultyChange(level)}
            disabled={isPending || isLocked}
            className={cn(
              'h-10 flex-1 rounded-lg border text-sm font-bold transition-all active:scale-95',
              currentDifficulty === level
                ? 'border-primary bg-primary text-primary-foreground shadow-md'
                : 'border-muted bg-background text-muted-foreground',
              !isLocked && 'hover:border-primary/50',
              (isPending || isLocked) &&
                currentDifficulty !== level &&
                'cursor-not-allowed opacity-50',
            )}
          >
            {level}
          </button>
        ))}
      </div>

      <div className="flex items-center justify-between px-1">
        <p className="text-[10px] text-muted-foreground italic">
          {currentDifficulty <= 2 && t('editor.difficultyEasy')}
          {currentDifficulty === 3 && t('editor.difficultyMedium')}
          {currentDifficulty >= 4 && t('editor.difficultyHard')}
        </p>

        <button
          onClick={handleTogglePenalty}
          disabled={isPending || isLocked}
          title={t('editor.togglePenaltyDesc')}
          className={cn(
            'flex items-center gap-1.5 rounded px-2 py-0.5 text-[10px] font-semibold transition-all hover:scale-105 active:scale-95',
            hasPenalty
              ? 'border border-destructive/30 bg-destructive/10 text-destructive'
              : 'border border-transparent bg-muted/50 text-muted-foreground hover:bg-muted',
          )}
        >
          <Scale className="h-3 w-3" />
          <span className="text-success">+{currentDifficulty}</span>
          <span>/</span>
          <span className={hasPenalty ? '' : 'opacity-50'}>
            -{calculatedPenalty}
          </span>
        </button>
      </div>
    </div>
  )
}
