import { createFileRoute, Link } from '@tanstack/react-router'
import { Trophy, Zap, Target, ArrowRight, Sparkles } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { Button } from '@/components/ui/Button'
import { FeatureCard } from '@/components/ui/Card'

export const Route = createFileRoute('/')({
  component: Landing,
})

function Landing() {
  const { t } = useTranslation()

  return (
    <div className="flex min-h-screen flex-col bg-background text-foreground">
      <div className="flex-1">
        <section className="relative overflow-hidden py-20 sm:py-32 lg:pb-32 xl:pb-36">
          <div className="mx-auto max-w-7xl px-4 text-center sm:px-6 lg:px-8">
            <div className="mx-auto max-w-3xl space-y-8">
              <div className="inline-flex items-center rounded-full border border-border bg-secondary/50 px-3 py-1 text-sm font-medium">
                <Sparkles className="mr-2 h-4 w-4 text-nav-yellow" />
                <span>{t('landing.hero.pill')}</span>
              </div>

              <h1 className="text-4xl font-extrabold tracking-tight text-foreground sm:text-5xl md:text-6xl lg:text-7xl">
                {t('landing.hero.title1')}{' '}
                <span className="text-primary">
                  {t('landing.hero.titleHighlight')}
                </span>
              </h1>

              <p className="mx-auto max-w-2xl text-lg text-muted-foreground sm:text-xl">
                {t('landing.hero.description')}
              </p>

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
            </div>
          </div>
        </section>

        <div className="border-t border-border bg-secondary/20 py-20 sm:py-32">
          <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
            <div className="mb-16 text-center">
              <h2 className="text-3xl font-bold tracking-tight text-foreground sm:text-4xl">
                {t('landing.features.title')}
              </h2>
              <p className="mt-4 text-lg text-muted-foreground">
                {t('landing.features.subtitle')}
              </p>
            </div>

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
          </div>
        </div>
      </div>
    </div>
  )
}
