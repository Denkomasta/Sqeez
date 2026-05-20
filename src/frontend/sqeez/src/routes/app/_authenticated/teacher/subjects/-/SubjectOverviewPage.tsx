import { useTranslation } from 'react-i18next'
import { Link } from '@tanstack/react-router'
import {
  BookCopy,
  ArrowLeft,
  Users,
  FileSignature,
  Calendar,
  GraduationCap,
  User as UserIcon,
  ChevronRight,
} from 'lucide-react'

import { useGetApiSubjectsId } from '@/api/generated/endpoints/subjects/subjects'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { Badge } from '@/components/ui/Badge/Badge'
import { PageLayout } from '@/components/layouting/PageLayout/PageLayout'
import { useAuthStore } from '@/store/useAuthStore'
import { formatDate } from '@/lib/dateHelpers'

/**
 * Teacher subject overview page.
 * Summarizes subject metadata and links to students, quizzes, and statistics.
 */
export function SubjectOverviewPage({
  subjectId,
}: {
  subjectId: string | number
}) {
  const { t } = useTranslation()
  const { isAdmin } = useAuthStore()

  const { data: subjectData, isLoading } = useGetApiSubjectsId(
    Number(subjectId),
    { query: { enabled: !!subjectId } },
  )

  const canManage =
    isAdmin || subjectData?.teacherId === useAuthStore.getState().user?.id
  const backPath = canManage ? '/app/teacher/subjects' : '/app/subjects'

  return (
    <PageLayout
      variant="app"
      containerClassName="max-w-5xl"
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
            <span className="text-3xl font-bold tracking-tight">
              {subjectData?.name}
            </span>
          </div>
        </div>
      }
      titleBadge={
        <Badge variant="secondary" className="mt-8 uppercase shadow-sm">
          {subjectData?.code}
        </Badge>
      }
      subtitle={
        <span className="mt-2 flex items-center gap-2 text-sm text-muted-foreground">
          <Calendar className="h-4 w-4" />
          {formatDate(subjectData?.startDate)}
          {subjectData?.endDate
            ? ` - ${formatDate(subjectData.endDate)}`
            : ` - ${t('subject.ongoing')}`}
        </span>
      }
    >
      {subjectData && (
        <div className="grid grid-cols-1 gap-6 md:grid-cols-3">
          <div className="space-y-6 md:col-span-1">
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="flex items-center gap-2 text-lg">
                  <UserIcon className="h-5 w-5 text-muted-foreground" />
                  {t('subject.taughtBy')}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="font-medium text-foreground">
                  {subjectData.teacherName || t('common.unknown')}
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="flex items-center gap-2 text-lg">
                  <GraduationCap className="h-5 w-5 text-muted-foreground" />
                  {t('subject.assignedClass')}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="font-medium text-foreground">
                  {subjectData.schoolClassName || t('subject.noClass')}
                </p>
              </CardContent>
            </Card>
          </div>

          <div className="space-y-6 md:col-span-2">
            <Card className="h-fit border-border bg-card shadow-sm">
              <CardHeader className="pb-3">
                <CardTitle>{t('subject.description')}</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="leading-relaxed text-muted-foreground">
                  {subjectData.description || t('subject.noDescription')}
                </p>
              </CardContent>
            </Card>

            <h3 className="pt-4 text-lg font-semibold tracking-tight">
              {t('subject.materials')}
            </h3>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Link
                to="/app/subjects/$subjectId/students"
                params={{ subjectId: subjectId.toString() }}
                className="group outline-none"
              >
                <Card className="h-full ring-primary transition-all hover:border-primary hover:shadow-md focus-visible:ring-2">
                  <CardHeader className="pb-2">
                    <div className="flex items-center justify-between">
                      <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-nav-blue/10 text-nav-blue transition-transform group-hover:scale-110">
                        <Users className="h-5 w-5" />
                      </div>
                      <ChevronRight className="h-5 w-5 text-muted-foreground transition-transform group-hover:translate-x-1" />
                    </div>
                    <CardTitle className="mt-4 text-xl">
                      {t('subject.enrolledStudents')}
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-sm font-medium text-muted-foreground">
                      {subjectData.enrollmentCount} {t('common.enrolled')}
                    </div>
                  </CardContent>
                </Card>
              </Link>

              <Link
                to="/app/teacher/quizzes"
                search={{
                  subjectId: subjectId.toString(),
                  activeOnly: true,
                }}
                className="group outline-none"
              >
                <Card className="h-full ring-primary transition-all hover:border-primary hover:shadow-md focus-visible:ring-2">
                  <CardHeader className="pb-2">
                    <div className="flex items-center justify-between">
                      <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-nav-purple/10 text-nav-purple transition-transform group-hover:scale-110">
                        <FileSignature className="h-5 w-5" />
                      </div>
                      <ChevronRight className="h-5 w-5 text-muted-foreground transition-transform group-hover:translate-x-1" />
                    </div>
                    <CardTitle className="mt-4 text-xl">
                      {t('subject.totalQuizzes')}
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-sm font-medium text-muted-foreground">
                      {subjectData.quizCount} {t('common.active')}
                    </div>
                  </CardContent>
                </Card>
              </Link>
            </div>
          </div>
        </div>
      )}
    </PageLayout>
  )
}
