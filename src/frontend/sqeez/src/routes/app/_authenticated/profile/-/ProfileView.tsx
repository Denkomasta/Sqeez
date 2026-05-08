import { useState } from 'react'
import { useAuthStore } from '@/store/useAuthStore'
import { SimpleAvatar } from '@/components/ui/Avatar'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/Card'
import { Input } from '@/components/ui/Input'
import {
  Mail,
  Phone,
  Briefcase,
  Shield,
  Star,
  User as UserIcon,
  Camera,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { EditableInfoItem } from '@/components/ui/InfoItem'
import { AsyncButton, Button } from '@/components/ui/Button'
import { BaseModal } from '@/components/ui/Modal'
import { PageLayout } from '@/components/layouting/PageLayout/PageLayout'
import { toast } from 'sonner'
import { calculateLevel, formatName } from '@/lib/userHelpers'
import {
  useGetApiUsersIdDetails,
  usePatchApiUsersId,
} from '@/api/generated/endpoints/user/user'
import type { PatchStudentDto, UserRole } from '@/api/generated/model'
import { getImageUrl } from '@/lib/imageHelpers'
import { getGetApiAuthMeQueryKey } from '@/api/generated/endpoints/auth/auth'
import { useQueryClient } from '@tanstack/react-query'
import { AvatarUploadModal } from './AvatarUploadModal'
import { Link } from '@tanstack/react-router'
import { StudentBadge } from '@/components/ui/StudentBadge'
import { useSystemConfig } from '@/hooks/useSystemConfig'
import { formatPhoneForDb, formatPhoneForDisplay } from '@/lib/phoneHelpers'
import { getLastSeenStatus } from '@/lib/dateHelpers'

type EditFieldState = {
  key: string
  label: string
  value: string
} | null

export function ProfileView({ targetUserId }: { targetUserId?: number }) {
  const { t } = useTranslation()
  const currentUser = useAuthStore((s) => s.user)

  const idToFetch = Number(targetUserId || currentUser?.id)
  const isOwnProfile = currentUser?.id === idToFetch

  const {
    data: profileData,
    isLoading,
    refetch,
  } = useGetApiUsersIdDetails(idToFetch ?? 0, {
    query: { enabled: !!idToFetch },
  })

  const { config, isLoading: isSystemConfigLoading } = useSystemConfig()

  const updateProfile = usePatchApiUsersId()

  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingField, setEditingField] = useState<EditFieldState>(null)
  const [editValue, setEditValue] = useState('')
  const [editError, setEditError] = useState<string | undefined>(undefined)

  const [isAvatarModalOpen, setIsAvatarModalOpen] = useState(false)

  const queryClient = useQueryClient()

  const recentBadges = [...(profileData?.badges || [])]
    .sort((a, b) => {
      const dateA = a.earnedAt ? new Date(a.earnedAt).getTime() : 0
      const dateB = b.earnedAt ? new Date(b.earnedAt).getTime() : 0
      return dateB - dateA
    })
    .slice(0, 3)

  if (!profileData) {
    return (
      <PageLayout
        isLoading={isLoading || isSystemConfigLoading}
        containerClassName="max-w-7xl text-center text-muted-foreground"
      >
        {t('errors.userNotFound')}
      </PageLayout>
    )
  }

  const validateField = (key: string, value: string): string | null => {
    const trimmedValue = value.trim()
    if (!trimmedValue) return t('errors.required')

    switch (key) {
      case 'email': {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
        if (!emailRegex.test(trimmedValue)) return t('errors.invalidEmail')
        break
      }
      case 'username': {
        const safeUsernameRegex =
          /^[a-zA-Z0-9_\-áéíóúýčďěňřšťžÁÉÍÓÚÝČĎĚŇŘŠŤŽ]+$/
        if (trimmedValue.length < 3) return t('errors.usernameShort')
        if (trimmedValue.length > 20) return t('errors.usernameLong')
        if (!safeUsernameRegex.test(trimmedValue))
          return t('errors.invalidCharacters')
        break
      }
      case 'phoneNumber': {
        const cleanedPhone = trimmedValue.replace(/[\s\-()]/g, '')
        const phoneRegex = /^(?:\+|00)?[0-9]{7,15}$/

        if (trimmedValue && !phoneRegex.test(cleanedPhone))
          return t('errors.invalidPhone')
        break
      }
    }
    return null
  }

  const handleEditClick = (
    key: string,
    label: string,
    currentValue: string,
  ) => {
    if (!isOwnProfile) return
    setEditingField({ key, label, value: currentValue })
    setEditValue(currentValue)
    setIsModalOpen(true)
  }

  const handleCloseModal = () => {
    setIsModalOpen(false)
    setEditingField(null)
    setEditError(undefined)
  }

  const handleSave = async () => {
    if (!editingField || !isOwnProfile || !currentUser || !profileData.role)
      return

    const validationError = validateField(editingField.key, editValue)
    if (validationError) {
      setEditError(validationError)
      return
    }

    try {
      const discriminatorMap: Record<
        UserRole,
        'student' | 'teacher' | 'admin'
      > = {
        Student: 'student',
        Teacher: 'teacher',
        Admin: 'admin',
      }

      const discriminator =
        discriminatorMap[profileData.role as UserRole] ?? 'student'

      let finalValue = editValue.trim()
      if (editingField.key === 'phoneNumber') {
        finalValue = formatPhoneForDb(finalValue)
      }

      const payload: PatchStudentDto = {
        role: discriminator,
        [editingField.key]: finalValue,
      }

      await updateProfile.mutateAsync({
        id: currentUser.id,
        data: payload,
      })

      toast.success(t('common.successfulChange'))
      handleCloseModal()
      queryClient.invalidateQueries({
        queryKey: getGetApiAuthMeQueryKey(),
      })
      refetch()
    } catch (error) {
      console.error(error)
      toast.error(t('common.unsuccessfulChange'))
    }
  }

  const handleAvatarClick = () => {
    if (!isOwnProfile) return
    setIsAvatarModalOpen(true)
  }

  const handleCloseAvatarModal = () => {
    setIsAvatarModalOpen(false)
  }

  const onAvatarSave = () => {
    queryClient.invalidateQueries({
      queryKey: getGetApiAuthMeQueryKey(),
    })
    refetch()
  }

  const status = getLastSeenStatus(profileData.lastSeen, t)

  return (
    <PageLayout
      containerClassName="max-w-7xl"
      isLoading={isLoading || isSystemConfigLoading}
      title={
        isOwnProfile
          ? t('profile.title')
          : t('profile.userTitle', {
              name: formatName(profileData.firstName, profileData.lastName),
            })
      }
    >
      <div className="grid gap-6 md:grid-cols-3">
        <Card className="self-start shadow-sm md:col-span-1">
          <CardContent className="flex flex-col items-center pt-8 pb-8">
            <div
              className={`relative rounded-full ${isOwnProfile ? 'group cursor-pointer' : ''}`}
              onClick={handleAvatarClick}
            >
              <SimpleAvatar
                url={getImageUrl(profileData.avatarUrl)}
                firstName={profileData.firstName}
                lastName={profileData.lastName}
                wrapperClassName="size-32 border-4 border-primary/10"
                imageClassName="object-cover"
                fallbackClassName="bg-primary text-4xl font-bold text-primary-foreground"
              />

              {isOwnProfile && (
                <div className="absolute inset-0 flex items-center justify-center rounded-full bg-black/50 opacity-0 transition-opacity duration-200 group-hover:opacity-100">
                  <Camera className="h-8 w-8 text-white" />
                </div>
              )}
            </div>

            <h2 className="mt-5 text-center text-2xl font-bold">
              {formatName(profileData.firstName, profileData.lastName)}
            </h2>
            <p className="mt-1 text-center text-sm text-muted-foreground">
              @{profileData.username}
            </p>

            <div className="mt-2 flex items-center justify-center gap-2 text-sm text-muted-foreground">
              <span className="relative flex h-3 w-3">
                {status.isOnline && (
                  <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-success opacity-75"></span>
                )}
                <span
                  className={`relative inline-flex h-3 w-3 rounded-full ${
                    status.isOnline ? 'bg-success' : 'bg-muted-foreground/40'
                  }`}
                ></span>
              </span>
              <span className="font-medium">{status.text}</span>
            </div>

            <div className="mt-6 flex w-full flex-col gap-3">
              <div className="flex items-center justify-center gap-2 rounded-full bg-secondary/80 px-4 py-2 text-sm font-semibold text-secondary-foreground shadow-sm">
                <Star className="h-4 w-4 fill-nav-yellow text-nav-yellow" />
                {t('class.level', {
                  level: calculateLevel(profileData.currentXP),
                })}
                <span className="ml-1 font-normal text-muted-foreground">
                  ({profileData.currentXP ?? 0} {t('common.xp')})
                </span>
              </div>
            </div>
          </CardContent>
        </Card>

        <div className="flex flex-col gap-6 md:col-span-2">
          <Card className="h-fit shadow-sm">
            <CardHeader>
              <CardTitle className="text-xl">
                {t('profile.accountDetails')}
              </CardTitle>
              <CardDescription className="text-base">
                {isOwnProfile
                  ? t('profile.description')
                  : t('profile.viewOnlyDescription')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="grid gap-4 sm:grid-cols-2">
                <EditableInfoItem
                  icon={<UserIcon className="size-4" />}
                  label={t('common.username')}
                  value={profileData.username ?? 'john_doe'}
                  fieldKey="username"
                  canEdit={isOwnProfile}
                  onEdit={handleEditClick}
                />
                <EditableInfoItem
                  icon={<Mail className="size-4" />}
                  label={t('common.email')}
                  value={profileData.email ?? 'john_doe@sqeez.org'}
                  fieldKey="email"
                  canEdit={false} // Email is not editable for now
                  onEdit={() => {}}
                />
                <EditableInfoItem
                  icon={<Shield className="size-4" />}
                  label={t('common.role')}
                  value={profileData.role ?? 'Student'}
                  fieldKey="role"
                  canEdit={false}
                  onEdit={handleEditClick}
                />

                {profileData.department && (
                  <EditableInfoItem
                    icon={<Briefcase className="size-4" />}
                    label={t('common.department')}
                    value={profileData.department}
                    fieldKey="department"
                    canEdit={isOwnProfile}
                    onEdit={handleEditClick}
                  />
                )}

                {profileData.phoneNumber && (
                  <EditableInfoItem
                    icon={<Phone className="size-4" />}
                    label={t('common.phoneNumber')}
                    value={formatPhoneForDisplay(profileData.phoneNumber)}
                    fieldKey="phoneNumber"
                    canEdit={isOwnProfile}
                    onEdit={handleEditClick}
                  />
                )}
              </div>
            </CardContent>
          </Card>

          <Card className="h-fit shadow-sm">
            <CardHeader className="flex flex-row items-center justify-between pb-4">
              <div className="space-y-1">
                <CardTitle className="text-xl">
                  {t('profile.recentBadges')}{' '}
                </CardTitle>
                <CardDescription className="text-base">
                  {t('profile.badgesDescription')}
                </CardDescription>
              </div>

              <Button variant="ghost" size="sm" asChild>
                <Link
                  to="/app/badges/$userId"
                  params={{ userId: (idToFetch ?? 0).toString() }}
                >
                  {t('common.viewAll')}
                </Link>
              </Button>
            </CardHeader>

            <CardContent>
              {recentBadges.length > 0 ? (
                <div className="grid grid-cols-2 gap-4 sm:grid-cols-3">
                  {recentBadges.map((badge) => (
                    <StudentBadge
                      key={badge.badgeId}
                      name={badge.name}
                      iconUrl={badge.iconUrl}
                      earnedAt={badge.earnedAt}
                    />
                  ))}
                </div>
              ) : (
                <div className="flex flex-col items-center justify-center rounded-lg border border-dashed py-8 text-center">
                  <Shield className="mb-2 h-8 w-8 text-muted-foreground/50" />
                  <p className="text-sm font-medium text-muted-foreground">
                    {t('profile.noBadges')}
                  </p>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {isOwnProfile && (
        <BaseModal
          isOpen={isModalOpen}
          onClose={handleCloseModal}
          title={`${t('common.edit')} ${editingField?.label.toLowerCase()}`}
          description={t('profile.editModalDescription', {
            field: editingField?.label?.toLowerCase(),
          })}
          footer={
            <div className="flex w-full justify-center gap-4 sm:space-x-0">
              <Button
                variant="outline"
                size="lg"
                onClick={handleCloseModal}
                className="min-w-32"
              >
                {t('common.cancel')}
              </Button>
              <AsyncButton
                size="lg"
                onClick={handleSave}
                loadingText={t('common.saving') + '...'}
                className="min-w-32"
              >
                {t('common.save')}
              </AsyncButton>
            </div>
          }
        >
          <div className="grid gap-4">
            <div className="flex flex-col gap-2">
              <label htmlFor="edit-input" className="text-sm font-medium">
                {editingField?.label}
              </label>
              <Input
                id="edit-input"
                value={editValue}
                onChange={(e) => setEditValue(e.target.value)}
                autoFocus
                error={editError}
              />
            </div>
          </div>
        </BaseModal>
      )}

      {isOwnProfile && (
        <AvatarUploadModal
          maxFileSizeMB={
            config?.maxAvatarAndBadgeUploadSizeMB
              ? Number(config.maxAvatarAndBadgeUploadSizeMB)
              : undefined
          }
          isOpen={isAvatarModalOpen}
          onClose={handleCloseAvatarModal}
          onUpload={onAvatarSave}
        />
      )}
    </PageLayout>
  )
}
