export function getImageUrl(path?: string | null): string | undefined {
  if (!path) return undefined

  if (path.startsWith('http://') || path.startsWith('https://')) {
    return path
  }

  const baseUrl = import.meta.env.VITE_API_BASE_URL || ''

  const cleanBaseUrl = baseUrl.replace(/\/$/, '')
  const cleanPath = path.replace(/^\//, '')

  return `${cleanBaseUrl}/${cleanPath}`
}

export function getSafeImageSrc(src?: string | null): string | undefined {
  if (!src) return undefined

  const trimmedSrc = src.trim()
  if (!trimmedSrc) return undefined

  if (trimmedSrc.startsWith('/') || trimmedSrc.startsWith('./')) {
    return trimmedSrc
  }

  try {
    const parsedUrl = new URL(trimmedSrc, window.location.origin)

    if (
      parsedUrl.protocol === 'blob:' ||
      parsedUrl.protocol === 'http:' ||
      parsedUrl.protocol === 'https:'
    ) {
      return trimmedSrc
    }
  } catch {
    return undefined
  }

  return undefined
}
