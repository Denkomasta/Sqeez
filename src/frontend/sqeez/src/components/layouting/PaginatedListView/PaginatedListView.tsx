import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Search } from 'lucide-react'
import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { Pagination } from '@/components/ui/Pagination'
import { PageLayout } from '../PageLayout/PageLayout'

interface PaginatedListViewProps<T> {
  titleNode: ReactNode
  subtitleNode?: ReactNode
  backButtonNode?: ReactNode
  headerActions?: ReactNode
  items: T[]
  renderItem: (item: T, index: number) => ReactNode
  totalCount?: number
  emptyStateMessage: ReactNode | string
  isLoading: boolean
  isFetching?: boolean
  searchQuery?: string
  setSearchQuery?: (query: string) => void
  searchPlaceholder?: string
  pageNumber?: number
  totalPages?: number
  setPageNumber?: (page: number) => void
  filtersNode?: ReactNode
  gridClassName?: string
  containerClassName?: string
  layoutVariant?: 'default' | 'app'
}

/**
 * Shared shell for searchable, filterable, paginated card/list pages.
 * Search changes reset the page to 1 when pagination state is supplied.
 */
export function PaginatedListView<T>({
  titleNode,
  subtitleNode,
  backButtonNode,
  headerActions,
  items,
  renderItem,
  totalCount,
  emptyStateMessage,
  isLoading,
  isFetching = false,
  searchQuery,
  setSearchQuery,
  searchPlaceholder,
  pageNumber,
  totalPages,
  setPageNumber,
  filtersNode,
  gridClassName = 'grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4',
  containerClassName,
  layoutVariant = 'default',
}: PaginatedListViewProps<T>) {
  const { t } = useTranslation()

  const ControlsNode =
    setSearchQuery || filtersNode || totalCount !== undefined ? (
      <div className="flex w-full flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        {setSearchQuery && (
          <DebouncedInput
            id="list-search"
            value={searchQuery || ''}
            onChange={(newQuery) => {
              setSearchQuery(newQuery)
              if (setPageNumber) setPageNumber(1)
            }}
            placeholder={searchPlaceholder || t('common.search')}
            icon={<Search className="h-4 w-4" />}
            className="w-full sm:max-w-xs"
            hideErrors
          />
        )}

        <div className="flex w-full flex-1 items-center gap-4 sm:w-auto">
          {filtersNode && <div className="flex-1">{filtersNode}</div>}

          {totalCount !== undefined && (
            <div className="shrink-0 text-sm font-medium whitespace-nowrap text-muted-foreground">
              {t('common.totalCount', {
                count: totalCount,
                defaultValue: `Total: ${totalCount}`,
              })}
            </div>
          )}
        </div>
      </div>
    ) : undefined

  return (
    <PageLayout
      containerClassName={containerClassName}
      variant={layoutVariant}
      isLoading={isLoading}
      title={titleNode}
      subtitle={subtitleNode}
      headerActions={headerActions}
      headerControls={
        backButtonNode || ControlsNode ? (
          <div className="flex flex-col gap-4">
            {backButtonNode}
            {ControlsNode}
          </div>
        ) : undefined
      }
    >
      <div
        className={`transition-opacity duration-200 ${isFetching && !isLoading ? 'opacity-50' : 'opacity-100'}`}
      >
        <div className={gridClassName}>
          {items.length > 0 ? (
            items.map((item, index) => renderItem(item, index))
          ) : (
            <div className="col-span-full py-12 text-center text-muted-foreground">
              {searchQuery ? t('common.noSearchResults') : emptyStateMessage}
            </div>
          )}
        </div>

        {totalPages !== undefined &&
          totalPages > 1 &&
          setPageNumber &&
          pageNumber !== undefined && (
            <div className="mt-8 flex justify-center">
              <Pagination
                currentPage={pageNumber}
                totalPages={totalPages}
                onPageChange={setPageNumber}
              />
            </div>
          )}
      </div>
    </PageLayout>
  )
}
