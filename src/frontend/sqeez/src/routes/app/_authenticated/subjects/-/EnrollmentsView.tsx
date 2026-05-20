import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useAuthStore } from '@/store/useAuthStore'
import { SubjectCard } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { Badge } from '@/components/ui/Badge/Badge'
import { BookOpen, Calendar, Plus, GraduationCap, Trash2 } from 'lucide-react'
import { toast } from 'sonner'

import { EnrollSubjectModal } from './EnrollSubjectModal'
import { ConfirmModal } from '@/components/ui'
import {
  useGetApiEnrollments,
  useDeleteApiEnrollmentsId,
} from '@/api/generated/endpoints/enrollments/enrollments'
import { PaginatedListView } from '@/components/layouting/PaginatedListView/PaginatedListView'
import type { EnrollmentDto } from '@/api/generated/model'

/**
 * Current student's subject enrollment list.
 * Owns enrollment search/pagination and opens the subject-code enrollment modal.
 */
export function EnrollmentsView() {
  const { t } = useTranslation()
  const currentUser = useAuthStore((s) => s.user)

  const [isModalOpen, setIsModalOpen] = useState(false)
  const [enrollmentToDelete, setEnrollmentToDelete] = useState<number | null>(
    null,
  )

  const [page, setPage] = useState(1)
  const pageSize = 12
  const UNSUCCESFUL_MARK = 5

  const {
    data: enrollmentsResponse,
    isLoading,
    isFetching,
    refetch,
  } = useGetApiEnrollments(
    {
      StudentId: currentUser?.id,
      IsActive: true,
      IsDescending: true,
      PageNumber: page,
      PageSize: pageSize,
    },
    {
      query: {
        enabled: !!currentUser?.id,
        placeholderData: (previousData) => previousData,
      },
    },
  )

  const deleteMutation = useDeleteApiEnrollmentsId()

  const enrollments = enrollmentsResponse?.data || []
  const totalCount = Number(enrollmentsResponse?.totalCount || 0)
  const totalPages = Number(enrollmentsResponse?.totalPages || 1)

  const handleConfirmDelete = async () => {
    if (!enrollmentToDelete) return

    try {
      await deleteMutation.mutateAsync({ id: enrollmentToDelete })
      toast.success(t('enrollments.unenrollSuccess'))
      refetch()
    } catch (error) {
      console.error(error)
      toast.error(t('enrollments.unenrollError'))
    } finally {
      setEnrollmentToDelete(null)
    }
  }

  return (
    <>
      <PaginatedListView<EnrollmentDto>
        titleNode={
          <>
            <GraduationCap className="h-8 w-8 text-primary" />
            {t('enrollments.title')}
          </>
        }
        headerActions={
          <Button
            size="lg"
            onClick={() => setIsModalOpen(true)}
            className="flex items-center gap-2 shadow-sm"
          >
            <Plus className="h-5 w-5" />
            {t('enrollments.enrollButton')}
          </Button>
        }
        isLoading={isLoading && !enrollmentsResponse}
        isFetching={isFetching}
        items={enrollments}
        totalCount={totalCount}
        pageNumber={page}
        totalPages={totalPages}
        setPageNumber={setPage}
        emptyStateMessage={
          <div className="flex flex-col items-center justify-center gap-2">
            <BookOpen className="mb-4 h-16 w-16 text-muted-foreground/40" />
            <h2 className="text-xl font-semibold text-foreground">
              {t('enrollments.emptyTitle')}
            </h2>
            <p className="max-w-md text-muted-foreground">
              {t('enrollments.emptyDescription')}
            </p>
            <Button
              variant="outline"
              className="mt-4"
              onClick={() => setIsModalOpen(true)}
            >
              <Plus className="mr-2 h-4 w-4" />
              {t('enrollments.enrollButton')}
            </Button>
          </div>
        }
        renderItem={(enrollment) => (
          <SubjectCard
            key={enrollment.id}
            title={enrollment.subjectName || 'Unknown Subject'}
            code={enrollment.subjectCode}
            url="/app/subjects/$subjectId"
            params={{ subjectId: (enrollment.subjectId ?? 0).toString() }}
            borderColorClass="border-l-primary/60"
            badgeColorClass="bg-primary/10 text-primary"
            topRightSlot={
              enrollment.mark !== null && enrollment.mark !== undefined ? (
                <Badge
                  variant={
                    enrollment.mark === UNSUCCESFUL_MARK
                      ? 'destructive'
                      : 'success'
                  }
                  className="shrink-0 text-sm font-bold shadow-sm"
                >
                  {enrollment.mark}
                </Badge>
              ) : (
                <Badge
                  variant="outline"
                  className="shrink-0 text-xs font-normal text-muted-foreground"
                >
                  {t('enrollments.noGrade')}
                </Badge>
              )
            }
            metricsSlot={
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Calendar className="h-4 w-4" />
                <span>
                  {t('enrollments.enrolledOn')}{' '}
                  {new Date(enrollment.enrolledAt).toLocaleDateString()}
                </span>
              </div>
            }
            actionsSlot={
              <Button
                variant="ghost"
                size="icon"
                className="h-8 w-8 text-muted-foreground transition-colors hover:bg-destructive/10 hover:text-destructive"
                onClick={() => setEnrollmentToDelete(Number(enrollment.id))}
                title={t('common.delete')}
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            }
          />
        )}
      />

      <EnrollSubjectModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSuccess={() => refetch()}
      />

      <ConfirmModal
        isOpen={enrollmentToDelete !== null}
        onClose={() => setEnrollmentToDelete(null)}
        onConfirm={handleConfirmDelete}
        title={t('common.confirmAction')}
        description={t('enrollments.confirmDelete')}
        confirmText={t('common.delete')}
        isDestructive={true}
        isLoading={deleteMutation.isPending}
      />
    </>
  )
}
