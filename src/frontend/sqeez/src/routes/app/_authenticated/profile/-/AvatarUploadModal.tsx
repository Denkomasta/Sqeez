import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Camera } from 'lucide-react'
import { BaseModal } from '@/components/ui/Modal'
import { AsyncButton, Button } from '@/components/ui/Button'
import { toast } from 'sonner'
import { usePostApiUsersMeAvatar } from '@/api/generated/endpoints/user/user'
import { getSafeImageSrc } from '@/lib/imageHelpers'

interface AvatarUploadModalProps {
  isOpen: boolean
  onClose: () => void
  onUpload: () => void
  maxFileSizeMB?: number
  title?: string
  description?: string
}

export function AvatarUploadModal({
  isOpen,
  onClose,
  onUpload,
  maxFileSizeMB = 1,
  title,
  description,
}: AvatarUploadModalProps) {
  const { t } = useTranslation()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)

  const uploadAvatar = usePostApiUsersMeAvatar()
  const safePreviewUrl = getSafeImageSrc(previewUrl)

  const handleClose = () => {
    setSelectedFile(null)
    if (previewUrl) {
      URL.revokeObjectURL(previewUrl)
      setPreviewUrl(null)
    }
    onClose()
  }

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) {
      if (file.size > maxFileSizeMB * 1024 * 1024) {
        toast.error(t('errors.fileTooLarge', { maxValue: maxFileSizeMB }))
        return
      }
      setSelectedFile(file)
      setPreviewUrl(URL.createObjectURL(file))
    }
  }

  const handleSave = async () => {
    if (!selectedFile) return

    try {
      await uploadAvatar.mutateAsync({
        data: {
          file: selectedFile,
        },
      })

      toast.success(t('profile.avatarUpdated'))
      onUpload()
      handleClose()
    } catch (error) {
      console.error(error)
      toast.error(t('profile.avatarUpdateFailed'))
    }
  }

  return (
    <BaseModal
      isOpen={isOpen}
      onClose={handleClose}
      title={title || t('profile.updateAvatar')}
      description={description || t('profile.updateAvatarDescription')}
      footer={
        <div className="flex w-full justify-center gap-4 sm:space-x-0">
          <Button
            variant="outline"
            size="lg"
            onClick={handleClose}
            className="min-w-32"
          >
            {t('common.cancel')}
          </Button>
          <AsyncButton
            size="lg"
            onClick={handleSave}
            disabled={!selectedFile}
            loadingText={t('common.saving') + '...'}
            className="min-w-32"
          >
            {t('common.confirm')}
          </AsyncButton>
        </div>
      }
    >
      <div className="flex flex-col items-center gap-6 py-4">
        <input
          type="file"
          accept="image/jpeg, image/png, image/gif"
          className="hidden"
          ref={fileInputRef}
          onChange={handleFileChange}
        />

        {safePreviewUrl ? (
          <div className="relative">
            <img
              src={safePreviewUrl}
              alt="Avatar Preview"
              className="size-40 rounded-full border-4 border-primary/20 object-cover shadow-md"
            />
            <Button
              size="sm"
              variant="secondary"
              className="absolute right-0 bottom-0 rounded-full"
              onClick={() => fileInputRef.current?.click()}
            >
              <Camera className="h-4 w-4" />
            </Button>
          </div>
        ) : (
          <div
            className="flex size-40 cursor-pointer flex-col items-center justify-center rounded-full border-2 border-dashed border-muted-foreground/50 bg-secondary/30 transition-colors hover:bg-secondary/50"
            onClick={() => fileInputRef.current?.click()}
          >
            <Camera className="mb-2 h-8 w-8 text-muted-foreground" />
            <span className="text-xs font-medium text-muted-foreground">
              {t('profile.clickToSelect')}
            </span>
          </div>
        )}

        {selectedFile && (
          <p className="max-w-62.5 truncate text-sm text-muted-foreground">
            {selectedFile.name}
          </p>
        )}
      </div>
    </BaseModal>
  )
}
