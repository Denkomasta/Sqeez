import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import type { StudentBadgeBasicDto } from '@/api/generated/model'
import type { QuizPhase } from '@/hooks/useQuizEngine'

interface QuizState {
  /** Quiz id currently owned by this state machine; changing it resets stale attempt state. */
  activeQuizId: string | null
  /** Current screen/state of the quiz runner. */
  phase: QuizPhase
  /** Active attempt id returned by the backend after starting or resuming. */
  attemptId: number | null
  /** Question currently being loaded or answered. */
  currentQuestionId: number | null
  /** Next question id returned by the last answer response; null means the attempt can be completed. */
  nextQuestionId: number | null
  /** Timestamp captured when a question enters the answering phase. */
  questionStartTime: number
  /** Response time returned by the backend for the most recently submitted answer. */
  lastResponseTimeMs: number | null
  /** Expected free-text answer shown in recap; the teacher still performs final correction. */
  correctFreeTextAnswer: string | null
  /** Local count of fully correct automatically graded choice answers. */
  correctAnswersCount: number
  /** Number of answered questions, seeded from backend progress when an attempt is resumed. */
  questionsAnswered: number
  /** Locally selected option ids for the current question. */
  selectedOptionIds: (number | string)[]
  /** Local free-text answer for the current question. */
  freeTextValue: string
  /** Badges returned when the backend completes the attempt. */
  earnedBadges: StudentBadgeBasicDto[]
  /** Correct option ids returned by the last answer response for recap display. */
  currentCorrectOptionIds: (number | string)[]
  /** Final attempt status hint used when free-text answers are waiting for teacher correction. */
  isPendingCorrection: boolean | null

  actions: {
    /** Clears stale quiz state and marks the UI as resuming an existing backend attempt. */
    initResume: (attemptId: number) => void
    /**
     * Starts or resumes an attempt using backend progress.
     * `answeredQuestionsCount` must come from the API when resuming so the progress bar stays correct.
     */
    startAttempt: (
      attemptId: number,
      firstQuestionId: number | null,
      answeredQuestionsCount?: number,
    ) => void
    /** Replaces answered-question progress with the backend value. */
    setQuestionsAnswered: (count: number) => void
    /** Moves from transition to answering and resets per-question inputs/timing. */
    finishTransition: () => void
    /** Stores local option selection for the current question. */
    setSelectedOptions: (ids: (number | string)[]) => void
    /** Stores local free-text input for the current question. */
    setFreeText: (text: string) => void
    /** Stores recap payload returned after answer submission and advances progress by one. */
    submitAnswer: (payload: {
      correctIds: (number | string)[]
      nextQuestionId: number | null
      correctFreeTextAnswer: string | null
      responseTimeMs: number
      isFullyCorrect: boolean
    }) => void
    /** Moves to the next backend-provided question id and clears recap/input state. */
    continueToNext: () => void
    /** Marks the attempt as completed and stores final badges/status returned by the backend. */
    completeQuiz: (
      badges?: StudentBadgeBasicDto[],
      isPendingCorrection?: boolean,
    ) => void
    /** Returns the quiz runner to its initial state. */
    resetQuiz: () => void
  }
}

/** Initial state for a fresh quiz runner with no active attempt. */
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

/**
 * Drives the student quiz-taking state machine.
 * Server data decides the next question and final grading status; the store only tracks
 * local phase, UI inputs, timer checkpoints, and the latest response recap payload.
 *
 * Important invariants:
 * - `finishTransition` is the only action that starts the per-question timer.
 * - `submitAnswer` increments `questionsAnswered` exactly once per accepted response.
 * - `nextQuestionId === null` means the next continue action should complete the attempt.
 * - `correctFreeTextAnswer` is an expected answer for display, not an automatic final grade.
 */
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
