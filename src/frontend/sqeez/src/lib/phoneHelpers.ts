/**
 * Formats the database phone shape for display.
 * Backend values may start with `00`; the UI presents that prefix as `+`.
 */
export const formatPhoneForDisplay = (phone?: string | null) => {
  if (!phone) return ''

  const withPlus = phone.replace(/^00/, '+')

  return withPlus.replace(/^(\+\d{3})(\d+)/, '($1) $2')
}

/**
 * Normalizes user-entered phone numbers for backend storage.
 * Spaces, hyphens, and parentheses are removed and a leading `+` becomes `00`.
 */
export const formatPhoneForDb = (phone: string) => {
  let cleaned = phone.replace(/[\s\-()]/g, '')
  cleaned = cleaned.replace(/^\+/, '00')
  return cleaned
}
