import type { BadgeOperator, BadgeMetric } from '@/api/generated/model'
import type { TranslationKey } from '@/i18next'

export type SupportedBadgeMetrics = Exclude<
  BadgeMetric,
  'PerfectAnswersCount' | 'TotalAttempts'
>

export const OPERATOR_MAP: Record<BadgeOperator, string> = {
  Equals: '=',
  GreaterThan: '>',
  GreaterThanOrEqual: '>=',
  LessThan: '<',
  LessThanOrEqual: '<=',
  NotEquals: '!=',
}

export const OPERATOR_TRANSLATIONS: Record<BadgeOperator, TranslationKey> = {
  Equals: 'badges.operators.equals',
  GreaterThan: 'badges.operators.greaterThan',
  GreaterThanOrEqual: 'badges.operators.greaterThanOrEqual',
  LessThan: 'badges.operators.lessThan',
  LessThanOrEqual: 'badges.operators.lessThanOrEqual',
  NotEquals: 'badges.operators.notEquals',
}

export const METRIC_TRANSLATIONS: Record<BadgeMetric, TranslationKey> = {
  ScorePercentage: 'badges.metrics.scorePercentage',
  TotalScore: 'badges.metrics.totalScore',
  PerfectAnswersCount: 'badges.metrics.perfectAnswers',
  TotalAttempts: 'badges.metrics.totalAttempts',
}

export const SUPPORTED_METRICS_TRANSLATIONS: Record<
  SupportedBadgeMetrics,
  TranslationKey
> = {
  ScorePercentage: 'badges.metrics.scorePercentage',
  TotalScore: 'badges.metrics.totalScore',
}
