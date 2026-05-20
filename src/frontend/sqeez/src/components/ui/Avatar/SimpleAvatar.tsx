import { getNameInitials } from '@/lib/userHelpers'
import { Avatar, AvatarFallback, AvatarImage } from './Avatar'
import { cn } from '@/lib/utils'

interface SimpleAvatarProps {
  url?: string | null
  username?: string
  firstName?: string
  lastName?: string
  wrapperClassName?: string
  imageClassName?: string
  fallbackClassName?: string
}

/**
 * Avatar with a consistent initials fallback.
 * Prefer passing first/last name when available; username is used only when no name data exists.
 *
 * @param props.url - Already sanitized image URL to render.
 * @param props.username - Username fallback used when no first name is available.
 * @param props.firstName - Preferred source for initials when name data exists.
 * @param props.lastName - Preferred source for initials when name data exists.
 */
export const SimpleAvatar = ({
  url,
  username,
  firstName,
  lastName,
  wrapperClassName,
  imageClassName,
  fallbackClassName,
}: SimpleAvatarProps) => {
  const initials = getNameInitials(firstName, lastName)

  return (
    <Avatar className={cn('border-2', wrapperClassName)}>
      {url ? (
        <AvatarImage
          src={url}
          alt={`${username ?? initials}'s avatar`}
          className={imageClassName}
        />
      ) : (
        <AvatarFallback className={fallbackClassName}>
          {firstName
            ? initials
            : (username?.substring(0, 2).toUpperCase() ?? 'JD')}
        </AvatarFallback>
      )}
    </Avatar>
  )
}
