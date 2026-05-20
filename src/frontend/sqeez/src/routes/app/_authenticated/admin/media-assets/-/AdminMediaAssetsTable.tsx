import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import {
  File,
  FileImage,
  FileText,
  Lock,
  Music,
  Unlock,
  User,
  Video,
  Eye,
} from 'lucide-react'

import { Badge } from '@/components/ui/Badge/Badge'
import { DataTable, type ColumnDef } from '@/components/ui/Table/DataTable'
import type { MediaAssetDto, MediaType } from '@/api/generated/model'
import { getMediaAssetName } from '@/lib/mediaAssetHelpers'

interface AdminMediaAssetsTableProps {
  mediaAssets: MediaAssetDto[]
  isLoading: boolean
  onPreviewMediaAsset: (mediaAsset: MediaAssetDto) => void
}

const mediaTypeIcons: Record<MediaType, typeof File> = {
  Audio: Music,
  Document: FileText,
  Image: FileImage,
  Video: Video,
}

/**
 * Admin media assets table.
 * The id column is represented by a preview action to keep file metadata easier to scan.
 */
export function AdminMediaAssetsTable({
  mediaAssets,
  isLoading,
  onPreviewMediaAsset,
}: AdminMediaAssetsTableProps) {
  const { t } = useTranslation()

  const columns = useMemo<ColumnDef<MediaAssetDto>[]>(
    () => [
      {
        header: t('admin.mediaAssets.asset'),
        cell: (asset) => {
          const Icon = mediaTypeIcons[asset.mimeType] || File

          return (
            <div className="flex min-w-72 items-center gap-3">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-nav-purple/15 text-nav-purple ring-1 ring-nav-purple/25">
                <Icon className="h-5 w-5" />
              </div>
              <div className="min-w-0">
                <p className="line-clamp-1 font-semibold text-foreground">
                  {getMediaAssetName(asset.locationUrl)}
                </p>
                {asset.description && (
                  <p className="line-clamp-1 max-w-72 text-xs text-muted-foreground">
                    {asset.description}
                  </p>
                )}
              </div>
            </div>
          )
        },
      },
      {
        header: t('admin.mediaAssets.type'),
        cell: (asset) => (
          <div className="flex flex-col items-start gap-2">
            <Badge variant="outline" className="border-border">
              {asset.mimeType}
            </Badge>
            <Badge
              className={
                asset.isPrivate
                  ? 'bg-muted text-muted-foreground hover:bg-muted'
                  : 'bg-success text-success-foreground hover:bg-success/90'
              }
            >
              {asset.isPrivate ? (
                <Lock className="mr-1 h-3 w-3" />
              ) : (
                <Unlock className="mr-1 h-3 w-3" />
              )}
              {asset.isPrivate
                ? t('admin.mediaAssets.private')
                : t('admin.mediaAssets.public')}
            </Badge>
          </div>
        ),
      },
      {
        header: t('admin.mediaAssets.owner'),
        cell: (asset) =>
          asset.ownerUsername ? (
            <div className="flex flex-col gap-1">
              <span className="flex items-center gap-1 font-medium">
                <User className="h-3.5 w-3.5 text-muted-foreground" />
                {asset.ownerUsername}
              </span>
              <span className="text-xs text-muted-foreground">
                {t('admin.mediaAssets.id')}: {asset.ownerId}
              </span>
            </div>
          ) : (
            <Badge
              variant="secondary"
              className="bg-warning/15 text-warning-foreground ring-1 ring-warning/30"
            >
              {t('admin.mediaAssets.unassigned')}
            </Badge>
          ),
      },
      {
        header: '',
        className: 'text-right w-[80px]',
        cell: (asset) => (
          <button
            type="button"
            onClick={() => onPreviewMediaAsset(asset)}
            className="inline-flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-primary/10 hover:text-primary focus-visible:ring-2 focus-visible:ring-primary focus-visible:outline-none"
            title={t('admin.mediaAssets.preview')}
            aria-label={t('admin.mediaAssets.preview')}
          >
            <Eye className="h-4 w-4" />
          </button>
        ),
      },
    ],
    [onPreviewMediaAsset, t],
  )

  return (
    <DataTable
      data={mediaAssets}
      columns={columns}
      isLoading={isLoading}
      emptyMessage={t('admin.mediaAssets.noMediaAssetsFound')}
      keyExtractor={(asset) => asset.id}
    />
  )
}
