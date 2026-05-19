import { type TFunction } from 'i18next'
import * as z from 'zod'

/**
 * Builds the localized login schema.
 * Password validation mirrors registration because unverified users may resend email here.
 */
export const getLoginSchema = (t: TFunction) =>
  z.object({
    email: z.string().email({ message: t('register.validation.emailInvalid') }),
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
    remember: z.boolean(),
  })

export type LoginFormValues = z.infer<ReturnType<typeof getLoginSchema>>
