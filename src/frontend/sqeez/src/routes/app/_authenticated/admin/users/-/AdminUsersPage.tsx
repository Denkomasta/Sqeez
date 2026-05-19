import { useEffect, useMemo, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ChevronDown, Search, Users } from 'lucide-react'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'

import { ConfirmModal } from '@/components/ui'
import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { Pagination } from '@/components/ui/Pagination'
import { PageLayout } from '@/components/layouting/PageLayout/PageLayout'
import { ScrollableSelectList } from '@/components/ui/ScrollableSelectList/ScrollableSelectList'
import type { UserRole } from '@/api/generated/model'
import {
  getGetApiUsersQueryKey,
  useDeleteApiUsersId,
  useDeleteApiUsersIdHard,
  useGetApiUsers,
  usePatchApiUsersIdRestore,
} from '@/api/generated/endpoints/user/user'
import { useAuthStore } from '@/store/useAuthStore'

import { RoleModificationModal } from './RoleModificationModal'
import { AdminUsersTable, type SelectedUserForRole } from './AdminUsersTable'

type ArchiveFilter = 'active' | 'archived' | 'all'
type VerificationFilter = 'all' | 'verified' | 'unverified'
type AdminUserFilterDropdown = 'role' | 'archive' | 'verification'

/**
 * Admin user management page.
 * Filters become API query params, and destructive actions require confirmation.
 */
