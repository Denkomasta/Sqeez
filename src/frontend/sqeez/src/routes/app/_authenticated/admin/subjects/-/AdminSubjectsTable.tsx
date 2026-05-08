import { useTranslation } from 'react-i18next'
import {
  BookCopy,
  Users,
  FileSignature,
  Edit2,
  Trash2,
  Calendar,
  School,
  User,
} from 'lucide-react'

import { Badge } from '@/components/ui/Badge/Badge'
import { DataTable, type ColumnDef } from '@/components/ui/Table/DataTable'
import type { SubjectDto } from '@/api/generated/model'
import { Link } from '@tanstack/react-router'

interface AdminSubjectsTableProps {
  subjects: SubjectDto[]
  isLoading: boolean
  onEditSubject?: (subject: SubjectDto) => void
  onDeleteSubject?: (subject: SubjectDto) => void
}

export function AdminSubjectsTable({
  subjects,
  isLoading,
  onEditSubject,
  onDeleteSubject,
}: AdminSubjectsTableProps) {
  const { t } = useTranslation()

  const columns: ColumnDef<SubjectDto>[] = [
    {
      header: t('admin.subjects.subjectDetails'),
      cell: (subject) => (
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-nav-cyan/10 text-nav-cyan">
            <BookCopy className="h-5 w-5" />
          </div>
          <div className="flex flex-col">
            <div className="flex items-center gap-2">
              <Link
                className="font-semibold text-foreground hover:underline"
                to="/app/subjects/$subjectId"
                params={{ subjectId: subject.id.toString() }}
              >
                {subject.name}
              </Link>
              <Badge variant="secondary" className="text-[10px] uppercase">
                {subject.code}
              </Badge>
            </div>
            {subject.description && (
              <span className="line-clamp-1 max-w-62.5 text-xs text-muted-foreground">
                {subject.description}
              </span>
            )}
          </div>
        </div>
      ),
    },
    {
      header: t('admin.subjects.schedule'),
      cell: (subject) => (
        <div className="flex flex-col gap-1 text-xs text-muted-foreground">
          <span className="flex items-center gap-1">
            <Calendar className="h-3 w-3" />{' '}
            {new Date(subject.startDate).toLocaleDateString()}
          </span>
          {subject.endDate && (
            <span className="flex items-center gap-1 opacity-75">
              <Calendar className="h-3 w-3" />{' '}
              {new Date(subject.endDate).toLocaleDateString()}
            </span>
          )}
        </div>
      ),
    },
    {
      header: t('admin.subjects.assignments'),
      cell: (subject) => (
        <div className="flex flex-col gap-1.5">
          {subject.teacherId ? (
            <Link
              to="/app/profile/$userId"
              params={{ userId: subject.teacherId.toString() }}
            >
              <Badge className="w-fit bg-info text-info-foreground hover:bg-info/90">
                <User className="mr-1 h-3 w-3" /> {subject.teacherName}
              </Badge>
            </Link>
          ) : (
            <span className="text-xs text-muted-foreground italic">
              {t('admin.unassignedTeacher')}
            </span>
          )}

          {subject.schoolClassId ? (
            <Link
              to="/app/admin/classes/$classId"
              params={{ classId: subject.schoolClassId.toString() }}
            >
              <Badge
                variant="outline"
                className="w-fit border-nav-purple/30 text-nav-purple"
              >
                <School className="mr-1 h-3 w-3" /> {subject.schoolClassName}
              </Badge>
            </Link>
          ) : (
            <span className="text-xs text-muted-foreground italic">
              {t('admin.unassignedClass')}
            </span>
          )}
        </div>
      ),
    },
    {
      header: t('admin.classes.statistics'),
      cell: (subject) => (
        <div className="flex flex-col gap-1 text-xs text-muted-foreground">
          <span className="flex items-center gap-1">
            <Users className="h-3 w-3" /> {t('admin.subjects.enrollments')}
            {': '}
            {subject.enrollmentCount}
          </span>
          <span className="flex items-center gap-1">
            <FileSignature className="h-3 w-3" /> {t('admin.subjects.quizzes')}
            {': '}
            {subject.quizCount}
          </span>
        </div>
      ),
    },
    {
      header: '',
      className: 'text-right w-[100px]',
      cell: (subject) => {
        const canDelete =
          Number(subject.enrollmentCount) === 0 &&
          Number(subject.quizCount) === 0

        return (
          <div className="flex justify-end gap-1">
            <button
              onClick={() => onEditSubject?.(subject)}
              className="flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-primary/10 hover:text-primary focus-visible:ring-2 focus-visible:ring-primary focus-visible:outline-none"
              title={t('common.edit')}
            >
              <Edit2 className="h-4 w-4" />
            </button>
            <button
              onClick={() => canDelete && onDeleteSubject?.(subject)}
              disabled={!canDelete}
              className={`flex h-8 w-8 items-center justify-center rounded-md transition-colors focus-visible:ring-2 focus-visible:ring-destructive focus-visible:outline-none ${
                canDelete
                  ? 'text-muted-foreground hover:bg-destructive/10 hover:text-destructive'
                  : 'cursor-not-allowed text-muted-foreground/30'
              }`}
              title={
                canDelete
                  ? t('common.delete')
                  : t('admin.subjects.cannotDelete')
              }
            >
              <Trash2 className="h-4 w-4" />
            </button>
          </div>
        )
      },
    },
  ]

  return (
    <DataTable
      data={subjects}
      columns={columns}
      isLoading={isLoading}
      emptyMessage={t('admin.subjects.noSubjectsFound')}
      keyExtractor={(subject) => subject.id}
    />
  )
}
