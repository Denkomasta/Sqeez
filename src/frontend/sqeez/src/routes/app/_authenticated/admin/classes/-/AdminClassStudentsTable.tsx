import { useTranslation } from 'react-i18next'
import { Link } from '@tanstack/react-router'
import { UserMinus } from 'lucide-react'

import { DataTable, type ColumnDef } from '@/components/ui/Table/DataTable'
import { SimpleAvatar } from '@/components/ui/Avatar'
import { getImageUrl } from '@/lib/imageHelpers'
import { formatName } from '@/lib/userHelpers'
import type { StudentDto } from '@/api/generated/model'

interface AdminClassStudentsTableProps {
  students: StudentDto[]
  isLoading: boolean
  onRemoveStudent: (student: StudentDto) => void
}

/**
 * Student membership table for an admin class detail page.
 * Removal is delegated to the parent so it can own confirmation and refetching.
 */
export function AdminClassStudentsTable({
  students,
  isLoading,
  onRemoveStudent,
}: AdminClassStudentsTableProps) {
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
    {
      header: '',
      className: 'text-right w-[80px]',
      cell: (student) => (
        <div className="flex justify-end">
          <button
            onClick={() => onRemoveStudent(student)}
            className="flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-destructive/10 hover:text-destructive focus-visible:ring-2 focus-visible:ring-destructive focus-visible:outline-none"
            title={t('admin.class.removeFromClass')}
          >
            <UserMinus className="h-4 w-4" />
          </button>
        </div>
      ),
    },
  ]

  return (
    <DataTable
      data={students}
      columns={columns}
      isLoading={isLoading}
      emptyMessage={t('admin.class.noStudentsInClass')}
      keyExtractor={(student) => student.id ?? 'unknown'}
    />
  )
}
