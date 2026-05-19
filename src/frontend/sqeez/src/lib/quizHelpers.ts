import { isAxiosError } from 'axios'
import { toast } from 'sonner'
import { type TFunction } from 'i18next'
import { useQuizEditorUIStore } from '@/store/useQuizEditorUIStore'
import { parseUtcTime } from '@/lib/dateHelpers'
import { getErrorMessage } from '@/lib/errorHelpers'
import { MAX_QUIZ_QUESTION_OPTIONS } from '@/constants/quizConstants'

export interface QuizDateInfo {
  publishDate?: string | null
  closingDate?: string | null
}

/**
 * Checks if a quiz is currently closed.
 */
export function isQuizClosed(closingDate?: string | null): boolean {
  if (!closingDate) return false

  const now = Date.now()
  const closingTime = parseUtcTime(closingDate)

  return now > closingTime
}

/**
 * Checks if a quiz is currently active and available to be played.
 */
export function isQuizActive(quiz?: QuizDateInfo | null): boolean {
  if (!quiz || !quiz.publishDate) return false

  const now = Date.now()
  const publishTime = parseUtcTime(quiz.publishDate)

  if (now < publishTime) {
    return false
  }

  return !isQuizClosed(quiz.closingDate)
}

/**
 * Returns a specific status string for UI badges.
 * Returns: 'draft' | 'scheduled' | 'active' | 'closed'
 */
export function getQuizStatus(
  quiz?: QuizDateInfo | null,
): 'draft' | 'scheduled' | 'active' | 'closed' {
  if (!quiz || !quiz.publishDate) return 'draft'

  const now = Date.now()
  const publishTime = parseUtcTime(quiz.publishDate)

  if (now < publishTime) return 'scheduled'

  if (quiz.closingDate) {
    const closingTime = parseUtcTime(quiz.closingDate)
    if (now > closingTime) return 'closed'
  }

  return 'active'
}

const translateQuizMutationErrorMessage = (
  message: string,
  t: TFunction,
): string => {
  if (message.includes(`maximum of ${MAX_QUIZ_QUESTION_OPTIONS} options`)) {
    return t('editor.maxOptionsReached', {
      max: MAX_QUIZ_QUESTION_OPTIONS,
    })
  }

  return message
}

/**
 * Shared toast handling for quiz-builder mutations.
 * A 409 locks the editor because the backend rejected edits to an attempted quiz.
 */
export function handleQuizMutationError(error: unknown, t: TFunction) {
  if (isAxiosError(error) && error.response?.status === 409) {
    useQuizEditorUIStore.getState().actions.setLocked(true)
    toast.error(t('editor.quizLockedTitle'), {
      description: t('editor.quizLockedDesc'),
      duration: 8000,
    })
  } else {
    const message = getErrorMessage(error)

    toast.error(t('common.error'), {
      description: message
        ? translateQuizMutationErrorMessage(message, t)
        : undefined,
    })
  }
}
