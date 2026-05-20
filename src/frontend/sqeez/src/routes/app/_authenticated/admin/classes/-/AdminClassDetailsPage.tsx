import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from '@tanstack/react-router'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'
import { School, ArrowLeft, UserPlus, Search } from 'lucide-react'

import {
  getGetApiUsersQueryKey,
  useGetApiUsers,
} from '@/api/generated/endpoints/user/user'
import {
  getGetApiClassesIdQueryKey,
  usePostApiClassesIdStudentsRemove,
} from '@/api/generated/endpoints/school-classes/school-classes'

import { Button } from '@/components/ui/Button'
import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { Pagination } from '@/components/ui/Pagination'
import { PageLayout } from '@/components/layouting/PageLayout/PageLayout'
import { AdminClassStudentsTable } from './AdminClassStudentsTable'
import { AddStudentToClassModal } from './AddStudentToClassModal'
import type { StudentDto } from '@/api/generated/model'
import { useGetApiClassesId } from '@/api/generated/endpoints/school-classes/school-classes'
import { formatName } from '@/lib/userHelpers'
import { ConfirmModal } from '@/components/ui'

/**
 * Admin detail page for one school class.
 * Loads class metadata and student membership, then coordinates add/remove modals.
 */
export function AdminClassDetailsPage({
  classId,
}: {
  classId: string | number
}) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const [searchQuery, setSearchQuery] = useState('')
  const [pageNumber, setPageNumber] = useState(1)
  const pageSize = 15

  const [isAddModalOpen, setIsAddModalOpen] = useState(false)
  const [studentToRemove, setStudentToRemove] = useState<StudentDto | null>(
    null,
  )

  const { data: classData, isLoading: isLoadingClass } = useGetApiClassesId(
    Number(classId),
  )

  const { data: studentsResponse, isLoading: isLoadingStudents } =
    useGetApiUsers({
      Role: 'Student',
      SchoolClassId: Number(classId),
      SearchTerm: searchQuery || undefined,
      PageNumber: pageNumber,
      PageSize: pageSize,
    })

  const students = studentsResponse?.data || []
  const totalPages = Number(studentsResponse?.totalPages || 1)
  const totalCount = studentsResponse?.totalCount || 0

  const removeStudentMutation = usePostApiClassesIdStudentsRemove({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: getGetApiUsersQueryKey() })
        queryClient.invalidateQueries({
          queryKey: getGetApiClassesIdQueryKey(Number(classData?.id)),
        })

        toast.success(t('admin.class.studentRemoved'))
        setStudentToRemove(null)
      },
      onError: () => toast.error(t('common.error')),
    },
  })

  const handleRemoveConfirm = async () => {
    if (!studentToRemove?.id) return
    try {
      await removeStudentMutation.mutateAsync({
        id: classId,
        data: {
          studentIds: [studentToRemove.id],
        },
      })
    } catch (error) {
      console.error('Failed to remove student:', error)
    }
  }

  return (
    <>
      <PageLayout
        variant="app"
        containerClassName="max-w-7xl"
        title={
          <>
            <span className="flex h-16 w-16 items-center justify-center rounded-xl bg-primary/10 text-primary">
              <School className="h-8 w-8" />
            </span>
            {classData?.name}
          </>
        }
        subtitle={
          <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-muted-foreground">
            <span>
              {t('admin.academicYear')}:{' '}
              <strong className="text-foreground">
                {classData?.academicYear}
              </strong>
            </span>
            <span>
              {t('admin.section')}:{' '}
              <strong className="text-foreground">{classData?.section}</strong>
            </span>
            <span>
              {t('common.teacher')}:{' '}
              <strong className="text-foreground">
                {formatName(
                  classData?.teacher?.firstName,
                  classData?.teacher?.lastName,
                ) || t('admin.unassigned')}
              </strong>
            </span>
          </div>
        }
        headerActions={
          <Button onClick={() => setIsAddModalOpen(true)} className="gap-2">
            <UserPlus className="h-4 w-4" />
            {t('admin.class.addStudent')}
          </Button>
        }
        headerControls={
          <div className="flex flex-col gap-4">
            <Link
              to="/app/admin/classes"
              className="flex w-fit items-center gap-2 text-sm text-muted-foreground hover:text-foreground"
            >
              <ArrowLeft className="h-4 w-4" />
              {t('admin.class.backToClasses')}
            </Link>
            <DebouncedInput
              id="class-student-search"
              value={searchQuery}
              onChange={(val) => {
                setSearchQuery(val)
                setPageNumber(1)
              }}
              placeholder={t('admin.searchStudents')}
              icon={<Search className="h-4 w-4" />}
              className="max-w-md bg-background"
              hideErrors
            />
          </div>
        }
      >
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold">
            {t('admin.class.enrolledStudents')}
          </h2>
          <span className="text-sm text-muted-foreground">
            {totalCount} {t('common.students')}
          </span>
        </div>

        <AdminClassStudentsTable
          students={students}
          isLoading={isLoadingStudents || isLoadingClass}
          onRemoveStudent={setStudentToRemove}
        />

        {!isLoadingStudents && !isLoadingClass && totalPages > 1 && (
          <div className="mt-6 flex justify-center">
            <Pagination
              currentPage={pageNumber}
              totalPages={totalPages}
              onPageChange={setPageNumber}
            />
          </div>
        )}
      </PageLayout>

      <AddStudentToClassModal
        isOpen={isAddModalOpen}
        onClose={() => setIsAddModalOpen(false)}
        schoolClass={classData}
      />

      <ConfirmModal
        isOpen={!!studentToRemove}
        onClose={() => setStudentToRemove(null)}
        onConfirm={handleRemoveConfirm}
        title={t('admin.class.removeFromClass')}
        description={t('admin.class.removeConfirmDesc', {
          studentName: studentToRemove
            ? formatName(studentToRemove.firstName, studentToRemove.lastName)
            : '',
        })}
        confirmText={t('common.remove')}
        isDestructive={true}
        isLoading={removeStudentMutation.isPending}
      />
    </>
  )
}
