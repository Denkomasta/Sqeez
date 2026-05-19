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

export function getAttemptStatusTranslationKey(
  status?: AttemptStatusValue | null,
): AttemptStatusTranslationKey | null {
  if (!status) return null

  return (
    attemptStatusTranslationKeys[String(status) as KnownAttemptStatusValue] ??
    null
  )
}

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

export function isCompletedAttemptStatus(
  status?: AttemptStatusValue | null,
): boolean {
  return status === 'Completed'
}

export function isPendingCorrectionAttemptStatus(
  status?: AttemptStatusValue | null,
): boolean {
  return (
    status === 'PendingCorrection' ||
    status === 'PendingGrading' ||
    status === 'NeedsGrading'
  )
}

export function isInProgressAttemptStatus(
  status?: AttemptStatusValue | null,
): boolean {
  return status === 'Created' || status === 'Started' || status === 'InProgress'
}
