import { createFileRoute } from '@tanstack/react-router'
import { ClassView } from './-/ClassView'

export const Route = createFileRoute('/app/_authenticated/class/')({
  component: MyClassRoute,
})

/** Class route for the signed-in user's own class. */
function MyClassRoute() {
  return <ClassView />
}
