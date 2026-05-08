import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { Loader2, CheckCircle2, XCircle, MailCheck } from 'lucide-react'
import {
  Card,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { toast } from 'sonner'
import { queryClient } from '@/main'

import {
  usePostApiAuthVerifyEmail,
  getGetApiAuthMeQueryKey,
} from '@/api/generated/endpoints/auth/auth'
import { useAuthStore } from '@/store/useAuthStore'

export const Route = createFileRoute('/verify-email/')({
  validateSearch: z.object({
    token: z.string().optional(),
    rememberMe: z.boolean().optional().catch(false),
  }),
  component: VerifyEmailPage,
})

function VerifyEmailPage() {
  const { token, rememberMe } = Route.useSearch()
  const { t } = useTranslation()
  const navigate = useNavigate()

  const { isAuthenticated } = useAuthStore()

  const verifyMutation = usePostApiAuthVerifyEmail()

  const handleVerifyClick = () => {
    if (!token) return

    verifyMutation.mutate(
      { params: { token, rememberMe } },
      {
        onSuccess: () => {
          toast.success(t('verifyEmail.successTitle'))

          setTimeout(async () => {
            // Invalidate cache and redirect
            await queryClient.invalidateQueries({
              queryKey: getGetApiAuthMeQueryKey(),
            })
            navigate({ to: '/app', replace: true })
          }, 2000)
        },
      },
    )
  }

  if (!token) {
    return (
      <VerificationLayout>
        <XCircle className="mx-auto mb-4 h-12 w-12 text-destructive" />
        <CardTitle>{t('verifyEmail.invalidLinkTitle')}</CardTitle>
        <CardDescription className="mt-2">
          {t('verifyEmail.invalidLinkDesc')}
        </CardDescription>

        {isAuthenticated && (
          <div className="mx-auto mt-6 rounded-md bg-muted p-3 text-sm text-muted-foreground">
            {t('verifyEmail.alreadyLoggedInMsg')}
          </div>
        )}

        <CardFooter className="mt-6 flex justify-center p-0">
          {isAuthenticated ? (
            <Button variant="outline" onClick={() => navigate({ to: '/app' })}>
              {t('verifyEmail.goToApp')}
            </Button>
          ) : (
            <Button
              variant="outline"
              onClick={() => navigate({ to: '/login' })}
            >
              {t('verifyEmail.goToLogin')}
            </Button>
          )}
        </CardFooter>
      </VerificationLayout>
    )
  }

  if (verifyMutation.isIdle) {
    return (
      <VerificationLayout>
        <MailCheck className="mx-auto mb-4 h-12 w-12 text-primary" />
        <CardTitle>{t('verifyEmail.readyTitle')}</CardTitle>
        <CardDescription className="mt-2 mb-6">
          {t('verifyEmail.readyDesc')}
        </CardDescription>
        <Button onClick={handleVerifyClick} className="w-full">
          {t('verifyEmail.verifyButton')}
        </Button>
      </VerificationLayout>
    )
  }

  if (verifyMutation.isPending) {
    return (
      <VerificationLayout>
        <Loader2 className="mx-auto mb-4 h-12 w-12 animate-spin text-primary" />
        <CardTitle>{t('verifyEmail.verifyingTitle')}</CardTitle>
        <CardDescription className="mt-2">
          {t('verifyEmail.verifyingDesc')}
        </CardDescription>
      </VerificationLayout>
    )
  }

  if (verifyMutation.isError) {
    return (
      <VerificationLayout>
        <XCircle className="mx-auto mb-4 h-12 w-12 text-destructive" />
        <CardTitle>{t('verifyEmail.failedTitle')}</CardTitle>
        <CardDescription className="mt-2">
          {t('verifyEmail.failedDescFallback')}
        </CardDescription>

        {isAuthenticated && (
          <div className="mx-auto mt-6 rounded-md bg-muted p-3 text-sm text-muted-foreground">
            {t('verifyEmail.alreadyLoggedInMsg')}
          </div>
        )}

        <CardFooter className="mt-6 flex justify-center p-0">
          {isAuthenticated ? (
            <Button variant="outline" onClick={() => navigate({ to: '/app' })}>
              {t('verifyEmail.goToApp')}
            </Button>
          ) : (
            <Button
              variant="outline"
              onClick={() => navigate({ to: '/login' })}
            >
              {t('verifyEmail.goToLogin')}
            </Button>
          )}
        </CardFooter>
      </VerificationLayout>
    )
  }

  if (verifyMutation.isSuccess) {
    return (
      <VerificationLayout>
        <CheckCircle2 className="mx-auto mb-4 h-12 w-12 text-success" />
        <CardTitle>{t('verifyEmail.successTitle')}</CardTitle>
        <CardDescription className="mt-2">
          {t('verifyEmail.successDesc')}
        </CardDescription>
        <CardFooter className="mt-6 flex justify-center p-0">
          <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
        </CardFooter>
      </VerificationLayout>
    )
  }

  return null
}

function VerificationLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <Card className="w-full max-w-md text-center shadow-lg">
        <CardHeader>{children}</CardHeader>
      </Card>
    </div>
  )
}
