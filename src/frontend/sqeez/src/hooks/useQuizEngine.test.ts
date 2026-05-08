import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type {
  DetailedQuizQuestionDto,
  QuestionAnsweredDto,
  QuizAttemptDto,
} from '@/api/generated/model'
import { useAuthStore } from '@/store/useAuthStore'
import { useQuizStore } from '@/store/useQuizStore'
import { useQuizEngine } from './useQuizEngine'
import {
  useGetApiQuizzesQuizId,
  useGetApiQuizzesQuizIdQuestionsQuestionIdDetailed,
} from '@/api/generated/endpoints/quizzes/quizzes'
import { useGetApiSubjectsSubjectIdEnrollments } from '@/api/generated/endpoints/subjects/subjects'

const mocks = vi.hoisted(() => ({
  quizQuery: { data: undefined as unknown, isLoading: false },
  enrollmentQuery: { data: undefined as unknown, isLoading: false },
  questionQuery: { data: undefined as unknown, isLoading: false },
  answerMutation: {
    isPending: false,
    mutateAsync: vi.fn(),
  },
  completeMutation: {
    isPending: false,
    mutateAsync: vi.fn(),
  },
  nextQuestion: vi.fn(),
  queryClient: {
    fetchQuery: vi.fn(),
    prefetchQuery: vi.fn(),
  },
  mediaFile: vi.fn(),
  toast: {
    warning: vi.fn(),
    error: vi.fn(),
  },
}))

vi.mock('@/main', () => ({
  queryClient: mocks.queryClient,
}))

vi.mock('sonner', () => ({
  toast: mocks.toast,
}))

vi.mock('@/api/generated/endpoints/quizzes/quizzes', () => ({
  useGetApiQuizzesQuizId: vi.fn(() => mocks.quizQuery),
  useGetApiQuizzesQuizIdQuestionsQuestionIdDetailed: vi.fn(
    () => mocks.questionQuery,
  ),
}))

vi.mock('@/api/generated/endpoints/subjects/subjects', () => ({
  useGetApiSubjectsSubjectIdEnrollments: vi.fn(() => mocks.enrollmentQuery),
}))

vi.mock('@/api/generated/endpoints/quiz-attempts/quiz-attempts', () => ({
  getApiQuizAttemptsIdNextQuestion: mocks.nextQuestion,
  getGetApiQuizAttemptsIdNextQuestionQueryKey: (id: number | string) => [
    'next-question',
    id,
  ],
  usePostApiQuizAttemptsIdAnswer: vi.fn(() => mocks.answerMutation),
  usePostApiQuizAttemptsIdComplete: vi.fn(() => mocks.completeMutation),
}))

vi.mock('@/api/generated/endpoints/media-assets/media-assets', () => ({
  getApiMediaAssetsIdFile: mocks.mediaFile,
  getGetApiMediaAssetsIdFileQueryKey: (id: number | string) => [
    'media-file',
    id,
  ],
}))

const quizData = {
  id: 10,
  subjectId: 55,
  quizQuestions: 3,
}

const enrollmentData = {
  data: [{ id: 77, subjectId: 55, studentId: 7 }],
}

const question = (
  overrides: Partial<DetailedQuizQuestionDto> = {},
): DetailedQuizQuestionDto => ({
  id: 99,
  title: 'Question',
  difficulty: 1,
  hasPenalty: false,
  calculatedPenalty: 0,
  timeLimit: 30,
  isStrictMultipleChoice: false,
  quizId: 10,
  mediaAssetId: null,
  options: [
    {
      id: 1,
      text: 'A',
      isFreeText: false,
      quizQuestionId: 99,
      mediaAssetId: null,
    },
    {
      id: 2,
      text: 'B',
      isFreeText: false,
      quizQuestionId: 99,
      mediaAssetId: null,
    },
  ],
  ...overrides,
})

const answerResponse = (
  overrides: Partial<QuestionAnsweredDto> = {},
): QuestionAnsweredDto => ({
  id: 5,
  quizQuestionId: 99,
  responseTimeMs: 4500,
  freeTextAnswer: null,
  isLiked: false,
  score: null,
  selectedOptionIds: [1],
  correctOptionIds: [1],
  correctFreeTextAnswer: null,
  nextQuestionId: 100,
  ...overrides,
})

const completeResponse = (
  overrides: Partial<QuizAttemptDto> = {},
): QuizAttemptDto => ({
  id: 123,
  quizId: 10,
  enrollmentId: 77,
  startTime: null,
  endTime: null,
  status: 'Completed',
  totalScore: 10,
  mark: null,
  ...overrides,
})

