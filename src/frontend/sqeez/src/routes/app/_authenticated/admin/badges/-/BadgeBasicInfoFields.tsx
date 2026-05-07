import { useTranslation } from 'react-i18next'
import { useFormContext } from 'react-hook-form'
import { Camera } from 'lucide-react'

import { Input } from '@/components/ui/Input'
import { TextArea } from '@/components/ui/TextArea'
import { Button } from '@/components/ui/Button'
import type { BadgeFormValues } from '@/schemas/badgeSchema'
import { getSafeImageSrc } from '@/lib/imageHelpers'

interface BadgeBasicInfoFieldsProps {
  fileInputRef: React.RefObject<HTMLInputElement | null>
  onFileChange: (e: React.ChangeEvent<HTMLInputElement>) => void
  previewUrl: string | null
  hasSelectedFile: boolean
  isEditMode?: boolean
}

export function BadgeBasicInfoFields({
  fileInputRef,
  onFileChange,
  previewUrl,
  hasSelectedFile,
  isEditMode = false,
}: BadgeBasicInfoFieldsProps) {
  const { t } = useTranslation()
  const {
    register,
    formState: { errors },
  } = useFormContext<BadgeFormValues>()
  const safePreviewUrl = getSafeImageSrc(previewUrl)

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col items-center gap-2 pt-2">
        <input
          type="file"
          accept="image/jpeg, image/png, image/svg+xml"
          className="hidden"
          ref={fileInputRef}
          onChange={onFileChange}
        />
        {safePreviewUrl ? (
          <div className="relative">
            <div className="flex size-32 items-center justify-center rounded-xl border border-border bg-muted/30 p-2 shadow-sm">
              <img
                src={safePreviewUrl}
                alt="Preview"
                className="h-full w-full object-contain"
              />
            </div>
            <Button
              type="button"
              size="sm"
              variant="secondary"
              className="absolute -right-3 -bottom-3 rounded-full shadow-md"
              onClick={() => fileInputRef?.current?.click()}
              title={
                isEditMode
                  ? t('admin.badges.changeIcon')
                  : t('admin.badges.uploadIcon')
              }
            >
              <Camera className="size-4" />
            </Button>
          </div>
        ) : (
          <div
            className="flex size-32 cursor-pointer flex-col items-center justify-center rounded-xl border-2 border-dashed border-muted-foreground/50 bg-secondary/30 transition-colors hover:bg-secondary/50"
            onClick={() => fileInputRef?.current?.click()}
          >
            <Camera className="mb-2 size-8 text-muted-foreground" />
            <span className="text-center text-xs font-medium text-muted-foreground">
              {t('admin.badges.uploadIcon')}
            </span>
          </div>
        )}
        {!isEditMode && !hasSelectedFile && (
          <p className="text-xs text-destructive">{t('common.required')}</p>
        )}
      </div>

      <div className="flex flex-col gap-4">
        <Input
          label={t('common.name')}
          placeholder={t('admin.badges.namePlaceholder')}
          error={errors.name?.message}
          {...register('name')}
        />
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Input
            type="number"
            label={t('admin.badges.xpBonus')}
            placeholder="100"
            min={0}
            error={errors.xpBonus?.message}
            {...register('xpBonus', { valueAsNumber: true })}
          />
        </div>
        <TextArea
          label={t('common.description')}
          placeholder={t('admin.badges.descPlaceholder')}
          error={errors.description?.message}
          {...register('description')}
        />
      </div>
    </div>
  )
}
