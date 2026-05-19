import { useState, useMemo } from 'react'
import { createFileRoute, Link } from '@tanstack/react-router'
import { Mail, ArrowLeft, KeyRound, CheckCircle2 } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { useTranslation } from 'react-i18next'
import { useForm, type SubmitHandler } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { usePostApiAuthForgotPassword } from '@/api/generated/endpoints/auth/auth'
import { AxiosError } from 'axios'
import type { AspNetProblemDetails } from '@/api/custom-axios'
import { toast } from 'sonner'
import { Spinner } from '@/components/ui/Spinner'
import type { TFunction } from 'i18next'
import { BrandingPanel } from '@/components/layouting/BrandingPanel'
import { getStoredLanguage } from '@/lib/languageHelpers'

export const Route = createFileRoute('/forgot-password/')({
  component: ForgotPasswordPage,
})

function ForgotPasswordPage() {
  return (
    <div className="flex min-h-screen">
      <div className="hidden lg:flex lg:w-1/2">
        <BrandingPanel />
      </div>

      <div className="flex w-full flex-col items-center justify-center bg-background px-6 py-12 lg:w-1/2 lg:px-16">
        <ForgotPasswordForm />
      </div>
    </div>
  )
}

const getForgotSchema = (t: TFunction) =>
  z.object({
    email: z.email({
      message: t('register.validation.emailInvalid'),
    }),
  })

type ForgotFormValues = z.infer<ReturnType<typeof getForgotSchema>>

/**
 * Requests a password reset email.
 * The API receives the stored UI language so the email can be localized.
 */
function ForgotPasswordForm() {
  const { t } = useTranslation()
  const [isSuccess, setIsSuccess] = useState(false)
  const schema = useMemo(() => getForgotSchema(t), [t])

  const {
    register,
    handleSubmit,
    getValues,
    formState: { errors },
  } = useForm<ForgotFormValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: '' },
  })

  const { mutate, isPending } = usePostApiAuthForgotPassword({
    mutation: {
      onSuccess: () => {
        setIsSuccess(true)
      },
      onError: (error: AxiosError<AspNetProblemDetails>) => {
        const data = error.response?.data
        const backendMessage = data?.error || data?.detail || data?.title
        toast.error(backendMessage || t('error.generic'))
      },
    },
  })

  const onSubmit: SubmitHandler<ForgotFormValues> = (values) => {
    mutate({ data: { email: values.email, language: getStoredLanguage() } })
  }

  if (isSuccess) {
    return (
      <div className="flex w-full max-w-md animate-in flex-col items-center gap-6 text-center duration-500 zoom-in-95 fade-in">
        <div className="flex h-20 w-20 items-center justify-center rounded-full bg-success/10">
          <CheckCircle2 className="h-10 w-10 text-success" />
        </div>
        <div className="flex flex-col gap-2">
          <h1 className="text-3xl font-bold tracking-tight text-foreground">
            {t('forgotPassword.checkEmailTitle')}
          </h1>
          <p className="leading-relaxed text-muted-foreground">
            {t('forgotPassword.checkEmailDesc')} <br />
            <span className="font-semibold text-foreground">
              {getValues('email')}
            </span>
          </p>
        </div>
        <Button asChild className="mt-4 h-11 w-full rounded-xl">
          <Link to="/login">{t('forgotPassword.backToLogin')}</Link>
        </Button>
      </div>
    )
  }

  return (
    <div className="flex w-full max-w-md flex-col gap-8">
      <div className="flex flex-col gap-2">
        <div className="mb-2 flex h-12 w-12 items-center justify-center rounded-2xl bg-primary/10">
          <KeyRound className="h-6 w-6 text-primary" />
        </div>
        <h1 className="text-3xl font-bold tracking-tight text-balance text-foreground">
          {t('forgotPassword.title')}
        </h1>
        <p className="leading-relaxed text-muted-foreground">
          {t('forgotPassword.subtitle')}
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
        <Input
          {...register('email')}
          id="email"
          type="email"
          label={t('login.email')}
          placeholder={t('login.emailPlaceholder')}
          error={errors.email?.message}
          disabled={isPending}
          icon={<Mail className="h-4 w-4" />}
        />

        <Button
          type="submit"
          className="mt-2 h-11 w-full rounded-xl bg-primary font-semibold text-primary-foreground transition-all hover:bg-primary/90 active:scale-[0.98]"
          disabled={isPending}
        >
          {isPending && <Spinner size="sm" className="mr-2" />}
          {t('forgotPassword.sendLink')}
        </Button>

        <Link
          to="/login"
          className="mt-4 flex items-center justify-center gap-2 text-sm font-medium text-muted-foreground transition-colors hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          {t('forgotPassword.backToLogin')}
        </Link>
      </form>
    </div>
  )
}
