import { describe, expect, it, beforeEach, vi } from 'vitest'
import { useQuizStore } from './useQuizStore'

describe('useQuizStore', () => {
  beforeEach(() => {
    vi.useRealTimers()
    useQuizStore.getState().actions.resetQuiz()
  })

  it('initializes resume state', () => {
    useQuizStore.getState().actions.initResume(42)

    expect(useQuizStore.getState()).toMatchObject({
      attemptId: 42,
      phase: 'resuming',
      currentQuestionId: null,
      selectedOptionIds: [],
    })
  })

  it('starts an attempt with or without a first question', () => {
    useQuizStore.getState().actions.startAttempt(42, 10)

    expect(useQuizStore.getState()).toMatchObject({
      attemptId: 42,
      currentQuestionId: 10,
      phase: 'transition',
      questionsAnswered: 0,
    })

    useQuizStore.getState().actions.startAttempt(43, null, 4)

    expect(useQuizStore.getState()).toMatchObject({
      attemptId: 43,
      currentQuestionId: null,
      phase: 'completed',
      questionsAnswered: 4,
    })
  })

  it('stores backend answered question progress', () => {
    useQuizStore.getState().actions.setQuestionsAnswered(3)

    expect(useQuizStore.getState().questionsAnswered).toBe(3)
  })

  it('finishes transition and resets answer input', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-05-02T10:00:00.000Z'))

    useQuizStore.setState({
      phase: 'transition',
      selectedOptionIds: [1, 2],
      freeTextValue: 'answer',
      currentCorrectOptionIds: [1],
    })

    useQuizStore.getState().actions.finishTransition()

    expect(useQuizStore.getState()).toMatchObject({
      phase: 'answering',
      selectedOptionIds: [],
      freeTextValue: '',
      currentCorrectOptionIds: [],
      questionStartTime: Date.now(),
    })
  })

  it('submits an answer and tracks correctness', () => {
    useQuizStore.setState({
      correctAnswersCount: 1,
      questionsAnswered: 2,
    })

    useQuizStore.getState().actions.submitAnswer({
      correctIds: [1, 2],
      nextQuestionId: 99,
      correctFreeTextAnswer: null,
      responseTimeMs: 1234,
      isFullyCorrect: true,
    })

    expect(useQuizStore.getState()).toMatchObject({
      phase: 'recap',
      currentCorrectOptionIds: [1, 2],
      nextQuestionId: 99,
      lastResponseTimeMs: 1234,
      correctAnswersCount: 2,
      questionsAnswered: 3,
    })
  })

  it('continues to the next question and clears recap state', () => {
    useQuizStore.setState({
      phase: 'recap',
      nextQuestionId: 22,
      selectedOptionIds: [1],
      freeTextValue: 'answer',
      currentCorrectOptionIds: [1],
      correctFreeTextAnswer: 'correct',
    })

    useQuizStore.getState().actions.continueToNext()

    expect(useQuizStore.getState()).toMatchObject({
      currentQuestionId: 22,
      phase: 'transition',
      selectedOptionIds: [],
      freeTextValue: '',
      currentCorrectOptionIds: [],
      correctFreeTextAnswer: null,
    })
  })

  it('completes and resets a quiz', () => {
    const badges = [{ badgeId: 1, name: 'Starter' }]

    useQuizStore.getState().actions.completeQuiz(badges, true)

    expect(useQuizStore.getState()).toMatchObject({
      phase: 'completed',
      earnedBadges: badges,
      isPendingCorrection: true,
    })

    useQuizStore.getState().actions.resetQuiz()

    expect(useQuizStore.getState()).toMatchObject({
      activeQuizId: null,
      phase: 'start',
      attemptId: null,
      earnedBadges: [],
    })
  })
})
