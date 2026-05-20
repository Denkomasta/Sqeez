import { createFileRoute, Link } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { ShieldAlert, ArrowLeft } from 'lucide-react'

import { RegisterForm } from './-/RegisterForm'
import { BrandingPanel } from '@/components/layouting/BrandingPanel'
import { Button } from '@/components/ui/Button'
import { useSystemConfig } from '@/hooks/useSystemConfig' // Adjust import path as needed

export const Route = createFileRoute('/register/')({
  component: Register,
})

/** Public registration route rendered only when registration is enabled by config. */
function Register() {
  const { t } = useTranslation()
  const { config, isLoading } = useSystemConfig()

  const isRegistrationEnabled = config?.allowPublicRegistration ?? false

  return (
    <>
      <div className="flex min-h-screen">
        <div className="hidden lg:flex lg:w-1/2">
          <BrandingPanel />
        </div>

        <div className="flex w-full flex-col items-center justify-center bg-background px-6 py-12 lg:w-1/2 lg:px-16">
          {isLoading ? (
            <div className="h-100 w-full max-w-sm animate-pulse rounded-xl bg-muted/50" />
          ) : isRegistrationEnabled ? (
            <RegisterForm />
          ) : (
            <div className="flex w-full max-w-sm animate-in flex-col items-center justify-center space-y-6 text-center zoom-in-95 fade-in">
              <div className="rounded-full bg-destructive/10 p-6">
                <ShieldAlert className="h-12 w-12 text-destructive" />
              </div>

              <div className="space-y-2">
                <h1 className="text-2xl font-bold tracking-tight text-foreground">
                  {t('register.disabledTitle')}
                </h1>
                <p className="text-muted-foreground">
                  {t('register.disabledMessage')}
                </p>
              </div>

              <Button variant="outline" className="mt-4 w-full gap-2" asChild>
                <Link to="/login">
                  <ArrowLeft className="h-4 w-4" />
                  {t('register.backToLogin')}
                </Link>
              </Button>
            </div>
          )}
        </div>
      </div>
    </>
  )
}
