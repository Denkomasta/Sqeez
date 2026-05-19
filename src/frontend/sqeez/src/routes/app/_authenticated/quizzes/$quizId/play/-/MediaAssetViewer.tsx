import { useState, useEffect } from 'react'
import { AlertCircle } from 'lucide-react'
import { Spinner } from '@/components/ui/Spinner'
import { useGetApiMediaAssetsIdFile } from '@/api/generated/endpoints/media-assets/media-assets'
import { useTranslation } from 'react-i18next'

interface MediaAssetViewerProps {
  assetId: number | string
  isOption?: boolean
}

/**
 * Fetches and displays a media asset by id.
 * Rendering is type-aware and falls back to a download link for documents/unknown files.
 */
export function MediaAssetViewer({
  assetId,
  isOption = false,
}: MediaAssetViewerProps) {
  const [mediaUrl, setMediaUrl] = useState<string | null>(null)
  const { t } = useTranslation()

  const safeId = Number(assetId)

  const {
    data: blob,
    isLoading,
    isError,
  } = useGetApiMediaAssetsIdFile(safeId, {
    query: {
      staleTime: 1000 * 60 * 60,
      refetchOnWindowFocus: false,
    },
  })

  const mimeType = blob instanceof Blob ? blob.type : null

  useEffect(() => {
    if (!blob || !(blob instanceof Blob)) return

    const objectUrl = URL.createObjectURL(blob)
    // eslint-disable-next-line -- OR biome-ignore lint/performance/noSyncSetStateInEffect: Required for memory management
    setMediaUrl(objectUrl)

    return () => {
      URL.revokeObjectURL(objectUrl)
    }
  }, [blob])

  if (isLoading) {
    return (
      <div
        className={`flex w-full items-center justify-center bg-muted/10 ${isOption ? 'h-24 rounded-md' : 'h-48 rounded-xl'}`}
      >
        <Spinner size="sm" />
      </div>
    )
  }

  if (isError || !mediaUrl) {
    return (
      <div
        className={`flex w-full flex-col items-center justify-center bg-destructive/5 text-muted-foreground ${isOption ? 'h-24 rounded-md' : 'h-48 rounded-xl'}`}
      >
        <AlertCircle className="mb-1 h-6 w-6 text-destructive opacity-50" />
        <span className="text-xs">{t('media.notFound')}</span>
      </div>
    )
  }

  if (mimeType?.startsWith('video/')) {
    return (
      <div
        className={`relative flex w-full justify-center ${isOption ? 'rounded-md' : 'overflow-hidden rounded-xl'}`}
      >
        <video
          controls
          src={mediaUrl}
          className={`w-full max-w-full ${isOption ? 'max-h-32' : 'max-h-100'}`}
        />
      </div>
    )
  }

  if (mimeType?.startsWith('audio/')) {
    return (
      <div
        className={`flex w-full items-center justify-center p-4 ${isOption ? 'rounded-md' : 'rounded-xl'}`}
      >
        <audio controls src={mediaUrl} className="w-full max-w-md" />
      </div>
    )
  }

  return (
    <div
      className={`flex w-full justify-center ${isOption ? 'rounded-md' : 'rounded-xl'}`}
    >
      <img
        src={mediaUrl}
        alt={`Media asset ${assetId}`}
        className={`object-contain ${isOption ? 'max-h-32 rounded-md' : 'max-h-100 rounded-xl'}`}
        loading="lazy"
      />
    </div>
  )
}
