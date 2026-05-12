import { type ReactNode } from 'react'
import { cn } from '@/lib/utils'

interface FeatureCardProps {
  icon: ReactNode
  iconWrapperClassName?: string
  accentClassName?: string
  visual?: ReactNode
  title: string
  description: string
  className?: string
}

export function FeatureCard({
  icon,
  iconWrapperClassName,
  accentClassName,
  visual,
  title,
  description,
  className,
}: FeatureCardProps) {
  const hasVisual = visual != null

  if (!hasVisual) {
    return (
      <div
        className={cn(
          'group relative flex self-start overflow-hidden rounded-lg border border-border bg-card p-7 text-center shadow-sm transition-all duration-300 hover:-translate-y-1 hover:border-primary/30 hover:shadow-lg sm:p-8',
          className,
        )}
      >
        <div
          className={cn(
            'absolute inset-x-0 top-0 h-1 bg-primary',
            accentClassName,
          )}
          aria-hidden="true"
        />
        <div className="flex flex-col items-center">
          <div
            className={cn(
              'mb-5 flex h-14 w-14 items-center justify-center rounded-full border border-border/70 bg-secondary/60 shadow-sm transition-transform duration-300 group-hover:scale-105',
              iconWrapperClassName,
            )}
          >
            {icon}
          </div>
          <h3 className="mb-3 text-xl font-bold text-foreground">{title}</h3>
          <p className="leading-relaxed text-muted-foreground">{description}</p>
        </div>
      </div>
    )
  }

  return (
    <div
      className={cn(
        'group relative flex h-full flex-col overflow-hidden rounded-lg border border-border bg-card shadow-sm transition-all duration-300 hover:-translate-y-1 hover:border-primary/30 hover:shadow-lg',
        'text-left',
        className,
      )}
    >
      <div
        className={cn(
          'absolute inset-x-0 top-0 h-1 bg-primary',
          accentClassName,
        )}
        aria-hidden="true"
      />

      <div className="relative border-b border-border/70 bg-secondary/20 px-6 pt-6">
        <div
          className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_20%_20%,hsl(var(--primary)/0.12),transparent_28%),linear-gradient(135deg,hsl(var(--background)/0.85),transparent)] opacity-70"
          aria-hidden="true"
        />
        <div className="relative flex min-h-34 items-end justify-center">
          {visual}
        </div>
      </div>

      <div className="flex flex-1 flex-col p-6">
        <div
          className={cn(
            'mb-5 flex items-center justify-center border border-border/70 bg-secondary/60 shadow-sm transition-transform duration-300 group-hover:scale-105',
            'h-12 w-12 rounded-lg',
            iconWrapperClassName,
          )}
        >
          {icon}
        </div>
        <h3 className="mb-3 text-xl font-bold text-foreground">{title}</h3>
        <p className="leading-relaxed text-muted-foreground">{description}</p>
      </div>
    </div>
  )
}
