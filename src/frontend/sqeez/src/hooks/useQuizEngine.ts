import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { useAuthStore } from '@/store/useAuthStore'

import {
  useGetApiQuizzesQuizId,
  useGetApiQuizzesQuizIdQuestionsQuestionIdDetailed,
} from '@/api/generated/endpoints/quizzes/quizzes'
import { useGetApiSubjectsSubjectIdEnrollments } from '@/api/generated/endpoints/subjects/subjects'
import {
  getApiQuizAttemptsIdNextQuestion,
  getGetApiQuizAttemptsIdNextQuestionQueryKey,
  usePostApiQuizAttemptsIdAnswer,
  usePostApiQuizAttemptsIdComplete,
} from '@/api/generated/endpoints/quiz-attempts/quiz-attempts'
import { queryClient } from '@/main'
import {
  getApiMediaAssetsIdFile,
  getGetApiMediaAssetsIdFileQueryKey,
} from '@/api/generated/endpoints/media-assets/media-assets'
import { useQuizStore } from '@/store/useQuizStore'

export type QuizPhase =
  | 'start'
  | 'resuming'
  | 'transition'
  | 'answering'
  | 'recap'
  | 'completed'

export type BootUpError = 'QUIZ_NOT_FOUND' | 'NOT_ENROLLED' | null

/**
 * Orchestrates the student quiz flow from start/resume through completion.
 * The backend remains authoritative for enrollment, next-question progress, and grading.
 */
