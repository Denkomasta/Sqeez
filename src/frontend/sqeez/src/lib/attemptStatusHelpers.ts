import type { TFunction } from 'i18next'

import type { AttemptStatus } from '@/api/generated/model'

export type AttemptStatusValue =
  | AttemptStatus
  | 'InProgress'
  | 'PendingGrading'
  | 'NeedsGrading'
  | string

const attemptStatusTranslationKeys = {
  Abandoned: 'attempts.statusAbandoned',
  Completed: 'attempts.statusCompleted',
  Created: 'attempts.statusCreated',
  InProgress: 'attempts.statusStarted',
  NeedsGrading: 'attempts.statusPendingCorrection',
  PendingCorrection: 'attempts.statusPendingCorrection',
  PendingGrading: 'attempts.statusPendingCorrection',
  Started: 'attempts.statusStarted',
} as const

type KnownAttemptStatusValue = keyof typeof attemptStatusTranslationKeys

export type AttemptStatusTranslationKey =
  (typeof attemptStatusTranslationKeys)[KnownAttemptStatusValue]

/**
 * Returns the translation key for known attempt statuses.
 * Unknown backend values intentionally fall back to raw display elsewhere.
 *
 * @param status - Attempt status from the API or a legacy UI alias.
 * @returns Translation key for known statuses, otherwise null.
 */
export function getAttemptStatusTranslationKey(
  status?: AttemptStatusValue | null,
): AttemptStatusTranslationKey | null {
  if (!status) return null

  return (
    attemptStatusTranslationKeys[String(status) as KnownAttemptStatusValue] ??
    null
  )
}

/**
 * Formats an attempt status for UI badges and summaries.
 * Keeps unknown statuses visible instead of hiding a backend contract change.
 *
 * @param t - i18next translator used for known statuses and the unknown fallback.
 * @param status - Attempt status from the API or a legacy UI alias.
 */
export function getAttemptStatusLabel(
  t: TFunction,
  status?: AttemptStatusValue | null,
): string {
  const translationKey = getAttemptStatusTranslationKey(status)

  if (translationKey) {
    return t(translationKey)
  }

  return status ? String(status) : t('attempts.statusUnknown')
}

/**
 * True only for attempts fully completed by the backend.
 *
 * @param status - Attempt status from the API or a legacy UI alias.
 */
export function isCompletedAttemptStatus(
  status?: AttemptStatusValue | null,
): boolean {
  return status === 'Completed'
}

/**
 * Covers both current and older UI names for attempts waiting for teacher review.
 *
 * @param status - Attempt status from the API or a legacy UI alias.
 */
export function isPendingCorrectionAttemptStatus(
  status?: AttemptStatusValue | null,
): boolean {
  return (
    status === 'PendingCorrection' ||
    status === 'PendingGrading' ||
    status === 'NeedsGrading'
  )
}

/**
 * Covers attempts that can still be resumed or are not finished yet.
 *
 * @param status - Attempt status from the API or a legacy UI alias.
 */
export function isInProgressAttemptStatus(
  status?: AttemptStatusValue | null,
): boolean {
  return status === 'Created' || status === 'Started' || status === 'InProgress'
}
