import type { TFunction } from 'i18next'

const dateOnlyPattern = /^\d{4}-\d{2}-\d{2}$/
const timeZonePattern = /(Z|[+-]\d{2}:?\d{2})$/i

const parseDateInputValue = (dateString: string) => {
  const trimmedDateString = dateString.trim()

  if (dateOnlyPattern.test(trimmedDateString)) {
    return new Date(`${trimmedDateString}T00:00`)
  }

  return new Date(trimmedDateString)
}

/**
 * Converts a local date or datetime input value to a UTC ISO string for API writes.
 * Date-only values are treated as local midnight before conversion.
 *
 * @param dateString - Local date or datetime value from the UI.
 * @returns UTC ISO string, or null when the input is empty or invalid.
 */
export const toUtcIsoString = (dateString?: string | null): string | null => {
  if (!dateString) return null

  const date = parseDateInputValue(dateString)

  if (isNaN(date.getTime())) return null

  return date.toISOString()
}

/**
 * Parses API date values as UTC when they do not already include a timezone.
 * This prevents timezone-less .NET timestamps from being treated as browser-local time.
 *
 * @param dateString - API date value that may be timezone-less.
 * @returns Parsed Date, or null when the input is empty or invalid.
 */
export const parseUtcDate = (dateString?: string | null): Date | null => {
  if (!dateString) return null

  const trimmedDateString = dateString.trim()
  const safeDateString =
    dateOnlyPattern.test(trimmedDateString) ||
    timeZonePattern.test(trimmedDateString)
      ? trimmedDateString
      : `${trimmedDateString}Z`
  const date = new Date(safeDateString)

  if (isNaN(date.getTime())) return null

  return date
}

/**
 * Returns the UTC timestamp for an API date value, or NaN when it cannot be parsed.
 *
 * @param dateString - API date value that may be timezone-less.
 */
export const parseUtcTime = (dateString: string): number => {
  return parseUtcDate(dateString)?.getTime() ?? NaN
}

/**
 * Formats an API UTC date for an `<input type="datetime-local">` value.
 * The returned value intentionally has no timezone suffix because the input expects local time.
 *
 * @param dateString - API UTC date value.
 * @returns Local `YYYY-MM-DDTHH:mm` input value, or an empty string when invalid.
 */
export const toLocalDateTimeInputValue = (
  dateString?: string | null,
): string => {
  const date = parseUtcDate(dateString)

  if (!date) return ''

  return new Date(date.getTime() - date.getTimezoneOffset() * 60000)
    .toISOString()
    .slice(0, 16)
}

export const formatDate = (dateString?: string | null) => {
  const date = parseUtcDate(dateString)

  if (!date) return null

  return date.toLocaleDateString()
}

/**
 * Formats an ISO date string into a localized medium date and short time format.
 * Defaults to the user's system locale.
 *
 * @param dateString - API date value that may be timezone-less.
 * @returns The formatted string, or null if the input is empty or invalid.
 */
export const formatDateTime = (dateString?: string | null): string | null => {
  const date = parseUtcDate(dateString)

  if (!date) return null

  return date.toLocaleString([], {
    dateStyle: 'medium',
    timeStyle: 'short',
  })
}

/**
 * Formats an ISO date string into a localized medium date format (no time).
 *
 * @param dateString - API date value that may be timezone-less.
 * @returns Localized date, or null when the input is empty or invalid.
 */
export const formatDateOnly = (dateString?: string | null): string | null => {
  const date = parseUtcDate(dateString)

  if (!date) return null

  return date.toLocaleDateString([], {
    dateStyle: 'medium',
  })
}

/**
 * Formats the elapsed time between two timestamps for quiz attempt summaries.
 * Returns '-' when either timestamp is missing or the interval is negative.
 *
 * @param start - Attempt start timestamp.
 * @param end - Attempt completion timestamp.
 */
export const formatDuration = (start: string | null, end: string | null) => {
  if (!start || !end) return '-'

  const startTime = new Date(start).getTime()
  const endTime = new Date(end).getTime()

  const diffInSeconds = Math.floor((endTime - startTime) / 1000)

  if (diffInSeconds < 0) return '-'

  const minutes = Math.floor(diffInSeconds / 60)
  const seconds = diffInSeconds % 60

  return minutes > 0 ? `${minutes}m ${seconds}s` : `${seconds}s`
}

/**
 * Formats a duration in seconds into a digital stopwatch format (e.g., 65 -> "1:05").
 * Useful for live counters and timers.
 * @param totalSeconds - The total number of elapsed seconds.
 * @returns The formatted string.
 */
export const formatTimer = (totalSeconds: number): string => {
  if (totalSeconds < 0) return '0:00'

  const m = Math.floor(totalSeconds / 60)
  const s = totalSeconds % 60

  return `${m}:${s.toString().padStart(2, '0')}`
}

/**
 * Maps the latest activity timestamp to the profile presence label.
 * A user is considered online only for the first five minutes after `lastSeen`.
 *
 * @param lastSeen - Last activity timestamp from the API.
 * @param t - i18next translator used for online/offline labels.
 */
export const getLastSeenStatus = (
  lastSeen: string | undefined,
  t: TFunction,
) => {
  // If lastSeen is null/undefined or invalid, fallback to offline
  if (!lastSeen) return { isOnline: false, text: t('profile.offline') }

  const lastSeenDate = new Date(lastSeen)
  const now = new Date()

  const diffMs = now.getTime() - lastSeenDate.getTime()
  const diffMinutes = Math.floor(diffMs / (1000 * 60))

  // Less than 5 minutes = Online
  if (diffMinutes < 5) {
    return { isOnline: true, text: t('profile.online') }
  }

  // Format relative time for offline status
  if (diffMinutes < 60) {
    return {
      isOnline: false,
      text: t('profile.lastSeen.minutes', {
        count: diffMinutes,
      }),
    }
  }

  const diffHours = Math.floor(diffMinutes / 60)
  if (diffHours < 24) {
    return {
      isOnline: false,
      text: t('profile.lastSeen.hours', {
        count: diffHours,
      }),
    }
  }

  const diffDays = Math.floor(diffHours / 24)
  return {
    isOnline: false,
    text: t('profile.lastSeen.days', {
      count: diffDays,
    }),
  }
}
