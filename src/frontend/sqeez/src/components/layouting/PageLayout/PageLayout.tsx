import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Spinner } from '@/components/ui/Spinner'
import { cn } from '@/lib/utils'

export interface PageLayoutProps {
  title?: ReactNode
  titleBadge?: ReactNode
  subtitle?: ReactNode
  headerActions?: ReactNode
  headerControls?: ReactNode
  isLoading?: boolean
  children: ReactNode
  containerClassName?: string
  variant?: 'default' | 'app'
}

/**
 * Standard page wrapper for authenticated screens.
 * The `app` variant owns its own scroll area for editor/dashboard-style layouts.
 */
export function PageLayout({
  title,
  titleBadge,
  subtitle,
  headerActions,
  headerControls,
  isLoading,
  children,
  containerClassName = 'max-w-7xl',
  variant = 'default',
}: PageLayoutProps) {
  const { t } = useTranslation()

  if (isLoading) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4">
        <Spinner size="lg" />
        <p className="animate-pulse font-medium text-muted-foreground">
          {t('common.loading')}...
        </p>
      </div>
    )
  }

  const HeaderContent = (title || subtitle || headerActions) && (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
      <div>
        {(title || titleBadge) && (
          <div className="flex flex-wrap items-center gap-4">
            {title && (
              <h1 className="flex items-center gap-3 text-3xl font-bold tracking-tight text-foreground">
                {title}
              </h1>
            )}
            {titleBadge && (
              <span className="text-lg font-medium text-muted-foreground">
                {titleBadge}
              </span>
            )}
          </div>
        )}
        {subtitle && <p className="mt-1 text-muted-foreground">{subtitle}</p>}
      </div>
      {headerActions && <div>{headerActions}</div>}
    </div>
  )

  if (variant === 'app') {
    return (
      <div className="flex h-full flex-col bg-background">
        {(HeaderContent || headerControls) && (
          <div className="border-b border-border bg-card p-6 md:p-8">
            <div
              className={cn('mx-auto flex flex-col gap-6', containerClassName)}
            >
              {HeaderContent}
              {headerControls}
            </div>
          </div>
        )}
        <div className="flex-1 overflow-y-auto p-6 md:p-8">
          <div className={cn('mx-auto', containerClassName)}>{children}</div>
        </div>
      </div>
    )
  }

  return (
    <div className={cn('container mx-auto space-y-8 p-6', containerClassName)}>
      <div className="flex flex-col gap-6">
        {HeaderContent}
        {headerControls}
      </div>
      <div>{children}</div>
    </div>
  )
}
