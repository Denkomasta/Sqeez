import { Shield, Lock, Edit2, Trash2 } from 'lucide-react'
import { getImageUrl } from '@/lib/imageHelpers'

interface StudentBadgeProps {
  name?: string
  iconUrl?: string | null
  earnedAt?: string
  isEarned?: boolean
  lockedText?: string
  isAdmin?: boolean
  onEdit?: () => void
  onDelete?: () => void
}

/**
 * Badge tile used by student profiles and admin badge management.
 * Admin controls are optional and do not render unless `isAdmin` is true.
 */
export function StudentBadge({
  name,
  iconUrl,
  earnedAt,
  isEarned = true,
  lockedText,
  isAdmin,
  onEdit,
  onDelete,
}: StudentBadgeProps) {
  return (
    <div
      className={`relative flex flex-col items-center gap-3 rounded-lg border bg-card p-4 text-center transition-all ${
        isEarned
          ? 'shadow-sm hover:bg-muted/50'
          : 'opacity-50 grayscale hover:opacity-70'
      }`}
    >
      {isAdmin && (
        <div className="absolute top-2 right-2 z-10 flex gap-1 rounded-md bg-background/80 p-0.5 shadow-sm backdrop-blur-sm">
          <button
            type="button"
            onClick={(e) => {
              e.preventDefault()
              e.stopPropagation()
              onEdit?.()
            }}
            className="rounded-sm p-1.5 text-muted-foreground transition-colors hover:bg-primary/10 hover:text-primary focus:ring-2 focus:ring-primary focus:outline-none"
            title="Edit Badge"
          >
            <Edit2 className="h-3.5 w-3.5" />
          </button>
          <button
            type="button"
            onClick={(e) => {
              e.preventDefault()
              e.stopPropagation()
              onDelete?.()
            }}
            className="rounded-sm p-1.5 text-muted-foreground transition-colors hover:bg-destructive/10 hover:text-destructive focus:ring-2 focus:ring-destructive focus:outline-none"
            title="Delete Badge"
          >
            <Trash2 className="h-3.5 w-3.5" />
          </button>
        </div>
      )}

      <div className="relative flex h-16 w-16 shrink-0 items-center justify-center rounded-full bg-primary/10 p-2">
        {iconUrl ? (
          <img
            src={getImageUrl(iconUrl)}
            alt={name}
            className="h-full w-full object-contain"
          />
        ) : (
          <Shield className="h-8 w-8 text-primary" />
        )}

        {!isEarned && (
          <div className="absolute -right-1 -bottom-1 flex h-6 w-6 items-center justify-center rounded-full border bg-background shadow-sm">
            <Lock className="h-3 w-3 text-muted-foreground" />
          </div>
        )}
      </div>

      <div className="flex flex-col gap-1">
        <span className="line-clamp-2 text-sm leading-tight font-semibold text-foreground">
          {name}
        </span>

        {isEarned ? (
          earnedAt && (
            <span className="text-xs text-muted-foreground">
              {new Date(earnedAt).toLocaleDateString()}
            </span>
          )
        ) : (
          <span className="text-xs font-medium text-muted-foreground">
            {lockedText ?? 'Locked'}
          </span>
        )}
      </div>
    </div>
  )
}
