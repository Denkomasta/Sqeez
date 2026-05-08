import { useTranslation } from 'react-i18next'
import { Star, Shield, Target } from 'lucide-react'
import { BaseModal } from '@/components/ui/Modal'
import { getImageUrl } from '@/lib/imageHelpers'
import type { BadgeDto, BadgeMetric } from '@/api/generated/model'
import {
  METRIC_TRANSLATIONS,
  OPERATOR_MAP,
} from '@/constants/badgeRulesMappings'

interface BadgeDetailsModalProps {
  isOpen: boolean
  onClose: () => void
  badge: BadgeDto | null
  isEarned: boolean
  earnedDate?: string
}

export function BadgeDetailsModal({
  isOpen,
  onClose,
  badge,
  isEarned,
  earnedDate,
}: BadgeDetailsModalProps) {
  const { t } = useTranslation()

  if (!badge) return null

  const getReadableMetric = (metric: BadgeMetric) => {
    return t(METRIC_TRANSLATIONS[metric], metric)
  }

  return (
    <BaseModal isOpen={isOpen} onClose={onClose} title={badge.name}>
      <div className="flex w-full flex-col items-center gap-6 pt-4 pb-2">
        <div
          className={`flex h-32 w-32 items-center justify-center rounded-full p-4 ${
            isEarned ? 'bg-primary/10' : 'bg-muted grayscale'
          }`}
        >
          {badge.iconUrl ? (
            <img
              src={getImageUrl(badge.iconUrl)}
              alt={badge.name}
              className="h-full w-full object-contain"
            />
          ) : (
            <Shield className="h-16 w-16 text-primary" />
          )}
        </div>

        <div className="flex flex-col items-center gap-2">
          <div className="flex items-center gap-1.5 rounded-full bg-warning/10 px-3 py-1 text-sm font-bold text-warning">
            <Star className="h-4 w-4 fill-warning" />+{badge.xpBonus} XP
          </div>
          {isEarned ? (
            <span className="text-sm font-medium text-success">
              {t('badges.earnedOn')}{' '}
              {earnedDate ? new Date(earnedDate).toLocaleDateString() : ''}
            </span>
          ) : (
            <span className="text-sm font-medium text-muted-foreground">
              {t('badges.notEarned')}
            </span>
          )}
        </div>

        <p className="text-center text-sm text-foreground">
          {badge.description}
        </p>

        {badge.rules && badge.rules.length > 0 && (
          <div className="w-full rounded-lg border bg-muted/30 p-4">
            <h4 className="mb-3 flex items-center gap-2 text-sm font-semibold text-foreground">
              <Target className="h-4 w-4" />
              {t('badges.requirements')}
            </h4>
            <ul className="flex flex-col gap-2">
              {badge.rules.map((rule) => {
                const isPercentage = rule.metric === 'ScorePercentage'
                const displayValue = isPercentage
                  ? `${rule.targetValue}%`
                  : rule.targetValue

                return (
                  <li
                    key={rule.id}
                    className="flex items-start gap-2 text-sm text-muted-foreground"
                  >
                    <div className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-primary" />
                    <span>
                      <span className="font-medium text-foreground">
                        {getReadableMetric(rule.metric)}
                      </span>{' '}
                      <span className="mx-1 font-bold text-primary">
                        {OPERATOR_MAP[rule.operator]}
                      </span>{' '}
                      <span className="font-medium text-foreground">
                        {displayValue}
                      </span>
                    </span>
                  </li>
                )
              })}
            </ul>
          </div>
        )}
      </div>
    </BaseModal>
  )
}
