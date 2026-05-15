import { useTranslation } from 'react-i18next'
import { Plus, Loader2, List, Trash2, Settings } from 'lucide-react'
import { useQuizEditorUIStore } from '@/store/useQuizEditorUIStore'
import {
  useDeleteApiQuizzesQuizIdQuestionsQuestionId,
  useGetApiQuizzesQuizIdQuestions,
  usePostApiQuizzesQuizIdQuestions,
} from '@/api/generated/endpoints/quizzes/quizzes'
import { Button } from '@/components/ui/Button'
import { cn } from '@/lib/utils'
import { CollapsibleSidebar } from '@/components/ui/CollapsibleSidebar'
import { handleQuizMutationError } from '@/lib/quizHelpers'
import { useState } from 'react'
import { ConfirmModal } from '@/components/ui'

interface QuizEditorSidebarProps {
  quizId: string
}

export function QuizEditorSidebar({ quizId }: QuizEditorSidebarProps) {
  const { t } = useTranslation()
  const { activeQuestionId, actions, isLocked } = useQuizEditorUIStore()

  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false)
  const [questionToDelete, setQuestionToDelete] = useState<
    number | string | null
  >(null)

  const {
    data: pagedResponse,
    isLoading,
    refetch,
  } = useGetApiQuizzesQuizIdQuestions(quizId)

  const questions = pagedResponse?.data ?? []
  const createQuestion = usePostApiQuizzesQuizIdQuestions({
    mutation: {
      onError: (error) => handleQuizMutationError(error, t),
    },
  })
  const deleteQuestion = useDeleteApiQuizzesQuizIdQuestionsQuestionId({
    mutation: {
      onError: (error) => handleQuizMutationError(error, t),
    },
  })

  const handleAddQuestion = async () => {
    const response = await createQuestion.mutateAsync({
      quizId,
      data: {
        title: t('editor.newQuestionDefault'),
        quizId,
        difficulty: 1,
        mediaAssetId: null,
        timeLimit: 30,
      },
    })

    await refetch()
    if (response.id) {
      actions.selectQuestion(Number(response.id))
    }
  }

  const handleDeleteClick = (e: React.MouseEvent, qId: number | string) => {
    e.stopPropagation()
    setQuestionToDelete(qId)
    setIsDeleteModalOpen(true)
  }

  const handleConfirmDelete = async () => {
    if (!questionToDelete) return

    await deleteQuestion.mutateAsync({
      quizId,
      questionId: questionToDelete.toString(),
    })

    if (activeQuestionId === questionToDelete) {
      actions.selectQuestion(null)
    }

    await refetch()
    setIsDeleteModalOpen(false)
    setQuestionToDelete(null)
  }

  return (
    <>
      <CollapsibleSidebar
        title={t('editor.questionsTitle')}
        icon={<List className="h-4 w-4 text-primary" />}
        expandTooltip={t('editor.showSidebar')}
        collapseTooltip={t('editor.hideSidebar')}
        actions={
          <Button
            variant="outline"
            size="sm"
            className="h-8 w-8 p-0"
            onClick={handleAddQuestion}
            disabled={createQuestion.isPending || isLocked}
            title={t('editor.addQuestion')}
          >
            {createQuestion.isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Plus className="h-4 w-4" />
            )}
          </Button>
        }
      >
        <div className="mb-4">
          <button
            onClick={() => actions.selectQuestion(null)}
            className={cn(
              'flex w-full items-center gap-3 rounded-xl border p-3 text-left transition-all',
              activeQuestionId === null
                ? 'border-primary bg-primary text-primary-foreground shadow-lg'
                : 'border-transparent bg-background text-foreground hover:border-muted-foreground/20 hover:bg-muted/50',
            )}
          >
            <div
              className={cn(
                'flex h-7 w-7 shrink-0 items-center justify-center rounded-lg border text-xs font-black',
                activeQuestionId === null
                  ? 'border-primary-foreground/40 bg-primary-foreground/20'
                  : 'bg-muted text-muted-foreground',
              )}
            >
              <Settings className="h-4 w-4" />
            </div>
            <div className="flex flex-1 flex-col truncate pr-6">
              <span className="truncate text-sm font-semibold">
                {t('editor.quizSettings')}
              </span>
            </div>
          </button>
        </div>

        <div className="my-2 h-px bg-border/50" />
        <h3 className="mb-2 px-2 text-xs font-semibold tracking-wider text-muted-foreground uppercase">
          {t('editor.questionsListTitle')}
        </h3>

        {isLoading ? (
          <div className="flex flex-col items-center gap-2 py-10 opacity-50">
            <Loader2 className="h-5 w-5 animate-spin text-primary" />
            <span>{t('common.loading')}</span>
          </div>
        ) : questions.length === 0 ? (
          <div className="rounded-xl border-2 border-dashed border-muted/50 px-6 py-12 text-center">
            <p className="text-xs font-medium text-muted-foreground">
              {t('editor.noQuestionsYet')}
            </p>
          </div>
        ) : (
          questions.map((q, index) => (
            <div key={q.id} className="group relative mb-2 w-full last:mb-0">
              <button
                onClick={() => actions.selectQuestion(Number(q.id))}
                className={cn(
                  'flex w-full items-center gap-3 rounded-xl border p-3 text-left transition-all',
                  activeQuestionId === q.id
                    ? 'border-primary bg-primary text-primary-foreground shadow-lg'
                    : 'border-transparent bg-background text-foreground hover:border-muted-foreground/20 hover:bg-muted/50',
                )}
              >
                <div
                  className={cn(
                    'flex h-7 w-7 shrink-0 items-center justify-center rounded-lg border text-xs font-black',
                    activeQuestionId === q.id
                      ? 'border-primary-foreground/40 bg-primary-foreground/20'
                      : 'bg-muted text-muted-foreground',
                  )}
                >
                  {index + 1}
                </div>

                <div className="flex flex-1 flex-col truncate pr-6">
                  <span className="truncate text-sm font-semibold">
                    {q.title || t('editor.untitledQuestion')}
                  </span>
                </div>
              </button>

              <Button
                variant="ghost"
                size="icon"
                className={cn(
                  'absolute top-1/2 right-2 h-7 w-7 -translate-y-1/2 opacity-0 transition-opacity group-hover:opacity-100',
                  activeQuestionId === q.id
                    ? 'text-primary-foreground hover:bg-primary-foreground/20 hover:text-primary-foreground'
                    : 'text-muted-foreground hover:bg-destructive/10 hover:text-destructive',
                )}
                onClick={(e) => handleDeleteClick(e, q.id)}
                aria-label={t('common.delete')}
                disabled={deleteQuestion.isPending || isLocked}
              >
                {deleteQuestion.isPending ? (
                  <Loader2 className="h-3 w-3 animate-spin" />
                ) : (
                  <Trash2 className="h-3 w-3" />
                )}
              </Button>
            </div>
          ))
        )}
      </CollapsibleSidebar>
      <ConfirmModal
        isOpen={isDeleteModalOpen}
        onClose={() => {
          setIsDeleteModalOpen(false)
          setQuestionToDelete(null)
        }}
        onConfirm={handleConfirmDelete}
        title={t('editor.deleteQuestionTitle')}
        description={t('editor.deleteQuestionDesc')}
        isDestructive={true}
        confirmText={t('common.delete')}
      />
    </>
  )
}
