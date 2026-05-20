import { createFileRoute } from '@tanstack/react-router'
import { AdminUsersPage } from './-/AdminUsersPage'

/** Route entry for admin user management. */
export const Route = createFileRoute('/app/_authenticated/admin/users/')({
  component: AdminUsersPage,
})
