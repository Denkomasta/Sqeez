import { createFileRoute, Link } from '@tanstack/react-router'
import {
  Trophy,
  Zap,
  Target,
  ArrowRight,
  Sparkles,
  CheckCircle2,
  Star,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { Button } from '@/components/ui/Button'
import { FeatureCard } from '@/components/ui/Card'
import {
  PublicHero,
  PublicPageLayout,
  PublicSection,
  PublicSectionHeader,
} from '@/components/layouting/PublicPageLayout'

export const Route = createFileRoute('/')({
  component: Landing,
})

function Landing() {
  const { t } = useTranslation()

  return (
    <PublicPageLayout>
      <PublicHero
        tone="default"
        size="hero"
        contentClassName="space-y-8"
        titleClassName="text-4xl sm:text-5xl md:text-6xl lg:text-7xl"
        subtitleClassName="mx-auto max-w-2xl text-lg sm:text-xl"
        eyebrow={
          <div className="inline-flex items-center rounded-full border border-border bg-secondary/50 px-3 py-1 text-sm font-medium">
            <Sparkles className="mr-2 h-4 w-4 text-nav-yellow" />
            <span>{t('landing.hero.pill')}</span>
          </div>
        }
        title={
          <>
            {t('landing.hero.title1')}{' '}
            <span className="text-primary">
              {t('landing.hero.titleHighlight')}
            </span>
          </>
        }
        subtitle={t('landing.hero.description')}
        actions={
          <div className="flex flex-col items-center justify-center gap-4 sm:flex-row">
            <Link to="/register">
              <Button
                size="lg"
                className="h-12 w-full gap-2 px-8 text-base sm:w-auto"
              >
                {t('landing.hero.startBtn')}
                <ArrowRight className="h-4 w-4" />
              </Button>
            </Link>
            <Link to="/about">
              <Button
                size="lg"
                variant="outline"
                className="h-12 w-full px-8 text-base sm:w-auto"
              >
                {t('landing.hero.learnMoreBtn')}
              </Button>
            </Link>
          </div>
        }
      />

      <PublicSection tone="muted" withTopBorder>
        <PublicSectionHeader
          title={t('landing.features.title')}
          subtitle={t('landing.features.subtitle')}
        />

        <div className="grid grid-cols-1 gap-8 md:grid-cols-3">
          <FeatureCard
            icon={<Zap className="h-7 w-7 text-primary" />}
            iconWrapperClassName="bg-primary/10"
            accentClassName="bg-primary"
            visual={<QuizFlowVisual />}
            title={t('landing.features.quizzes.title')}
            description={t('landing.features.quizzes.desc')}
          />

          <FeatureCard
            icon={<Trophy className="h-7 w-7 text-nav-yellow" />}
            iconWrapperClassName="bg-nav-yellow/10"
            accentClassName="bg-nav-yellow"
            visual={<BadgeShelfVisual />}
            title={t('landing.features.badges.title')}
            description={t('landing.features.badges.desc')}
          />

          <FeatureCard
            icon={<Target className="h-7 w-7 text-success" />}
            iconWrapperClassName="bg-success/10"
            accentClassName="bg-success"
            visual={<ProgressVisual />}
            title={t('landing.features.progress.title')}
            description={t('landing.features.progress.desc')}
          />
        </div>
      </PublicSection>
    </PublicPageLayout>
  )
}

function QuizFlowVisual() {
  return (
    <div
      className="flex w-full max-w-64 flex-col gap-3 pb-6"
      aria-hidden="true"
    >
      <div className="rounded-lg border border-border bg-card p-3 shadow-sm">
        <div className="mb-3 h-2 w-2/3 rounded-full bg-muted-foreground/20" />
        <div className="grid grid-cols-2 gap-2">
          <div className="h-8 rounded-md bg-primary/15" />
          <div className="h-8 rounded-md bg-muted-foreground/10" />
          <div className="h-8 rounded-md bg-muted-foreground/10" />
          <div className="flex h-8 items-center justify-center rounded-md bg-success/15">
            <CheckCircle2 className="h-4 w-4 text-success" />
          </div>
        </div>
      </div>
    </div>
  )
}

function BadgeShelfVisual() {
  return (
    <div
      className="flex w-full max-w-64 items-end justify-center gap-3 pb-6"
      aria-hidden="true"
    >
      {['bg-nav-yellow/20', 'bg-primary/15', 'bg-success/15'].map(
        (className, index) => (
          <div
            key={className}
            className={`${className} flex h-16 w-16 items-center justify-center rounded-lg border border-border shadow-sm ${index === 1 ? 'mb-4 h-20 w-20' : ''}`}
          >
            <Star className="h-6 w-6 fill-current text-nav-yellow" />
          </div>
        ),
      )}
    </div>
  )
}

function ProgressVisual() {
  return (
    <div
      className="flex w-full max-w-64 flex-col gap-3 pb-6"
      aria-hidden="true"
    >
      {[68, 86, 52].map((width, index) => (
        <div
          key={width}
          className="rounded-lg border border-border bg-card p-3 shadow-sm"
        >
          <div className="mb-2 flex items-center gap-2">
            <div
              className={`h-2.5 w-2.5 rounded-full ${
                index === 0
                  ? 'bg-primary'
                  : index === 1
                    ? 'bg-success'
                    : 'bg-nav-yellow'
              }`}
            />
            <div className="h-2 w-18 rounded-full bg-muted-foreground/20" />
          </div>
          <div className="h-2 rounded-full bg-muted">
            <div
              className="h-full rounded-full bg-success"
              style={{ width: `${width}%` }}
            />
          </div>
        </div>
      ))}
    </div>
  )
}
