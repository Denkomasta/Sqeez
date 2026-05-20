import { createFileRoute } from '@tanstack/react-router'
import { ProfileView } from './-/ProfileView'

export const Route = createFileRoute('/app/_authenticated/profile/$userId')({
  component: OtherUserProfileRoute,
})

/** Profile route for viewing another user by URL id. */
function OtherUserProfileRoute() {
  const { userId } = Route.useParams()

  return <ProfileView targetUserId={Number(userId)} />
}
