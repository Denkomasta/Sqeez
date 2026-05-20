import { type ReactNode } from 'react'

interface CtaSectionProps {
  title: string
  subtitle: string
  actionButton: ReactNode
}

/**
 * Simple centered call-to-action section for public pages.
 *
 * @param props.actionButton - Caller-owned action so routing and styling stay flexible.
 */
export function CtaSection({ title, subtitle, actionButton }: CtaSectionProps) {
  return (
    <section className="py-20 sm:py-32">
      <div className="mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
        <h2 className="mb-6 text-3xl font-bold tracking-tight text-foreground sm:text-4xl">
          {title}
        </h2>
        <p className="mb-8 text-lg text-muted-foreground">{subtitle}</p>
        {actionButton}
      </div>
    </section>
  )
}
