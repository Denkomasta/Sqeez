import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  ShieldAlert,
  GraduationCap,
  BookOpen,
  AlertTriangle,
  type LucideIcon,
} from 'lucide-react'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'

import { usePatchApiAuthElevate } from '@/api/generated/endpoints/auth/auth'

import { BaseModal } from '@/components/ui/Modal'
import { Button, AsyncButton } from '@/components/ui/Button'
import type { UserRole } from '@/api/generated/model'
import { getGetApiUsersQueryKey } from '@/api/generated/endpoints/user/user'

interface RoleModificationModalProps {
  isOpen: boolean
  onClose: () => void
  user: {
    id: string | number
    name: string
    currentRole: UserRole
  } | null
}

/**
 * Modal for changing a user's role.
 * Display labels are translated, while submitted values remain backend UserRole values.
 */
export function RoleModificationModal({
  isOpen,
  onClose,
  user,
}: RoleModificationModalProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const [selectedRole, setSelectedRole] = useState<UserRole | null>(
    user?.currentRole ?? null,
  )

  const elevateMutation = usePatchApiAuthElevate({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: getGetApiUsersQueryKey() })
        toast.success(t('admin.roleUpdated'))
        onClose()
      },
      onError: () => {
        toast.error(t('common.error'))
      },
    },
  })

  const handleSave = async () => {
    if (!user || !selectedRole || selectedRole === user.currentRole) {
      onClose()
      return
    }

    try {
      await elevateMutation.mutateAsync({
        data: {
          id: user.id,
          role: selectedRole,
        },
      })
    } catch (error) {
      console.error('Failed to elevate user:', error)
    }
  }

  const isDestructive =
    selectedRole === 'Admin' && user?.currentRole !== 'Admin'

  const roleOptions: Array<readonly [UserRole, string, LucideIcon]> = [
    ['Student', t('common.student'), GraduationCap],
    ['Teacher', t('common.teacher'), BookOpen],
    ['Admin', t('common.admin'), ShieldAlert],
  ]

  return (
    <BaseModal
      isOpen={isOpen}
      onClose={onClose}
      title={t('admin.modifyRoleTitle')}
      description={t('admin.modifyRoleDesc', { name: user?.name })}
      footer={
        <div className="flex w-full justify-between gap-4">
          <Button
            variant="outline"
            size="lg"
            onClick={onClose}
            className="min-w-32"
          >
            {t('common.cancel')}
          </Button>
          <AsyncButton
            size="lg"
            variant={isDestructive ? 'destructive' : 'default'}
            onClick={handleSave}
            disabled={!selectedRole || selectedRole === user?.currentRole}
            loadingText={t('common.saving') + '...'}
            className="min-w-32"
          >
            {t('common.save')}
          </AsyncButton>
        </div>
      }
    >
      <div className="flex flex-col gap-4 py-4">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          {roleOptions.map(([role, label, Icon]) => {
            const isSelected = selectedRole === role
            return (
              <button
                key={role}
                onClick={() => setSelectedRole(role)}
                className={`flex flex-col items-center gap-2 rounded-xl border-2 p-4 transition-all ${
                  isSelected
                    ? 'border-primary bg-primary/10 shadow-sm'
                    : 'border-border bg-card hover:bg-muted/50'
                }`}
              >
                <Icon
                  className={`h-6 w-6 ${isSelected ? 'text-primary' : 'text-muted-foreground'}`}
                />
                <span
                  className={`font-semibold ${isSelected ? 'text-primary' : 'text-foreground'}`}
                >
                  {label}
                </span>
              </button>
            )
          })}
        </div>

        {isDestructive && (
          <div className="mt-4 flex items-start gap-3 rounded-lg border border-destructive/20 bg-destructive/10 p-4 text-destructive">
            <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0" />
            <div className="flex flex-col gap-1 text-sm">
              <span className="font-bold">{t('admin.warning')}</span>
              <span>{t('admin.adminWarningDesc')}</span>
            </div>
          </div>
        )}
      </div>
    </BaseModal>
  )
}
