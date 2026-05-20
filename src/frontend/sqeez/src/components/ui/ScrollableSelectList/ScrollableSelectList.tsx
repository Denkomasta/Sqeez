import type { ReactNode } from 'react'
import { AsyncButton } from '@/components/ui/Button'

export interface SelectOption {
  id: string | number
  title: string | ReactNode
  subtitle?: string | ReactNode
}

interface ScrollableSelectListProps {
  options: SelectOption[]
  selectedId: string | number | ''
  onSelect: (id: string | number) => void
  isLoading?: boolean
  loadingText?: string
  emptyText?: string
  maxHeight?: string
  loadMoreText?: string
  hasMore?: boolean
  onLoadMore?: () => void
  isFetchingNextPage?: boolean
}

/**
 * Scrollable option picker for compact modals and filters.
 * Supports loading, empty, selected, and manual "load more" states without owning pagination state itself.
 *
 * @param props.options - Rendered options; ids must be stable across pagination.
 * @param props.selectedId - Currently selected option id, or an empty string when nothing is selected.
 * @param props.onLoadMore - Optional callback for fetching the next page when `hasMore` is true.
 */
export function ScrollableSelectList({
  options,
  selectedId,
  onSelect,
  isLoading = false,
  loadingText = 'Loading...',
  emptyText = 'No options found.',
  maxHeight = 'max-h-[240px]',
  hasMore = false,
  loadMoreText,
  onLoadMore,
  isFetchingNextPage = false,
}: ScrollableSelectListProps) {
  return (
    <div
      className={`flex flex-col gap-2 overflow-y-auto rounded-md border border-input bg-muted/20 p-2 ${maxHeight}`}
    >
      {isLoading && options.length === 0 ? (
        <div className="py-8 text-center text-sm text-muted-foreground">
          {loadingText}
        </div>
      ) : options.length === 0 ? (
        <div className="py-8 text-center text-sm text-muted-foreground">
          {emptyText}
        </div>
      ) : (
        <>
          {options.map((option) => {
            const isSelected = selectedId === option.id
            return (
              <button
                key={option.id}
                type="button"
                onClick={() => onSelect(option.id)}
                className={`flex items-center justify-between rounded-md border px-3 py-3 text-left text-sm transition-all focus:ring-1 focus:ring-primary focus:outline-none ${
                  isSelected
                    ? 'border-primary bg-primary/10 font-medium'
                    : 'border-transparent bg-background hover:border-input hover:bg-muted/50'
                }`}
              >
                <span className="truncate pr-4">
                  <span className="mr-2 font-semibold text-foreground">
                    {option.title}
                  </span>
                  {option.subtitle && (
                    <span className="text-muted-foreground">
                      {option.subtitle}
                    </span>
                  )}
                </span>
                <div
                  className={`flex h-4 w-4 shrink-0 items-center justify-center rounded-full border ${isSelected ? 'border-primary bg-primary' : 'border-input'}`}
                >
                  {isSelected && (
                    <div className="h-1.5 w-1.5 rounded-full bg-primary-foreground" />
                  )}
                </div>
              </button>
            )
          })}

          {hasMore && (
            <div className="flex justify-center pt-2 pb-1">
              <AsyncButton
                variant="outline"
                size="sm"
                onClick={onLoadMore}
                isLoading={isFetchingNextPage}
                className="w-full text-xs"
              >
                {loadMoreText ?? 'Load More'}
              </AsyncButton>
            </div>
          )}
        </>
      )}
    </div>
  )
}
