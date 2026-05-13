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
} from 'lucide-react'

import { Badge } from '@/components/ui/Badge/Badge'
import { DataTable, type ColumnDef } from '@/components/ui/Table/DataTable'
import type { MediaAssetDto, MediaType } from '@/api/generated/model'

interface AdminMediaAssetsTableProps {
  mediaAssets: MediaAssetDto[]
  isLoading: boolean
}

const mediaTypeIcons: Record<MediaType, typeof File> = {
  Audio: Music,
  Document: FileText,
  Image: FileImage,
  Video: Video,
}

function getAssetName(locationUrl: string) {
  const name = locationUrl.split('/').pop() || locationUrl

  try {
    return decodeURIComponent(name)
  } catch {
    return name
  }
}

export function AdminMediaAssetsTable({
  mediaAssets,
  isLoading,
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
                  {getAssetName(asset.locationUrl)}
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
        header: t('admin.mediaAssets.id'),
        className: 'text-right',
        cell: (asset) => (
          <span className="font-mono text-xs text-muted-foreground">
            {asset.id}
          </span>
        ),
      },
    ],
    [t],
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
