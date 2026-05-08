import { useTranslation } from 'react-i18next'
import { School, Users, BookOpen, Edit2, UserX, Trash2 } from 'lucide-react'

import { Badge } from '@/components/ui/Badge/Badge'
import { DataTable, type ColumnDef } from '@/components/ui/Table/DataTable'
import type { SchoolClassDto } from '@/api/generated/model'
import { Link } from '@tanstack/react-router'

interface AdminSchoolClassTableProps {
  classes: SchoolClassDto[]
  isLoading: boolean
  onEditTeacher: (schoolClass: SchoolClassDto) => void
  onDeleteClass: (schoolClass: SchoolClassDto) => void
}

export function AdminSchoolClassTable({
  classes,
  isLoading,
  onEditTeacher,
  onDeleteClass,
}: AdminSchoolClassTableProps) {
  const { t } = useTranslation()

  const columns: ColumnDef<SchoolClassDto>[] = [
    {
      header: t('admin.classes.className'),
      cell: (cls) => (
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <School className="h-5 w-5" />
          </div>
          <Link
            to="/app/admin/classes/$classId"
            params={{ classId: cls.id.toString() }}
            className="flex flex-col hover:underline"
          >
            <span className="font-semibold text-foreground">{cls.name}</span>
            <span className="text-xs text-muted-foreground">
              {t('admin.classes.section')}: {cls.section}
            </span>
          </Link>
        </div>
      ),
    },
    {
      header: t('admin.classes.academicYear'),
      cell: (cls) => <span className="font-medium">{cls.academicYear}</span>,
    },
    {
      header: t('admin.classes.teacher'),
      cell: (cls) => (
        <button
          onClick={() => onEditTeacher(cls)}
          className="group relative flex items-center gap-2 rounded-md transition-all outline-none focus-visible:ring-2 focus-visible:ring-primary"
          title={t('admin.classes.clickToEditTeacher')}
        >
          {cls.teacherId ? (
            <Badge className="bg-info text-info-foreground hover:bg-info/90">
              {cls.teacherName}
            </Badge>
          ) : (
            <Badge
              variant="outline"
              className="border-dashed text-muted-foreground"
            >
              <UserX className="mr-1 h-3 w-3" /> {t('admin.classes.unassigned')}
            </Badge>
          )}
          <Edit2 className="h-3 w-3 text-muted-foreground opacity-0 transition-opacity group-hover:opacity-100" />
        </button>
      ),
    },
    {
      header: t('admin.classes.statistics'),
      cell: (cls) => (
        <div className="flex flex-col gap-1 text-xs text-muted-foreground">
          <span className="flex items-center gap-1">
            <Users className="h-3 w-3" /> {t('common.students')}
            {': '}
            {cls.studentCount}
          </span>
          <span className="flex items-center gap-1">
            <BookOpen className="h-3 w-3" /> {t('admin.classes.subjects')}
            {': '}
            {cls.subjectCount}
          </span>
        </div>
      ),
    },
    {
      header: '',
      className: 'text-right w-20',
      cell: (cls) => {
        const canDelete = Number(cls.studentCount) === 0 && !cls.teacherId

        return (
          <div className="flex justify-end">
            <button
              onClick={() => canDelete && onDeleteClass(cls)}
              disabled={!canDelete}
              className={`flex h-8 w-8 items-center justify-center rounded-md transition-colors focus-visible:ring-2 focus-visible:ring-destructive focus-visible:outline-none ${
                canDelete
                  ? 'text-muted-foreground hover:bg-destructive/10 hover:text-destructive'
                  : 'cursor-not-allowed text-muted-foreground/30'
              }`}
              title={
                canDelete ? t('common.delete') : t('admin.classes.cannotDelete')
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
      data={classes}
      columns={columns}
      isLoading={isLoading}
      emptyMessage={t('admin.classes.noClassesFound')}
      keyExtractor={(cls) => cls.id}
    />
  )
}
