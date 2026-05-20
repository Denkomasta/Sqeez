import type { BadgeOperator, BadgeMetric } from '@/api/generated/model'
import type { TranslationKey } from '@/i18next'

/** Badge metrics that are currently editable in the frontend rule builder. */
export type SupportedBadgeMetrics = Exclude<
  BadgeMetric,
  'PerfectAnswersCount' | 'TotalAttempts'
>

/** Compact operator symbols used in rule summaries. */
export const OPERATOR_MAP: Record<BadgeOperator, string> = {
  Equals: '=',
  GreaterThan: '>',
  GreaterThanOrEqual: '>=',
  LessThan: '<',
  LessThanOrEqual: '<=',
  NotEquals: '!=',
}

/** Translation keys for all backend-supported badge operators. */
export const OPERATOR_TRANSLATIONS: Record<BadgeOperator, TranslationKey> = {
  Equals: 'badges.operators.equals',
  GreaterThan: 'badges.operators.greaterThan',
  GreaterThanOrEqual: 'badges.operators.greaterThanOrEqual',
  LessThan: 'badges.operators.lessThan',
  LessThanOrEqual: 'badges.operators.lessThanOrEqual',
  NotEquals: 'badges.operators.notEquals',
}

/** Translation keys for all backend-known badge metrics. */
export const METRIC_TRANSLATIONS: Record<BadgeMetric, TranslationKey> = {
  ScorePercentage: 'badges.metrics.scorePercentage',
  TotalScore: 'badges.metrics.totalScore',
  PerfectAnswersCount: 'badges.metrics.perfectAnswers',
  TotalAttempts: 'badges.metrics.totalAttempts',
}

/** Translation keys for the metrics exposed in the current rule builder UI. */
export const SUPPORTED_METRICS_TRANSLATIONS: Record<
  SupportedBadgeMetrics,
  TranslationKey
> = {
  ScorePercentage: 'badges.metrics.scorePercentage',
  TotalScore: 'badges.metrics.totalScore',
}
