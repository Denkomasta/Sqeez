import { createFileRoute } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { Lock } from 'lucide-react'
import { useSystemConfig } from '@/hooks/useSystemConfig'
import {
  PublicContactCard,
  PublicHero,
  PublicPageLayout,
  PublicSection,
} from '@/components/layouting/PublicPageLayout'

export const Route = createFileRoute('/privacy/')({
  component: PrivacyPolicy,
})

function PrivacyPolicy() {
  const { t } = useTranslation()
  const { config, isLoading: isSystemConfigLoading } = useSystemConfig()

  return (
    <PublicPageLayout>
      <PublicHero
        size="compact"
        maxWidth="4xl"
        titleClassName="text-3xl sm:text-5xl"
        subtitleClassName="text-lg sm:text-lg"
        icon={<Lock className="mx-auto h-12 w-12 text-primary" />}
        title={t('privacy.title')}
        subtitle={t('privacy.lastUpdated', {
          date: '4. 4. 2026',
        })}
      />

      <PublicSection size="compact" maxWidth="3xl">
        <div className="space-y-12 text-base leading-7 text-muted-foreground">
          <div className="space-y-4">
            <h2 className="text-2xl font-bold tracking-tight text-foreground">
              1. {t('privacy.intro.title')}
            </h2>
            <p>{t('privacy.intro.p1')}</p>
          </div>

          <div className="space-y-4">
            <h2 className="text-2xl font-bold tracking-tight text-foreground">
              2. {t('privacy.collection.title')}
            </h2>
            <p>{t('privacy.collection.p1')}</p>
            <ul className="ml-6 list-disc space-y-2">
              <li>{t('privacy.collection.rule1')}</li>
              <li>{t('privacy.collection.rule2')}</li>
            </ul>
          </div>

          <div className="space-y-4">
            <h2 className="text-2xl font-bold tracking-tight text-foreground">
              3. {t('privacy.storage.title')}
            </h2>
            <p>{t('privacy.storage.p1')}</p>
          </div>

          <div className="space-y-4">
            <h2 className="text-2xl font-bold tracking-tight text-foreground">
              4. {t('privacy.rights.title')}
            </h2>
            <p>{t('privacy.rights.p1')}</p>
          </div>

          <PublicContactCard
            title={t('privacy.contact.title')}
            description={t('privacy.contact.desc')}
            supportEmail={config?.supportEmail}
            isLoading={isSystemConfigLoading}
          />
        </div>
      </PublicSection>
    </PublicPageLayout>
  )
}
