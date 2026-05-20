import { createFileRoute } from '@tanstack/react-router'
import { SubjectOverviewPage } from '../-/SubjectOverviewPage'

export const Route = createFileRoute(
  '/app/_authenticated/teacher/subjects/$subjectId/',
)({
  component: OtherClassRoute,
})

/** Teacher route for managing one subject by URL id. */
function OtherClassRoute() {
  const { subjectId } = Route.useParams()

  return <SubjectOverviewPage subjectId={Number(subjectId)} />
}
