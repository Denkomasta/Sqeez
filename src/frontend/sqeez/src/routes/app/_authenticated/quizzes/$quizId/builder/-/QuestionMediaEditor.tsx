import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  getGetApiQuizzesQuizIdQuestionsQuestionIdQueryKey,
  usePatchApiQuizzesQuizIdQuestionsQuestionId,
} from '@/api/generated/endpoints/quizzes/quizzes'
import { usePostApiMediaAssetsUpload } from '@/api/generated/endpoints/media-assets/media-assets'
import { Image as ImageIcon, UploadCloud, X, Loader2, Lock } from 'lucide-react'
import { toast } from 'sonner'
import { MediaAssetViewer } from '../../play/-/MediaAssetViewer'
import { AsyncButton, ConfirmModal } from '@/components/ui'
import { useQueryClient } from '@tanstack/react-query'
import { useSystemConfig } from '@/hooks/useSystemConfig'
import { handleQuizMutationError } from '@/lib/quizHelpers'
import { useQuizEditorUIStore } from '@/store/useQuizEditorUIStore'
import { cn } from '@/lib/utils'

interface QuestionMediaEditorProps {
  quizId: string
  questionId: string
  currentMediaAssetId: number | string | null
}

/**
 * Uploads, previews, or clears media attached directly to a quiz question.
 * Mutations are disabled when the quiz editor store marks the quiz as locked.
 *
 * @param props.quizId - Quiz id required by the question patch endpoint.
 * @param props.questionId - Question id whose media should be changed.
 * @param props.currentMediaAssetId - Existing media id to preview or remove.
 */
export function QuestionMediaEditor({
  quizId,
  questionId,
  currentMediaAssetId,
}: QuestionMediaEditorProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const { config } = useSystemConfig()

  const isLocked = useQuizEditorUIStore((s) => s.isLocked)

  const [isUploading, setIsUploading] = useState(false)
  const [isRemoveModalOpen, setIsRemoveModalOpen] = useState(false)

  const updateMutation = usePatchApiQuizzesQuizIdQuestionsQuestionId({
    mutation: {
      onSuccess: (updatedQuizData) => {
        queryClient.setQueryData(
          getGetApiQuizzesQuizIdQuestionsQuestionIdQueryKey(quizId, questionId),
          updatedQuizData,
        )
      },
      onError: (error) => handleQuizMutationError(error, t),
    },
  })

  const uploadMutation = usePostApiMediaAssetsUpload()
  const maxSizeMB = Number(config?.maxQuizMediaUploadSizeMB) || 10

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    if (isLocked) return

    const file = e.target.files?.[0]
    if (!file) return

    const maxSizeBytes = maxSizeMB * 1024 * 1024

    if (file.size > maxSizeBytes) {
      toast.error(t('errors.fileTooLarge', { maxValue: maxSizeMB }))
      e.target.value = ''
      return
    }

    setIsUploading(true)
    try {
      const uploadResponse = await uploadMutation.mutateAsync({
        data: {
          File: file,
          IsPrivate: false,
        },
      })

      const newAssetId = uploadResponse.id

      if (!newAssetId) throw new Error('No asset ID returned from upload')

      await updateMutation.mutateAsync({
        quizId,
        questionId,
        data: { mediaAssetId: newAssetId },
      })

      toast.success(t('editor.mediaUploaded'))
    } catch (error) {
      console.error('Upload failed', error)
      if (!updateMutation.isError) {
        toast.error(t('common.error'))
      }
    } finally {
      setIsUploading(false)
      e.target.value = ''
    }
  }

  const handleRemoveMedia = async () => {
    if (isLocked) return

    try {
      await updateMutation.mutateAsync({
        quizId,
        questionId,
        data: { mediaAssetId: 0 }, // never existing id
      })
      setIsRemoveModalOpen(false)
      toast.success(t('editor.mediaRemoved'))
    } catch {
      // Handled by mutation onError
    }
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2 text-xs font-black tracking-widest text-muted-foreground uppercase">
        <ImageIcon className="h-4 w-4" /> {t('editor.mediaAsset')}
        {isLocked && <Lock className="h-3 w-3" />}
      </div>

      {currentMediaAssetId ? (
        <div className="w-full overflow-hidden rounded-xl border-2 border-muted bg-muted/5">
          {!isLocked && (
            <div className="flex items-center justify-end border-b-2 border-muted bg-muted/20 p-2">
              <AsyncButton
                onClick={() => setIsRemoveModalOpen(true)}
                variant="ghost"
                size="sm"
                className="h-8 gap-2 text-destructive hover:bg-destructive hover:text-destructive-foreground"
                title={t('editor.removeMedia')}
              >
                <X className="size-4" />
                <span className="text-sm font-medium">
                  {t('common.remove')}
                </span>
              </AsyncButton>
            </div>
          )}

          <div className="flex min-h-30 w-full items-center justify-center p-4">
            <MediaAssetViewer assetId={currentMediaAssetId} />
          </div>
        </div>
      ) : (
        <label
          className={cn(
            'flex h-32 w-full flex-col items-center justify-center rounded-xl border-2 border-dashed border-muted-foreground/30 bg-muted/5 transition-colors',
            isLocked
              ? 'cursor-not-allowed opacity-70 grayscale-[0.2]'
              : 'cursor-pointer hover:border-primary/50 hover:bg-primary/5',
          )}
        >
          {isUploading ? (
            <div className="flex flex-col items-center gap-2">
              <Loader2 className="h-6 w-6 animate-spin text-primary" />
              <span className="text-xs font-medium text-muted-foreground">
                {t('common.loading')}
              </span>
            </div>
          ) : (
            <>
              <UploadCloud className="mb-2 h-6 w-6 text-muted-foreground" />
              <span className="text-sm font-medium text-muted-foreground">
                {t('editor.uploadMediaHint')}
              </span>
              <span className="mt-1 text-xs text-muted-foreground/70">
                {t('editor.maxFileSizeHintTypes', { maxSize: maxSizeMB })}
              </span>
              <input
                type="file"
                accept="image/*,video/*,audio/*"
                className="hidden"
                onChange={handleFileUpload}
                disabled={isUploading || isLocked}
              />
            </>
          )}
        </label>
      )}

      {!isLocked && (
        <ConfirmModal
          isOpen={isRemoveModalOpen}
          onClose={() => setIsRemoveModalOpen(false)}
          onConfirm={handleRemoveMedia}
          title={t('editor.removeMediaTitle')}
          description={t('editor.removeMediaDesc')}
          isDestructive={true}
          confirmText={t('common.remove')}
        />
      )}
    </div>
  )
}
