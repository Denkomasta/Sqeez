import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'
import { Search } from 'lucide-react'

import {
  getGetApiClassesIdQueryKey,
  usePostApiClassesIdStudents,
} from '@/api/generated/endpoints/school-classes/school-classes'
import { useGetApiUsersInfinite } from '@/hooks/useGetApiUsersInfinite'

import { BaseModal } from '@/components/ui/Modal'
import { Button, AsyncButton } from '@/components/ui/Button'
import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { ScrollableSelectList } from '@/components/ui/ScrollableSelectList/ScrollableSelectList'
import { formatName } from '@/lib/userHelpers'
import type { SchoolClassDetailDto } from '@/api/generated/model'
import { getGetApiUsersQueryKey } from '@/api/generated/endpoints/user/user'

interface AddStudentToClassModalProps {
  isOpen: boolean
  onClose: () => void
  schoolClass?: SchoolClassDetailDto
}

export function AddStudentToClassModal({
  isOpen,
  onClose,
  schoolClass,
}: AddStudentToClassModalProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const [selectedStudentId, setSelectedStudentId] = useState<number | ''>('')
  const [searchTerm, setSearchTerm] = useState('')

  const {
    data: infiniteData,
    isLoading: isLoadingStudents,
    isFetchingNextPage,
    hasNextPage,
    fetchNextPage,
  } = useGetApiUsersInfinite(
    {
      Role: 'Student',
      ...(searchTerm ? { SearchTerm: searchTerm } : {}),
      PageSize: 20,
    },
    {
      enabled: isOpen,
    },
  )

  const addStudentMutation = usePostApiClassesIdStudents({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: getGetApiClassesIdQueryKey(Number(schoolClass?.id)),
        })
        queryClient.invalidateQueries({ queryKey: getGetApiUsersQueryKey() })

        toast.success(t('admin.class.studentAdded'))
        handleClose()
      },
      onError: () => toast.error(t('common.error')),
    },
  })

  const handleClose = () => {
    setSelectedStudentId('')
    setSearchTerm('')
    onClose()
  }

  const handleAdd = async () => {
    if (!schoolClass || !selectedStudentId) return

    try {
      if (!schoolClass.id) {
        console.error('School class ID is missing')
        return
      }

      await addStudentMutation.mutateAsync({
        id: schoolClass.id.toString(),
        data: {
          studentIds: [selectedStudentId],
        },
      })
    } catch (error) {
      console.error('Failed to add student:', error)
    }
  }

  const students = infiniteData?.pages.flatMap((page) => page.data || []) || []
  const studentOptions = students.map((student) => ({
    id: Number(student.id),
    title: formatName(student.firstName, student.lastName),
    subtitle: `@${student.username}`,
  }))

  return (
    <BaseModal
      isOpen={isOpen}
      onClose={handleClose}
      title={t('admin.classes.addStudentTitle')}
      description={t('admin.classes.addStudentDesc', {
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
            onClick={handleAdd}
            disabled={!selectedStudentId}
            // isLoading={addStudentMutation.isPending}
            loadingText={t('common.saving') + '...'}
            className="min-w-32"
          >
            {t('common.add')}
          </AsyncButton>
        </div>
      }
    >
      <div className="flex flex-col gap-5 py-4">
        <DebouncedInput
          id="student-search"
          value={searchTerm}
          onChange={setSearchTerm}
          placeholder={t('admin.classes.searchStudentsPlaceholder')}
          icon={<Search className="size-4" />}
          hideErrors
        />

        <div className="flex flex-col">
          <label className="mb-2 block text-sm font-medium">
            {t('admin.classes.selectStudent')}
          </label>
          <ScrollableSelectList
            options={studentOptions}
            selectedId={selectedStudentId}
            onSelect={(id) => setSelectedStudentId(id === '' ? '' : Number(id))}
            isLoading={isLoadingStudents}
            loadingText={`${t('common.loading')}...`}
            emptyText={t('common.noResults')}
            hasMore={!!hasNextPage}
            isFetchingNextPage={isFetchingNextPage}
            onLoadMore={() => fetchNextPage()}
            maxHeight="max-h-65"
          />
        </div>
      </div>
    </BaseModal>
  )
}
