import { useState, useEffect } from 'react'
import { Trophy, X } from 'lucide-react'
import type { StudentBadgeBasicDto } from '@/api/generated/model'
import { getImageUrl } from '@/lib/imageHelpers'

interface BadgeUnlockNotificationProps {
  badges?: StudentBadgeBasicDto[]
  achievementText?: string
}

export function BadgeUnlockNotification({
  badges,
  achievementText,
}: BadgeUnlockNotificationProps) {
  const [visibleBadges, setVisibleBadges] = useState<StudentBadgeBasicDto[]>([])

  useEffect(() => {
    if (badges && badges.length > 0) {
      const timer = setTimeout(() => {
        setVisibleBadges(badges)
      }, 500)
      return () => clearTimeout(timer)
    }
  }, [badges])

  const dismissBadge = (badgeId?: number | string) => {
    if (badgeId === undefined) return

    setVisibleBadges((prev) => prev.filter((b) => b.badgeId !== badgeId))
  }

  if (visibleBadges.length === 0) return null

  return (
    <div className="pointer-events-none fixed right-6 bottom-6 z-50 flex flex-col gap-3">
      {visibleBadges.map((badge, index) => (
        <div
          key={badge.badgeId}
          style={{ animationDelay: `${index * 200}ms` }}
          className="pointer-events-auto relative flex min-w-70 animate-in items-center gap-4 rounded-xl border border-warning/40 bg-background/95 p-4 shadow-xl shadow-warning/10 backdrop-blur-md duration-500 fill-mode-both zoom-in-95 fade-in slide-in-from-bottom-12"
        >
          <button
            onClick={() => dismissBadge(badge.badgeId)}
            className="absolute -top-2 -right-2 rounded-full border border-border bg-background p-1 text-muted-foreground shadow-sm transition-colors hover:text-foreground"
          >
            <X className="h-3 w-3" />
          </button>

          <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-warning/10 text-warning ring-1 ring-warning/30">
            {badge.iconUrl ? (
              <img
                src={getImageUrl(badge.iconUrl)}
                alt={badge.name}
                className="h-8 w-8 object-contain drop-shadow-md"
              />
            ) : (
              <Trophy className="h-6 w-6" />
            )}
          </div>

          <div className="flex flex-col">
            <span className="text-[10px] font-bold tracking-wider text-warning uppercase">
              {achievementText}
            </span>
            <span className="mt-0.5 text-sm leading-tight font-bold text-foreground">
              {badge.name}
            </span>
          </div>
        </div>
      ))}
    </div>
  )
}
