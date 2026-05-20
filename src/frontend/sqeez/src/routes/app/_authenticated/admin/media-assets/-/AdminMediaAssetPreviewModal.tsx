import { useTranslation } from 'react-i18next'

import { BaseModal } from '@/components/ui/Modal'
import { MediaAssetViewer } from '@/routes/app/_authenticated/quizzes/$quizId/play/-/MediaAssetViewer'
import type { MediaAssetDto } from '@/api/generated/model'
import { getMediaAssetName } from '@/lib/mediaAssetHelpers'

interface AdminMediaAssetPreviewModalProps {
  mediaAsset: MediaAssetDto | null
  onClose: () => void
}

/**
 * Admin media preview modal.
 * Reuses the quiz media viewer so asset rendering stays consistent across the app.
 */
export function AdminMediaAssetPreviewModal({
  mediaAsset,
  onClose,
}: AdminMediaAssetPreviewModalProps) {
  const { t } = useTranslation()

  return (
    <BaseModal
      isOpen={!!mediaAsset}
      onClose={onClose}
      title={t('admin.mediaAssets.previewTitle')}
      description={
        mediaAsset ? getMediaAssetName(mediaAsset.locationUrl) : undefined
      }
      className="w-[min(92vw,48rem)]"
    >
      {mediaAsset && (
        <div className="w-full">
          <MediaAssetViewer assetId={mediaAsset.id} />
        </div>
      )}
    </BaseModal>
  )
}
