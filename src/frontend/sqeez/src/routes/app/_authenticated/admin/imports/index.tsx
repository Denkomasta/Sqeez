import { createFileRoute } from '@tanstack/react-router'
import { AdminImportPage } from './-/AdminImportPage'

/** Route entry for admin CSV import. */
export const Route = createFileRoute('/app/_authenticated/admin/imports/')({
  component: RouteComponent,
})

function RouteComponent() {
  return <AdminImportPage />
}
