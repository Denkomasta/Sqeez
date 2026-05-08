import { createFileRoute, Link } from '@tanstack/react-router'
import { Brain, Heart, Shield, Sparkles, Users } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { Button } from '@/components/ui/Button'
import { FeatureCard } from '@/components/ui/Card'
import { CtaSection } from '@/components/ui/Section'

export const Route = createFileRoute('/about/')({
  component: About,
})

function About() {
  const { t } = useTranslation()

  return (
    <div className="flex min-h-screen flex-col bg-background text-foreground">
      <div className="flex-1">
        <section className="border-b border-border bg-secondary/20 py-20 sm:py-32">
          <div className="mx-auto max-w-7xl px-4 text-center sm:px-6 lg:px-8">
            <div className="mx-auto max-w-3xl space-y-6">
              <h1 className="text-4xl font-extrabold tracking-tight text-foreground sm:text-5xl lg:text-6xl">
                {t('about.hero.title')}
              </h1>
              <p className="text-xl text-muted-foreground sm:text-2xl">
                {t('about.hero.subtitle')}
              </p>
            </div>
          </div>
        </section>

        <section className="py-20 sm:py-32">
          <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
            <div className="grid grid-cols-1 items-center gap-16 lg:grid-cols-2">
              <div className="space-y-6 text-lg text-muted-foreground">
                <h2 className="text-3xl font-bold tracking-tight text-foreground sm:text-4xl">
                  {t('about.story.title')}
                </h2>
                <p>{t('about.story.p1')}</p>
                <p>{t('about.story.p2')}</p>
              </div>

              <div className="relative flex aspect-square items-center justify-center rounded-3xl border border-border bg-secondary/50 p-8 shadow-inner lg:aspect-auto lg:h-125">
                <div className="absolute inset-0 flex items-center justify-center opacity-10">
                  <Brain className="h-64 w-64 text-primary" />
                </div>
                <div className="relative z-10 flex flex-col items-center gap-4 text-center">
                  <Sparkles className="h-12 w-12 text-nav-yellow" />
                  <h3 className="text-2xl font-bold text-foreground">
                    {t('about.story.graphicTitle')}
                  </h3>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section className="border-t border-border bg-secondary/20 py-20 sm:py-32">
          <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
            <div className="mb-16 text-center">
              <h2 className="text-3xl font-bold tracking-tight text-foreground sm:text-4xl">
                {t('about.values.title')}
              </h2>
            </div>

            <div className="grid grid-cols-1 gap-8 sm:grid-cols-2 lg:grid-cols-3">
              <FeatureCard
                icon={<Heart className="h-7 w-7 text-destructive" />}
                iconWrapperClassName="bg-destructive/10"
                title={t('about.values.v1.title')}
                description={t('about.values.v1.desc')}
              />
              <FeatureCard
                icon={<Users className="h-7 w-7 text-info" />}
                iconWrapperClassName="bg-info/10"
                title={t('about.values.v2.title')}
                description={t('about.values.v2.desc')}
              />
              <FeatureCard
                icon={<Shield className="h-7 w-7 text-success" />}
                iconWrapperClassName="bg-success/10"
                title={t('about.values.v3.title')}
                description={t('about.values.v3.desc')}
              />
            </div>
          </div>
        </section>

        <CtaSection
          title={t('about.cta.title')}
          subtitle={t('about.cta.subtitle')}
          actionButton={
            <Link to="/register">
              <Button size="lg" className="h-14 px-10 text-lg">
                {t('landing.hero.startBtn')}
              </Button>
            </Link>
          }
        />
      </div>
    </div>
  )
}
