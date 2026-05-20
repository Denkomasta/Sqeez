import { createFileRoute, Link } from '@tanstack/react-router'
import { Button } from '@/components/ui'
import { LogOut, ArrowLeft, Loader2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useLogout } from '@/hooks/useLogout'

export const Route = createFileRoute('/logout/')({
  component: LogoutPage,
})

/** Logout route that clears auth state and redirects back to the login page. */
function LogoutPage() {
  const { t } = useTranslation()
  const { performLogout, isPending } = useLogout()

  return (
    <div className="flex min-h-[80vh] flex-col items-center justify-center p-4">
      <div className="w-full max-w-sm rounded-2xl border bg-card p-8 text-center shadow-sm">
        <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-destructive/10 text-destructive">
          <LogOut className="h-6 w-6" />
        </div>

        <h1 className="text-2xl font-bold tracking-tight">
          {t('logout.title')}
        </h1>
        <p className="mt-2 text-muted-foreground">{t('logout.description')}</p>

        <div className="mt-8 flex flex-col gap-3">
          <Button
            variant="destructive"
            className="h-11 w-full cursor-pointer rounded-xl font-semibold"
            onClick={performLogout}
            disabled={isPending}
          >
            {isPending ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                {t('logout.signingOut')}
              </>
            ) : (
              t('logout.confirm')
            )}
          </Button>

          <Button
            variant="ghost"
            className="h-11 w-full rounded-xl"
            disabled={isPending}
            asChild
          >
            <Link to="/app">
              <ArrowLeft className="mr-2 h-4 w-4" />
              {t('common.goBack')}
            </Link>
          </Button>
        </div>
      </div>
    </div>
  )
}