type QuizStoreState = ReturnType<typeof useQuizStore.getState>

const prepareAnsweringState = (
  overrides: Partial<Omit<QuizStoreState, 'actions'>> = {},
) => {
  useQuizStore.setState({
    attemptId: 123,
    currentQuestionId: 99,
    phase: 'answering',
    questionStartTime: 1_000,
    selectedOptionIds: [1],
    freeTextValue: '',
    ...overrides,
  })
}

describe('useQuizEngine', () => {
  beforeEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
    vi.clearAllMocks()

    mocks.quizQuery.data = quizData
    mocks.quizQuery.isLoading = false
    mocks.enrollmentQuery.data = enrollmentData
    mocks.enrollmentQuery.isLoading = false
    mocks.questionQuery.data = undefined
    mocks.questionQuery.isLoading = false
    mocks.answerMutation.isPending = false
    mocks.answerMutation.mutateAsync.mockResolvedValue(answerResponse())
    mocks.completeMutation.isPending = false
    mocks.completeMutation.mutateAsync.mockResolvedValue(completeResponse())
    mocks.queryClient.fetchQuery.mockResolvedValue({
      nextQuestionId: 99,
      answeredQuestionsCount: 0,
    })
    mocks.queryClient.prefetchQuery.mockResolvedValue(undefined)

    useQuizStore.getState().actions.resetQuiz()
    useAuthStore.getState().setUser({
      id: 7,
      username: 'student',
      email: 'student@example.com',
      currentXP: '0',
      role: 'Student',
      avatarUrl: null,
    })
  })

  it('exposes boot data and query-derived state', () => {
    const { result } = renderHook(() => useQuizEngine('10'))

    expect(result.current.state).toMatchObject({
      isBootingUp: false,
      bootUpError: null,
      quizData,
      userEnrollment: enrollmentData.data[0],
      totalQuestions: 3,
      hasSelection: false,
    })
    expect(useGetApiQuizzesQuizId).toHaveBeenCalledWith(
      '10',
      { studentId: 7 },
      { query: { enabled: true } },
    )
    expect(useGetApiSubjectsSubjectIdEnrollments).toHaveBeenCalledWith(
      55,
      { StudentId: 7 },
      { query: { enabled: true } },
    )
  })

  it('reports boot errors for missing quizzes and enrollments', () => {
    mocks.quizQuery.data = undefined

    const { result, rerender } = renderHook(() => useQuizEngine('10'))

    expect(result.current.state.bootUpError).toBe('QUIZ_NOT_FOUND')

    mocks.quizQuery.data = quizData
    mocks.enrollmentQuery.data = { data: [] }
    rerender()

    expect(result.current.state.bootUpError).toBe('NOT_ENROLLED')
  })

  it('reports boot loading while quiz or enrollment is loading', () => {
    mocks.quizQuery.isLoading = true

    const { result, rerender } = renderHook(() => useQuizEngine('10'))

    expect(result.current.state.isBootingUp).toBe(true)

    mocks.quizQuery.isLoading = false
    mocks.enrollmentQuery.isLoading = true
    rerender()

    expect(result.current.state.isBootingUp).toBe(true)
  })

  it('tracks active quiz identity and resets when quiz id changes', async () => {
    const { rerender } = renderHook(({ quizId }) => useQuizEngine(quizId), {
      initialProps: { quizId: '10' },
    })

    await waitFor(() => expect(useQuizStore.getState().activeQuizId).toBe('10'))

    act(() => {
      useQuizStore.getState().actions.setSelectedOptions([1])
    })

    rerender({ quizId: '11' })

    await waitFor(() => expect(useQuizStore.getState().activeQuizId).toBe('11'))
    expect(useQuizStore.getState().selectedOptionIds).toEqual([])
  })

  it('starts attempts and finishes transitions through public actions', () => {
    const { result } = renderHook(() => useQuizEngine('10'))

    act(() => {
      result.current.actions.handleAttemptStarted(123, 99)
    })

    expect(useQuizStore.getState()).toMatchObject({
      attemptId: 123,
      currentQuestionId: 99,
      phase: 'transition',
    })

    act(() => {
      result.current.actions.handleTransitionComplete()
    })

    expect(useQuizStore.getState()).toMatchObject({
      phase: 'answering',
      selectedOptionIds: [],
      freeTextValue: '',
    })
  })

  it('selects single-choice and multi-choice answers', () => {
    mocks.questionQuery.data = question()
    prepareAnsweringState()

    const { result, rerender } = renderHook(() => useQuizEngine('10'))

    act(() => {
      result.current.actions.handleOptionSelect(2)
    })

    expect(useQuizStore.getState().selectedOptionIds).toEqual([2])

    mocks.questionQuery.data = question({ isStrictMultipleChoice: true })
    rerender()

    act(() => {
      result.current.actions.handleOptionSelect(3)
    })
    rerender()

    expect(useQuizStore.getState().selectedOptionIds).toEqual([2, 3])

    act(() => {
      result.current.actions.handleOptionSelect(2)
    })

    expect(useQuizStore.getState().selectedOptionIds).toEqual([3])
  })

  it('tracks free text and selection state', () => {
    const { result, rerender } = renderHook(() => useQuizEngine('10'))

    act(() => {
      result.current.actions.handleFreeTextChange('   ')
    })
    rerender()

    expect(result.current.state.hasSelection).toBe(false)

    act(() => {
      result.current.actions.handleFreeTextChange('Written answer')
    })
    rerender()

    expect(result.current.state.hasSelection).toBe(true)
  })

  it('submits an answer payload and moves to recap', async () => {
    vi.spyOn(Date, 'now').mockReturnValue(5_500)
    prepareAnsweringState({
      selectedOptionIds: [1],
      freeTextValue: 'answer',
    })

    const { result } = renderHook(() => useQuizEngine('10'))

    await act(async () => {
      await result.current.actions.handleAnswerSubmit()
    })

    expect(mocks.answerMutation.mutateAsync).toHaveBeenCalledWith({
      id: 123,
      data: {
        quizQuestionId: 99,
        responseTimeMs: 4_500,
        freeTextAnswer: 'answer',
        selectedOptionIds: [1],
      },
    })
    expect(useQuizStore.getState()).toMatchObject({
      phase: 'recap',
      currentCorrectOptionIds: [1],
      nextQuestionId: 100,
      correctAnswersCount: 1,
      questionsAnswered: 1,
      lastResponseTimeMs: 4500,
    })
  })

  it('stores expected free-text answers without auto-scoring them as correct', async () => {
    vi.spyOn(Date, 'now').mockReturnValue(5_500)
    prepareAnsweringState({
      selectedOptionIds: [],
      freeTextValue: 'Written answer',
    })
    mocks.answerMutation.mutateAsync.mockResolvedValue(
      answerResponse({
        selectedOptionIds: [],
        correctOptionIds: [],
        correctFreeTextAnswer: 'Expected teacher-facing answer',
      }),
    )

    const { result } = renderHook(() => useQuizEngine('10'))

    await act(async () => {
      await result.current.actions.handleAnswerSubmit()
    })

    expect(mocks.answerMutation.mutateAsync).toHaveBeenCalledWith({
      id: 123,
      data: {
        quizQuestionId: 99,
        responseTimeMs: 4_500,
        freeTextAnswer: 'Written answer',
        selectedOptionIds: [],
      },
    })
    expect(useQuizStore.getState()).toMatchObject({
      phase: 'recap',
      correctFreeTextAnswer: 'Expected teacher-facing answer',
      correctAnswersCount: 0,
      questionsAnswered: 1,
    })
  })

  it('blocks answer submission without selection unless it is a timeout', async () => {
    prepareAnsweringState({ selectedOptionIds: [], freeTextValue: '' })

    const { result } = renderHook(() => useQuizEngine('10'))

    await act(async () => {
      await result.current.actions.handleAnswerSubmit()
    })

    expect(mocks.toast.warning).toHaveBeenCalledWith('quiz.selectAnswerWarning')
    expect(mocks.answerMutation.mutateAsync).not.toHaveBeenCalled()

    await act(async () => {
      await result.current.actions.handleAnswerSubmit(true)
    })

    expect(mocks.answerMutation.mutateAsync).toHaveBeenCalled()
  })

  it('does not submit while pending or without attempt/question ids', async () => {
    prepareAnsweringState()
    mocks.answerMutation.isPending = true

    const { result, rerender } = renderHook(() => useQuizEngine('10'))

    await act(async () => {
      await result.current.actions.handleAnswerSubmit()
    })

    expect(mocks.answerMutation.mutateAsync).not.toHaveBeenCalled()

    mocks.answerMutation.isPending = false
    act(() => {
      useQuizStore.setState({ attemptId: null })
    })
    rerender()

    await act(async () => {
      await result.current.actions.handleAnswerSubmit()
    })

    expect(mocks.answerMutation.mutateAsync).not.toHaveBeenCalled()

    act(() => {
      useQuizStore.setState({ attemptId: 123, currentQuestionId: null })
    })
    rerender()

    await act(async () => {
      await result.current.actions.handleAnswerSubmit()
    })

    expect(mocks.answerMutation.mutateAsync).not.toHaveBeenCalled()
  })

  it('does not count partial or over-selected answers as fully correct', async () => {
    prepareAnsweringState({ selectedOptionIds: [1] })
    mocks.answerMutation.mutateAsync.mockResolvedValue(
      answerResponse({ correctOptionIds: [1, 2] }),
    )

    const { result, rerender } = renderHook(() => useQuizEngine('10'))

    await act(async () => {
      await result.current.actions.handleAnswerSubmit()
    })

    expect(useQuizStore.getState().correctAnswersCount).toBe(0)

    act(() => {
      useQuizStore.getState().actions.continueToNext()
      useQuizStore.setState({
        phase: 'answering',
        currentQuestionId: 100,
        selectedOptionIds: [1, 2, 3],
      })
    })
    mocks.answerMutation.mutateAsync.mockResolvedValue(
      answerResponse({ quizQuestionId: 100, correctOptionIds: [1, 2] }),
    )
    rerender()

    await act(async () => {
      await result.current.actions.handleAnswerSubmit()
    })

    expect(useQuizStore.getState().correctAnswersCount).toBe(0)
  })

  it('shows an error when answer submission fails', async () => {
    const consoleErrorSpy = vi
      .spyOn(console, 'error')
      .mockImplementation(() => undefined)
    prepareAnsweringState()
    mocks.answerMutation.mutateAsync.mockRejectedValue(new Error('Nope'))

    const { result } = renderHook(() => useQuizEngine('10'))

    await act(async () => {
      await result.current.actions.handleAnswerSubmit()
    })

    expect(consoleErrorSpy).toHaveBeenCalledWith(
      'Failed to submit answer',
      expect.any(Error),
    )
    expect(mocks.toast.error).toHaveBeenCalledWith('common.error')
    expect(useQuizStore.getState().phase).toBe('answering')
  })

  it('continues recap to the next question or completes the attempt', async () => {
    useQuizStore.setState({
      attemptId: 123,
      phase: 'recap',
      currentQuestionId: 99,
      nextQuestionId: 100,
      selectedOptionIds: [1],
      freeTextValue: 'answer',
      currentCorrectOptionIds: [1],
    })

    const { result, rerender } = renderHook(() => useQuizEngine('10'))

    await act(async () => {
      await result.current.actions.handleRecapContinue()
    })

    expect(useQuizStore.getState()).toMatchObject({
      phase: 'transition',
      currentQuestionId: 100,
      selectedOptionIds: [],
      freeTextValue: '',
    })

    act(() => {
      useQuizStore.setState({
        phase: 'recap',
        nextQuestionId: null,
        attemptId: 123,
      })
    })
    mocks.completeMutation.mutateAsync.mockResolvedValue(
      completeResponse({
        status: 'PendingCorrection',
        earnedBadges: [{ badgeId: 1, name: 'Finisher' }],
      }),
    )
    rerender()

    await act(async () => {
      await result.current.actions.handleRecapContinue()
    })

    expect(mocks.completeMutation.mutateAsync).toHaveBeenCalledWith({
      id: 123,
    })
    expect(useQuizStore.getState()).toMatchObject({
      phase: 'completed',
      earnedBadges: [{ badgeId: 1, name: 'Finisher' }],
      isPendingCorrection: true,
    })
  })

  it('completes locally when no attempt exists and blocks duplicate completion', async () => {
    useQuizStore.setState({
      phase: 'recap',
      nextQuestionId: null,
      attemptId: null,
    })

    const { result, rerender } = renderHook(() => useQuizEngine('10'))

    await act(async () => {
      await result.current.actions.handleRecapContinue()
    })

    expect(useQuizStore.getState().phase).toBe('completed')
    expect(mocks.completeMutation.mutateAsync).not.toHaveBeenCalled()

    act(() => {
      useQuizStore.setState({
        phase: 'recap',
        nextQuestionId: null,
        attemptId: 123,
      })
    })
    mocks.completeMutation.isPending = true
    rerender()

    await act(async () => {
      await result.current.actions.handleRecapContinue()
    })

    expect(mocks.completeMutation.mutateAsync).not.toHaveBeenCalled()
  })

  it('shows an error when completion fails', async () => {
    const consoleErrorSpy = vi
      .spyOn(console, 'error')
      .mockImplementation(() => undefined)
    useQuizStore.setState({
      phase: 'recap',
      nextQuestionId: null,
      attemptId: 123,
    })
    mocks.completeMutation.mutateAsync.mockRejectedValue(new Error('Nope'))

    const { result } = renderHook(() => useQuizEngine('10'))

    await act(async () => {
      await result.current.actions.handleRecapContinue()
    })

    expect(consoleErrorSpy).toHaveBeenCalledWith(
      'Failed to complete quiz',
      expect.any(Error),
    )
    expect(mocks.toast.error).toHaveBeenCalledWith('common.error')
    expect(useQuizStore.getState().phase).toBe('recap')
  })

  it('strictly resumes an attempt with a next question', async () => {
    mocks.queryClient.fetchQuery.mockResolvedValue({
      nextQuestionId: 101,
      answeredQuestionsCount: 2,
    })

    renderHook(() => useQuizEngine('10', 456))

    await waitFor(() =>
      expect(useQuizStore.getState()).toMatchObject({
        attemptId: 456,
        currentQuestionId: 101,
        questionsAnswered: 2,
        phase: 'transition',
      }),
    )
    expect(mocks.queryClient.fetchQuery).toHaveBeenCalledWith(
      expect.objectContaining({
        queryKey: ['next-question', 456],
        staleTime: 0,
      }),
    )
  })

  it('strictly resumes and completes when there is no next question', async () => {
    mocks.queryClient.fetchQuery.mockResolvedValue({
      nextQuestionId: null,
      answeredQuestionsCount: 3,
    })
    mocks.completeMutation.mutateAsync.mockResolvedValue(
      completeResponse({ earnedBadges: [{ badgeId: 2, name: 'Done' }] }),
    )

    renderHook(() => useQuizEngine('10', 456))

    await waitFor(() =>
      expect(useQuizStore.getState()).toMatchObject({
        phase: 'completed',
        questionsAnswered: 3,
        earnedBadges: [{ badgeId: 2, name: 'Done' }],
      }),
    )
  })

  it('handles strict resume failure', async () => {
    const consoleErrorSpy = vi
      .spyOn(console, 'error')
      .mockImplementation(() => undefined)
    const historyBackSpy = vi
      .spyOn(history, 'back')
      .mockImplementation(() => undefined)
    mocks.queryClient.fetchQuery.mockRejectedValue(new Error('Resume failed'))

    renderHook(() => useQuizEngine('10', 456))

    await waitFor(() =>
      expect(mocks.toast.error).toHaveBeenCalledWith('quiz.resumeError'),
    )
    expect(consoleErrorSpy).toHaveBeenCalledWith(
      'Strict resume failed:',
      expect.any(Error),
    )
    expect(historyBackSpy).toHaveBeenCalled()
  })

  it('prefetches current question and option media during transition', async () => {
    mocks.questionQuery.data = question({
      mediaAssetId: '88',
      options: [
        {
          id: 1,
          text: 'A',
          isFreeText: false,
          quizQuestionId: 99,
          mediaAssetId: '89',
        },
        {
          id: 2,
          text: 'B',
          isFreeText: false,
          quizQuestionId: 99,
          mediaAssetId: null,
        },
      ],
    })
    useQuizStore.setState({
      attemptId: 123,
      currentQuestionId: 99,
      phase: 'transition',
    })

    renderHook(() => useQuizEngine('10'))

    await waitFor(() =>
      expect(mocks.queryClient.prefetchQuery).toHaveBeenCalledTimes(2),
    )
    expect(mocks.queryClient.prefetchQuery).toHaveBeenCalledWith(
      expect.objectContaining({ queryKey: ['media-file', 88] }),
    )
    expect(mocks.queryClient.prefetchQuery).toHaveBeenCalledWith(
      expect.objectContaining({ queryKey: ['media-file', 89] }),
    )
    expect(
      vi.mocked(useGetApiQuizzesQuizIdQuestionsQuestionIdDetailed),
    ).toHaveBeenCalledWith(
      10,
      99,
      expect.objectContaining({
        query: expect.objectContaining({ enabled: true }),
      }),
    )
  })
})
