import { createFileRoute } from '@tanstack/react-router'
import { EnrollmentsView } from './-/EnrollmentsView'

/** Route entry for the signed-in student's subject enrollments. */
export const Route = createFileRoute('/app/_authenticated/subjects/')({
  component: EnrollmentsView,
})
