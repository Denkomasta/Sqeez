import { useTranslation } from 'react-i18next'
import { Link } from '@tanstack/react-router'
import { Eye, Clock, AlertCircle } from 'lucide-react'

import { DataTable, type ColumnDef } from '@/components/ui/Table/DataTable'
import { SimpleAvatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge/Badge'
import { Button } from '@/components/ui/Button'
import type { QuizAttemptDto } from '@/api/generated/model'
import {
  getAttemptStatusLabel,
  isCompletedAttemptStatus,
  isPendingCorrectionAttemptStatus,
} from '@/lib/attemptStatusHelpers'

interface QuizAttemptsTableProps {
  attempts: QuizAttemptDto[]
  isLoading?: boolean
}

/**
 * Teacher-facing quiz attempts table.
 * Rows link into the attempt detail where pending free-text answers can be graded.
 */
export function QuizAttemptsTable({
  attempts,
  isLoading = false,
}: QuizAttemptsTableProps) {
  const { t } = useTranslation()

  const columns: ColumnDef<QuizAttemptDto>[] = [
    {
      header: t('common.student'),
      cell: (attempt) => {
        const nameParts = (attempt.studentName || '').split(' ')
        const firstName = nameParts[0] || ''
        const lastName = nameParts.slice(1).join(' ') || ''

        return (
          <div className="flex items-center gap-3">
            <SimpleAvatar
              firstName={firstName}
              lastName={lastName}
              wrapperClassName="size-8 shrink-0"
            />
            <div className="flex flex-col">
              <span className="font-medium text-foreground">
                {attempt.studentName || t('common.unknownUser')}
              </span>
              <span className="text-xs text-muted-foreground">
                ID: {attempt.studentId || '-'}
              </span>
            </div>
          </div>
        )
      },
    },
    {
      header: t('quiz.submittedAt'),
      cell: (attempt) => (
        <span className="text-muted-foreground">
          {attempt.endTime ? (
            new Date(attempt.endTime).toLocaleString()
          ) : (
            <span className="flex items-center gap-1 italic">
              <Clock className="h-3 w-3" />
              {t('quiz.inProgress')}
            </span>
          )}
        </span>
      ),
    },
    {
      header: t('quiz.status'),
      cell: (attempt) => {
        const isCompleted = isCompletedAttemptStatus(attempt.status)
        const isPending = isPendingCorrectionAttemptStatus(attempt.status)

        if (isCompleted) {
          return (
            <Badge variant="default" className="shadow-none">
              {getAttemptStatusLabel(t, attempt.status)}
            </Badge>
          )
        }

        if (isPending) {
          return (
            <Badge
              variant="secondary"
              className="gap-1.5 bg-warning/10 text-warning shadow-none hover:bg-warning/20 dark:text-warning"
            >
              <AlertCircle className="h-3 w-3" />
              {getAttemptStatusLabel(t, attempt.status)}
            </Badge>
          )
        }

        return (
          <Badge variant="secondary" className="shadow-none">
            {getAttemptStatusLabel(t, attempt.status)}
          </Badge>
        )
      },
    },
    {
      header: t('quiz.score'),
      cell: (attempt) => {
        if (!attempt.endTime) {
          return <span className="text-muted-foreground">-</span>
        }

        const score = Number(attempt.totalScore || 0)

        const isPending = isPendingCorrectionAttemptStatus(attempt.status)

        return (
          <div className="flex items-center gap-2">
            <span
              className={`font-bold ${isPending ? 'text-muted-foreground' : 'text-foreground'}`}
            >
              {score} {t('quiz.points')}
              {isPending && '*'}
            </span>
          </div>
        )
      },
    },
    {
      header: '',
      className: 'text-right w-[120px]',
      cell: (attempt) => (
        <div className="flex justify-end gap-2">
          <Link
            to="/app/quizzes/$quizId/attempts/$attemptId"
            params={{
              quizId: String(attempt.quizId),
              attemptId: String(attempt.id),
            }}
          >
            <Button
              variant="ghost"
              size="sm"
              className="gap-2 text-primary hover:bg-primary/10"
            >
              <Eye className="h-4 w-4" />
              <span className="hidden sm:inline">{t('quiz.review')}</span>
            </Button>
          </Link>
        </div>
      ),
    },
  ]

  return (
    <DataTable
      data={attempts}
      columns={columns}
      isLoading={isLoading}
      emptyMessage={t('quiz.noAttemptsYet')}
      keyExtractor={(attempt) => String(attempt.id)}
    />
  )
}
