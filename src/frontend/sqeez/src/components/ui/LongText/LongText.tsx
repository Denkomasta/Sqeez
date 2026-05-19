import type { ComponentPropsWithoutRef, ElementType, ReactNode } from 'react'

import { cn } from '@/lib/utils'

type LongTextProps<TElement extends ElementType = 'span'> = {
  as?: TElement
  children: ReactNode
  className?: string
} & Omit<ComponentPropsWithoutRef<TElement>, 'as' | 'children' | 'className'>

export function LongText<TElement extends ElementType = 'span'>({
  as,
  children,
  className,
  ...props
}: LongTextProps<TElement>) {
  const Component = as ?? 'span'

  return (
    <Component
      className={cn(
        'max-w-full [overflow-wrap:anywhere] break-words whitespace-pre-wrap',
        className,
      )}
      {...props}
    >
      {children}
    </Component>
  )
}
