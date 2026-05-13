import { useTranslation } from 'react-i18next'
import { Link } from '@tanstack/react-router'
import {
  Archive,
  BookOpen,
  Edit2,
  GraduationCap,
  RotateCcw,
  ShieldAlert,
} from 'lucide-react'

import { SimpleAvatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge/Badge'
import { Button } from '@/components/ui/Button'
import { getImageUrl } from '@/lib/imageHelpers'
import { formatName } from '@/lib/userHelpers'
import type { StudentDto, UserRole } from '@/api/generated/model'

import { DataTable, type ColumnDef } from '@/components/ui/Table/DataTable'

export interface SelectedUserForRole {
  id: string | number
  name: string
  currentRole: UserRole
}

interface AdminUsersTableProps {
  users: StudentDto[]
  isLoading: boolean
  onEditRole: (user: SelectedUserForRole) => void
  archiveAction?: 'archive' | 'restore' | null
  onArchiveUser?: (user: SelectedUserForRole) => void
  onRestoreUser?: (user: SelectedUserForRole) => void
  pendingUserId?: string | number
}

export function AdminUsersTable({
  users,
  isLoading,
  onEditRole,
  archiveAction,
  onArchiveUser,
  onRestoreUser,
  pendingUserId,
}: AdminUsersTableProps) {
  const { t } = useTranslation()

  const getRoleBadge = (role?: UserRole) => {
    switch (role) {
      case 'Admin':
        return (
          <Badge className="bg-destructive hover:bg-destructive/80">
            <ShieldAlert className="mr-1 h-3 w-3" /> {t('common.admin')}
          </Badge>
        )
      case 'Teacher':
        return (
          <Badge className="bg-info text-info-foreground hover:bg-info/90">
            <BookOpen className="mr-1 h-3 w-3" /> {t('common.teacher')}
          </Badge>
        )
      case 'Student':
      default:
        return (
          <Badge variant="secondary">
            <GraduationCap className="mr-1 h-3 w-3" /> {t('common.student')}
          </Badge>
        )
    }
  }

  const columns: ColumnDef<StudentDto>[] = [
    {
      header: t('common.user'),
      cell: (user) => (
        <div className="flex items-center gap-3">
          <SimpleAvatar
            url={getImageUrl(user.avatarUrl)}
            firstName={user.firstName}
            lastName={user.lastName}
            wrapperClassName="size-10 shrink-0"
          />
          <div className="flex flex-col">
            <Link
              to="/app/profile/$userId"
              params={{ userId: (user.id ?? 0).toString() }}
              className="font-semibold text-foreground hover:underline"
              disabled={!user.id}
            >
              {formatName(user.firstName, user.lastName)}
            </Link>
            <span className="text-xs text-muted-foreground">
              @{user.username}
            </span>
          </div>
        </div>
      ),
    },
    {
      header: t('common.contact'),
      cell: (user) => (
        <span className="text-muted-foreground">{user.email}</span>
      ),
    },
    {
      header: t('common.role'),
      cell: (user) => (
        <button
          onClick={() =>
            onEditRole({
              id: user.id!,
              name:
                formatName(user.firstName, user.lastName) ||
                t('admin.unassigned'),
              currentRole: user.role!,
            })
          }
          className="group relative flex items-center gap-2 rounded-md transition-all outline-none focus-visible:ring-2 focus-visible:ring-primary"
          title={t('admin.clickToEditRole')}
        >
          {getRoleBadge(user.role)}
          <Edit2 className="h-3 w-3 text-muted-foreground opacity-0 transition-opacity group-hover:opacity-100" />
        </button>
      ),
    },
  ]

  if (archiveAction) {
    columns.push({
      header: t('common.actions'),
      className: 'text-right',
      cell: (user) => {
        const selectedUser = {
          id: user.id!,
          name:
            formatName(user.firstName, user.lastName) || t('admin.unassigned'),
          currentRole: user.role!,
        }
        const isPending = pendingUserId === user.id
        const isArchiveAction = archiveAction === 'archive'
        const Icon = isArchiveAction ? Archive : RotateCcw

        return (
          <div className="flex justify-end">
            <Button
              size="sm"
              variant={isArchiveAction ? 'outline' : 'secondary'}
              className="gap-2"
              disabled={isPending}
              onClick={() =>
                isArchiveAction
                  ? onArchiveUser?.(selectedUser)
                  : onRestoreUser?.(selectedUser)
              }
            >
              <Icon className="h-4 w-4" />
              {isArchiveAction
                ? t('admin.archiveUser')
                : t('admin.restoreUser')}
            </Button>
          </div>
        )
      },
    })
  }

  return (
    <DataTable
      data={users}
      columns={columns}
      isLoading={isLoading}
      emptyMessage={t('admin.noUsersFound')}
      keyExtractor={(user) => user?.id ?? Math.random().toString()}
    />
  )
}
