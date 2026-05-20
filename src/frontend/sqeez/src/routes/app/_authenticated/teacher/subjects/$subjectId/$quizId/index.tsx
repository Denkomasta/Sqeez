import { createFileRoute } from '@tanstack/react-router'
import { QuizStatisticsPage } from './-/QuizStatisticsPage'

export const Route = createFileRoute(
  '/app/_authenticated/teacher/subjects/$subjectId/$quizId/',
)({
  component: RouteComponent,
})

/** Teacher route for quiz statistics inside a selected subject. */
function RouteComponent() {
  const { subjectId, quizId } = Route.useParams()

  return <QuizStatisticsPage subjectId={subjectId} quizId={quizId} />
}
