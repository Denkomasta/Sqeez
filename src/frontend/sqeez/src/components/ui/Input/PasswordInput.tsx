import * as React from 'react'
import { Eye, EyeOff } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { Input } from './Input'

type PasswordInputProps = Omit<
  React.ComponentProps<typeof Input>,
  'rightElement' | 'type'
>

/**
 * Password field with a localized visibility toggle.
 * The underlying Input owns layout and browser password styling overrides.
 */
export function PasswordInput({
  disabled,
  className,
  ...props
}: PasswordInputProps) {
  const { t } = useTranslation()
  const [isVisible, setIsVisible] = React.useState(false)
  const Icon = isVisible ? EyeOff : Eye

  return (
    <Input
      {...props}
      disabled={disabled}
      type={isVisible ? 'text' : 'password'}
      className={className}
      rightElement={
        <button
          type="button"
          className="flex h-7 w-7 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-muted hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none disabled:pointer-events-none disabled:opacity-50"
          onClick={() => setIsVisible((value) => !value)}
          disabled={disabled}
          aria-label={t(
            isVisible ? 'login.hidePassword' : 'login.showPassword',
          )}
        >
          <Icon className="h-4 w-4" aria-hidden="true" />
        </button>
      }
    />
  )
}
