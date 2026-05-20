import * as React from 'react'
import { type VariantProps } from 'class-variance-authority'
import { Slot } from 'radix-ui'
import { buttonVariants } from './buttonVariant'

import { cn } from '@/lib/utils'

/** Shared button props, including Slot-based composition through `asChild`. */
export interface ButtonProps
  extends React.ComponentProps<'button'>, VariantProps<typeof buttonVariants> {
  asChild?: boolean
}

/**
 * App button primitive with variant and size tokens.
 *
 * @param props.asChild - Renders a Radix Slot so links can receive button styling.
 */
export function Button({
  className,
  variant = 'default',
  size = 'default',
  asChild = false,
  ...props
}: ButtonProps) {
  const Tag = asChild ? Slot.Root : 'button'

  return (
    <Tag
      data-slot="button"
      data-variant={variant}
      data-size={size}
      className={cn(buttonVariants({ variant, size, className }))}
      {...props}
    />
  )
}
