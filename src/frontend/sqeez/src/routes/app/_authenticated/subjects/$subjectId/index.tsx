import { createFileRoute, Link } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import {
  BookOpen,
  Calendar,
  User as UserIcon,
  GraduationCap,
  Users,
  FileText,
} from 'lucide-react'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/Card'
import { Badge } from '@/components/ui/Badge/Badge'
import { PageLayout } from '@/components/layouting/PageLayout/PageLayout'
import { useGetApiSubjectsId } from '@/api/generated/endpoints/subjects/subjects'
import { formatDate } from '@/lib/dateHelpers'

export const Route = createFileRoute(
  '/app/_authenticated/subjects/$subjectId/',
)({
  component: SubjectPage,
})

/**
 * Student subject overview route.
 * Loads subject details and the current user's enrollment before rendering.
 */
function SubjectPage() {
  const { t } = useTranslation()
  const { subjectId } = Route.useParams()

  const { data: subjectData, isLoading } = useGetApiSubjectsId(
    Number(subjectId),
    {
      query: { enabled: !!subjectId },
    },
  )

  if (!subjectData) {
    return (
      <PageLayout
        isLoading={isLoading}
        containerClassName="flex min-h-[60vh] max-w-7xl items-center justify-center"
      >
        <Card className="w-full max-w-md border-2 border-dashed text-center shadow-sm">
          <CardHeader>
            <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-secondary">
              <BookOpen className="h-8 w-8 text-muted-foreground" />
            </div>
            <CardTitle className="text-2xl">{t('subject.notFound')}</CardTitle>
            <CardDescription className="mt-2 text-base">
              {t('subject.notFoundDesc')}
            </CardDescription>
          </CardHeader>
        </Card>
      </PageLayout>
    )
  }

  return (
    <PageLayout
      containerClassName="max-w-7xl"
      isLoading={isLoading}
      title={
        <>
          <BookOpen className="h-8 w-8 text-primary" />
          {subjectData.name}
        </>
      }
      titleBadge={
        <Badge variant="secondary" className="text-sm">
          {subjectData.code}
        </Badge>
      }
      subtitle={
        <span className="flex items-center gap-2 text-sm text-muted-foreground">
          <Calendar className="h-4 w-4" />
          {formatDate(subjectData.startDate)}
          {subjectData.endDate
            ? ` - ${formatDate(subjectData.endDate)}`
            : ` - ${t('subject.ongoing')}`}
        </span>
      }
    >
      <div className="grid grid-cols-1 gap-6 md:grid-cols-3">
        <div className="space-y-6 md:col-span-1">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <UserIcon className="h-5 w-5" />
                {t('subject.taughtBy')}
              </CardTitle>
            </CardHeader>
            <CardContent>
              {subjectData.teacherId ? (
                <Link
                  to="/app/profile/$userId"
                  params={{ userId: subjectData.teacherId.toString() }}
                  className="group flex items-center justify-between rounded-lg border p-3 transition-colors hover:bg-accent/50"
                >
                  <span className="font-medium group-hover:underline">
                    {subjectData.teacherName || t('common.unknown')}
                  </span>
                </Link>
              ) : (
                <p className="text-sm text-muted-foreground">
                  {t('subject.noTeacher')}
                </p>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <GraduationCap className="h-5 w-5" />
                {t('subject.assignedClass')}
              </CardTitle>
            </CardHeader>
            <CardContent>
              {subjectData.schoolClassId ? (
                <Link
                  to="/app/class/$classId"
                  params={{ classId: subjectData.schoolClassId.toString() }}
                  className="group flex items-center justify-between rounded-lg border p-3 transition-colors hover:bg-accent/50"
                >
                  <span className="font-medium group-hover:underline">
                    {subjectData.schoolClassName || t('common.unknown')}
                  </span>
                </Link>
              ) : (
                <p className="text-sm text-muted-foreground">
                  {t('subject.noClass')}
                </p>
              )}
            </CardContent>
          </Card>
        </div>

        <div className="space-y-6 md:col-span-2">
          <Card className="h-fit">
            <CardHeader>
              <CardTitle>{t('subject.description')}</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="leading-relaxed text-muted-foreground">
                {subjectData.description || t('subject.noDescription')}
              </p>
            </CardContent>
          </Card>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Link
              to="/app/subjects/$subjectId/students"
              params={{ subjectId }}
              className="block rounded-xl ring-primary outline-none focus-visible:ring-2"
            >
              <Card className="group h-full cursor-pointer transition-all hover:border-primary hover:shadow-md">
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">
                    {t('subject.enrolledStudents')}
                  </CardTitle>
                  <div className="transition-transform duration-200 group-hover:scale-110">
                    <Users className="size-4 text-muted-foreground transition-colors group-hover:text-primary" />
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">
                    {subjectData.enrollmentCount}
                  </div>
                </CardContent>
              </Card>
            </Link>

            <Link
              to="/app/subjects/$subjectId/quizzes"
              params={{ subjectId }}
              className="block rounded-xl ring-primary outline-none focus-visible:ring-2"
            >
              <Card className="group h-full cursor-pointer transition-all hover:border-primary hover:shadow-md">
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">
                    {t('subject.totalQuizzes')}
                  </CardTitle>
                  <div className="transition-transform duration-200 group-hover:scale-110">
                    <FileText className="size-4 text-muted-foreground transition-colors group-hover:text-primary" />
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">
                    {subjectData.quizCount}
                  </div>
                </CardContent>
              </Card>
            </Link>
          </div>
        </div>
      </div>
    </PageLayout>
  )
}
