import { useMemo } from 'react'
import { createFileRoute, Link, useNavigate } from '@tanstack/react-router'
import { Lock, XCircle } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { useTranslation } from 'react-i18next'
import { useForm, type SubmitHandler } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { usePostApiAuthResetPassword } from '@/api/generated/endpoints/auth/auth'
import { AxiosError } from 'axios'
import type { AspNetProblemDetails } from '@/api/custom-axios'
import { toast } from 'sonner'
import { Spinner } from '@/components/ui/Spinner'
import type { TFunction } from 'i18next'
import { BrandingPanel } from '@/components/layouting/BrandingPanel'

export const Route = createFileRoute('/reset-password/')({
  validateSearch: z.object({
    token: z.string().optional(),
  }),
  component: ResetPasswordPage,
})

/**
 * Password reset page.
 * Requires token and email search params from the reset email.
 */
export function ResetPasswordPage() {
  return (
    <div className="flex min-h-screen">
      <div className="hidden lg:flex lg:w-1/2">
        <BrandingPanel />
      </div>

      <div className="flex w-full flex-col items-center justify-center bg-background px-6 py-12 lg:w-1/2 lg:px-16">
        <ResetPasswordForm />
      </div>
    </div>
  )
}

const getResetSchema = (t: TFunction) =>
  z
    .object({
      newPassword: z.string().min(8, {
        message: t('register.validation.passwordMin'),
      }),
      confirmPassword: z.string(),
    })
    .refine((data) => data.newPassword === data.confirmPassword, {
      message: t('register.validation.passwordsMatch'),
      path: ['confirmPassword'],
    })

type ResetFormValues = z.infer<ReturnType<typeof getResetSchema>>

function ResetPasswordForm() {
  const { token } = Route.useSearch()
  const { t } = useTranslation()
  const navigate = useNavigate()
  const schema = useMemo(() => getResetSchema(t), [t])

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ResetFormValues>({
    resolver: zodResolver(schema),
    defaultValues: { newPassword: '', confirmPassword: '' },
  })

  const { mutate, isPending } = usePostApiAuthResetPassword({
    mutation: {
      onSuccess: () => {
        toast.success(t('resetPassword.successToast'))
        navigate({ to: '/login', replace: true })
      },
      onError: (error: AxiosError<AspNetProblemDetails>) => {
        const data = error.response?.data
        const backendMessage = data?.error || data?.detail || data?.title
        setError('root', {
          type: 'manual',
          message: backendMessage || t('error.generic'),
        })
      },
    },
  })

  const onSubmit: SubmitHandler<ResetFormValues> = (values) => {
    if (!token) return
    mutate({ data: { token, newPassword: values.newPassword } })
  }

  if (!token) {
    return (
      <div className="flex w-full max-w-md flex-col items-center gap-6 text-center">
        <XCircle className="h-16 w-16 text-destructive" />
        <div className="flex flex-col gap-2">
          <h1 className="text-3xl font-bold tracking-tight text-foreground">
            {t('resetPassword.invalidLinkTitle')}
          </h1>
          <p className="leading-relaxed text-muted-foreground">
            {t('resetPassword.invalidLinkDesc')}
          </p>
        </div>
        <Button asChild className="mt-4 h-11 w-full rounded-xl">
          <Link to="/forgot-password">{t('resetPassword.requestNewLink')}</Link>
        </Button>
      </div>
    )
  }

  return (
    <div className="flex w-full max-w-md flex-col gap-8">
      <div className="flex flex-col gap-2">
        <h1 className="text-3xl font-bold tracking-tight text-balance text-foreground">
          {t('resetPassword.title')}
        </h1>
        <p className="leading-relaxed text-muted-foreground">
          {t('resetPassword.subtitle')}
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
        <Input
          {...register('newPassword')}
          id="newPassword"
          type="password"
          label={t('resetPassword.newPassword')}
          placeholder="••••••••"
          error={errors.newPassword?.message}
          disabled={isPending}
          icon={<Lock className="h-4 w-4" />}
        />

        <Input
          {...register('confirmPassword')}
          id="confirmPassword"
          type="password"
          label={t('register.confirmPassword')}
          placeholder="••••••••"
          error={errors.confirmPassword?.message}
          disabled={isPending}
          icon={<Lock className="h-4 w-4" />}
        />

        {errors.root && (
          <div className="animate-in rounded-lg bg-destructive/15 p-3 text-[0.8rem] font-medium text-destructive ring-1 ring-destructive/20 transition-all zoom-in-95 fade-in">
            {errors.root.message}
          </div>
        )}

        <Button
          type="submit"
          className="mt-2 h-11 w-full rounded-xl bg-primary font-semibold text-primary-foreground transition-all hover:bg-primary/90 active:scale-[0.98]"
          disabled={isPending}
        >
          {isPending && <Spinner size="sm" className="mr-2" />}
          {t('resetPassword.resetButton')}
        </Button>
      </form>
    </div>
  )
}
