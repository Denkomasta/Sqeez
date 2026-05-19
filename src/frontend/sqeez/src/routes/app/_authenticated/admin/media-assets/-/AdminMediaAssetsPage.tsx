import { useEffect, useMemo, useRef, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { ChevronDown, Images, Search, Trash2 } from 'lucide-react'
import { toast } from 'sonner'

import { Button } from '@/components/ui/Button'
import { ConfirmModal } from '@/components/ui/Modal'
import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { PageLayout } from '@/components/layouting/PageLayout/PageLayout'
import { Pagination } from '@/components/ui/Pagination'
import { ScrollableSelectList } from '@/components/ui/ScrollableSelectList/ScrollableSelectList'
import {
  getGetApiMediaAssetsQueryKey,
  useDeleteApiMediaAssets,
  useGetApiMediaAssets,
} from '@/api/generated/endpoints/media-assets/media-assets'
import type {
  GetApiMediaAssetsParams,
  MediaAssetDto,
  MediaType,
} from '@/api/generated/model'

import { AdminMediaAssetsTable } from './AdminMediaAssetsTable'
import { AdminMediaAssetPreviewModal } from './AdminMediaAssetPreviewModal'

type UnassignedFilter = 'all' | 'unassigned'
type MediaTypeFilter = 'all' | MediaType
type MediaAssetsFilterDropdown = 'assignment' | 'type'

const mediaTypeOptions: MediaType[] = ['Image', 'Video', 'Audio', 'Document']

/**
 * Admin media asset overview.
 * Filters request server-side media pages and bulk deletion is limited to unassigned assets.
 */
export function AdminMediaAssetsPage() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const [searchQuery, setSearchQuery] = useState('')
  const [unassignedFilter, setUnassignedFilter] =
    useState<UnassignedFilter>('all')
  const [mediaTypeFilter, setMediaTypeFilter] = useState<MediaTypeFilter>('all')
  const [pageNumber, setPageNumber] = useState(1)
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false)
  const [previewedMediaAsset, setPreviewedMediaAsset] =
    useState<MediaAssetDto | null>(null)
  const [openDropdown, setOpenDropdown] =
    useState<MediaAssetsFilterDropdown | null>(null)
  const pageSize = 15

  const assignmentFilterRef = useRef<HTMLDivElement>(null)
  const mediaTypeFilterRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      const target = event.target as Node

      if (
        openDropdown === 'assignment' &&
        assignmentFilterRef.current &&
        !assignmentFilterRef.current.contains(target)
      ) {
        setOpenDropdown(null)
      } else if (
        openDropdown === 'type' &&
        mediaTypeFilterRef.current &&
        !mediaTypeFilterRef.current.contains(target)
      ) {
        setOpenDropdown(null)
      }
    }

    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [openDropdown])

  const assignmentFilterOptions = useMemo(
    () => [
      { id: 'all', title: t('admin.mediaAssets.filterAll') },
      { id: 'unassigned', title: t('admin.mediaAssets.filterUnassigned') },
    ],
    [t],
  )

  const mediaTypeFilterOptions = useMemo(
    () => [
      { id: 'all', title: t('admin.mediaAssets.filterAllTypes') },
      ...mediaTypeOptions.map((mediaType) => ({
        id: mediaType,
        title: t(`admin.mediaAssets.mediaTypes.${mediaType}`),
      })),
    ],
    [t],
  )

  const filterDropdownWrapperClassName =
    'absolute top-full left-0 z-50 mt-1 w-full min-w-56 rounded-md border border-border bg-card shadow-lg sm:w-64'
  const filterDropdownButtonClassName =
    'flex h-10 w-full items-center justify-between gap-2 rounded-md border border-input bg-background px-3 py-2 text-left text-sm shadow-sm transition-colors hover:bg-muted focus:border-primary focus:outline-none sm:w-56'

  const mediaAssetsQueryParams: GetApiMediaAssetsParams = {
    SearchTerm: searchQuery || undefined,
    MimeType: mediaTypeFilter === 'all' ? undefined : mediaTypeFilter,
    UnassignedOnly: unassignedFilter === 'unassigned' ? true : undefined,
    PageNumber: pageNumber,
    PageSize: pageSize,
  }

  const { data: mediaAssetsResponse, isLoading } = useGetApiMediaAssets(
    mediaAssetsQueryParams,
  )

  const mediaAssets = mediaAssetsResponse?.data || []
  const totalPages = Number(mediaAssetsResponse?.totalPages || 1)
  const totalCount = mediaAssetsResponse?.totalCount || 0

  const deleteUnassignedMutation = useDeleteApiMediaAssets({
    mutation: {
      onSuccess: (deletedCount) => {
        const normalizedDeletedCount = Number(deletedCount || 0)

        queryClient.invalidateQueries({
          queryKey: getGetApiMediaAssetsQueryKey(),
        })
        setPageNumber(1)
        setIsDeleteModalOpen(false)
        toast.success(
          t('admin.mediaAssets.mediaDeleted', {
            count: normalizedDeletedCount,
          }),
        )
      },
      onError: () => {
        toast.error(t('admin.mediaAssets.deleteUnassignedFailed'))
      },
    },
  })

  const handleDeleteUnassigned = async () => {
    await deleteUnassignedMutation.mutateAsync({
      params: { UnassignedOnly: true },
    })
  }

  return (
    <>
      <PageLayout
        variant="app"
        containerClassName="max-w-7xl"
        title={
          <span className="flex items-center gap-3">
            <span className="flex h-12 w-12 items-center justify-center rounded-xl bg-nav-purple/15 text-nav-purple ring-1 ring-nav-purple/25">
              <Images className="h-6 w-6" />
            </span>
            {t('admin.mediaAssets.title')}
          </span>
        }
        subtitle={
          <>
            {t('admin.mediaAssets.totalMediaAssets')}:{' '}
            <span className="font-bold">{totalCount}</span>
          </>
        }
        headerActions={
          <Button
            variant="destructive"
            onClick={() => setIsDeleteModalOpen(true)}
            className="gap-2"
          >
            <Trash2 className="h-4 w-4" />
            {t('admin.mediaAssets.deleteUnassigned')}
          </Button>
        }
        headerControls={
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center">
            <DebouncedInput
              id="admin-media-assets-search"
              value={searchQuery}
              onChange={(val) => {
                setSearchQuery(val)
                setPageNumber(1)
              }}
              placeholder={t('admin.mediaAssets.searchPlaceholder')}
              icon={<Search className="h-4 w-4" />}
              className="max-w-md bg-background"
              hideErrors
            />

            <div
              ref={assignmentFilterRef}
              className="relative w-full sm:w-auto"
            >
              <button
                type="button"
                onClick={() =>
                  setOpenDropdown(
                    openDropdown === 'assignment' ? null : 'assignment',
                  )
                }
                className={filterDropdownButtonClassName}
              >
                <span className="truncate">
                  {assignmentFilterOptions.find(
                    (option) => option.id === unassignedFilter,
                  )?.title ?? t('admin.mediaAssets.filterAll')}
                </span>
                <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
              </button>

              {openDropdown === 'assignment' && (
                <div className={filterDropdownWrapperClassName}>
                  <ScrollableSelectList
                    options={assignmentFilterOptions}
                    selectedId={unassignedFilter}
                    onSelect={(id) => {
                      setUnassignedFilter(id as UnassignedFilter)
                      setPageNumber(1)
                      setOpenDropdown(null)
                    }}
                    maxHeight="max-h-[220px]"
                  />
                </div>
              )}
            </div>

            <div ref={mediaTypeFilterRef} className="relative w-full sm:w-auto">
              <button
                type="button"
                onClick={() =>
                  setOpenDropdown(openDropdown === 'type' ? null : 'type')
                }
                className={filterDropdownButtonClassName}
              >
                <span className="truncate">
                  {mediaTypeFilterOptions.find(
                    (option) => option.id === mediaTypeFilter,
                  )?.title ?? t('admin.mediaAssets.filterAllTypes')}
                </span>
                <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
              </button>

              {openDropdown === 'type' && (
                <div className={filterDropdownWrapperClassName}>
                  <ScrollableSelectList
                    options={mediaTypeFilterOptions}
                    selectedId={mediaTypeFilter}
                    onSelect={(id) => {
                      setMediaTypeFilter(id as MediaTypeFilter)
                      setPageNumber(1)
                      setOpenDropdown(null)
                    }}
                    maxHeight="max-h-[220px]"
                  />
                </div>
              )}
            </div>
          </div>
        }
      >
        <AdminMediaAssetsTable
          mediaAssets={mediaAssets}
          isLoading={isLoading}
          onPreviewMediaAsset={setPreviewedMediaAsset}
        />

        {!isLoading && totalPages > 1 && (
          <div className="mt-6 flex justify-center">
            <Pagination
              currentPage={pageNumber}
              totalPages={totalPages}
              onPageChange={setPageNumber}
            />
          </div>
        )}
      </PageLayout>

      <ConfirmModal
        isOpen={isDeleteModalOpen}
        onClose={() => setIsDeleteModalOpen(false)}
        onConfirm={handleDeleteUnassigned}
        title={t('admin.mediaAssets.deleteUnassignedTitle')}
        description={t('admin.mediaAssets.deleteUnassignedDesc')}
        confirmText={t('admin.mediaAssets.deleteUnassigned')}
        isDestructive
        isLoading={deleteUnassignedMutation.isPending}
      />

      <AdminMediaAssetPreviewModal
        mediaAsset={previewedMediaAsset}
        onClose={() => setPreviewedMediaAsset(null)}
      />
    </>
  )
}
