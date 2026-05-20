/**
 * Converts total XP to a display level.
 * Invalid, missing, or negative XP is treated as the first level.
 *
 * @param xp - Total XP from the API; strings are parsed as numbers.
 */
export const calculateLevel = (xp?: number | string) => {
  if (!xp) return 1

  const parsedXp = Number(xp)

  if (Number.isNaN(parsedXp) || parsedXp < 0) return 1

  const MULTIPLIER = 0.05
  const EXPONENT = 0.85

  return Math.floor(MULTIPLIER * Math.pow(parsedXp, EXPONENT)) + 1
}

/**
 * Joins optional first and last names, returning undefined when both are missing.
 *
 * @param firstName - User first name, when available.
 * @param lastName - User last name, when available.
 */
export const formatName = (
  firstName: string | undefined,
  lastName: string | undefined,
) => {
  const name = [firstName, lastName].filter(Boolean).join(' ')

  return name || undefined
}

/**
 * Builds avatar initials with stable fallbacks for incomplete user data.
 *
 * @param firstName - User first name, when available.
 * @param lastName - User last name, when available.
 */
export const getNameInitials = (
  firstName: string | undefined,
  lastName: string | undefined,
) => {
  return `${firstName?.substring(0, 1) ?? 'J'}${lastName?.substring(0, 1) ?? 'D'}`
}
