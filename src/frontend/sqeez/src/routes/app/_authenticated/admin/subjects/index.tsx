import { createFileRoute } from '@tanstack/react-router'
import { AdminSubjectsPage } from './-/AdminSubjectPage'

/** Route entry for admin subject management. */
export const Route = createFileRoute('/app/_authenticated/admin/subjects/')({
  component: AdminSubjectsPage,
})
