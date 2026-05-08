import { createFileRoute } from '@tanstack/react-router'
import {
  Brain,
  School,
  BookOpen,
  BarChart3,
  Mail,
  HelpCircle,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { Button } from '@/components/ui/Button'
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@/components/ui/Accordion'
import { FeatureCard } from '@/components/ui/Card'
import { CtaSection } from '@/components/ui/Section'
import { useSystemConfig } from '@/hooks/useSystemConfig'
import { Spinner } from '@/components/ui/Spinner'

export const Route = createFileRoute('/help/')({
  component: Help,
})

function Help() {
  const { t } = useTranslation()
  const { config, isLoading: isSystemConfigLoading } = useSystemConfig()

  return (
    <div className="flex min-h-screen flex-col bg-background text-foreground">
      <div className="flex-1">
        <section className="border-b border-border bg-primary/5 py-20 sm:py-32">
          <div className="mx-auto max-w-7xl px-4 text-center sm:px-6 lg:px-8">
            <div className="mx-auto max-w-3xl space-y-6">
              <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-primary/10">
                <School className="h-8 w-8 text-primary" />
              </div>
              <h1 className="text-4xl font-extrabold tracking-tight text-foreground sm:text-5xl">
                {t('help.hero.title')}
              </h1>
              <p className="text-xl text-muted-foreground">
                {t('help.hero.subtitle')}
              </p>
            </div>
          </div>
        </section>

        <section className="py-20">
          <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
            <div className="grid grid-cols-1 gap-8 md:grid-cols-3">
              <FeatureCard
                icon={<BookOpen className="h-7 w-7 text-info" />}
                iconWrapperClassName="bg-info/10"
                title={t('help.features.create.title')}
                description={t('help.features.create.desc')}
              />

              <FeatureCard
                icon={<Brain className="h-7 w-7 text-nav-purple" />}
                iconWrapperClassName="bg-nav-purple/10"
                title={t('help.features.engage.title')}
                description={t('help.features.engage.desc')}
              />

              <FeatureCard
                icon={<BarChart3 className="h-7 w-7 text-success" />}
                iconWrapperClassName="bg-success/10"
                title={t('help.features.track.title')}
                description={t('help.features.track.desc')}
              />
            </div>
          </div>
        </section>

        <section className="border-t border-border bg-secondary/20 py-20">
          <div className="mx-auto max-w-3xl px-4 sm:px-6 lg:px-8">
            <div className="mb-12 text-center">
              <HelpCircle className="mx-auto mb-4 h-10 w-10 text-muted-foreground" />
              <h2 className="text-3xl font-bold tracking-tight text-foreground sm:text-4xl">
                {t('help.faq.title')}
              </h2>
            </div>

            <Accordion
              type="single"
              collapsible
              className="w-full rounded-2xl border border-border bg-card px-6 py-2 shadow-sm"
            >
              <AccordionItem value="item-1">
                <AccordionTrigger className="text-left text-lg font-medium hover:text-primary">
                  {t('help.faq.q1.question')}
                </AccordionTrigger>
                <AccordionContent className="text-base text-muted-foreground">
                  {t('help.faq.q1.answer')}
                </AccordionContent>
              </AccordionItem>

              <AccordionItem value="item-2">
                <AccordionTrigger className="text-left text-lg font-medium hover:text-primary">
                  {t('help.faq.q2.question')}
                </AccordionTrigger>
                <AccordionContent className="text-base text-muted-foreground">
                  {t('help.faq.q2.answer')}
                </AccordionContent>
              </AccordionItem>

              <AccordionItem value="item-3">
                <AccordionTrigger className="text-left text-lg font-medium hover:text-primary">
                  {t('help.faq.q3.question')}
                </AccordionTrigger>
                <AccordionContent className="text-base text-muted-foreground">
                  {t('help.faq.q3.answer')}
                </AccordionContent>
              </AccordionItem>

              <AccordionItem value="item-4" className="border-b-0">
                <AccordionTrigger className="text-left text-lg font-medium hover:text-primary">
                  {t('help.faq.q4.question')}
                </AccordionTrigger>
                <AccordionContent className="text-base text-muted-foreground">
                  {t('help.faq.q4.answer')}
                </AccordionContent>
              </AccordionItem>
            </Accordion>
          </div>
        </section>

        <CtaSection
          title={t('help.contact.title')}
          subtitle={t('help.contact.subtitle')}
          actionButton={
            <Button size="lg" className="h-12 gap-2 px-8" asChild>
              {isSystemConfigLoading ? (
                <Spinner size="default" />
              ) : (
                <a
                  href={`mailto:${config?.supportEmail}`}
                  className="font-semibold text-primary hover:underline"
                >
                  <Mail className="size-4" />
                  {t('help.contact.btn')}
                </a>
              )}
            </Button>
          }
        />
      </div>
    </div>
  )
}
