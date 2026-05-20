import { createFileRoute } from '@tanstack/react-router'
import { AdminSchoolClassPage } from './-/AdminSchoolClassPage'

/** Route entry for admin school-class management. */
export const Route = createFileRoute('/app/_authenticated/admin/classes/')({
  component: AdminSchoolClassPage,
})
