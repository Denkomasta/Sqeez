import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  createSafeLocalPreviewSrc,
  getImageUrl,
  getSafeImageSrc,
  revokeSafeLocalPreviewSrc,
} from './imageHelpers'

afterEach(() => {
  vi.restoreAllMocks()
})

describe('getImageUrl', () => {
  it('returns undefined for empty values', () => {
    expect(getImageUrl()).toBeUndefined()
    expect(getImageUrl(null)).toBeUndefined()
  })

  it('leaves absolute URLs unchanged', () => {
    expect(getImageUrl('https://example.com/image.png')).toBe(
      'https://example.com/image.png',
    )
    expect(getImageUrl('http://example.com/image.png')).toBe(
      'http://example.com/image.png',
    )
  })

  it('resolves relative paths against the API base URL', () => {
    expect(getImageUrl('/avatars/user.png')).toContain('/avatars/user.png')
  })
})

describe('getSafeImageSrc', () => {
  it('allows app-relative and safe remote image URLs', () => {
    expect(getSafeImageSrc('/avatars/user.png')).toBe('/avatars/user.png')
    expect(getSafeImageSrc('https://example.com/image.png')).toBe(
      'https://example.com/image.png',
    )
  })

  it('rejects executable or inline payload URLs', () => {
    expect(getSafeImageSrc('javascript:alert(1)')).toBeUndefined()
    expect(getSafeImageSrc('data:image/svg+xml,<svg></svg>')).toBeUndefined()
    expect(getSafeImageSrc('blob:http://localhost/image-id')).toBeUndefined()
    expect(getSafeImageSrc('//example.com/image.png')).toBeUndefined()
    expect(
      getSafeImageSrc('https://example.com/image name.png'),
    ).toBeUndefined()
  })
})

describe('local preview helpers', () => {
  it('creates and revokes branded object URLs', () => {
    const createObjectURL = vi.fn(() => 'blob:http://localhost/image-id')
    const revokeObjectURL = vi.fn()

    Object.defineProperty(URL, 'createObjectURL', {
      configurable: true,
      value: createObjectURL,
    })
    Object.defineProperty(URL, 'revokeObjectURL', {
      configurable: true,
      value: revokeObjectURL,
    })

    const file = new File(['image'], 'image.png', { type: 'image/png' })
    const previewUrl = createSafeLocalPreviewSrc(file)

    expect(previewUrl).toBe('blob:http://localhost/image-id')
    expect(createObjectURL).toHaveBeenCalledWith(file)

    revokeSafeLocalPreviewSrc(previewUrl)

    expect(revokeObjectURL).toHaveBeenCalledWith(previewUrl)
  })
})
