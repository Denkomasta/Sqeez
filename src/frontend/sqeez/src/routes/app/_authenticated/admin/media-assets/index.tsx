import { createFileRoute } from '@tanstack/react-router'

import { AdminMediaAssetsPage } from './-/AdminMediaAssetsPage'

/** Route entry for admin media asset management. */
export const Route = createFileRoute('/app/_authenticated/admin/media-assets/')(
  {
    component: AdminMediaAssetsPage,
  },
)
