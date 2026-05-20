import { useState } from 'react'
import { useNavigate } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { BaseModal } from '@/components/ui/Modal'
import { AsyncButton, Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { usePostApiSubjectsSubjectIdQuizzes } from '@/api/generated/endpoints/subjects/subjects'
import { TextArea } from '@/components/ui/TextArea'

interface CreateQuizModalProps {
  isOpen: boolean
  onClose: () => void
  subjectId: string | number
}

/**
 * Teacher modal for creating a quiz under a selected subject.
 * Subject selection is limited to subjects owned by the teacher.
 */
export function CreateQuizModal({
  isOpen,
  onClose,
  subjectId,
}: CreateQuizModalProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()

  const [newQuizTitle, setNewQuizTitle] = useState('')
  const [newQuizDescription, setNewQuizDescription] = useState('')

  const createQuizMutation = usePostApiSubjectsSubjectIdQuizzes()

  const handleClose = () => {
    setNewQuizTitle('')
    setNewQuizDescription('')
    onClose()
  }

  const handleCreateQuiz = async () => {
    if (!newQuizTitle.trim() || !newQuizDescription.trim()) return

    try {
      const response = await createQuizMutation.mutateAsync({
        subjectId,
        data: {
          title: newQuizTitle.trim(),
          description: newQuizDescription.trim(),
          subjectId: Number(subjectId),
        },
      })

      if (response.id) {
        toast.success(t('dashboard.quizCreatedSuccess'))
        handleClose()

        navigate({
          to: '/app/quizzes/$quizId/builder',
          params: { quizId: response.id.toString() },
        })
      }
    } catch (error) {
      console.error(error)
      toast.error(t('common.error'))
    }
  }

  return (
    <BaseModal
      isOpen={isOpen}
      onClose={handleClose}
      title={t('dashboard.createQuizTitle')}
      description={t('dashboard.createQuizDescription')}
      footer={
        <div className="flex w-full justify-end gap-2">
          <Button variant="outline" onClick={handleClose}>
            {t('common.cancel')}
          </Button>
          <AsyncButton
            onClick={handleCreateQuiz}
            disabled={
              !newQuizTitle.trim() ||
              !newQuizDescription.trim() ||
              createQuizMutation.isPending
            }
            loadingText={t('common.saving')}
          >
            {t('common.confirm')}
          </AsyncButton>
        </div>
      }
    >
      <div className="flex flex-col gap-4 py-4">
        <Input
          value={newQuizTitle}
          label={t('dashboard.quizNameLabel')}
          onChange={(e) => setNewQuizTitle(e.target.value)}
          placeholder={t('dashboard.quizNamePlaceholder')}
          autoFocus
        />

        <TextArea
          value={newQuizDescription}
          label={t('dashboard.quizDescriptionLabel')}
          onChange={(e) => setNewQuizDescription(e.target.value)}
          placeholder={t('dashboard.quizDescriptionPlaceholder')}
          className="flex min-h-20 w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:ring-1 focus-visible:ring-ring focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50"
          onKeyDown={(e) => {
            if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
              if (newQuizTitle.trim() && newQuizDescription.trim()) {
                handleCreateQuiz()
              }
            }
          }}
          hideErrors
        />
      </div>
    </BaseModal>
  )
}
