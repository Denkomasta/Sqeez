import { createFileRoute } from '@tanstack/react-router'
import { ProfileView } from './-/ProfileView'

export const Route = createFileRoute('/app/_authenticated/profile/')({
  component: MyProfileRoute,
})

/** Profile route for the signed-in user's own profile. */
function MyProfileRoute() {
  return <ProfileView />
}
