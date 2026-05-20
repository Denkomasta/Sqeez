import { SubjectDetailsPage } from '@/routes/app/_authenticated/subjects/$subjectId/students/-/SubjectDetailsPage'
import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute(
  '/app/_authenticated/teacher/subjects/$subjectId/students/',
)({
  component: RouteComponent,
})

/** Teacher route for managing enrolled students in one subject. */
function RouteComponent() {
  const { subjectId } = Route.useParams()

  return <SubjectDetailsPage subjectId={Number(subjectId)} />
}
