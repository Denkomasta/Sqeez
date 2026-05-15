import { useState, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, FormProvider } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'

import { BaseModal } from '@/components/ui/Modal'
import { Button, AsyncButton } from '@/components/ui/Button'

import {
  usePatchApiBadgesId,
  getGetApiBadgesQueryKey,
} from '@/api/generated/endpoints/badges/badges'
import type {
  BadgeDto,
  BadgeMetric,
  BadgeOperator,
} from '@/api/generated/model'
import {
  createSafeLocalPreviewSrc,
  getImageUrl,
  isAllowedImageUploadFile,
  revokeSafeLocalPreviewSrc,
  type SafeLocalPreviewSrc,
} from '@/lib/imageHelpers'
import { getBadgeSchema } from '@/schemas/badgeSchema'
import { BadgeRulesBuilder } from './BadgeRulesBuilder'
import { BadgeBasicInfoFields } from './BadgeBasicInfoFields'
import { useSystemConfig } from '@/hooks/useSystemConfig'

interface EditBadgeModalProps {
  isOpen: boolean
  onClose: () => void
  badge: BadgeDto | null
}

export function EditBadgeModal({
  isOpen,
  onClose,
  badge,
}: EditBadgeModalProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const schema = getBadgeSchema(t)

  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [localPreviewUrl, setLocalPreviewUrl] =
    useState<SafeLocalPreviewSrc | null>(null)
  const { config } = useSystemConfig()

  const badgeIconUrl = badge?.iconUrl ? getImageUrl(badge.iconUrl) : null

  type EditBadgeFormValues = z.infer<typeof schema>

  const methods = useForm<EditBadgeFormValues>({
    resolver: zodResolver(schema),
    mode: 'onChange',
    values: badge
      ? {
          name: badge.name || '',
          description: badge.description || '',
          xpBonus: Number(badge.xpBonus || 0),
          rules: badge.rules?.length
            ? badge.rules.map((r) => ({
                id: r.id,
                metric: r.metric as BadgeMetric,
                operator: r.operator as BadgeOperator,
                targetValue: Number(r.targetValue),
              }))
            : [],
        }
      : undefined,
  })

  const {
    handleSubmit,
    reset,
    formState: { isValid, isDirty },
  } = methods

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) {
      if (!isAllowedImageUploadFile(file)) {
        toast.error(t('errors.invalidFileType'))
        return
      }

      const maxSizeMB = Number(config?.maxAvatarAndBadgeUploadSizeMB) || 1
      const maxSizeBytes = maxSizeMB * 1024 * 1024

      if (file.size > maxSizeBytes) {
        toast.error(t('errors.fileTooLarge', { maxValue: maxSizeMB }))
        return
      }

      revokeSafeLocalPreviewSrc(localPreviewUrl)

      setSelectedFile(file)
      setLocalPreviewUrl(createSafeLocalPreviewSrc(file))
    }
  }

  const handleModalClose = () => {
    reset()
    setSelectedFile(null)

    revokeSafeLocalPreviewSrc(localPreviewUrl)
    setLocalPreviewUrl(null)

    onClose()
  }

  const updateBadgeMutation = usePatchApiBadgesId({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: getGetApiBadgesQueryKey() })
        toast.success(t('admin.badges.updatedSuccessfully'))
        handleModalClose()
      },
      onError: () => toast.error(t('common.error')),
    },
  })

  const onSubmit = async (data: EditBadgeFormValues) => {
    if (!badge) return

    try {
      await updateBadgeMutation.mutateAsync({
        id: badge.id.toString(),
        data: {
          Name: data.name,
          Description: data.description,
          XpBonus: data.xpBonus,
          NewIconFile: selectedFile || undefined,
          Rules: data.rules.map((rule) => ({
            id: rule.id ?? null,
            metric: rule.metric,
            operator: rule.operator,
            targetValue: rule.targetValue,
          })),
        },
      })
    } catch (error) {
      console.error('Failed to update badge:', error)
    }
  }

  const canSave = isValid && (isDirty || !!selectedFile)

  return (
    <BaseModal
      isOpen={isOpen}
      onClose={handleModalClose}
      title={t('admin.badges.editBadge')}
      description={t('admin.badges.editDesc')}
      className="sm:max-w-fit"
      footer={
        <div className="flex w-full justify-center gap-4 sm:space-x-0">
          <Button
            variant="outline"
            size="lg"
            onClick={handleModalClose}
            className="min-w-32"
          >
            {t('common.cancel')}
          </Button>
          <AsyncButton
            size="lg"
            onClick={handleSubmit(onSubmit)}
            disabled={!canSave}
            isLoading={updateBadgeMutation.isPending}
            loadingText={t('common.saving') + '...'}
            className="min-w-32"
          >
            {t('common.save')}
          </AsyncButton>
        </div>
      }
    >
      <FormProvider {...methods}>
        <div className="flex max-h-[55vh] min-h-0 w-full max-w-full flex-col gap-8 overflow-x-hidden overflow-y-auto p-1 pr-3 sm:w-162.5">
          <BadgeBasicInfoFields
            fileInputRef={fileInputRef}
            onFileChange={handleFileChange}
            previewUrl={badgeIconUrl}
            localPreviewUrl={localPreviewUrl}
            hasSelectedFile={!!selectedFile}
            isEditMode={true}
          />

          <hr className="border-border" />

          <BadgeRulesBuilder />
        </div>
      </FormProvider>
    </BaseModal>
  )
}
