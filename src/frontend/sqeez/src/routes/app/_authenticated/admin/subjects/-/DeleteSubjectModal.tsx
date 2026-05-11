import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'

import { useDeleteApiSubjectsId } from '@/api/generated/endpoints/subjects/subjects'
import { getGetApiSubjectsQueryKey } from '@/api/generated/endpoints/subjects/subjects'

import type { SubjectDto } from '@/api/generated/model'
import { ConfirmModal } from '@/components/ui'

interface DeleteSubjectModalProps {
  isOpen: boolean
  onClose: () => void
  subject: SubjectDto | null
}

export function DeleteSubjectModal({
  isOpen,
  onClose,
  subject,
}: DeleteSubjectModalProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const deleteSubjectMutation = useDeleteApiSubjectsId({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: getGetApiSubjectsQueryKey() })
        toast.success(t('admin.subjects.subjectDeleted'))
        onClose()
      },
      onError: () => toast.error(t('common.error')),
    },
  })

  const handleDelete = async () => {
    if (!subject) return

    try {
      await deleteSubjectMutation.mutateAsync({ id: subject.id.toString() })
      onClose()
    } catch (error) {
      console.error('Failed to delete subject:', error)
    }
  }

  return (
    <ConfirmModal
      isOpen={isOpen}
      onClose={onClose}
      onConfirm={handleDelete}
      title={t('admin.subjects.deleteSubjectTitle')}
      description={t('admin.subjects.deleteSubjectDesc', {
        subjectName: subject?.name,
      })}
      confirmText={t('common.delete')}
      isDestructive={true}
      isLoading={deleteSubjectMutation.isPending}
    />
  )
}
