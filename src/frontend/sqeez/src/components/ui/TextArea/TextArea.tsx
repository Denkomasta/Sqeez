import * as React from 'react'

import { cn } from '@/lib/utils'

interface TextAreaProps extends React.ComponentProps<'textarea'> {
  label?: string
  error?: string
  containerClassName?: string
  hideErrors?: boolean
}

/** Textarea primitive with shared label, help text, and validation layout. */
function TextArea({
  className,
  label,
  id,
  containerClassName,
  error,
  hideErrors,
  ...props
}: TextAreaProps) {
  return (
    <div className={cn('flex w-full flex-col', containerClassName)}>
      {label && (
        <div className="flex items-center justify-between pb-1.5">
          {label && (
            <label
              htmlFor={id}
              className="cursor-pointer text-sm font-medium text-foreground"
            >
              {label}
            </label>
          )}
        </div>
      )}
      <textarea
        data-slot="textarea"
        className={cn(
          'flex field-sizing-content min-h-16 w-full rounded-md border border-input bg-transparent px-3 py-2 text-base shadow-xs transition-[color,box-shadow] outline-none placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-destructive/20 md:text-sm dark:bg-input/30 dark:aria-invalid:ring-destructive/40',
          className,
        )}
        id={id}
        {...props}
      />
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

export { TextArea }