export function AdminUsersPage() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const currentUser = useAuthStore((s) => s.user)

  const [searchQuery, setSearchQuery] = useState('')
  const [roleFilter, setRoleFilter] = useState<UserRole | ''>('')
  const [archiveFilter, setArchiveFilter] = useState<ArchiveFilter>('active')
  const [verificationFilter, setVerificationFilter] =
    useState<VerificationFilter>('all')
  const [pageNumber, setPageNumber] = useState(1)
  const pageSize = 15

  const [selectedUserForRole, setSelectedUserForRole] =
    useState<SelectedUserForRole | null>(null)
  const [openDropdown, setOpenDropdown] =
    useState<AdminUserFilterDropdown | null>(null)
  const [userToArchive, setUserToArchive] =
    useState<SelectedUserForRole | null>(null)
  const [userToRestore, setUserToRestore] =
    useState<SelectedUserForRole | null>(null)
  const [userToDelete, setUserToDelete] = useState<SelectedUserForRole | null>(
    null,
  )

  const roleFilterRef = useRef<HTMLDivElement>(null)
  const archiveFilterRef = useRef<HTMLDivElement>(null)
  const verificationFilterRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      const target = event.target as Node

      if (
        openDropdown === 'role' &&
        roleFilterRef.current &&
        !roleFilterRef.current.contains(target)
      ) {
        setOpenDropdown(null)
      } else if (
        openDropdown === 'archive' &&
        archiveFilterRef.current &&
        !archiveFilterRef.current.contains(target)
      ) {
        setOpenDropdown(null)
      } else if (
        openDropdown === 'verification' &&
        verificationFilterRef.current &&
        !verificationFilterRef.current.contains(target)
      ) {
        setOpenDropdown(null)
      }
    }

    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [openDropdown])

  const { data: usersResponse, isLoading } = useGetApiUsers({
    SearchTerm: searchQuery || undefined,
    Role: roleFilter || undefined,
    StrictRoleOnly: !!roleFilter,
    IsArchived:
      archiveFilter === 'all' ? undefined : archiveFilter === 'archived',
    IsEmailVerified:
      verificationFilter === 'all'
        ? undefined
        : verificationFilter === 'verified',
    PageNumber: pageNumber,
    PageSize: pageSize,
  })

  const archiveMutation = useDeleteApiUsersId({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: getGetApiUsersQueryKey() })
        toast.success(t('admin.userArchived'))
      },
      onError: () => {
        toast.error(t('admin.userArchiveFailed'))
      },
    },
  })

  const restoreMutation = usePatchApiUsersIdRestore({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: getGetApiUsersQueryKey() })
        toast.success(t('admin.userRestored'))
      },
      onError: () => {
        toast.error(t('admin.userRestoreFailed'))
      },
    },
  })

  const deleteMutation = useDeleteApiUsersIdHard({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: getGetApiUsersQueryKey() })
        toast.success(t('admin.userDeleted'))
      },
      onError: () => {
        toast.error(t('admin.userDeleteFailed'))
      },
    },
  })

  const users = usersResponse?.data || []
  const totalPages = Number(usersResponse?.totalPages || 1)
  const totalCount = usersResponse?.totalCount || 0
  const archiveAction =
    archiveFilter === 'active'
      ? 'archive'
      : archiveFilter === 'archived'
        ? 'restore'
        : null
  const canDeleteArchivedUsers = archiveFilter === 'archived'

  const roleOptions = useMemo(
    () => [
      { id: '', title: t('admin.allRoles') },
      { id: 'Student', title: t('common.students') },
      { id: 'Teacher', title: t('common.teachers') },
      { id: 'Admin', title: t('common.admins') },
    ],
    [t],
  )

  const archiveOptions = useMemo(
    () => [
      { id: 'active', title: t('admin.archiveStatusActive') },
      { id: 'archived', title: t('admin.archiveStatusArchived') },
      { id: 'all', title: t('admin.archiveStatusAll') },
    ],
    [t],
  )

  const verificationOptions = useMemo(
    () => [
      { id: 'all', title: t('admin.emailVerificationAll') },
      { id: 'verified', title: t('admin.emailVerificationVerified') },
      { id: 'unverified', title: t('admin.emailVerificationUnverified') },
    ],
    [t],
  )

  const filterDropdownWrapperClassName =
    'absolute top-full left-0 z-50 mt-1 w-full min-w-56 rounded-md border border-border bg-card shadow-lg sm:w-64'
  const filterDropdownButtonClassName =
    'flex h-10 w-full items-center justify-between gap-2 rounded-md border border-input bg-background px-3 py-2 text-left text-sm shadow-sm transition-colors hover:bg-muted focus:border-primary focus:outline-none sm:w-52'

  const handleArchiveConfirm = async () => {
    if (!userToArchive) return

    try {
      await archiveMutation.mutateAsync({ id: userToArchive.id })
      setUserToArchive(null)
    } catch (error) {
      console.error(error)
    }
  }

  const handleRestoreConfirm = async () => {
    if (!userToRestore) return

    try {
      await restoreMutation.mutateAsync({ id: userToRestore.id })
      setUserToRestore(null)
    } catch (error) {
      console.error(error)
    }
  }

  const handleDeleteConfirm = async () => {
    if (!userToDelete) return

    if (
      archiveFilter !== 'archived' ||
      (userToDelete.currentRole !== 'Student' &&
        userToDelete.currentRole !== 'Teacher')
    ) {
      toast.error(t('admin.userDeleteNotAllowed'))
      setUserToDelete(null)
      return
    }

    if (!currentUser?.id || currentUser.role !== 'Admin') {
      toast.error(t('admin.userDeleteMissingReplacementOwner'))
      return
    }

    try {
      await deleteMutation.mutateAsync({
        id: userToDelete.id,
        params: { replacementMediaOwnerId: currentUser.id },
      })
      setUserToDelete(null)
    } catch (error) {
      console.error(error)
    }
  }

  return (
    <>
      <PageLayout
        variant="app"
        containerClassName="max-w-7xl"
        title={
          <span className="flex items-center gap-3">
            <span className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10 text-primary">
              <Users className="h-6 w-6" />
            </span>
            {t('admin.userManagement')}
          </span>
        }
        subtitle={
          <>
            {t('admin.totalUsers')}:{' '}
            <span className="font-bold">{totalCount}</span>
          </>
        }
        headerControls={
          <div className="flex flex-col gap-4 sm:flex-row sm:flex-wrap sm:items-center">
            <DebouncedInput
              id="admin-user-search"
              value={searchQuery}
              onChange={(val) => {
                setSearchQuery(val)
                setPageNumber(1)
              }}
              placeholder={t('admin.searchUsers')}
              icon={<Search className="h-4 w-4" />}
              className="max-w-md bg-background"
              hideErrors
            />

            <div ref={roleFilterRef} className="relative w-full sm:w-auto">
              <button
                type="button"
                onClick={() =>
                  setOpenDropdown(openDropdown === 'role' ? null : 'role')
                }
                className={filterDropdownButtonClassName}
              >
                <span className="truncate">
                  {roleOptions.find((option) => option.id === roleFilter)
                    ?.title ?? t('admin.allRoles')}
                </span>
                <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
              </button>

              {openDropdown === 'role' && (
                <div className={filterDropdownWrapperClassName}>
                  <ScrollableSelectList
                    options={roleOptions}
                    selectedId={roleFilter}
                    onSelect={(id) => {
                      setRoleFilter(id as UserRole | '')
                      setPageNumber(1)
                      setOpenDropdown(null)
                    }}
                    maxHeight="max-h-[220px]"
                  />
                </div>
              )}
            </div>

            <div ref={archiveFilterRef} className="relative w-full sm:w-auto">
              <button
                type="button"
                onClick={() =>
                  setOpenDropdown(openDropdown === 'archive' ? null : 'archive')
                }
                className={filterDropdownButtonClassName}
              >
                <span className="truncate">
                  {archiveOptions.find((option) => option.id === archiveFilter)
                    ?.title ?? t('admin.archiveStatusActive')}
                </span>
                <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
              </button>

              {openDropdown === 'archive' && (
                <div className={filterDropdownWrapperClassName}>
                  <ScrollableSelectList
                    options={archiveOptions}
                    selectedId={archiveFilter}
                    onSelect={(id) => {
                      setArchiveFilter(id as ArchiveFilter)
                      setPageNumber(1)
                      setOpenDropdown(null)
                    }}
                    maxHeight="max-h-[220px]"
                  />
                </div>
              )}
            </div>

            <div
              ref={verificationFilterRef}
              className="relative w-full sm:w-auto"
            >
              <button
                type="button"
                onClick={() =>
                  setOpenDropdown(
                    openDropdown === 'verification' ? null : 'verification',
                  )
                }
                className={filterDropdownButtonClassName}
              >
                <span className="truncate">
                  {verificationOptions.find(
                    (option) => option.id === verificationFilter,
                  )?.title ?? t('admin.emailVerificationAll')}
                </span>
                <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
              </button>

              {openDropdown === 'verification' && (
                <div className={filterDropdownWrapperClassName}>
                  <ScrollableSelectList
                    options={verificationOptions}
                    selectedId={verificationFilter}
                    onSelect={(id) => {
                      setVerificationFilter(id as VerificationFilter)
                      setPageNumber(1)
                      setOpenDropdown(null)
                    }}
                    maxHeight="max-h-[220px]"
                  />
                </div>
              )}
            </div>
          </div>
        }
      >
        <AdminUsersTable
          users={users}
          isLoading={isLoading}
          onEditRole={setSelectedUserForRole}
          archiveAction={archiveAction}
          canDeleteArchivedUsers={canDeleteArchivedUsers}
          onArchiveUser={setUserToArchive}
          onRestoreUser={setUserToRestore}
          onDeleteUser={setUserToDelete}
          pendingUserId={
            archiveMutation.isPending
              ? archiveMutation.variables?.id
              : restoreMutation.isPending
                ? restoreMutation.variables?.id
                : deleteMutation.isPending
                  ? deleteMutation.variables?.id
                  : undefined
          }
        />

        {!isLoading && totalPages > 1 && (
          <div className="mt-6 flex justify-center">
            <Pagination
              currentPage={pageNumber}
              totalPages={totalPages}
              onPageChange={setPageNumber}
            />
          </div>
        )}
      </PageLayout>

      <RoleModificationModal
        key={selectedUserForRole?.id ?? 'empty-modal'}
        isOpen={!!selectedUserForRole}
        onClose={() => setSelectedUserForRole(null)}
        user={selectedUserForRole}
      />

      <ConfirmModal
        isOpen={!!userToArchive}
        onClose={() => setUserToArchive(null)}
        onConfirm={handleArchiveConfirm}
        title={t('admin.archiveUserTitle')}
        description={t('admin.archiveUserConfirm', {
          name: userToArchive?.name,
        })}
        confirmText={t('admin.archiveUser')}
        isDestructive
        isLoading={archiveMutation.isPending}
      />

      <ConfirmModal
        isOpen={!!userToRestore}
        onClose={() => setUserToRestore(null)}
        onConfirm={handleRestoreConfirm}
        title={t('admin.restoreUserTitle')}
        description={t('admin.restoreUserConfirm', {
          name: userToRestore?.name,
        })}
        confirmText={t('admin.restoreUser')}
        isLoading={restoreMutation.isPending}
      />

      <ConfirmModal
        isOpen={!!userToDelete}
        onClose={() => setUserToDelete(null)}
        onConfirm={handleDeleteConfirm}
        title={t('admin.deleteUserTitle')}
        description={t('admin.deleteUserConfirm', {
          name: userToDelete?.name,
        })}
        confirmText={t('admin.deleteUser')}
        isDestructive
        isLoading={deleteMutation.isPending}
      />
    </>
  )
}
