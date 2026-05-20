import { useTranslation } from 'react-i18next'
import { Plus, Loader2, Type, ListChecks, Info } from 'lucide-react'
import {
  getGetApiQuizzesQuizIdQuestionsQuestionIdOptionsQueryKey,
  useGetApiQuizzesQuizIdQuestionsQuestionIdOptions,
  usePostApiQuizzesQuizIdQuestionsQuestionIdOptions,
} from '@/api/generated/endpoints/quizzes/quizzes'
import { Button } from '@/components/ui/Button'
import { useQueryClient } from '@tanstack/react-query'
import { QuizOptionItem } from './QuizOptionItem'
import { handleQuizMutationError } from '@/lib/quizHelpers'
import { useQuizEditorUIStore } from '@/store/useQuizEditorUIStore'

interface QuizOptionsEditorProps {
  quizId: string
  questionId: string
}

/**
 * Chooses the option editor mode for a question.
 * Free-text questions manage one expected-answer option instead of many choices.
 *
 * @param props.quizId - Quiz id used by option list and create endpoints.
 * @param props.questionId - Question id whose options are managed.
 */
export function QuizOptionsEditor({
  quizId,
  questionId,
}: QuizOptionsEditorProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const { isLocked } = useQuizEditorUIStore()

  const { data: optionsData, isLoading } =
    useGetApiQuizzesQuizIdQuestionsQuestionIdOptions(quizId, questionId)

  const options = optionsData?.data ?? []

  const isFreeTextMode = options.some((opt) => opt.isFreeText)

  const addOptionMutation = usePostApiQuizzesQuizIdQuestionsQuestionIdOptions({
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

  const handleAddOption = async (asFreeText: boolean = false) => {
    if (isLocked) return

    await addOptionMutation.mutateAsync({
      quizId,
      questionId,
      data: {
        text: asFreeText ? '' : t('editor.newOptionDefault'),
        isCorrect: asFreeText ? true : false,
        quizQuestionID: questionId,
        isFreeText: asFreeText,
      },
    })
  }

  if (isLoading)
    return (
      <Loader2 className="mx-auto mt-4 h-6 w-6 animate-spin text-primary" />
    )

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <label className="text-xs font-black tracking-widest text-muted-foreground uppercase">
          {isFreeTextMode ? t('editor.expectedAnswer') : t('editor.options')}
        </label>

        {!isFreeTextMode && options.length > 0 && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => handleAddOption(false)}
            disabled={addOptionMutation.isPending || isLocked}
            className="h-8 gap-1"
          >
            {addOptionMutation.isPending ? (
              <Loader2 className="h-3.5 w-3.5 animate-spin" />
            ) : (
              <Plus className="h-3.5 w-3.5" />
            )}
            {t('editor.addOption')}
          </Button>
        )}
      </div>

      {isFreeTextMode && (
        <div className="flex items-start gap-3 rounded-xl border border-primary/20 bg-primary/5 p-4 text-sm text-primary">
          <Info className="mt-0.5 h-4 w-4 shrink-0" />
          <div className="flex flex-col gap-1">
            <p className="font-semibold">{t('editor.freeTextModeActive')}</p>
            <p className="text-primary/80">{t('editor.freeTextHint')}</p>
          </div>
        </div>
      )}

      <div className="space-y-3">
        {options.map((opt) => (
          <QuizOptionItem
            key={opt.id}
            quizId={quizId}
            questionId={questionId}
            option={opt}
            isFreeTextMode={isFreeTextMode}
          />
        ))}

        {options.length === 0 && (
          <div className="flex flex-col items-center justify-center gap-4 rounded-xl border-2 border-dashed border-muted/50 p-8 text-center">
            <p className="text-sm text-muted-foreground">
              {t('editor.chooseQuestionType')}
            </p>
            <div className="flex flex-wrap justify-center gap-4">
              <Button
                variant="outline"
                className="gap-2"
                onClick={() => handleAddOption(false)}
                disabled={addOptionMutation.isPending || isLocked}
              >
                <ListChecks className="h-4 w-4" />
                {t('editor.startMultipleChoice')}
              </Button>
              <Button
                variant="outline"
                className="gap-2"
                onClick={() => handleAddOption(true)}
                disabled={addOptionMutation.isPending || isLocked}
              >
                <Type className="h-4 w-4" />
                {t('editor.startFreeText')}
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
