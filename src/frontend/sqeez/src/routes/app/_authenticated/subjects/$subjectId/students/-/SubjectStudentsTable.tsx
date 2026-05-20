import { useTranslation } from 'react-i18next'
import { Link } from '@tanstack/react-router'
import { GraduationCap, UserMinus } from 'lucide-react'

import { DataTable, type ColumnDef } from '@/components/ui/Table/DataTable'
import { SimpleAvatar } from '@/components/ui/Avatar'
import { getImageUrl } from '@/lib/imageHelpers'
import { formatName } from '@/lib/userHelpers'

import type { StudentDto } from '@/api/generated/model'

interface SubjectStudentsTableProps {
  students: StudentDto[]
  isLoading: boolean
  onRemoveStudent?: (student: StudentDto) => void
  onEditMark?: (student: StudentDto) => void
}

/**
 * Teacher subject-student table.
 * Exposes grading and student profile navigation for each enrolled student.
 */
export function SubjectStudentsTable({
  students,
  isLoading,
  onRemoveStudent,
  onEditMark,
}: SubjectStudentsTableProps) {
  const { t } = useTranslation()

  const columns: ColumnDef<StudentDto>[] = [
    {
      header: t('common.student'),
      cell: (student) => (
        <div className="flex items-center gap-3">
          <SimpleAvatar
            url={getImageUrl(student.avatarUrl)}
            firstName={student.firstName}
            lastName={student.lastName}
            wrapperClassName="size-10 shrink-0"
          />
          <div className="flex flex-col">
            <Link
              to="/app/profile/$userId"
              params={{ userId: (student.id ?? 0).toString() }}
              className="font-semibold text-foreground hover:underline"
            >
              {student.username}
            </Link>
            <span className="text-xs text-muted-foreground">
              {formatName(student.firstName, student.lastName)}
            </span>
          </div>
        </div>
      ),
    },
    {
      header: t('common.email'),
      cell: (student) => (
        <span className="text-muted-foreground">{student.email}</span>
      ),
    },
    // Optional Action Column (e.g., for Teachers/Admins to remove a student)
    ...(onRemoveStudent || onEditMark
      ? [
          {
            header: '',
            className: 'text-right w-[120px]',
            cell: (student: StudentDto) => (
              <div className="flex justify-end gap-2">
                {onEditMark && (
                  <button
                    onClick={() => onEditMark(student)}
                    className="flex h-8 w-8 items-center justify-center rounded-md text-primary transition-colors hover:bg-primary/10 focus-visible:ring-2 focus-visible:ring-primary focus-visible:outline-none"
                    title={t('subject.gradeStudent')}
                  >
                    <GraduationCap className="h-4 w-4" />
                  </button>
                )}

                {onRemoveStudent && (
                  <button
                    onClick={() => onRemoveStudent(student)}
                    className="flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-destructive/10 hover:text-destructive focus-visible:ring-2 focus-visible:ring-destructive focus-visible:outline-none"
                    title={t('subject.removeFromSubject')}
                  >
                    <UserMinus className="h-4 w-4" />
                  </button>
                )}
              </div>
            ),
          },
        ]
      : []),
  ]

  return (
    <DataTable
      data={students}
      columns={columns}
      isLoading={isLoading}
      emptyMessage={t('subject.noStudentsEnrolled')}
      keyExtractor={(student) => student.id ?? 'unknown'}
    />
  )
}
