import { createFileRoute, Link } from '@tanstack/react-router'
import { Trophy, Zap, Target, ArrowRight, Sparkles } from 'lucide-react'
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
            title={t('landing.features.quizzes.title')}
            description={t('landing.features.quizzes.desc')}
          />

          <FeatureCard
            icon={<Trophy className="h-7 w-7 text-nav-yellow" />}
            iconWrapperClassName="bg-nav-yellow/10"
            title={t('landing.features.badges.title')}
            description={t('landing.features.badges.desc')}
          />

          <FeatureCard
            icon={<Target className="h-7 w-7 text-success" />}
            iconWrapperClassName="bg-success/10"
            title={t('landing.features.progress.title')}
            description={t('landing.features.progress.desc')}
          />
        </div>
      </PublicSection>
    </PublicPageLayout>
  )
}
