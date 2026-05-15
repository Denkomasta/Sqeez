import type { ReactNode } from 'react'
import { AlertTriangle, Home, RotateCcw } from 'lucide-react'
import { Link, type ErrorComponentProps } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'

import { Button } from '@/components/ui/Button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'

interface ErrorPageContentProps {
  title: string
  description: string
  action?: ReactNode
  detail?: string
}

function ErrorPageContent({
  title,
  description,
  action,
  detail,
}: ErrorPageContentProps) {
  const { t } = useTranslation()

  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-6 text-foreground">
      <Card className="w-full max-w-lg border-destructive/20 bg-card text-center shadow-sm">
        <CardHeader>
          <div className="mx-auto mb-4 flex size-14 items-center justify-center rounded-full bg-destructive/10">
            <AlertTriangle
              className="size-7 text-destructive"
              aria-hidden="true"
            />
          </div>
          <CardTitle className="text-2xl font-bold text-foreground">
            {title}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          <p className="text-muted-foreground">{description}</p>

          {detail && (
            <pre className="max-h-40 overflow-auto rounded-md border border-destructive/20 bg-destructive/5 p-3 text-left text-xs whitespace-pre-wrap text-destructive">
              {detail}
            </pre>
          )}

          <div className="flex flex-col justify-center gap-3 sm:flex-row">
            {action}
            <Button asChild variant="outline">
              <Link to="/">
                <Home className="mr-2 h-4 w-4" />
                {t('common.home')}
              </Link>
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}

export function AppErrorPage({ error, reset }: ErrorComponentProps) {
  const { t } = useTranslation()
  const detail =
    import.meta.env.DEV && error instanceof Error ? error.message : undefined

  return (
    <ErrorPageContent
      title={t('errors.defaultErrorTitle')}
      description={t('errors.defaultErrorDescription')}
      detail={detail}
      action={
        <Button onClick={reset}>
          <RotateCcw className="mr-2 h-4 w-4" />
          {t('common.tryAgain')}
        </Button>
      }
    />
  )
}

export function AppNotFoundPage() {
  const { t } = useTranslation()

  return (
    <ErrorPageContent
      title={t('errors.notFoundTitle')}
      description={t('errors.notFoundDescription')}
    />
  )
}
