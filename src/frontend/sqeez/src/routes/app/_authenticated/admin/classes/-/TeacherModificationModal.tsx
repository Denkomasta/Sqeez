import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'
import { Search } from 'lucide-react'

import { usePatchApiClassesId } from '@/api/generated/endpoints/school-classes/school-classes'
import { getGetApiClassesQueryKey } from '@/api/generated/endpoints/school-classes/school-classes'
import { useGetApiUsersInfinite } from '@/hooks/useGetApiUsersInfinite'

import { BaseModal } from '@/components/ui/Modal'
import { Button, AsyncButton } from '@/components/ui/Button'
import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { ScrollableSelectList } from '@/components/ui/ScrollableSelectList/ScrollableSelectList'
import { formatName } from '@/lib/userHelpers'
import type { SchoolClassDto } from '@/api/generated/model'

interface TeacherModificationModalProps {
  isOpen: boolean
  onClose: () => void
  schoolClass: SchoolClassDto | null
}

/**
 * Modal for assigning or clearing a class teacher.
 * Teacher options are loaded lazily while the modal is open.
 */
export function TeacherModificationModal({
  isOpen,
  onClose,
  schoolClass,
}: TeacherModificationModalProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const [selectedTeacherId, setSelectedTeacherId] = useState<number | ''>(
    schoolClass?.teacherId ? Number(schoolClass.teacherId) : '',
  )
  const [searchTerm, setSearchTerm] = useState('')

  const {
    data: infiniteData,
    isLoading: isLoadingTeachers,
    isFetchingNextPage,
    hasNextPage,
    fetchNextPage,
  } = useGetApiUsersInfinite(
    {
      Role: 'Teacher',
      HasAssignedClass: false,
      ...(searchTerm ? { SearchTerm: searchTerm } : {}),
      PageSize: 20,
    },
    {
      enabled: isOpen,
    },
  )

  const updateClassMutation = usePatchApiClassesId({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: getGetApiClassesQueryKey() })
        toast.success(t('admin.classes.teacherUpdated'))
        handleClose()
      },
      onError: () => toast.error(t('common.error')),
    },
  })

  const handleClose = () => {
    setSearchTerm('')
    setSelectedTeacherId('')
    onClose()
  }

  const handleSave = async () => {
    if (!schoolClass) return

    try {
      await updateClassMutation.mutateAsync({
        id: schoolClass.id.toString(),
        data: {
          teacherId: selectedTeacherId === '' ? 0 : Number(selectedTeacherId),
        },
      })
    } catch (error) {
      console.error('Failed to assign teacher:', error)
    }
  }

  const teachers = infiniteData?.pages.flatMap((page) => page.data || []) || []

  const teacherOptions = [
    {
      id: '',
      title: t('admin.classes.unassigned'),
      subtitle: t('admin.classes.removeTeacher'),
    },
    ...teachers.map((teacher) => ({
      id: Number(teacher.id),
      title: formatName(teacher.firstName, teacher.lastName),
      subtitle: `@${teacher.username}`,
    })),
  ]

  const isSaveDisabled =
    (selectedTeacherId === '' && !schoolClass?.teacherId) ||
    selectedTeacherId === Number(schoolClass?.teacherId)

  return (
    <BaseModal
      isOpen={isOpen}
      onClose={handleClose}
      title={t('admin.classes.assignTeacherTitle')}
      description={t('admin.classes.assignTeacherDesc', {
        className: schoolClass?.name,
      })}
      footer={
        <div className="flex w-full justify-center gap-4 sm:space-x-0">
          <Button
            variant="outline"
            size="lg"
            onClick={handleClose}
            className="min-w-32"
          >
            {t('common.cancel')}
          </Button>
          <AsyncButton
            size="lg"
            onClick={handleSave}
            disabled={isSaveDisabled}
            isLoading={updateClassMutation.isPending}
            loadingText={t('common.saving') + '...'}
            className="min-w-32"
          >
            {t('common.save')}
          </AsyncButton>
        </div>
      }
    >
      <div className="flex flex-col gap-5 py-4">
        <div className="flex flex-col gap-3">
          <DebouncedInput
            id="teacher-search"
            value={searchTerm}
            onChange={setSearchTerm}
            placeholder={t('admin.classes.searchTeachersPlaceholder')}
            icon={<Search className="size-4" />}
            label={t('common.search')}
            hideErrors
          />
        </div>

        <div className="flex flex-col">
          <label className="mb-2 block text-sm font-medium">
            {t('admin.classes.selectTeacher')}
          </label>

          <ScrollableSelectList
            options={teacherOptions}
            selectedId={selectedTeacherId}
            onSelect={(id) => setSelectedTeacherId(id === '' ? '' : Number(id))}
            isLoading={isLoadingTeachers}
            loadingText={`${t('common.loading')}...`}
            emptyText={t('common.noResults')}
            hasMore={!!hasNextPage}
            isFetchingNextPage={isFetchingNextPage}
            onLoadMore={() => fetchNextPage()}
            maxHeight="max-h-60"
          />
        </div>
      </div>
    </BaseModal>
  )
}
