import { createFileRoute } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { ShieldAlert } from 'lucide-react'
import { useSystemConfig } from '@/hooks/useSystemConfig'
import {
  PublicContactCard,
  PublicHero,
  PublicPageLayout,
  PublicSection,
} from '@/components/layouting/PublicPageLayout'

export const Route = createFileRoute('/terms/')({
  component: Terms,
})

function Terms() {
  const { t } = useTranslation()
  const { config, isLoading: isSystemConfigLoading } = useSystemConfig()

  return (
    <PublicPageLayout>
      <PublicHero
        size="compact"
        maxWidth="4xl"
        titleClassName="text-3xl sm:text-5xl"
        subtitleClassName="text-lg sm:text-lg"
        icon={<ShieldAlert className="mx-auto h-12 w-12 text-primary" />}
        title={t('terms.title')}
        subtitle={t('terms.lastUpdated', {
          date: '4. 4. 2026',
        })}
      />

      <PublicSection size="compact" maxWidth="3xl">
        <div className="space-y-12 text-base leading-7 text-muted-foreground">
          <div className="space-y-4">
            <h2 className="text-2xl font-bold tracking-tight text-foreground">
              1. {t('terms.acceptance.title')}
            </h2>
            <p>{t('terms.acceptance.p1')}</p>
          </div>

          <div className="space-y-4">
            <h2 className="text-2xl font-bold tracking-tight text-foreground">
              2. {t('terms.accounts.title')}
            </h2>
            <p>{t('terms.accounts.p1')}</p>
            <ul className="ml-6 list-disc space-y-2">
              <li>{t('terms.accounts.rule1')}</li>
              <li>{t('terms.accounts.rule2')}</li>
            </ul>
          </div>

          <div className="space-y-4">
            <h2 className="text-2xl font-bold tracking-tight text-foreground">
              3. {t('terms.conduct.title')}
            </h2>
            <p>{t('terms.conduct.p1')}</p>
          </div>

          <div className="space-y-4">
            <h2 className="text-2xl font-bold tracking-tight text-foreground">
              4. {t('terms.liability.title')}
            </h2>
            <p className="font-medium text-foreground">
              {t('terms.liability.p1')}
            </p>
          </div>

          <PublicContactCard
            title={t('terms.contact.title')}
            description={t('terms.contact.desc')}
            supportEmail={config?.supportEmail}
            isLoading={isSystemConfigLoading}
          />
        </div>
      </PublicSection>
    </PublicPageLayout>
  )
}
