import { useTranslation } from 'react-i18next'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/components/ui/Button'

interface PaginationProps {
  currentPage: number
  totalPages: number
  onPageChange: (page: number) => void
  className?: string
}

/**
 * Minimal previous/next pagination control for server-paginated lists.
 * Renders nothing when there is only one page.
 *
 * @param props.currentPage - Current 1-based page number.
 * @param props.totalPages - Total page count from the backend.
 * @param props.onPageChange - Called with the next 1-based page number.
 */
export function Pagination({
  currentPage,
  totalPages,
  onPageChange,
  className = '',
}: PaginationProps) {
  const { t } = useTranslation()

  if (totalPages <= 1) return null

  return (
    <div className={`flex w-full justify-center py-6 ${className}`}>
      <div className="inline-flex items-center gap-1 rounded-lg border border-border bg-background p-1 shadow-sm">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => onPageChange(currentPage - 1)}
          disabled={currentPage <= 1}
          className="rounded-md"
        >
          <ChevronLeft className="mr-1.5 size-4" />
          {t('common.previous')}
        </Button>

        <div className="flex h-8 items-center justify-center border-x border-border/50 px-4 text-sm font-medium text-muted-foreground">
          {t('common.page')} {currentPage} {t('common.of')} {totalPages}
        </div>

        <Button
          variant="ghost"
          size="sm"
          onClick={() => onPageChange(currentPage + 1)}
          disabled={currentPage >= totalPages}
          className="rounded-md"
        >
          {t('common.next')}
          <ChevronRight className="ml-1.5 size-4" />
        </Button>
      </div>
    </div>
  )
}
