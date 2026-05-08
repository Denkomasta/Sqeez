import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import type { StudentBadgeBasicDto } from '@/api/generated/model'
import type { QuizPhase } from '@/hooks/useQuizEngine'

interface QuizState {
  activeQuizId: string | null
  phase: QuizPhase
  attemptId: number | null
  currentQuestionId: number | null
  nextQuestionId: number | null
  questionStartTime: number
  lastResponseTimeMs: number | null
  correctFreeTextAnswer: string | null
  correctAnswersCount: number
  questionsAnswered: number
  selectedOptionIds: (number | string)[]
  freeTextValue: string
  earnedBadges: StudentBadgeBasicDto[]
  currentCorrectOptionIds: (number | string)[]
  isPendingCorrection: boolean | null

  actions: {
    initResume: (attemptId: number) => void
    startAttempt: (
      attemptId: number,
      firstQuestionId: number | null,
      answeredQuestionsCount?: number,
    ) => void
    setQuestionsAnswered: (count: number) => void
    finishTransition: () => void
    setSelectedOptions: (ids: (number | string)[]) => void
    setFreeText: (text: string) => void
    submitAnswer: (payload: {
      correctIds: (number | string)[]
      nextQuestionId: number | null
      correctFreeTextAnswer: string | null
      responseTimeMs: number
      isFullyCorrect: boolean
    }) => void
    continueToNext: () => void
    completeQuiz: (
      badges?: StudentBadgeBasicDto[],
      isPendingCorrection?: boolean,
    ) => void
    resetQuiz: () => void
  }
}

const initialState = {
  activeQuizId: null,
  phase: 'start' as QuizPhase,
  attemptId: null,
  currentQuestionId: null,
  nextQuestionId: null,
  questionStartTime: Date.now(),
  lastResponseTimeMs: null,
  correctFreeTextAnswer: null,
  correctAnswersCount: 0,
  questionsAnswered: 0,
  selectedOptionIds: [],
  freeTextValue: '',
  earnedBadges: [],
  currentCorrectOptionIds: [],
  isPendingCorrection: null,
}

export const useQuizStore = create<QuizState>()(
  devtools((set) => ({
    ...initialState,

    actions: {
      initResume: (attemptId) =>
        set({
          ...initialState,
          attemptId,
          phase: 'resuming',
        }),

      startAttempt: (attemptId, firstQuestionId, answeredQuestionsCount = 0) =>
        set({
          attemptId,
          currentQuestionId: firstQuestionId,
          questionsAnswered: answeredQuestionsCount,
          phase: firstQuestionId ? 'transition' : 'completed',
        }),

      setQuestionsAnswered: (count) => set({ questionsAnswered: count }),

      finishTransition: () =>
        set({
          phase: 'answering',
          questionStartTime: Date.now(),
          selectedOptionIds: [],
          freeTextValue: '',
          currentCorrectOptionIds: [],
        }),

      setSelectedOptions: (ids) => set({ selectedOptionIds: ids }),

      setFreeText: (text) => set({ freeTextValue: text }),

      submitAnswer: (payload) =>
        set((state) => ({
          phase: 'recap',
          currentCorrectOptionIds: payload.correctIds,
          nextQuestionId: payload.nextQuestionId,
          correctFreeTextAnswer: payload.correctFreeTextAnswer,
          lastResponseTimeMs: payload.responseTimeMs,
          correctAnswersCount:
            state.correctAnswersCount + (payload.isFullyCorrect ? 1 : 0),
          questionsAnswered: state.questionsAnswered + 1,
        })),

      continueToNext: () =>
        set((state) => ({
          currentQuestionId: state.nextQuestionId,
          phase: 'transition',

          selectedOptionIds: [],
          freeTextValue: '',
          currentCorrectOptionIds: [],
          correctFreeTextAnswer: null,
        })),

      completeQuiz: (badges, isPendingCorrection) =>
        set((state) => ({
          phase: 'completed',
          earnedBadges: badges || state.earnedBadges,
          isPendingCorrection: isPendingCorrection ?? state.isPendingCorrection,
        })),

      resetQuiz: () => set(initialState),
    },
  })),
)
