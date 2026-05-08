import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  allowedImageUploadAccept,
  createSafeLocalPreviewSrc,
  getImageUrl,
  getSafeImageSrc,
  isAllowedImageUploadFile,
  isSafeLocalPreviewSrc,
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

describe('image upload validation', () => {
  it('exposes the accepted image mime types for file pickers', () => {
    expect(allowedImageUploadAccept).toBe('image/jpeg, image/png, image/gif')
  })

  it('allows backend-supported image upload types', () => {
    expect(isAllowedImageUploadFile({ type: 'image/jpeg' })).toBe(true)
    expect(isAllowedImageUploadFile({ type: 'image/png' })).toBe(true)
    expect(isAllowedImageUploadFile({ type: 'image/gif' })).toBe(true)
  })

  it('rejects svg and unknown upload types', () => {
    expect(isAllowedImageUploadFile({ type: 'image/svg+xml' })).toBe(false)
    expect(isAllowedImageUploadFile({ type: 'text/html' })).toBe(false)
    expect(isAllowedImageUploadFile({ type: '' })).toBe(false)
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
    expect(isSafeLocalPreviewSrc(previewUrl)).toBe(true)
    expect(isSafeLocalPreviewSrc('blob:http://localhost/unknown-id')).toBe(
      false,
    )
    expect(createObjectURL).toHaveBeenCalledWith(file)

    revokeSafeLocalPreviewSrc(previewUrl)

    expect(isSafeLocalPreviewSrc(previewUrl)).toBe(false)
    expect(revokeObjectURL).toHaveBeenCalledWith(previewUrl)
  })
})
