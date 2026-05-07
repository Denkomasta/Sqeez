import { useRef, useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import {
  UploadCloud,
  X,
  FileAudio,
  FileVideo,
  ImageIcon,
  Lock,
} from 'lucide-react'
import { BaseModal } from '@/components/ui/Modal'
import { AsyncButton, Button } from '@/components/ui/Button'
import { toast } from 'sonner'
import { usePostApiMediaAssetsUpload } from '@/api/generated/endpoints/media-assets/media-assets'
import { MediaAssetViewer } from '../../play/-/MediaAssetViewer'
import { useSystemConfig } from '@/hooks/useSystemConfig'
import { useQuizEditorUIStore } from '@/store/useQuizEditorUIStore'
import { handleQuizMutationError } from '@/lib/quizHelpers'
import { cn } from '@/lib/utils'
import { getSafeImageSrc } from '@/lib/imageHelpers'

interface OptionMediaModalProps {
  isOpen: boolean
  onClose: () => void
  onSave: (newMediaAssetId: number | string | null) => Promise<void>
  currentMediaAssetId?: number | string | null
}

export function OptionMediaModal({
  isOpen,
  onClose,
  onSave,
  currentMediaAssetId,
}: OptionMediaModalProps) {
  const { t } = useTranslation()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const { config } = useSystemConfig()

  const isLocked = useQuizEditorUIStore((s) => s.isLocked)

  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [isConfirmingRemove, setIsConfirmingRemove] = useState(false)

  const uploadMedia = usePostApiMediaAssetsUpload()

  const maxSizeMB = Number(config?.maxQuizMediaUploadSizeMB) || 5

  useEffect(() => {
    return () => {
      if (previewUrl) URL.revokeObjectURL(previewUrl)
    }
  }, [previewUrl])

  const handleClose = () => {
    setSelectedFile(null)
    setIsConfirmingRemove(false)
    if (previewUrl) {
      URL.revokeObjectURL(previewUrl)
      setPreviewUrl(null)
    }
    onClose()
  }

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (isLocked) return

    const file = e.target.files?.[0]
    if (file) {
      if (file.size > maxSizeMB * 1024 * 1024) {
        toast.error(t('errors.fileTooLarge', { maxValue: maxSizeMB }))
        return
      }
      setSelectedFile(file)
      setPreviewUrl(URL.createObjectURL(file))
    }
  }

  const handleConfirmSave = async () => {
    if (isLocked) return

    try {
      if (selectedFile) {
        const response = await uploadMedia.mutateAsync({
          data: { File: selectedFile, IsPrivate: false },
        })

        if (!response.id) throw new Error('Upload failed to return an ID')

        await onSave(response.id)
        toast.success(t('editor.mediaUploaded'))
      } else {
        handleClose()
        return
      }
      handleClose()
    } catch (error) {
      handleQuizMutationError(error, t)
    }
  }

  const handleRemoveCurrentMedia = async () => {
    if (isLocked) return

    try {
      await onSave(0)
      toast.success(t('editor.mediaRemoved'))
      handleClose()
    } catch (error) {
      handleQuizMutationError(error, t)
    }
  }

  const renderLocalPreview = () => {
    if (!selectedFile || !previewUrl) return null

    const safePreviewUrl = getSafeImageSrc(previewUrl)

    if (selectedFile.type.startsWith('image/')) {
      if (!safePreviewUrl) return null

      return (
        <img
          src={safePreviewUrl}
          alt="Local Preview"
          className="max-h-48 w-full rounded-xl object-contain"
        />
      )
    }
    const Icon = selectedFile.type.startsWith('video/') ? FileVideo : FileAudio
    return (
      <div className="flex flex-col items-center justify-center rounded-xl bg-muted/20 p-4">
        <Icon className="mb-2 h-12 w-12 text-muted-foreground" />
        <p className="text-sm font-medium">{selectedFile.name}</p>
      </div>
    )
  }

  return (
    <BaseModal
      isOpen={isOpen}
      onClose={handleClose}
      title={
        isLocked ? t('editor.viewOptionMedia') : t('editor.optionMediaTitle')
      }
      description={isLocked ? undefined : t('editor.optionMediaDescription')}
      footer={
        isConfirmingRemove ? (
          <div className="flex w-full animate-in flex-col items-center justify-evenly gap-4 fade-in slide-in-from-bottom-2 sm:flex-row">
            <Button
              variant="outline"
              size="lg"
              onClick={() => setIsConfirmingRemove(false)}
              className="flex-1 sm:min-w-32 sm:flex-none"
            >
              {t('common.cancel')}
            </Button>
            <AsyncButton
              variant="destructive"
              size="lg"
              onClick={handleRemoveCurrentMedia}
              disabled={isLocked}
              className="flex-1 sm:min-w-32 sm:flex-none"
            >
              {t('common.remove')}
            </AsyncButton>
          </div>
        ) : (
          <div className="flex w-full animate-in justify-between gap-4 fade-in sm:space-x-0">
            {currentMediaAssetId && !selectedFile && !isLocked ? (
              <Button
                variant="destructive"
                size="lg"
                onClick={() => setIsConfirmingRemove(true)}
                className="min-w-32"
              >
                {t('common.remove')}
              </Button>
            ) : (
              <div />
            )}

            <div className="flex gap-2">
              <Button
                variant="outline"
                size="lg"
                onClick={handleClose}
                className="min-w-32"
              >
                {t('common.cancel')}
              </Button>
              {!isLocked && (
                <AsyncButton
                  size="lg"
                  onClick={handleConfirmSave}
                  disabled={!selectedFile}
                  loadingText={t('common.saving') + '...'}
                  className="min-w-32"
                >
                  {t('common.save')}
                </AsyncButton>
              )}
            </div>
          </div>
        )
      }
    >
      <div className="flex w-full flex-col items-center gap-6 py-4">
        <input
          type="file"
          accept="image/*, video/*, audio/*"
          className="hidden"
          ref={fileInputRef}
          onChange={handleFileChange}
          disabled={isLocked}
        />

        <div className="w-full">
          {selectedFile ? (
            <div className="group relative w-full rounded-xl border-2 border-primary/20 bg-primary/5 p-2">
              {renderLocalPreview()}
              <Button
                size="icon"
                variant="secondary"
                className="absolute top-2 right-2 rounded-full opacity-0 shadow-md transition-opacity group-hover:opacity-100"
                onClick={() => {
                  setSelectedFile(null)
                  if (previewUrl) URL.revokeObjectURL(previewUrl)
                }}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
          ) : currentMediaAssetId ? (
            <div className="group relative w-full">
              <MediaAssetViewer assetId={currentMediaAssetId} isOption />
              {!isLocked && (
                <Button
                  size="sm"
                  variant="secondary"
                  className="absolute right-2 bottom-2 gap-2 shadow-md"
                  onClick={() => fileInputRef.current?.click()}
                >
                  <UploadCloud className="h-4 w-4" />
                  {t('editor.changeMedia')}
                </Button>
              )}
            </div>
          ) : (
            <div
              className={cn(
                'flex h-40 w-full flex-col items-center justify-center rounded-xl border-2 border-dashed border-muted-foreground/50 bg-secondary/30 transition-colors',
                isLocked
                  ? 'cursor-not-allowed opacity-50'
                  : 'cursor-pointer hover:bg-secondary/50',
              )}
              onClick={() => !isLocked && fileInputRef.current?.click()}
            >
              {isLocked ? (
                <Lock className="mb-2 h-8 w-8 text-muted-foreground" />
              ) : (
                <ImageIcon className="mb-2 h-8 w-8 text-muted-foreground" />
              )}
              <span className="text-sm font-medium text-muted-foreground">
                {isLocked
                  ? t('editor.quizLockedTitle')
                  : t('editor.clickToSelectMedia')}
              </span>
              {!isLocked && (
                <span className="mt-1 text-xs text-muted-foreground/70">
                  {t('editor.maxFileSizeHintTypes', { maxSize: maxSizeMB })}
                </span>
              )}
            </div>
          )}
        </div>
      </div>
    </BaseModal>
  )
}
