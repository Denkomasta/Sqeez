import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { BaseModal } from '@/components/ui/Modal'
import { AsyncButton, Button } from '@/components/ui/Button'
import { toast } from 'sonner'
import { useAuthStore } from '@/store/useAuthStore'
import { usePostApiSubjectsSubjectIdEnrollments as useEnrollToSubject } from '@/api/generated/endpoints/subjects/subjects'
import { ScrollableSelectList } from '@/components/ui/ScrollableSelectList/ScrollableSelectList'
import { useGetApiSubjectsInfinite } from '@/hooks/useGetApiSubjectsInfinite'
import { Search } from 'lucide-react'
import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'

interface EnrollSubjectModalProps {
  isOpen: boolean
  onClose: () => void
  onSuccess: () => void
}

/**
 * Student subject enrollment modal.
 * Uses the subject code entered by the student and refreshes enrollments after success.
 */
export function EnrollSubjectModal({
  isOpen,
  onClose,
  onSuccess,
}: EnrollSubjectModalProps) {
  const { t } = useTranslation()
  const currentUser = useAuthStore((s) => s.user)

  const [selectedSubjectId, setSelectedSubjectId] = useState<number | ''>('')
  const [isActive, setIsActive] = useState(true)
  const [searchTerm, setSearchTerm] = useState('')

  const {
    data: infiniteData,
    isLoading: isLoadingSubjects,
    isFetchingNextPage,
    hasNextPage,
    fetchNextPage,
  } = useGetApiSubjectsInfinite(
    {
      IsActive: isActive ? isActive : undefined,
      StudentId: currentUser?.id ? currentUser.id : undefined,
      ...(searchTerm ? { SearchTerm: searchTerm } : {}),
    },
    {
      enabled: isOpen,
    },
  )

  const enrollMutation = useEnrollToSubject()

  const handleClose = () => {
    setSelectedSubjectId('')
    setSearchTerm('')
    setIsActive(true)
    onClose()
  }

  const handleEnroll = async () => {
    if (!currentUser?.id || !selectedSubjectId) return

    try {
      const result = await enrollMutation.mutateAsync({
        subjectId: selectedSubjectId,
        data: { studentIds: [currentUser.id] },
      })

      if (result.newlyEnrolledIds?.includes(currentUser.id)) {
        toast.success(t('enrollments.enrollSuccess'))
        onSuccess()
      } else if (result.alreadyEnrolledIds?.includes(currentUser.id)) {
        toast.info(t('enrollments.enrollAlreadyEnrolled'))
      } else {
        toast.error(t('enrollments.enrollError'))
      }

      handleClose()
    } catch (error) {
      console.error(error)
      toast.error(t('enrollments.enrollError'))
    }
  }

  const subjects = infiniteData?.pages.flatMap((page) => page.data || []) || []

  const subjectOptions = subjects.map((subject) => ({
    id: subject.id,
    title: subject.code,
    subtitle: subject.name,
  }))

  return (
    <BaseModal
      isOpen={isOpen}
      onClose={handleClose}
      title={t('enrollments.enrollNew')}
      description={t('enrollments.enrollDescription')}
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
            onClick={handleEnroll}
            disabled={!selectedSubjectId}
            loadingText={t('common.saving') + '...'}
            className="min-w-32"
          >
            {t('enrollments.confirmEnroll')}
          </AsyncButton>
        </div>
      }
    >
      <div className="flex flex-col gap-5">
        <div className="flex flex-col gap-3">
          <div>
            <DebouncedInput
              id="subject-search"
              value={searchTerm}
              onChange={setSearchTerm}
              placeholder={t('enrollments.searchPlaceholder')}
              icon={<Search className="size-4" />}
              label={t('common.search')}
            />
          </div>

          <div className="flex items-center gap-2">
            <input
              id="subject-active"
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              className="h-4 w-4 cursor-pointer rounded border-primary text-primary focus:ring-primary"
            />
            <label
              htmlFor="subject-active"
              className="cursor-pointer text-sm font-medium select-none"
            >
              {t('enrollments.onlyActive')}
            </label>
          </div>
        </div>

        <div className="flex flex-col">
          <label className="mb-2 block text-sm font-medium">
            {t('enrollments.selectSubject')}
          </label>

          <ScrollableSelectList
            options={subjectOptions}
            selectedId={selectedSubjectId}
            onSelect={(id) => setSelectedSubjectId(Number(id))}
            isLoading={isLoadingSubjects}
            loadingText={`${t('common.loading')}...`}
            emptyText={t('common.noResults')}
            hasMore={hasNextPage}
            isFetchingNextPage={isFetchingNextPage}
            onLoadMore={() => fetchNextPage()}
          />
        </div>
      </div>
    </BaseModal>
  )
}
