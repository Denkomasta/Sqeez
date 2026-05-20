import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from '@tanstack/react-router'
import { BookCopy, ArrowLeft, Search } from 'lucide-react'

import {
  getGetApiSubjectsIdQueryKey,
  useDeleteApiSubjectsSubjectIdEnrollments,
  useGetApiSubjectsId,
} from '@/api/generated/endpoints/subjects/subjects'
import {
  getGetApiUsersQueryKey,
  useGetApiUsers,
} from '@/api/generated/endpoints/user/user'

import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { Pagination } from '@/components/ui/Pagination'
import { Badge } from '@/components/ui/Badge/Badge'
import { SubjectStudentsTable } from './SubjectStudentsTable'
import { useAuthStore } from '@/store/useAuthStore'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import type { StudentDto } from '@/api/generated/model'
import { ConfirmModal } from '@/components/ui'
import { formatName } from '@/lib/userHelpers'
import { StudentGradingModal } from './StudentGradingModal'
import { PageLayout } from '@/components/layouting/PageLayout/PageLayout'

/**
 * Student-facing subject detail page.
 * Loads subject metadata and enrollment-specific student information for the route subject.
 */
export function SubjectDetailsPage({
  subjectId,
}: {
  subjectId: string | number
}) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const { isAdmin } = useAuthStore()

  const [searchQuery, setSearchQuery] = useState('')
  const [pageNumber, setPageNumber] = useState(1)
  const [studentToRemove, setStudentToRemove] = useState<StudentDto | null>(
    null,
  )
  const [studentToGrade, setStudentToGrade] = useState<StudentDto | null>(null)
  const pageSize = 15

  const { data: subjectData, isLoading: isLoadingSubject } =
    useGetApiSubjectsId(Number(subjectId), { query: { enabled: !!subjectId } })

  const {
    data: studentsResponse,
    isLoading: isLoadingStudents,
    isFetching: isFetchingStudents,
  } = useGetApiUsers(
    {
      Role: 'Student',
      SubjectId: Number(subjectId),
      SearchTerm: searchQuery || undefined,
      PageNumber: pageNumber,
      PageSize: pageSize,
    },
    { query: { placeholderData: (prev) => prev } },
  )

  const removeStudentMutation = useDeleteApiSubjectsSubjectIdEnrollments({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: getGetApiUsersQueryKey() })
        queryClient.invalidateQueries({
          queryKey: getGetApiSubjectsIdQueryKey(Number(subjectId)),
        })
        toast.success(t('subject.studentRemoved'))
        setStudentToRemove(null)
      },
      onError: () => toast.error(t('common.error')),
    },
  })

  const handleRemoveConfirm = async () => {
    if (!studentToRemove?.id) return
    try {
      await removeStudentMutation.mutateAsync({
        subjectId: Number(subjectId),
        data: { studentIds: [studentToRemove.id] },
      })
    } catch (error) {
      console.error('Failed to remove student:', error)
    }
  }

  const canEditEnrollments =
    subjectData?.teacherId === useAuthStore.getState().user?.id
  const canRemoveStudents = isAdmin
  const isLoading = isLoadingSubject || (isLoadingStudents && !studentsResponse)

  const students = studentsResponse?.data || []
  const totalPages = Number(studentsResponse?.totalPages || 1)
  const totalCount = studentsResponse?.totalCount || 0

  const backPath = isAdmin
    ? '/app/admin/subjects'
    : canEditEnrollments
      ? '/app/teacher/subjects'
      : '/app/subjects'

  const ControlsNode = (
    <div className="flex w-full flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <DebouncedInput
        id="subject-student-search"
        value={searchQuery}
        onChange={(val) => {
          setSearchQuery(val)
          setPageNumber(1)
        }}
        placeholder={t('admin.searchStudents')}
        icon={<Search className="h-4 w-4" />}
        className="w-full bg-background sm:max-w-xs"
        hideErrors
      />
      <div className="text-sm font-medium whitespace-nowrap text-muted-foreground">
        {t('common.totalCount', {
          count: Number(totalCount),
          defaultValue: `Total: ${Number(totalCount)}`,
        })}
      </div>
    </div>
  )

  return (
    <>
      <PageLayout
        variant="app"
        containerClassName="max-w-7xl"
        isLoading={isLoading}
        title={
          <div className="flex flex-col items-start gap-4">
            <Link
              to={backPath}
              className="flex w-fit items-center gap-2 text-sm font-normal text-muted-foreground transition-colors hover:text-foreground"
            >
              <ArrowLeft className="h-4 w-4" />
              {t('subject.backToSubjects')}
            </Link>
            <div className="flex items-center gap-3">
              <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-nav-cyan/10 text-nav-cyan">
                <BookCopy className="h-6 w-6" />
              </div>
              <span>{subjectData?.name}</span>
            </div>
          </div>
        }
        titleBadge={
          <Badge variant="secondary" className="mt-8 uppercase shadow-sm">
            {subjectData?.code}
          </Badge>
        }
        subtitle={
          <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-muted-foreground">
            <span>
              {t('common.teacher')}:{' '}
              <strong className="text-foreground">
                {subjectData?.teacherName || t('admin.unassigned')}
              </strong>
            </span>
            <span>
              {t('admin.schoolClass')}:{' '}
              <strong className="text-foreground">
                {subjectData?.schoolClassName || t('admin.unassigned')}
              </strong>
            </span>
          </div>
        }
        headerControls={ControlsNode}
      >
        <div
          className={`transition-opacity duration-200 ${isFetchingStudents && !isLoadingStudents ? 'opacity-50' : 'opacity-100'}`}
        >
          <SubjectStudentsTable
            students={students}
            isLoading={isLoadingStudents && !studentsResponse}
            onRemoveStudent={canRemoveStudents ? setStudentToRemove : undefined}
            onEditMark={canEditEnrollments ? setStudentToGrade : undefined}
          />

          {totalPages > 1 && (
            <div className="mt-8 flex justify-center">
              <Pagination
                currentPage={pageNumber}
                totalPages={totalPages}
                onPageChange={setPageNumber}
              />
            </div>
          )}
        </div>
      </PageLayout>

      <ConfirmModal
        isOpen={!!studentToRemove}
        onClose={() => setStudentToRemove(null)}
        onConfirm={handleRemoveConfirm}
        title={t('subject.removeFromSubject')}
        description={t('subject.removeConfirmDesc', {
          studentName: studentToRemove
            ? formatName(studentToRemove.firstName, studentToRemove.lastName)
            : '',
        })}
        confirmText={t('common.remove')}
        isDestructive={true}
        isLoading={removeStudentMutation.isPending}
      />

      <StudentGradingModal
        isOpen={!!studentToGrade}
        onClose={() => setStudentToGrade(null)}
        student={studentToGrade}
        subjectId={Number(subjectId)}
      />
    </>
  )
}
