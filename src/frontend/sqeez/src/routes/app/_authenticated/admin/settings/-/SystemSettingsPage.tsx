import { useTranslation } from 'react-i18next'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { Building2, Globe2, HardDrive, Mail, ShieldCheck } from 'lucide-react'

import { PageLayout } from '@/components/layouting/PageLayout/PageLayout'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { Input } from '@/components/ui/Input'
import { Switch } from '@/components/ui/Switch'
import {
  getGetApiSystemConfigQueryKey,
  useGetApiSystemConfig,
  usePatchApiSystemConfig,
} from '@/api/generated/endpoints/system-config/system-config'
import type { UpdateSystemConfigDto } from '@/api/generated/model'

/**
 * Admin system settings page.
 * Uses form state to patch global configuration values exposed by the backend.
 */
export function SystemSettingsPage() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const { data: config, isLoading } = useGetApiSystemConfig()

  const patchMutation = usePatchApiSystemConfig({
    mutation: {
      onSuccess: (updatedConfig) => {
        queryClient.setQueryData(getGetApiSystemConfigQueryKey(), updatedConfig)
        toast.success(t('common.saved'))
      },
      onError: () => {
        toast.error(t('common.error'))
      },
    },
  })

  const handleUpdate = async (
    field: keyof UpdateSystemConfigDto,
    value: string | number | boolean | null,
  ) => {
    const currentCache = queryClient.getQueryData<typeof config>(
      getGetApiSystemConfigQueryKey(),
    )

    if (
      !currentCache ||
      currentCache[field as keyof typeof currentCache] === value
    )
      return

    const payload: UpdateSystemConfigDto = {
      schoolName: currentCache.schoolName,
      logoUrl: currentCache.logoUrl,
      supportEmail: currentCache.supportEmail,
      defaultLanguage: currentCache.defaultLanguage,
      currentAcademicYear: currentCache.currentAcademicYear,
      allowPublicRegistration: currentCache.allowPublicRegistration,
      requireEmailVerification: currentCache.requireEmailVerification,
      maxAvatarAndBadgeUploadSizeMB: currentCache.maxAvatarAndBadgeUploadSizeMB,
      maxQuizMediaUploadSizeMB: currentCache.maxQuizMediaUploadSizeMB,
      maxActiveSessionsPerUser: currentCache.maxActiveSessionsPerUser,
      [field]: value,
    }

    await patchMutation.mutateAsync({
      data: payload,
    })
  }

  return (
    <PageLayout
      variant="app"
      containerClassName="max-w-4xl"
      isLoading={isLoading}
      title={t('settings.systemConfig')}
      subtitle={t('settings.systemConfigDesc')}
    >
      <div className="space-y-10">
        <Card>
          <CardHeader className="border-b">
            <CardTitle className="flex items-center gap-2 text-xl">
              <Building2 className="h-5 w-5 text-primary" />
              {t('settings.general')}
            </CardTitle>
          </CardHeader>

          <CardContent className="grid grid-cols-1 gap-6 md:grid-cols-2">
            <DebouncedInput
              label={t('settings.schoolName')}
              value={config?.schoolName ?? ''}
              onChange={(val) => handleUpdate('schoolName', val)}
              placeholder="e.g. Hogwarts School"
            />

            <DebouncedInput
              label={t('settings.currentAcademicYear')}
              value={config?.currentAcademicYear ?? ''}
              onChange={(val) => handleUpdate('currentAcademicYear', val)}
              placeholder="e.g. 2025/2026"
            />

            <DebouncedInput
              label={t('settings.supportEmail')}
              type="email"
              value={config?.supportEmail ?? ''}
              onChange={(val) => handleUpdate('supportEmail', val)}
              placeholder="support@yourschool.com"
              icon={<Mail className="h-4 w-4" />}
            />

            <DebouncedInput
              label={t('settings.defaultLanguage')}
              value={config?.defaultLanguage ?? ''}
              onChange={(val) => handleUpdate('defaultLanguage', val)}
              placeholder="e.g. en, cs"
              icon={<Globe2 className="h-4 w-4" />}
            />

            <div className="col-span-1 md:col-span-2">
              <DebouncedInput
                label={t('settings.logoUrl')}
                value={config?.logoUrl ?? ''}
                onChange={(val) => handleUpdate('logoUrl', val)}
                placeholder="https://..."
                helpText={t('settings.logoUrlHelp')}
              />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="border-b">
            <CardTitle className="flex items-center gap-2 text-xl">
              <ShieldCheck className="h-5 w-5 text-primary" />
              {t('settings.security')}
            </CardTitle>
          </CardHeader>

          <CardContent className="flex flex-col gap-6">
            <div className="flex items-center justify-between rounded-lg border border-border bg-muted/30 p-4">
              <div className="space-y-0.5">
                <label className="text-base font-medium text-foreground">
                  {t('settings.allowPublicRegistration')}
                </label>
                <p className="text-sm text-muted-foreground">
                  {t('settings.allowPublicRegistrationDesc')}
                </p>
              </div>
              <Switch
                checked={!!config?.allowPublicRegistration}
                onCheckedChange={(val) =>
                  handleUpdate('allowPublicRegistration', val)
                }
                disabled={patchMutation.isPending}
              />
            </div>

            <div className="flex items-center justify-between rounded-lg border border-border bg-muted/30 p-4">
              <div className="space-y-0.5">
                <label className="text-base font-medium text-foreground">
                  {t('settings.requireEmailVerification')}
                </label>
                <p className="text-sm text-muted-foreground">
                  {t('settings.requireEmailVerificationDesc')}
                </p>
              </div>
              <Switch
                checked={!!config?.requireEmailVerification}
                onCheckedChange={(val) =>
                  handleUpdate('requireEmailVerification', val)
                }
                disabled={patchMutation.isPending}
              />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="border-b">
            <CardTitle className="flex items-center gap-2 text-xl">
              <HardDrive className="h-5 w-5 text-primary" />
              {t('settings.limits')}
            </CardTitle>
          </CardHeader>

          <CardContent className="grid grid-cols-1 gap-6 md:grid-cols-2">
            <Input
              type="number"
              min="1"
              label={t('settings.maxAvatarSizeMB')}
              value={config?.maxAvatarAndBadgeUploadSizeMB ?? 0}
              onChange={(e) =>
                handleUpdate(
                  'maxAvatarAndBadgeUploadSizeMB',
                  Number(e.target.value),
                )
              }
              disabled={patchMutation.isPending}
              helpText={t('settings.maxAvatarSizeHelp')}
            />

            <Input
              type="number"
              min="1"
              label={t('settings.maxQuizMediaSizeMB')}
              value={config?.maxQuizMediaUploadSizeMB ?? 0}
              onChange={(e) =>
                handleUpdate('maxQuizMediaUploadSizeMB', Number(e.target.value))
              }
              disabled={patchMutation.isPending}
              helpText={t('settings.maxQuizMediaSizeHelp')}
            />

            <Input
              type="number"
              min="1"
              label={t('settings.maxActiveSessions')}
              value={config?.maxActiveSessionsPerUser ?? 0}
              onChange={(e) =>
                handleUpdate('maxActiveSessionsPerUser', Number(e.target.value))
              }
              disabled={patchMutation.isPending}
              helpText={t('settings.maxActiveSessionsHelp')}
            />
          </CardContent>
        </Card>
      </div>
    </PageLayout>
  )
}
