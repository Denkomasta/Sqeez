import type { ReactNode } from 'react'

import { Spinner } from '@/components/ui/Spinner'
import { cn } from '@/lib/utils'

type SectionSize = 'compact' | 'default' | 'spacious' | 'hero'
type SectionTone = 'default' | 'muted' | 'primary'
type ContainerWidth = '3xl' | '4xl' | '7xl'

interface PublicPageLayoutProps {
  children: ReactNode
  className?: string
}

interface PublicHeroProps {
  title: ReactNode
  subtitle?: ReactNode
  icon?: ReactNode
  eyebrow?: ReactNode
  actions?: ReactNode
  tone?: SectionTone
  size?: SectionSize
  maxWidth?: ContainerWidth
  className?: string
  contentClassName?: string
  titleClassName?: string
  subtitleClassName?: string
}

interface PublicSectionProps {
  children: ReactNode
  tone?: SectionTone
  size?: SectionSize
  maxWidth?: ContainerWidth
  withTopBorder?: boolean
  withBottomBorder?: boolean
  className?: string
  containerClassName?: string
}

interface PublicSectionHeaderProps {
  title: ReactNode
  subtitle?: ReactNode
  className?: string
}

interface PublicContactCardProps {
  title: ReactNode
  description: ReactNode
  supportEmail?: string | null
  isLoading?: boolean
}

const sectionSizeClasses: Record<SectionSize, string> = {
  compact: 'py-16 sm:py-24',
  default: 'py-20',
  spacious: 'py-20 sm:py-32',
  hero: 'py-20 sm:py-32 lg:pb-32 xl:pb-36',
}

const sectionToneClasses: Record<SectionTone, string> = {
  default: '',
  muted: 'bg-secondary/20',
  primary: 'bg-primary/5',
}

const containerWidthClasses: Record<ContainerWidth, string> = {
  '3xl': 'max-w-3xl',
  '4xl': 'max-w-4xl',
  '7xl': 'max-w-7xl',
}

/** Layout primitives used by public marketing/legal/help pages. */
export function PublicPageLayout({
  children,
  className,
}: PublicPageLayoutProps) {
  return (
    <div
      className={cn(
        'flex flex-1 flex-col bg-background text-foreground',
        className,
      )}
    >
      {children}
    </div>
  )
}

/** First-section hero block with shared spacing and tone options. */
export function PublicHero({
  title,
  subtitle,
  icon,
  eyebrow,
  actions,
  tone = 'muted',
  size = 'spacious',
  maxWidth = '7xl',
  className,
  contentClassName,
  titleClassName,
  subtitleClassName,
}: PublicHeroProps) {
  return (
    <section
      className={cn(
        'border-b border-border text-center',
        sectionSizeClasses[size],
        sectionToneClasses[tone],
        tone === 'default' && 'border-b-0',
        className,
      )}
    >
      <div
        className={cn(
          'mx-auto px-4 sm:px-6 lg:px-8',
          containerWidthClasses[maxWidth],
        )}
      >
        <div className={cn('mx-auto max-w-3xl space-y-6', contentClassName)}>
          {icon}
          {eyebrow}
          <h1
            className={cn(
              'text-4xl font-extrabold tracking-tight text-foreground sm:text-5xl lg:text-6xl',
              titleClassName,
            )}
          >
            {title}
          </h1>
          {subtitle && (
            <p
              className={cn(
                'text-xl text-muted-foreground sm:text-2xl',
                subtitleClassName,
              )}
            >
              {subtitle}
            </p>
          )}
          {actions}
        </div>
      </div>
    </section>
  )
}

/** Full-width public section with shared spacing, tone, and container widths. */
export function PublicSection({
  children,
  tone = 'default',
  size = 'spacious',
  maxWidth = '7xl',
  withTopBorder = false,
  withBottomBorder = false,
  className,
  containerClassName,
}: PublicSectionProps) {
  return (
    <section
      className={cn(
        sectionSizeClasses[size],
        sectionToneClasses[tone],
        withTopBorder && 'border-t border-border',
        withBottomBorder && 'border-b border-border',
        className,
      )}
    >
      <div
        className={cn(
          'mx-auto px-4 sm:px-6 lg:px-8',
          containerWidthClasses[maxWidth],
          containerClassName,
        )}
      >
        {children}
      </div>
    </section>
  )
}

/** Standard title/subtitle block for public page sections. */
export function PublicSectionHeader({
  title,
  subtitle,
  className,
}: PublicSectionHeaderProps) {
  return (
    <div className={cn('mb-16 text-center', className)}>
      <h2 className="text-3xl font-bold tracking-tight text-foreground sm:text-4xl">
        {title}
      </h2>
      {subtitle && (
        <p className="mt-4 text-lg text-muted-foreground">{subtitle}</p>
      )}
    </div>
  )
}

/** Optional support contact block; renders the email only once config has loaded. */
export function PublicContactCard({
  title,
  description,
  supportEmail,
  isLoading = false,
}: PublicContactCardProps) {
  return (
    <div className="mt-12 rounded-xl border border-border bg-secondary/30 p-6 sm:p-8">
      <h3 className="text-xl font-bold text-foreground">{title}</h3>
      <p className="mt-2">
        {description}
        {isLoading ? (
          <Spinner size="lg" />
        ) : supportEmail ? (
          <a
            href={`mailto:${supportEmail}`}
            className="font-semibold text-primary hover:underline"
          >
            {supportEmail}
          </a>
        ) : null}
      </p>
    </div>
  )
}
