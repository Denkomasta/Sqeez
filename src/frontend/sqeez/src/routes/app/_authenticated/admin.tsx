import { adminRouteGuard } from '@/lib/routeGuards'
import { createFileRoute, Outlet } from '@tanstack/react-router'

/** Admin route shell guarded by the admin role. */
export const Route = createFileRoute('/app/_authenticated/admin')({
  beforeLoad: adminRouteGuard,
  component: () => <Outlet />,
})
