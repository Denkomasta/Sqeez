const I18NEXT_LANGUAGE_STORAGE_KEY = 'i18nextLng'

export function getStoredLanguage(): string | undefined {
  if (typeof window === 'undefined') return undefined

  const language = window.localStorage
    .getItem(I18NEXT_LANGUAGE_STORAGE_KEY)
    ?.trim()

  return language || undefined
}
