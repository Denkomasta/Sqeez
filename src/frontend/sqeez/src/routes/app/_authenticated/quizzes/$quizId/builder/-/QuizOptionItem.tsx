import { useCallback, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Trash2,
  CheckCircle2,
  Circle,
  Image as ImageIcon,
  Lock,
} from 'lucide-react'
import {
  getGetApiQuizzesQuizIdQuestionsQuestionIdOptionsQueryKey,
  usePatchApiQuizzesQuizIdQuestionsQuestionIdOptionsOptionId,
  useDeleteApiQuizzesQuizIdQuestionsQuestionIdOptionsOptionId,
} from '@/api/generated/endpoints/quizzes/quizzes'
import { Button } from '@/components/ui/Button'
import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { useQueryClient } from '@tanstack/react-query'
import { cn } from '@/lib/utils'
import { OptionMediaModal } from './OptionMediaModal'
import type { PatchQuizOptionDto, QuizOptionDto } from '@/api/generated/model'
import { handleQuizMutationError } from '@/lib/quizHelpers'
import { useQuizEditorUIStore } from '@/store/useQuizEditorUIStore'

interface QuizOptionItemProps {
  quizId: string
  questionId: string
  option: QuizOptionDto
  isFreeTextMode?: boolean
}

export function QuizOptionItem({
  quizId,
  questionId,
  option,
  isFreeTextMode,
}: QuizOptionItemProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [isMediaModalOpen, setIsMediaModalOpen] = useState(false)
  const lastSubmittedData = useRef<string | null>(null)

  const isLocked = useQuizEditorUIStore((s) => s.isLocked)

  const { mutate: updateOption, isPending: isUpdating } =
    usePatchApiQuizzesQuizIdQuestionsQuestionIdOptionsOptionId({
      mutation: {
        onSuccess: () => {
          queryClient.invalidateQueries({
            queryKey: getGetApiQuizzesQuizIdQuestionsQuestionIdOptionsQueryKey(
              quizId,
              questionId,
            ),
          })
        },
        onError: (error) => handleQuizMutationError(error, t),
      },
    })

  const { mutate: removeOption, isPending: isDeleting } =
    useDeleteApiQuizzesQuizIdQuestionsQuestionIdOptionsOptionId({
      mutation: {
        onSuccess: () => {
          queryClient.invalidateQueries({
            queryKey: getGetApiQuizzesQuizIdQuestionsQuestionIdOptionsQueryKey(
              quizId,
              questionId,
            ),
          })
        },
        onError: (error) => handleQuizMutationError(error, t),
      },
    })

  const handleUpdate = useCallback(
    (data: PatchQuizOptionDto) => {
      if (isLocked) return

      if (
        (data.text !== undefined && data.text === option.text) ||
        (data.isCorrect !== undefined && data.isCorrect === option.isCorrect) ||
        (data.isFreeText !== undefined &&
          data.isFreeText === option.isFreeText) ||
        (data.mediaAssetId !== undefined &&
          data.mediaAssetId === option.mediaAssetId)
      ) {
        lastSubmittedData.current = null
        return
      }

      const stringifiedData = JSON.stringify(data)
      if (stringifiedData === lastSubmittedData.current) {
        return
      }

      lastSubmittedData.current = stringifiedData

      updateOption({
        quizId,
        questionId,
        optionId: option.id.toString(),
        data,
      })
    },
    [
      option.id,
      option.text,
      option.isCorrect,
      option.isFreeText,
      option.mediaAssetId,
      quizId,
      questionId,
      updateOption,
      isLocked,
    ],
  )

  const handleDelete = () => {
    if (isLocked) return
    removeOption({
      quizId,
      questionId,
      optionId: option.id.toString(),
    })
  }

  return (
    <>
      <div
        className={cn(
          'group flex items-center gap-3 rounded-xl border p-2 transition-all',
          option.isCorrect && !isFreeTextMode
            ? 'border-success/30 bg-success/5'
            : 'border-border bg-background',
          isFreeTextMode && 'border-info/20 bg-info/5',
          isLocked && 'pointer-events-none opacity-70 grayscale-[0.2]',
        )}
      >
        {!isFreeTextMode && (
          <button
            onClick={() => handleUpdate({ isCorrect: !option.isCorrect })}
            disabled={isUpdating || isLocked}
            className="shrink-0 p-2 transition-transform active:scale-90 disabled:opacity-50"
            title={t('editor.markCorrect')}
          >
            {option.isCorrect ? (
              <CheckCircle2 className="h-6 w-6 fill-success/20 text-success" />
            ) : (
              <Circle className="h-6 w-6 text-muted-foreground/30 hover:text-muted-foreground" />
            )}
          </button>
        )}

        <div className="flex flex-1 flex-col gap-1">
          <DebouncedInput
            value={option.text ?? ''}
            onChange={(newText) => handleUpdate({ text: newText })}
            disabled={isLocked}
            placeholder={
              isFreeTextMode
                ? t('editor.freeTextAnswerHint')
                : t('editor.optionTextHint')
            }
            className={cn(
              'border-none bg-transparent shadow-none focus-visible:ring-1',
              isFreeTextMode && 'font-mono text-primary',
            )}
            hideErrors
          />
        </div>

        {isLocked && <Lock className="mr-2 h-4 w-4 text-muted-foreground" />}

        {!isFreeTextMode && !isLocked && (
          <div className="flex items-center gap-1 border-l border-border/50 px-2">
            <Button
              variant="ghost"
              size="icon"
              onClick={() => setIsMediaModalOpen(true)}
              className={cn(
                'h-8 w-8',
                option.mediaAssetId
                  ? 'bg-info/10 text-info'
                  : 'text-muted-foreground',
              )}
              title={t('editor.manageOptionMedia')}
            >
              <ImageIcon className="h-4 w-4" />
            </Button>
          </div>
        )}

        {!isLocked && (
          <Button
            variant="ghost"
            size="icon"
            disabled={isDeleting}
            className="h-8 w-8 text-destructive opacity-0 transition-opacity group-hover:opacity-100 hover:bg-destructive/10 hover:text-destructive"
            onClick={handleDelete}
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        )}
      </div>

      {!isFreeTextMode && !isLocked && (
        <OptionMediaModal
          isOpen={isMediaModalOpen}
          onClose={() => setIsMediaModalOpen(false)}
          currentMediaAssetId={option.mediaAssetId}
          onSave={async (newAssetId) => {
            handleUpdate({ mediaAssetId: newAssetId })
          }}
        />
      )}
    </>
  )
}
