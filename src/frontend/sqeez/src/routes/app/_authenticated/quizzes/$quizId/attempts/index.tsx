import { createFileRoute } from '@tanstack/react-router'
import { QuizAttemptsPage } from './-/QuizAttemptsPage'

export const Route = createFileRoute(
  '/app/_authenticated/quizzes/$quizId/attempts/',
)({
  component: RouteComponent,
})

/** Attempt history route for one quiz. */
function RouteComponent() {
  return <QuizAttemptsPage quizId={Route.useParams().quizId} />
}
