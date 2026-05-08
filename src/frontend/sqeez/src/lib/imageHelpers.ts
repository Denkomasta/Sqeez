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

export const allowedImageUploadMimeTypes = [
  'image/jpeg',
  'image/png',
  'image/gif',
] as const

export const allowedImageUploadAccept = allowedImageUploadMimeTypes.join(', ')

const allowedImageUploadMimeTypeSet = new Set<string>(
  allowedImageUploadMimeTypes,
)

export function isAllowedImageUploadFile(file: Pick<File, 'type'>): boolean {
  return allowedImageUploadMimeTypeSet.has(file.type)
}

function hasUnsafeUrlCharacter(value: string): boolean {
  return [...value].some((char) => {
    const code = char.charCodeAt(0)
    return code <= 32 || code === 127
  })
}

export function getSafeImageSrc(src?: string | null): string | undefined {
  if (!src) return undefined

  const trimmedSrc = src.trim()
  if (!trimmedSrc) return undefined

  if (hasUnsafeUrlCharacter(trimmedSrc)) return undefined

  if (
    (trimmedSrc.startsWith('/') && !trimmedSrc.startsWith('//')) ||
    trimmedSrc.startsWith('./')
  ) {
    return trimmedSrc
  }

  const lowerSrc = trimmedSrc.toLowerCase()
  if (lowerSrc.startsWith('http://') || lowerSrc.startsWith('https://')) {
    return trimmedSrc
  }

  return undefined
}

declare const safeLocalPreviewSrcBrand: unique symbol

export type SafeLocalPreviewSrc = string & {
  readonly [safeLocalPreviewSrcBrand]: true
}

export function createSafeLocalPreviewSrc(file: File): SafeLocalPreviewSrc {
  return URL.createObjectURL(file) as SafeLocalPreviewSrc
}

export function revokeSafeLocalPreviewSrc(
  src?: SafeLocalPreviewSrc | null,
): void {
  if (src) URL.revokeObjectURL(src)
}
