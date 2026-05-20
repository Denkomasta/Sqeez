/**
 * Builds an absolute API image URL when the backend returns a relative path.
 * Already absolute HTTP(S) URLs are returned unchanged.
 *
 * @param path - Relative API path or absolute HTTP(S) URL returned by the backend.
 * @returns Absolute URL for rendering, or undefined when no path is available.
 */
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

/**
 * Checks the MIME types accepted by backend image upload endpoints.
 *
 * @param file - File-like object; only the MIME `type` is inspected.
 */
export function isAllowedImageUploadFile(file: Pick<File, 'type'>): boolean {
  return allowedImageUploadMimeTypeSet.has(file.type)
}

function hasUnsafeUrlCharacter(value: string): boolean {
  return [...value].some((char) => {
    const code = char.charCodeAt(0)
    return code <= 32 || code === 127
  })
}

/**
 * Allows only image URLs that are safe to place into a DOM `src` attribute.
 * Rejects control characters, protocol-relative URLs, and non-HTTP schemes.
 *
 * @param src - Candidate image URL from user-controlled or backend data.
 * @returns Safe URL string for DOM rendering, or undefined when rejected.
 */
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

const safeLocalPreviewSrcRegistry = new Set<SafeLocalPreviewSrc>()

/**
 * Checks that a blob preview URL was created by this module.
 * This keeps local previews separate from arbitrary user-provided strings.
 *
 * @param src - Candidate local preview URL.
 */
export function isSafeLocalPreviewSrc(
  src?: string | null,
): src is SafeLocalPreviewSrc {
  return safeLocalPreviewSrcRegistry.has(src as SafeLocalPreviewSrc)
}

/**
 * Creates and brands a local blob URL that can be rendered as a trusted preview.
 *
 * @param file - Local file selected by the user.
 */
export function createSafeLocalPreviewSrc(file: File): SafeLocalPreviewSrc {
  const src = URL.createObjectURL(file) as SafeLocalPreviewSrc
  safeLocalPreviewSrcRegistry.add(src)
  return src
}

/**
 * Revokes a branded local preview URL and removes it from the trusted registry.
 *
 * @param src - Preview URL previously returned by `createSafeLocalPreviewSrc`.
 */
export function revokeSafeLocalPreviewSrc(
  src?: SafeLocalPreviewSrc | null,
): void {
  if (!src) return

  safeLocalPreviewSrcRegistry.delete(src)
  URL.revokeObjectURL(src)
}
