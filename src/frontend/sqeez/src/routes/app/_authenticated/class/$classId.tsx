import { createFileRoute } from '@tanstack/react-router'
import { ClassView } from './-/ClassView'

export const Route = createFileRoute('/app/_authenticated/class/$classId')({
  component: OtherClassRoute,
})

/** Class route for viewing a class selected by URL id. */
function OtherClassRoute() {
  const { classId } = Route.useParams()

  return <ClassView targetClassId={Number(classId)} />
}
