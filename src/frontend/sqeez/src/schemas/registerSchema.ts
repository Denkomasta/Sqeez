import { type TFunction } from 'i18next'
import * as z from 'zod'

/**
 * Builds the localized registration schema.
 * Username and password rules intentionally match the backend DTO constraints.
 */
export const getRegisterSchema = (t: TFunction) =>
  z
    .object({
      firstName: z
        .string()
        .min(1, { message: t('register.validation.firstNameRequired') }),
      lastName: z
        .string()
        .min(1, { message: t('register.validation.lastNameRequired') }),
      username: z
        .string()
        .min(3, { message: t('register.validation.usernameMin') })
        .max(20, { message: t('register.validation.usernameMax') })
        .regex(/^[a-zA-Z0-9_\-áéíóúýčďěňřšťžÁÉÍÓÚÝČĎĚŇŘŠŤŽ]+$/, {
          message: t('register.validation.usernameFormat'),
        }),
      email: z.email({ message: t('register.validation.emailInvalid') }),
      password: z
        .string()
        .min(8, { message: t('register.validation.passwordMin') })
        .regex(/[A-Z]/, {
          message: t('register.validation.passwordUppercase'),
        })
        .regex(/[a-z]/, {
          message: t('register.validation.passwordLowercase'),
        })
        .regex(/[0-9]/, { message: t('register.validation.passwordNumber') })
        .regex(/[^A-Za-z0-9]/, {
          message: t('register.validation.passwordSpecial'),
        }),
      confirmPassword: z.string(),
      remember: z.boolean(),
    })
    .refine((data) => data.password === data.confirmPassword, {
      message: t('register.validation.passwordsMatch'),
      path: ['confirmPassword'],
    })

export type RegisterFormValues = z.infer<ReturnType<typeof getRegisterSchema>>
