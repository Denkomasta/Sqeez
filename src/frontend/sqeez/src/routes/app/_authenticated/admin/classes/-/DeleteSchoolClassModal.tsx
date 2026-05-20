import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'

import { useDeleteApiClassesId } from '@/api/generated/endpoints/school-classes/school-classes'
import { getGetApiClassesQueryKey } from '@/api/generated/endpoints/school-classes/school-classes'

import type { SchoolClassDto } from '@/api/generated/model'
import { ConfirmModal } from '@/components/ui'

interface DeleteSchoolClassModalProps {
  isOpen: boolean
  onClose: () => void
  schoolClass: SchoolClassDto | null
}

/**
 * Confirmation modal for deleting a school class.
 * The caller is responsible for invalidating table data after success.
 */
export function DeleteSchoolClassModal({
  isOpen,
  onClose,
  schoolClass,
}: DeleteSchoolClassModalProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const deleteClassMutation = useDeleteApiClassesId({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: getGetApiClassesQueryKey() })
        toast.success(t('admin.classes.classDeleted'))
        onClose()
      },
      onError: () => toast.error(t('common.error')),
    },
  })

  const handleDelete = async () => {
    if (!schoolClass) return

    try {
      await deleteClassMutation.mutateAsync({ id: schoolClass.id.toString() })
      onClose()
    } catch (error) {
      console.error('Failed to delete class:', error)
    }
  }

  return (
    <ConfirmModal
      isOpen={isOpen}
      onClose={onClose}
      onConfirm={handleDelete}
      title={t('admin.classes.deleteClassTitle')}
      description={t('admin.classes.deleteClassDesc', {
        className: schoolClass?.name,
      })}
      confirmText={t('common.delete')}
      isDestructive={true}
      isLoading={deleteClassMutation.isPending}
    />
  )
}
