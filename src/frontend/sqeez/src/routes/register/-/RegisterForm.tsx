import { useMemo, useState } from 'react'
import { Mail, Lock, BookOpen, Loader2, User as UserIcon } from 'lucide-react'
import { Button } from '@/components/ui'
import { Input } from '@/components/ui/Input'
import { PasswordInput } from '@/components/ui/Input'
import { Label } from '@/components/ui/Label'
import { Checkbox } from '@/components/ui/Checkbox'
import { useTranslation } from 'react-i18next'
import { Link } from '@tanstack/react-router'
import { useForm, Controller, type SubmitHandler } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { usePostApiAuthRegister as useRegister } from '@/api/generated/endpoints/auth/auth'
import {
  getRegisterSchema,
  type RegisterFormValues,
} from '@/schemas/registerSchema'

export function RegisterForm() {
  const { t } = useTranslation()
  const registerSchema = useMemo(() => getRegisterSchema(t), [t])

  const [isSuccess, setIsSuccess] = useState(false)

  const {
    register,
    handleSubmit,
    setError,
    control,
    getValues,
    formState: { errors },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      username: '',
      email: '',
      password: '',
      confirmPassword: '',
      remember: false,
    },
  })

  const { mutate, isPending } = useRegister({
    mutation: {
      onSuccess: () => {
        setIsSuccess(true)
      },
      onError: () => {
        setError('root', {
          type: 'manual',
          message: t('error.generic'),
        })
      },
    },
  })

  const onSubmit: SubmitHandler<RegisterFormValues> = (values) => {
    mutate({
      data: {
        firstName: values.firstName.trim(),
        lastName: values.lastName.trim(),
        username: values.username.trim(),
        email: values.email.trim(),
        password: values.password,
        rememberMe: values.remember,
      },
    })
  }

  if (isSuccess) {
    return (
      <div className="flex w-full max-w-md animate-in flex-col items-center gap-6 text-center duration-500 zoom-in-95 fade-in">
        <div className="flex h-20 w-20 items-center justify-center rounded-full bg-success/10">
          <Mail className="h-10 w-10 text-success" />
        </div>
        <div className="flex flex-col gap-2">
          <h1 className="text-3xl font-bold tracking-tight text-foreground">
            {t('register.checkEmailTitle')}
          </h1>
          <p className="leading-relaxed text-muted-foreground">
            {t('register.checkEmailDesc')} <br />
            <span className="font-semibold text-foreground">
              {getValues('email')}
            </span>
          </p>
        </div>
        <Button asChild className="mt-4 h-11 w-full rounded-xl">
          <Link to="/login">{t('register.backToLogin')}</Link>
        </Button>
      </div>
    )
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
          {t('register.title')}
        </h1>
        <p className="leading-relaxed text-muted-foreground">
          {t('register.subtitle')}
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
        <div className="grid grid-cols-2 gap-4">
          <Input
            {...register('firstName')}
            id="firstName"
            type="text"
            label={t('register.firstName')}
            placeholder="John"
            error={errors.firstName?.message}
            disabled={isPending}
          />
          <Input
            {...register('lastName')}
            id="lastName"
            type="text"
            label={t('register.lastName')}
            placeholder="Doe"
            error={errors.lastName?.message}
            disabled={isPending}
          />
        </div>

        <Input
          {...register('username')}
          id="username"
          type="text"
          label={t('register.username')}
          placeholder="johndoe123"
          error={errors.username?.message}
          disabled={isPending}
          icon={<UserIcon className="h-4 w-4" />}
        />

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

        <PasswordInput
          {...register('password')}
          id="password"
          label={t('login.password')}
          placeholder="••••••••"
          error={errors.password?.message}
          disabled={isPending}
          icon={<Lock className="h-4 w-4" />}
        />

        <PasswordInput
          {...register('confirmPassword')}
          id="confirmPassword"
          label={t('register.confirmPassword')}
          placeholder="••••••••"
          error={errors.confirmPassword?.message}
          disabled={isPending}
          icon={<Lock className="h-4 w-4" />}
        />

        <div className="mt-1 flex items-center gap-2">
          <Controller
            name="remember"
            control={control}
            render={({ field }) => (
              <Checkbox
                id="remember"
                checked={field.value}
                onCheckedChange={field.onChange}
                disabled={isPending}
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
            {errors.root.message}
          </div>
        )}

        <Button
          type="submit"
          className="mt-2 h-11 w-full rounded-xl bg-primary font-semibold text-primary-foreground transition-all hover:bg-primary/90 active:scale-[0.98]"
          disabled={isPending}
        >
          {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          {t('register.register')}
        </Button>

        <p className="mt-2 text-center text-sm text-muted-foreground">
          {t('register.alreadyHaveAccount')}{' '}
          <Link
            to="/login"
            className="font-semibold text-primary transition-colors hover:text-primary/80"
          >
            {t('register.signIn')}
          </Link>
        </p>
      </form>
    </div>
  )
}
