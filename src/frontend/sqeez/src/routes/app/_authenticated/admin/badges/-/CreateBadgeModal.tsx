import { useState, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, FormProvider } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'

import { BaseModal } from '@/components/ui/Modal'
import { Button, AsyncButton } from '@/components/ui/Button'

import { usePostApiBadges } from '@/api/generated/endpoints/badges/badges'
import { getGetApiBadgesQueryKey } from '@/api/generated/endpoints/badges/badges'
import { BadgeRulesBuilder } from './BadgeRulesBuilder'
import { getBadgeSchema } from '@/schemas/badgeSchema'
import { BadgeBasicInfoFields } from './BadgeBasicInfoFields'
import { useSystemConfig } from '@/hooks/useSystemConfig'
import {
  createSafeLocalPreviewSrc,
  isAllowedImageUploadFile,
  revokeSafeLocalPreviewSrc,
  type SafeLocalPreviewSrc,
} from '@/lib/imageHelpers'

interface CreateBadgeModalProps {
  isOpen: boolean
  onClose: () => void
}

/**
 * Badge creation modal.
 * Sends multipart form data because badge icons are uploaded together with form values.
 */
export function CreateBadgeModal({ isOpen, onClose }: CreateBadgeModalProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const schema = getBadgeSchema(t)

  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [previewUrl, setPreviewUrl] = useState<SafeLocalPreviewSrc | null>(null)
  const { config } = useSystemConfig()

  type CreateBadgeFormValues = z.infer<typeof schema>

  const methods = useForm<CreateBadgeFormValues>({
    resolver: zodResolver(schema),
    mode: 'onChange',
    defaultValues: {
      name: '',
      description: '',
      xpBonus: 0,
      rules: [
        {
          metric: 'ScorePercentage',
          operator: 'GreaterThanOrEqual',
          targetValue: 1,
        },
      ],
    },
  })

  const {
    handleSubmit,
    reset,
    formState: { isValid },
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
      setSelectedFile(file)
      setPreviewUrl(createSafeLocalPreviewSrc(file))
    }
  }

  const handleModalClose = () => {
    reset()
    setSelectedFile(null)
    revokeSafeLocalPreviewSrc(previewUrl)
    setPreviewUrl(null)
    onClose()
  }

  const createBadgeMutation = usePostApiBadges({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: getGetApiBadgesQueryKey() })
        toast.success(t('admin.badges.createdSuccessfully'))
        handleModalClose()
      },
      onError: () => toast.error(t('common.error')),
    },
  })

  const onSubmit = async (data: CreateBadgeFormValues) => {
    if (!selectedFile) {
      toast.error(t('admin.badges.iconRequired'))
      return
    }

    try {
      await createBadgeMutation.mutateAsync({
        data: {
          Name: data.name,
          Description: data.description,
          XpBonus: data.xpBonus,
          IconFile: selectedFile,
          Rules: data.rules.map((rule) => ({
            metric: rule.metric,
            operator: rule.operator,
            targetValue: rule.targetValue,
          })),
        },
      })
    } catch (error) {
      console.error('Failed to create badge:', error)
    }
  }

  return (
    <BaseModal
      isOpen={isOpen}
      onClose={handleModalClose}
      title={t('admin.badges.createBadge')}
      description={t('admin.badges.createDesc')}
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
            disabled={!isValid || !selectedFile}
            isLoading={createBadgeMutation.isPending}
            loadingText={t('common.saving') + '...'}
            className="min-w-32"
          >
            {t('common.create')}
          </AsyncButton>
        </div>
      }
    >
      <FormProvider {...methods}>
        <div className="flex max-h-[55vh] min-h-0 w-full max-w-full flex-col gap-8 overflow-x-hidden overflow-y-auto p-1 pr-3 sm:w-162.5">
          <BadgeBasicInfoFields
            fileInputRef={fileInputRef}
            onFileChange={handleFileChange}
            localPreviewUrl={previewUrl}
            hasSelectedFile={!!selectedFile}
            isEditMode={false}
          />

          <hr className="border-border" />

          <BadgeRulesBuilder />
        </div>
      </FormProvider>
    </BaseModal>
  )
}
