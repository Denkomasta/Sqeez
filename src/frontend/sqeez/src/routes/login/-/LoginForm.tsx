import { Mail, Lock, BookOpen } from 'lucide-react'
import { Button } from '@/components/ui'
import { Input } from '@/components/ui/Input'
import { PasswordInput } from '@/components/ui/Input'
import { Label } from '@/components/ui/Label'
import { Checkbox } from '@/components/ui/Checkbox'
import { useTranslation } from 'react-i18next'
import { Link, useSearch } from '@tanstack/react-router'
import {
  usePostApiAuthLogin as useLogin,
  usePostApiAuthResendVerification,
} from '@/api/generated/endpoints/auth/auth'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm, Controller, type SubmitHandler } from 'react-hook-form'
import type { TranslationKey } from '@/i18next'
import { AxiosError } from 'axios'
import { useAuthSuccess } from '@/hooks/useAuthSuccess'
import { toast } from 'sonner'
import { Spinner } from '@/components/ui/Spinner'
import type { AspNetProblemDetails } from '@/api/custom-axios'

import { getLoginSchema, type LoginFormValues } from '@/schemas/loginSchema'
import { useMemo } from 'react'
import { getStoredLanguage } from '@/lib/languageHelpers'

const errorMapping: Record<number, TranslationKey> = {
  401: 'error.invalidCredentials',
  404: 'errors.userNotFound',
  500: 'error.serverError',
}

/**
 * Login form with unverified-email recovery.
 * Resent verification emails include the currently stored UI language.
 */
export function LoginForm() {
  const { t } = useTranslation()
  const search = useSearch({ strict: false })
  const handleAuthSuccess = useAuthSuccess()

  const loginSchema = useMemo(() => getLoginSchema(t), [t])

  const {
    register,
    handleSubmit,
    setError,
    control,
    getValues,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
      remember: false,
    },
  })

  const resendMutation = usePostApiAuthResendVerification({
    mutation: {
      onSuccess: () => {
        toast.success(t('login.resendSuccess'))
      },
      onError: (error: AxiosError<AspNetProblemDetails>) => {
        const status = error.response?.status

        if (status === 400) {
          toast.error(t('error.alreadyVerified'))
        } else if (status === 404) {
          toast.error(t('error.userNotFound'))
        } else if (status === 429) {
          toast.error(t('error.tooManyRequests'))
        } else {
          toast.error(t('error.generic'))
        }
      },
    },
  })

  const { mutate, isPending } = useLogin({
    mutation: {
      onSuccess: async () => {
        try {
          await handleAuthSuccess(search?.redirect)
        } catch {
          setError('root', {
            type: 'manual',
            message: t('error.serverError'),
          })
        }
      },
      onError: (error: AxiosError<AspNetProblemDetails>) => {
        const data = error.response?.data
        const backendError = data?.error || data?.detail || data?.title || ''

        const isUnverified = backendError.toLowerCase().includes('verif')

        if (isUnverified) {
          setError('root', {
            type: 'unverified',
            message: t('error.unverifiedEmail'),
          })
        } else {
          setError('root', {
            type: 'manual',
            message: t(
              error.response?.status
                ? errorMapping[error.response.status] || 'error.generic'
                : 'error.generic',
            ),
          })
        }
      },
    },
  })

  const onSubmit: SubmitHandler<LoginFormValues> = (values) => {
    mutate({
      data: {
        email: values.email,
        password: values.password,
        rememberMe: values.remember,
      },
    })
  }

  const handleResendClick = () => {
    const email = getValues('email')
    if (!email) return

    resendMutation.mutate({
      data: { email, language: getStoredLanguage() },
    })
  }

  return (
    <div className="flex w-full max-w-md flex-col gap-8">
      <div className="flex items-center gap-2.5 lg:hidden">
        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary">
          <BookOpen
            className="h-5 w-5 text-primary-foreground"
            aria-hidden="true"
          />
        </div>
        <span className="text-xl font-bold tracking-tight text-foreground">
          {t('system.name')}
        </span>
      </div>

      <div className="flex flex-col gap-2">
        <h1 className="text-3xl font-bold tracking-tight text-balance text-foreground">
          {t('login.welcomeBack')}
        </h1>
        <p className="leading-relaxed text-muted-foreground">
          {t('login.signInToYourAccount')}
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-2">
        <Input
          {...register('email')}
          id="email"
          type="email"
          label={t('login.email')}
          placeholder={t('login.emailPlaceholder')}
          error={errors.email?.message}
          disabled={isPending || resendMutation.isPending}
          icon={<Mail className="h-4 w-4" />}
        />

        <PasswordInput
          {...register('password')}
          id="password"
          label={t('login.password')}
          placeholder={t('login.password')}
          error={errors.password?.message}
          disabled={isPending || resendMutation.isPending}
          icon={<Lock className="h-4 w-4" />}
          rightTopChip={
            <Link
              to="/forgot-password"
              className="text-xs font-medium text-primary transition-colors hover:text-primary/80"
            >
              {t('login.forgotPassword')}
            </Link>
          }
        />

        <div className="flex items-center gap-2">
          <Controller
            name="remember"
            control={control}
            render={({ field }) => (
              <Checkbox
                id="remember"
                checked={field.value}
                onCheckedChange={field.onChange}
                disabled={isPending || resendMutation.isPending}
                aria-label="Remember me for 30 days"
              />
            )}
          />
          <Label
            htmlFor="remember"
            className="cursor-pointer text-sm font-medium text-foreground"
          >
            {t('login.rememberMe')}
          </Label>
        </div>

        {errors.root && (
          <div className="animate-in rounded-lg bg-destructive/15 p-3 text-[0.8rem] font-medium text-destructive ring-1 ring-destructive/20 transition-all zoom-in-95 fade-in">
            <div className="flex flex-col gap-2">
              <span>{errors.root.message}</span>

              {errors.root.type === 'unverified' && (
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="w-fit border-destructive/30 text-destructive hover:bg-destructive/20 hover:text-destructive"
                  disabled={resendMutation.isPending}
                  onClick={handleResendClick}
                >
                  {resendMutation.isPending && (
                    <Spinner size="sm" className="mr-2" />
                  )}
                  {t('login.resendVerification')}
                </Button>
              )}
            </div>
          </div>
        )}

        <Button
          type="submit"
          className="mt-2 h-11 w-full rounded-xl bg-primary font-semibold text-primary-foreground transition-all hover:bg-primary/90 active:scale-[0.98]"
          disabled={isPending || resendMutation.isPending}
        >
          {isPending && <Spinner size="sm" className="mr-2" />}
          {t('common.signIn')}
        </Button>

        <p className="mt-2 text-center text-sm text-muted-foreground">
          {t('login.areYouNew')}{' '}
          <Link
            to="/register"
            className="font-semibold text-primary transition-colors hover:text-primary/80"
          >
            {t('login.createAccount')}
          </Link>
        </p>
      </form>
    </div>
  )
}
