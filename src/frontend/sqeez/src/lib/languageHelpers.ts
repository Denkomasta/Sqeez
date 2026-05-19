const I18NEXT_LANGUAGE_STORAGE_KEY = 'i18nextLng'

/**
 * Reads the persisted i18next language for API calls that send localized emails.
 * Returns undefined during SSR-like execution or when the browser has no setting.
 */
export function getStoredLanguage(): string | undefined {
  if (typeof window === 'undefined') return undefined

  const language = window.localStorage
    .getItem(I18NEXT_LANGUAGE_STORAGE_KEY)
    ?.trim()

  return language || undefined
}
