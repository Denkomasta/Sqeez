import { useCallback, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuizEditorUIStore } from '@/store/useQuizEditorUIStore'
import {
  getGetApiQuizzesQuizIdQuestionsQuestionIdQueryKey,
  useGetApiQuizzesQuizIdQuestionsQuestionId,
  usePatchApiQuizzesQuizIdQuestionsQuestionId,
} from '@/api/generated/endpoints/quizzes/quizzes'
import { Loader2 } from 'lucide-react'
import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { QuestionDifficultyEditor } from './QuestionDifficultyEditor'
import { QuestionTimeLimitEditor } from './QuestionTimeLimitEditor'
import { QuestionMediaEditor } from './QuestionMediaEditor'
import { QuizOptionsEditor } from './QuizOptionsEditor'
import { QuizSettingsEditor } from './QuizSettingsEditor'
import { type InfiniteData, useQueryClient } from '@tanstack/react-query'
import { handleQuizMutationError } from '@/lib/quizHelpers'
import { QuestionMultipleChoiceEditor } from './QuestionMultipleChoiceEditor'
import { getApiQuizQuestionsInfiniteQueryKey } from '@/hooks/useGetApiQuizQuestionsInfinite'
import type { PagedResponseOfQuizQuestionDto } from '@/api/generated/model'

interface QuizQuestionEditorProps {
  quizId: string
}

/**
 * Main editor for the selected quiz question.
 * Edits are disabled when the quiz is locked by backend attempt history.
 *
 * @param props.quizId - Quiz whose selected question is edited.
 */
export function QuizQuestionEditor({ quizId }: QuizQuestionEditorProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const { activeQuestionId, isLocked } = useQuizEditorUIStore()

  const lastSubmittedTitle = useRef<string | null>(null)

  const { data: question, isLoading } =
    useGetApiQuizzesQuizIdQuestionsQuestionId(
      quizId,
      activeQuestionId?.toString() ?? '',
      { query: { enabled: !!activeQuestionId } },
    )

  const { mutate: updateQuestion } =
    usePatchApiQuizzesQuizIdQuestionsQuestionId({
      mutation: {
        onSuccess: (updatedQuizData) => {
          queryClient.setQueryData(
            getGetApiQuizzesQuizIdQuestionsQuestionIdQueryKey(
              quizId,
              activeQuestionId?.toString() ?? '',
            ),
            updatedQuizData,
          )

          lastSubmittedTitle.current = null

          queryClient.setQueriesData<
            InfiniteData<PagedResponseOfQuizQuestionDto>
          >(
            { queryKey: getApiQuizQuestionsInfiniteQueryKey(quizId) },
            (oldData) => {
              if (!oldData) return oldData

              return {
                ...oldData,
                pages: oldData.pages.map((page) => ({
                  ...page,
                  data: page.data?.map((question) =>
                    question.id === updatedQuizData.id
                      ? { ...question, ...updatedQuizData }
                      : question,
                  ),
                })),
              }
            },
          )

          queryClient.invalidateQueries({
            queryKey: getApiQuizQuestionsInfiniteQueryKey(quizId),
          })
        },
        onError: (error) => handleQuizMutationError(error, t),
      },
    })

  const handleUpdateTitle = useCallback(
    (title: string) => {
      if (!activeQuestionId || isLocked) return

      if (title === question?.title) {
        lastSubmittedTitle.current = null
        return
      }

      if (title === lastSubmittedTitle.current) return

      lastSubmittedTitle.current = title

      updateQuestion({
        quizId,
        questionId: activeQuestionId.toString(),
        data: { title },
      })
    },
    [activeQuestionId, isLocked, question?.title, quizId, updateQuestion],
  )

  if (isLoading) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary/30" />
      </div>
    )
  }

  if (activeQuestionId === null) {
    return <QuizSettingsEditor quizId={quizId} />
  }

  return (
    <div className="flex-1 overflow-y-auto bg-background p-8 lg:p-12">
      <div className="mx-auto max-w-3xl space-y-12">
        <DebouncedInput
          value={question?.title ?? ''}
          label={t('editor.questionText')}
          onChange={handleUpdateTitle}
          placeholder={t('editor.newQuestionDefault')}
          className="text-lg font-semibold"
          disabled={isLocked}
          debounceTime={800}
        />

        <QuestionMediaEditor
          quizId={quizId}
          questionId={activeQuestionId.toString()}
          currentMediaAssetId={question?.mediaAssetId ?? null}
        />

        <div className="grid grid-cols-1 gap-12 rounded-xl border bg-muted/5 p-6 md:grid-cols-2">
          <QuestionDifficultyEditor
            key={`diff-${question?.difficulty}`}
            quizId={quizId}
            questionId={activeQuestionId.toString()}
            currentDifficulty={Number(question?.difficulty ?? 1)}
            hasPenalty={question?.hasPenalty ?? false}
            calculatedPenalty={Number(question?.calculatedPenalty ?? 0)}
          />

          <QuestionTimeLimitEditor
            key={`time-${question?.timeLimit}`}
            quizId={quizId}
            questionId={activeQuestionId.toString()}
            currentTimeLimit={Number(question?.timeLimit ?? 30)}
          />
        </div>

        <QuestionMultipleChoiceEditor
          key={`strict-${question?.isStrictMultipleChoice}`}
          quizId={quizId}
          questionId={activeQuestionId.toString()}
          currentIsStrict={question?.isStrictMultipleChoice ?? false}
        />

        <hr className="border-muted" />

        <QuizOptionsEditor
          quizId={quizId}
          questionId={activeQuestionId.toString()}
        />
      </div>
    </div>
  )
}
