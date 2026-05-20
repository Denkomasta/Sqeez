import { createFileRoute } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { Spinner } from '@/components/ui/Spinner'

import { QuizStartScreen } from './-/QuizStartScreen'
import { QuestionTransitionScreen } from './-/QuizTransitionScreen'
import { QuestionRecapScreen } from './-/QuestionRecapScreen'
import { QuizRecapScreen } from './-/QuizRecapScreen'
import { ActiveQuestionScreen } from './-/ActiveQuestionScreen'
import { useQuizEngine } from '@/hooks/useQuizEngine'
import { MediaAssetViewer } from './-/MediaAssetViewer'
import { ErrorCard } from '@/components/ui/Card'

export const Route = createFileRoute(
  '/app/_authenticated/quizzes/$quizId/play/',
)({
  component: QuizTakePage,
  validateSearch: (search: Record<string, unknown>) => {
    return {
      attemptId: search.attemptId ? Number(search.attemptId) : undefined,
    }
  },
})

/**
 * Quiz play route driven by useQuizEngine.
 * Renders the correct screen for the current engine phase.
 */
function QuizTakePage() {
  const { t } = useTranslation()
  const { quizId } = Route.useParams()
  const search = Route.useSearch()

  const { state, actions } = useQuizEngine(quizId, search.attemptId)

  if (state.isBootingUp) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4">
        <Spinner size="lg" />
      </div>
    )
  }

  if (state.isBootingUp) {
    return <Spinner size="lg" className="m-auto" />
  }

  if (state.bootUpError) {
    const isNotEnrolled = state.bootUpError === 'NOT_ENROLLED'

    return (
      <ErrorCard
        title={
          isNotEnrolled ? t('quiz.notEnrolledTitle') : t('quiz.notFoundTitle')
        }
        description={
          isNotEnrolled ? t('quiz.notEnrolledDesc') : t('quiz.notFoundDesc')
        }
      />
    )
  }

  switch (state.phase) {
    case 'start':
      return (
        <QuizStartScreen
          quizId={Number(quizId)}
          quizTitle={state.quizData!.title}
          enrollmentId={Number(state.userEnrollment!.id)}
          onAttemptStarted={actions.handleAttemptStarted}
          onCancel={() => history.back()}
        />
      )

    case 'resuming':
      return (
        <div className="flex min-h-[60vh] animate-in flex-col items-center justify-center gap-4 duration-500 fade-in">
          <Spinner size="lg" />
          <p className="animate-pulse text-lg font-medium text-muted-foreground">
            {t('quiz.resumingAttempt')}
          </p>
        </div>
      )

    case 'transition':
      return (
        <QuestionTransitionScreen
          questionNumber={state.questionsAnswered + 1}
          totalQuestions={state.totalQuestions}
          onComplete={actions.handleTransitionComplete}
        />
      )

    case 'answering':
      return (
        <ActiveQuestionScreen
          question={state.currentQuestion!}
          isLoading={state.isQuestionLoading}
          currentNumber={state.questionsAnswered + 1}
          totalQuestions={state.totalQuestions}
          selectedOptionIds={state.selectedOptionIds}
          freeTextValue={state.freeTextValue}
          hasSelection={state.hasSelection}
          onSelectOption={actions.handleOptionSelect}
          onChangeFreeText={actions.handleFreeTextChange}
          onSubmit={actions.handleAnswerSubmit}
          renderMediaAsset={(assetId, isOption) => (
            <MediaAssetViewer assetId={assetId} isOption={isOption} />
          )}
        />
      )

    case 'recap':
      return (
        <QuestionRecapScreen
          question={state.currentQuestion!}
          selectedOptionIds={state.selectedOptionIds}
          correctOptionIds={state.currentCorrectOptionIds}
          userFreeTextAnswer={state.freeTextValue}
          correctFreeTextAnswer={state.correctFreeTextAnswer}
          timeSpentMs={state.lastResponseTimeMs || 0}
          onContinue={actions.handleRecapContinue}
          isLastQuestion={state.nextQuestionId === null}
        />
      )

    case 'completed':
      return (
        <QuizRecapScreen
          quizId={quizId}
          quizTitle={t('quiz.quizRecapTitle')}
          totalQuestions={state.questionsAnswered}
          correctCount={state.correctAnswersCount}
          badges={state.earnedBadges}
          resetQuiz={actions.resetEngine}
          isPendingCorrection={state.isPendingCorrection || false}
        />
      )

    default:
      return null
  }
}
