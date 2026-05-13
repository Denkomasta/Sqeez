import { createFileRoute } from '@tanstack/react-router'
import { useAuthStore } from '@/store/useAuthStore'
import {
  BookOpen,
  GraduationCap,
  Trophy,
  FileSignature,
  Users,
  Settings,
  ShieldAlert,
  Library,
  School,
  BookCopy,
  Award,
  FileUp,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useGetApiUsersId } from '@/api/generated/endpoints/user/user'
import { PageLayout } from '@/components/layouting/PageLayout/PageLayout'
import type { StudentDtoTeacherDto } from '@/api/generated/model'
import {
  DashboardNavCardGrid,
  type DashboardNavCardItem,
} from '@/components/layouting/DashboardNavCardGrid'

export const Route = createFileRoute('/app/_authenticated/')({
  component: DashboardLaunchpad,
})

function DashboardLaunchpad() {
  const { t } = useTranslation()
  const user = useAuthStore((s) => s.user)

  const userId = user?.id
  const isTeacher = user?.role === 'Teacher' || user?.role === 'Admin'
  const isAdmin = user?.role === 'Admin'

  const { data, isLoading } = useGetApiUsersId(Number(userId), {
    query: { enabled: !!userId && isTeacher },
  })

  if (!user) return null

  const teacherData = data as StudentDtoTeacherDto | undefined

  const studentLinks: DashboardNavCardItem[] = [
    {
      title: t('dashboard.subjects'),
      description: t('dashboard.subjectDescription'),
      icon: <Library className="h-8 w-8" />,
      href: '/app/subjects',
      iconPanelClassName:
        'bg-nav-blue/20 text-nav-blue ring-1 ring-nav-blue/30',
      accentClassName: 'bg-nav-blue',
    },
    {
      title: t('dashboard.myClass'),
      description: t('dashboard.myClassDescripiton'),
      icon: <Users className="h-8 w-8" />,
      href: '/app/class',
      iconPanelClassName:
        'bg-nav-green/20 text-nav-green ring-1 ring-nav-green/30',
      accentClassName: 'bg-nav-green',
    },
    {
      title: t('dashboard.quizzes'),
      description: t('dashboard.quizzesDescription'),
      icon: <FileSignature className="h-8 w-8" />,
      href: '/app/quizzes',
      iconPanelClassName:
        'bg-nav-purple/20 text-nav-purple ring-1 ring-nav-purple/30',
      accentClassName: 'bg-nav-purple',
    },
    {
      title: t('dashboard.leaderboards'),
      description: t('dashboard.leaderboardsDescription'),
      icon: <Trophy className="h-8 w-8" />,
      href: '/app/leaderboards',
      iconPanelClassName:
        'bg-nav-yellow/20 text-nav-yellow ring-1 ring-nav-yellow/35',
      accentClassName: 'bg-nav-yellow',
    },
  ]

  const teacherLinks: DashboardNavCardItem[] = [
    {
      title: t('dashboard.teacherSubjects'),
      description: t('dashboard.teacherSubjectsDescription'),
      icon: <Library className="h-8 w-8" />,
      href: '/app/teacher/subjects',
      iconPanelClassName:
        'bg-nav-blue/20 text-nav-blue ring-1 ring-nav-blue/30',
      accentClassName: 'bg-nav-blue',
    },
    {
      title: t('dashboard.manageQuizzes'),
      description: t('dashboard.manageQuizzesDescription'),
      icon: <GraduationCap className="h-8 w-8" />,
      href: '/app/teacher/quizzes',
      iconPanelClassName:
        'bg-nav-orange/20 text-nav-orange ring-1 ring-nav-orange/30',
      accentClassName: 'bg-nav-orange',
    },
    ...(teacherData?.managedClassId
      ? [
          {
            title: t('dashboard.classManagement'),
            description: t('dashboard.classManagementDescription'),
            icon: <BookOpen className="h-8 w-8" />,
            href: '/app/class/$classId',
            params: { classId: String(teacherData.managedClassId) },
            iconPanelClassName:
              'bg-nav-teal/20 text-nav-teal ring-1 ring-nav-teal/30',
            accentClassName: 'bg-nav-teal',
          },
        ]
      : []),
  ]

  const adminLinks: DashboardNavCardItem[] = [
    {
      title: t('dashboard.userManagement'),
      description: t('dashboard.userManagementDescription'),
      icon: <ShieldAlert className="h-8 w-8" />,
      href: '/app/admin/users',
      iconPanelClassName:
        'bg-destructive/20 text-destructive ring-1 ring-destructive/30',
      accentClassName: 'bg-destructive',
    },
    {
      title: t('dashboard.adminClasses'),
      description: t('dashboard.adminClassesDescription'),
      icon: <School className="h-8 w-8" />,
      href: '/app/admin/classes',
      iconPanelClassName:
        'bg-nav-indigo/20 text-nav-indigo ring-1 ring-nav-indigo/30',
      accentClassName: 'bg-nav-indigo',
    },
    {
      title: t('dashboard.adminSubjects'),
      description: t('dashboard.adminSubjectsDescription'),
      icon: <BookCopy className="h-8 w-8" />,
      href: '/app/admin/subjects',
      iconPanelClassName:
        'bg-nav-cyan/20 text-nav-cyan ring-1 ring-nav-cyan/30',
      accentClassName: 'bg-nav-cyan',
    },
    {
      title: t('dashboard.adminBadges'),
      description: t('dashboard.adminBadgesDescription'),
      icon: <Award className="h-8 w-8" />,
      href: '/app/admin/badges',
      iconPanelClassName:
        'bg-nav-amber/20 text-nav-amber ring-1 ring-nav-amber/35',
      accentClassName: 'bg-nav-amber',
    },
    {
      title: t('dashboard.adminImport'),
      description: t('dashboard.adminImportDescription'),
      icon: <FileUp className="h-8 w-8" />,
      href: '/app/admin/imports',
      iconPanelClassName:
        'bg-nav-emerald/20 text-nav-emerald ring-1 ring-nav-emerald/30',
      accentClassName: 'bg-nav-emerald',
    },
    {
      title: t('dashboard.systemSettings'),
      description: t('dashboard.systemSettingsDescription'),
      icon: <Settings className="h-8 w-8" />,
      href: '/app/admin/settings',
      iconPanelClassName: 'bg-foreground/20 text-foreground ring-1 ring-border',
      accentClassName: 'bg-foreground',
    },
  ]

  return (
    <PageLayout
      containerClassName="max-w-7xl"
      isLoading={isLoading}
      title={`${t('common.welcome')}, ${user.username}!`}
      subtitle={t('dashboard.navigationDecription')}
    >
      <div className="space-y-10">
        <section className="space-y-4">
          <h2 className="text-xl font-semibold tracking-tight">
            {t('dashboard.yourLearning')}
          </h2>
          <DashboardNavCardGrid items={studentLinks} />
        </section>

        {isTeacher && (
          <section className="space-y-4">
            <h2 className="text-xl font-semibold tracking-tight">
              {t('dashboard.teachingTools')}
            </h2>
            <DashboardNavCardGrid items={teacherLinks} />
          </section>
        )}

        {isAdmin && (
          <section className="space-y-4">
            <h2 className="text-xl font-semibold tracking-tight">
              {t('dashboard.administration')}
            </h2>
            <DashboardNavCardGrid items={adminLinks} />
          </section>
        )}
      </div>
    </PageLayout>
  )
}
