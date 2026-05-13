import { useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Images, Search, Trash2 } from 'lucide-react'
import { toast } from 'sonner'

import { Button } from '@/components/ui/Button'
import { ConfirmModal } from '@/components/ui/Modal'
import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { PageLayout } from '@/components/layouting/PageLayout/PageLayout'
import { Pagination } from '@/components/ui/Pagination'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/Select/Select'
import {
  getGetApiMediaAssetsQueryKey,
  useDeleteApiMediaAssets,
  useGetApiMediaAssets,
} from '@/api/generated/endpoints/media-assets/media-assets'
import type { GetApiMediaAssetsParams } from '@/api/generated/model'

import { AdminMediaAssetsTable } from './AdminMediaAssetsTable'

type UnassignedFilter = 'all' | 'unassigned'

export function AdminMediaAssetsPage() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const [searchQuery, setSearchQuery] = useState('')
  const [unassignedFilter, setUnassignedFilter] =
    useState<UnassignedFilter>('all')
  const [pageNumber, setPageNumber] = useState(1)
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false)
  const pageSize = 15

  const mediaAssetsQueryParams: GetApiMediaAssetsParams = {
    SearchTerm: searchQuery || undefined,
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

            <Select
              value={unassignedFilter}
              onValueChange={(value) => {
                setUnassignedFilter(value as UnassignedFilter)
                setPageNumber(1)
              }}
            >
              <SelectTrigger className="w-full bg-background sm:w-56">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">
                  {t('admin.mediaAssets.filterAll')}
                </SelectItem>
                <SelectItem value="unassigned">
                  {t('admin.mediaAssets.filterUnassigned')}
                </SelectItem>
              </SelectContent>
            </Select>
          </div>
        }
      >
        <AdminMediaAssetsTable
          mediaAssets={mediaAssets}
          isLoading={isLoading}
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
    </>
  )
}
