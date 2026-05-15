import * as React from 'react'
import { cn } from '@/lib/utils'

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
  icon?: React.ReactNode
  rightElement?: React.ReactNode
  rightTopChip?: React.ReactNode
  containerClassName?: string
  ref?: React.Ref<HTMLInputElement>
  hideErrors?: boolean
  helpText?: string
}

const Input = ({
  label,
  error,
  icon,
  rightElement,
  rightTopChip,
  containerClassName,
  className,
  id,
  ref,
  hideErrors = false,
  helpText,
  ...props
}: InputProps) => {
  return (
    <div className={cn('flex w-full flex-col', containerClassName)}>
      {(label || rightTopChip) && (
        <div className="flex items-center justify-between pb-1.5">
          {label && (
            <label
              htmlFor={id}
              className="cursor-pointer text-sm font-medium text-foreground"
            >
              {label}
            </label>
          )}
          {rightTopChip && (
            <div className="flex items-center">{rightTopChip}</div>
          )}
        </div>
      )}

      <div className="group relative">
        {icon && (
          <div className="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-muted-foreground transition-colors group-focus-within:text-foreground">
            {icon}
          </div>
        )}
        <input
          {...props}
          id={id}
          ref={ref}
          aria-invalid={!!error}
          className={cn(
            'h-9 w-full min-w-0 rounded-md border border-input bg-card px-3 py-1 text-base shadow-xs transition-[color,box-shadow] outline-none selection:bg-primary selection:text-primary-foreground file:inline-flex file:h-7 file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-foreground placeholder:text-muted-foreground disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm dark:bg-input/30',
            'focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50',
            'aria-invalid:border-destructive aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40',
            icon && 'pl-10',
            rightElement && 'pr-10',
            className,
          )}
        />
        {rightElement && (
          <div className="absolute top-1/2 right-2 -translate-y-1/2">
            {rightElement}
          </div>
        )}
      </div>

      {helpText && <p className="text-xs text-muted-foreground">{helpText}</p>}

      {!hideErrors && (
        <div className="min-h-5 pt-1">
          {error && (
            <p className="animate-in text-xs font-medium text-destructive fade-in slide-in-from-top-1">
              {error}
            </p>
          )}
        </div>
      )}
    </div>
  )
}

Input.displayName = 'Input'

export { Input }
