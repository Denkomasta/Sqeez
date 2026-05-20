import { createFileRoute } from '@tanstack/react-router'
import { AdminBadgesPage } from './-/AdminBadgesPage'

/** Route entry for admin badge management. */
export const Route = createFileRoute('/app/_authenticated/admin/badges/')({
  component: AdminBadgesPage,
})
