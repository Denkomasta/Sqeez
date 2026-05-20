import type { ReactNode } from 'react'
import { AlertCircle, ArrowLeft } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'

interface ErrorCardProps {
  title: string
  description: string
  icon?: ReactNode
  actionText?: string
  actionIcon?: ReactNode
  onAction?: () => void
}

/**
 * Reusable centered error card for recoverable page-level failures.
 * Falls back to browser history when no custom action is provided.
 */
export function ErrorCard({
  title,
  description,
  icon = <AlertCircle className="size-6 text-destructive" />,
  actionText,
  actionIcon = <ArrowLeft className="mr-2 h-4 w-4" />,
  onAction,
}: ErrorCardProps) {
  const { t } = useTranslation()

  const handleAction = () => {
    if (onAction) {
      onAction()
    } else {
      history.back()
    }
  }

  return (
    <div className="flex min-h-[50vh] w-full items-center justify-center p-6">
      <Card className="w-full max-w-md border-destructive/20 bg-destructive/5 text-center shadow-sm">
        <CardHeader>
          <div className="mx-auto mb-4 flex size-12 items-center justify-center rounded-full bg-destructive/10">
            {icon}
          </div>
          <CardTitle className="text-xl text-destructive">{title}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          <p className="text-muted-foreground">{description}</p>
          <Button
            variant="outline"
            onClick={handleAction}
            className="w-full sm:w-auto"
          >
            {actionIcon}
            {actionText || t('common.goBack')}
          </Button>
        </CardContent>
      </Card>
    </div>
  )
}
