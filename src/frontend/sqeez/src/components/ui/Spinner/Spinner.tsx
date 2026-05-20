import { Loader2 } from 'lucide-react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/utils'

const spinnerVariants = cva('animate-spin text-muted-foreground', {
  variants: {
    size: {
      default: 'h-6 w-6',
      sm: 'h-4 w-4',
      lg: 'h-8 w-8',
      xl: 'h-12 w-12',
    },
  },
  defaultVariants: {
    size: 'default',
  },
})

/** Spinner props with the shared size variant contract. */
export interface SpinnerProps
  extends
    React.SVGAttributes<SVGSVGElement>,
    VariantProps<typeof spinnerVariants> {}

/** Loading spinner built from the Lucide loader icon. */
export function Spinner({ className, size, ...props }: SpinnerProps) {
  return (
    <Loader2 className={cn(spinnerVariants({ size, className }))} {...props} />
  )
}
