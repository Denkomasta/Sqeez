import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { GraduationCap, Trash2, Loader2 } from 'lucide-react'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'

import { Button } from '@/components/ui/Button'
import {
  useGetApiEnrollments,
  usePatchApiEnrollmentsId,
  getGetApiEnrollmentsQueryKey,
} from '@/api/generated/endpoints/enrollments/enrollments'
import type { StudentDto } from '@/api/generated/model'
import { formatName } from '@/lib/userHelpers'
import { BaseModal } from '@/components/ui'

interface StudentGradingModalProps {
  isOpen: boolean
  onClose: () => void
  student: StudentDto | null
  subjectId: number
}

/**
 * Teacher modal for assigning a final mark to one student enrollment.
 * The enrollment is fetched only while the modal is open.
 */
export function StudentGradingModal({
  isOpen,
  onClose,
  student,
  subjectId,
}: StudentGradingModalProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const [selectedMark, setSelectedMark] = useState<number | null | undefined>(
    undefined,
  )

  const { data: enrollmentData, isLoading: isFetchingEnrollment } =
    useGetApiEnrollments(
      { SubjectId: subjectId, StudentId: student?.id },
      { query: { enabled: isOpen && !!student?.id } },
    )

  const enrollment = enrollmentData?.data?.[0]
  const currentMark = enrollment?.mark ?? null
  const enrollmentId = enrollment?.id

  const displayMark =
    selectedMark !== undefined
      ? selectedMark
      : currentMark != null
        ? Number(currentMark)
        : null

  const handleClose = () => {
    setSelectedMark(undefined)
    onClose()
  }

  const patchMarkMutation = usePatchApiEnrollmentsId({
    mutation: {
      onSuccess: () => {
        toast.success(t('subject.markUpdated'))
        queryClient.invalidateQueries({
          queryKey: getGetApiEnrollmentsQueryKey(),
        })
        handleClose()
      },
      onError: () => toast.error(t('common.error')),
    },
  })

  const handleSave = async () => {
    if (!enrollmentId) return

    await patchMarkMutation.mutateAsync({
      id: enrollmentId,
      data: {
        mark: displayMark !== null ? displayMark : undefined,
        removeMark: displayMark === null ? true : undefined,
      },
    })
  }

  const studentName = student
    ? formatName(student.firstName, student.lastName)
    : ''

  return (
    <BaseModal
      isOpen={isOpen}
      onClose={handleClose}
      title={t('subject.gradeStudent')}
    >
      <div className="flex flex-col gap-6 py-4">
        {isFetchingEnrollment ? (
          <div className="flex flex-col items-center justify-center py-8 text-muted-foreground">
            <Loader2 className="mb-4 h-8 w-8 animate-spin" />
            <p>{t('common.loading')}</p>
          </div>
        ) : !enrollmentId ? (
          <div className="py-8 text-center text-destructive">
            <p>{t('subject.enrollmentNotFound')}</p>
          </div>
        ) : (
          <>
            <div>
              <p className="text-sm text-muted-foreground">
                {t('subject.settingMarkFor')}{' '}
                <strong className="text-foreground">{studentName}</strong>
              </p>
            </div>

            <div className="flex justify-between gap-2">
              {[1, 2, 3, 4, 5].map((grade) => (
                <button
                  key={grade}
                  onClick={() => setSelectedMark(grade)}
                  disabled={patchMarkMutation.isPending}
                  className={`flex h-14 flex-1 items-center justify-center rounded-xl border-2 text-lg font-bold transition-all disabled:opacity-50 ${
                    displayMark === grade
                      ? 'border-primary bg-primary/10 text-primary'
                      : 'border-border bg-card hover:border-primary/50 hover:bg-muted'
                  }`}
                >
                  {grade}
                </button>
              ))}
            </div>

            <div className="flex justify-between gap-3 border-t border-border pt-4">
              <Button
                variant="destructive"
                className="gap-2 bg-destructive/10 text-destructive hover:bg-destructive hover:text-destructive-foreground"
                onClick={() => setSelectedMark(null)}
                disabled={patchMarkMutation.isPending || displayMark === null}
              >
                <Trash2 className="h-4 w-4" />
                {t('subject.clearMark')}
              </Button>

              <div className="flex gap-3">
                <Button
                  variant="secondary"
                  onClick={handleClose}
                  disabled={patchMarkMutation.isPending}
                >
                  {t('common.cancel')}
                </Button>
                <Button
                  onClick={handleSave}
                  disabled={
                    patchMarkMutation.isPending || displayMark === currentMark
                  }
                  className="gap-2"
                >
                  {patchMarkMutation.isPending ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    <GraduationCap className="h-4 w-4" />
                  )}
                  {t('common.save')}
                </Button>
              </div>
            </div>
          </>
        )}
      </div>
    </BaseModal>
  )
}
