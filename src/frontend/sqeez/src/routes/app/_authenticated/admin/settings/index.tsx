import { createFileRoute } from '@tanstack/react-router'
import { SystemSettingsPage } from './-/SystemSettingsPage'

/** Route entry for admin system settings. */
export const Route = createFileRoute('/app/_authenticated/admin/settings/')({
  component: SystemSettingsPage,
})
