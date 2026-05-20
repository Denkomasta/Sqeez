import { createFileRoute } from '@tanstack/react-router'
import { LeaderboardPage } from './-/LeaderboardPage'

/** Route entry for leaderboards. */
export const Route = createFileRoute('/app/_authenticated/leaderboards/')({
  component: LeaderboardPage,
})
