import { createFileRoute, Outlet } from '@tanstack/react-router'
import { teacherRouteGuard } from '@/lib/routeGuards'

/** Teacher route shell available to teachers and admins. */
export const Route = createFileRoute('/app/_authenticated/teacher')({
  beforeLoad: teacherRouteGuard,
  component: () => <Outlet />,
})
