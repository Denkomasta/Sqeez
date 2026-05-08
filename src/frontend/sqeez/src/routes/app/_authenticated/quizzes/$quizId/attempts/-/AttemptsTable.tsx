import { Link } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { Eye, PenTool, CheckCircle2, Clock } from 'lucide-react'

import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { DataTable, type ColumnDef } from '@/components/ui/Table'

export interface AttemptRowDto {
  id: number | string
  quizId: number | string
  quizTitle?: string
  studentName?: string
  studentId?: number | string
  status: 'InProgress' | 'PendingGrading' | 'Completed' | string
  totalScore: number
  startTime: string
}

interface AttemptsTableProps {
  attempts: AttemptRowDto[]
  isTeacherView: boolean
  isLoading?: boolean
  isQuizActive?: boolean
}

export function AttemptsTable({
  attempts,
  isTeacherView,
  isLoading,
  isQuizActive = false,
}: AttemptsTableProps) {
  const { t } = useTranslation()

  const columns: ColumnDef<AttemptRowDto>[] = [
    {
      header: isTeacherView ? t('common.student') : t('common.quiz'),
      cell: (item) => {
        if (isTeacherView) {
          if (item.studentId && item.studentName) {
            return (
              <Link
                to="/app/profile/$userId"
                params={{ userId: String(item.studentId) }}
                className="font-medium transition-colors hover:text-primary hover:underline"
              >
                {item.studentName}
              </Link>
            )
          }
          return <span className="font-medium">{item.studentName}</span>
        }

        return <span className="font-medium">{item.quizTitle}</span>
      },
    },
    {
      header: t('common.date'),
      cell: (item) => new Date(item.startTime).toLocaleDateString(),
      className: 'text-muted-foreground',
    },
    {
      header: t('attempts.status'),
      cell: (item) => {
        const isNeedsGrading =
          item.status === 'PendingGrading' || item.status === 'NeedsGrading'
        const isCompleted = item.status === 'Completed'

        if (isNeedsGrading) {
          return (
            <Badge
              variant="outline"
              className="border-warning/50 bg-warning/10 text-warning"
            >
              <Clock className="mr-1 h-3 w-3" />
              {t('grading.needsGrading')}
            </Badge>
          )
        }
        if (isCompleted) {
          return (
            <Badge
              variant="outline"
              className="border-success/50 bg-success/10 text-success"
            >
              <CheckCircle2 className="mr-1 h-3 w-3" />
              {t('attempts.completed')}
            </Badge>
          )
        }
        return <Badge variant="secondary">{item.status}</Badge>
      },
    },
    {
      header: t('common.score'),
      className: 'text-right',
      cell: (item) => (
        <span className="font-bold">
          {t('common.points')}: {item.totalScore}
        </span>
      ),
    },
    {
      header: t('common.actions'),
      className: 'w-[100px] text-center',
      cell: (item) => {
        const isNeedsGrading =
          item.status === 'PendingGrading' || item.status === 'NeedsGrading'
        const showGradeButton = isTeacherView && isNeedsGrading
        const isViewDisabled = !isTeacherView && isQuizActive

        return (
          <Link
            to={`/app/quizzes/$quizId/attempts/$attemptId`}
            params={{ quizId: String(item.quizId), attemptId: String(item.id) }}
            disabled={isViewDisabled}
            className={isViewDisabled ? 'pointer-events-none opacity-50' : ''}
          >
            <Button
              variant={showGradeButton ? 'default' : 'ghost'}
              size="sm"
              disabled={isViewDisabled}
              className={
                showGradeButton
                  ? 'bg-warning text-warning-foreground hover:bg-warning/90'
                  : ''
              }
            >
              {showGradeButton ? (
                <>
                  <PenTool className="mr-2 h-4 w-4" />
                  {t('common.grade')}
                </>
              ) : (
                <>
                  <Eye className="mr-2 h-4 w-4" />
                  {t('common.view')}
                </>
              )}
            </Button>
          </Link>
        )
      },
    },
  ]

  const emptyMessage = isTeacherView
    ? t('attempts.noAttemptsTeacher')
    : t('attempts.noAttemptsStudent')

  return (
    <DataTable
      data={attempts || []}
      columns={columns}
      isLoading={isLoading}
      emptyMessage={emptyMessage}
      keyExtractor={(item) => item.id}
    />
  )
}
