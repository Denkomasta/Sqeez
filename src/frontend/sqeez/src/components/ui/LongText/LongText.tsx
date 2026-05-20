import type { ComponentPropsWithoutRef, ElementType, ReactNode } from 'react'

import { cn } from '@/lib/utils'

type LongTextProps<TElement extends ElementType = 'span'> = {
  as?: TElement
  children: ReactNode
  className?: string
} & Omit<ComponentPropsWithoutRef<TElement>, 'as' | 'children' | 'className'>

/**
 * Renders user/backend text that may contain line breaks or very long tokens.
 * Use `as` when semantic markup should be a paragraph, div, or another element.
 *
 * @param props.as - Element to render while keeping the long-text wrapping styles.
 * @param props.children - Text or inline content that should wrap safely.
 */
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