export function useQuizEngine(quizId: string, initialAttemptId?: number) {
  const { t } = useTranslation()
  const userId = useAuthStore((s) => s.user?.id)

  const state = useQuizStore()
  const { actions } = state

  const { data: quizData, isLoading: isQuizLoading } = useGetApiQuizzesQuizId(
    quizId,
    { studentId: userId },
    { query: { enabled: !!userId } },
  )

  const subjectId = quizData?.subjectId

  const { data: enrollmentData, isLoading: isEnrollmentLoading } =
    useGetApiSubjectsSubjectIdEnrollments(
      subjectId!,
      { StudentId: userId },
      { query: { enabled: !!subjectId && !!userId } },
    )

  const { data: currentQuestion, isLoading: isQuestionLoading } =
    useGetApiQuizzesQuizIdQuestionsQuestionIdDetailed(
      Number(quizId),
      Number(state.currentQuestionId),
      {
        query: {
          enabled:
            !!state.currentQuestionId &&
            (state.phase === 'transition' || state.phase === 'answering'),
          refetchOnWindowFocus: false,
          staleTime: 1000 * 60,
        },
      },
    )

  useEffect(() => {
    if (state.activeQuizId !== null && state.activeQuizId !== quizId) {
      actions.resetQuiz()
    } else if (state.activeQuizId !== quizId) {
      useQuizStore.setState({ activeQuizId: quizId })
    }
  }, [quizId, state.activeQuizId, actions])

  useEffect(() => {
    if (!initialAttemptId || state.attemptId === initialAttemptId) return

    let isMounted = true

    const executeStrictResume = async () => {
      try {
        actions.initResume(initialAttemptId)

        const nextQuestionProgress = await queryClient.fetchQuery({
          queryKey:
            getGetApiQuizAttemptsIdNextQuestionQueryKey(initialAttemptId),
          queryFn: () => getApiQuizAttemptsIdNextQuestion(initialAttemptId),
          staleTime: 0,
        })

        if (!isMounted) return

        // Resume must trust backend progress so refreshed pages keep the progress bar correct.
        const nextQuestionId = nextQuestionProgress.nextQuestionId
          ? Number(nextQuestionProgress.nextQuestionId)
          : null
        const answeredQuestionsCount = Number(
          nextQuestionProgress.answeredQuestionsCount || 0,
        )

        if (nextQuestionId) {
          actions.startAttempt(
            initialAttemptId,
            nextQuestionId,
            answeredQuestionsCount,
          )
        } else {
          actions.setQuestionsAnswered(answeredQuestionsCount)
          const response = await completeMutation.mutateAsync({
            id: initialAttemptId,
          })
          actions.completeQuiz(
            !response.earnedBadges ? undefined : response.earnedBadges,
          )
        }
      } catch (error) {
        if (!isMounted) return
        console.error('Strict resume failed:', error)
        toast.error(t('quiz.resumeError'))
        history.back()
      }
    }

    executeStrictResume()

    return () => {
      isMounted = false
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initialAttemptId])

  useEffect(() => {
    if (currentQuestion && state.phase === 'transition') {
      const assetIdsToFetch: (number | string)[] = []

      if (currentQuestion.mediaAssetId) {
        assetIdsToFetch.push(currentQuestion.mediaAssetId)
      }

      currentQuestion.options.forEach((option) => {
        if (option.mediaAssetId) assetIdsToFetch.push(option.mediaAssetId)
      })

      // Preload media for the next visible screen to avoid blank option/question assets.
      assetIdsToFetch.forEach((id) => {
        const assetId = Number(id)

        queryClient.prefetchQuery({
          queryKey: getGetApiMediaAssetsIdFileQueryKey(assetId),
          queryFn: () => getApiMediaAssetsIdFile(assetId),
          staleTime: 1000 * 60 * 60,
        })
      })
    }
  }, [currentQuestion, state.phase])

  const answerMutation = usePostApiQuizAttemptsIdAnswer()
  const completeMutation = usePostApiQuizAttemptsIdComplete()

  const handleAnswerSubmit = async (isTimeout = false) => {
    if (answerMutation.isPending) return

    if (!state.attemptId || !state.currentQuestionId) return

    const hasSelection =
      state.selectedOptionIds.length > 0 ||
      state.freeTextValue.trim().length > 0

    // Timeouts may submit an empty answer; manual submits require a user selection.
    if (!hasSelection && !isTimeout) {
      toast.warning(t('quiz.selectAnswerWarning'))
      return
    }

    try {
      const timeSpentMs = Date.now() - state.questionStartTime
      const response = await answerMutation.mutateAsync({
        id: state.attemptId,
        data: {
          quizQuestionId: state.currentQuestionId,
          responseTimeMs: timeSpentMs,
          freeTextAnswer: state.freeTextValue || null,
          selectedOptionIds: state.selectedOptionIds,
        },
      })

      if (!response.correctOptionIds)
        throw new Error('Response did not return correct answers!')

      const correctIds = response.correctOptionIds

      const isFullyCorrect =
        state.selectedOptionIds.length > 0 &&
        state.selectedOptionIds.length === correctIds.length &&
        state.selectedOptionIds.every((id) => correctIds.includes(id))

      actions.submitAnswer({
        correctIds,
        nextQuestionId: response.nextQuestionId
          ? Number(response.nextQuestionId)
          : null,
        correctFreeTextAnswer: response.correctFreeTextAnswer || null,
        responseTimeMs: Number(response.responseTimeMs),
        isFullyCorrect,
      })
    } catch (error) {
      console.error('Failed to submit answer', error)
      toast.error(t('common.error'))
    }
  }

  const handleRecapContinue = async () => {
    if (completeMutation.isPending) return

    if (state.nextQuestionId !== null) {
      actions.continueToNext()
    } else {
      try {
        if (state.attemptId) {
          const response = await completeMutation.mutateAsync({
            id: state.attemptId,
          })
          actions.completeQuiz(
            !response.earnedBadges ? undefined : response.earnedBadges,
            response.status === 'PendingCorrection',
          )
        } else {
          actions.completeQuiz()
        }
      } catch (error) {
        console.error('Failed to complete quiz', error)
        toast.error(t('common.error'))
      }
    }
  }

  let bootUpError: BootUpError = null

  if (!isQuizLoading && !quizData) {
    bootUpError = 'QUIZ_NOT_FOUND'
  } else if (
    !isEnrollmentLoading &&
    quizData &&
    (!enrollmentData?.data || enrollmentData.data.length === 0)
  ) {
    bootUpError = 'NOT_ENROLLED'
  }

  const isBootingUp =
    (isQuizLoading || isEnrollmentLoading) && bootUpError === null

  return {
    state: {
      ...state,
      isBootingUp,
      bootUpError,
      isQuestionLoading,
      quizData,
      userEnrollment: enrollmentData?.data?.[0],
      currentQuestion,
      totalQuestions: Number(quizData?.quizQuestions || 0),
      hasSelection:
        state.selectedOptionIds.length > 0 ||
        state.freeTextValue.trim().length > 0,
    },
    actions: {
      handleAttemptStarted: (
        attemptId: number,
        firstQuestionId: number | null,
      ) => actions.startAttempt(attemptId, firstQuestionId),
      handleTransitionComplete: () => actions.finishTransition(),
      resetEngine: () => actions.resetQuiz(),

      handleOptionSelect: (optId: number | string) => {
        const isMultiChoice = currentQuestion?.isStrictMultipleChoice === true

        if (isMultiChoice) {
          const isAlreadySelected = state.selectedOptionIds.includes(optId)
          const newSelection = isAlreadySelected
            ? state.selectedOptionIds.filter((id) => id !== optId)
            : [...state.selectedOptionIds, optId]

          actions.setSelectedOptions(newSelection)
        } else {
          actions.setSelectedOptions([optId])
        }
      },

      handleFreeTextChange: (text: string) => actions.setFreeText(text),
      handleAnswerSubmit,
      handleRecapContinue,
    },
  }
}
