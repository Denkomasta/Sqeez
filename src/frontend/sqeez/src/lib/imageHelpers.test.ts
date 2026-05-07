import { describe, expect, it } from 'vitest'
import { getImageUrl, getSafeImageSrc } from './imageHelpers'

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
  it('allows app-relative and safe browser image URLs', () => {
    expect(getSafeImageSrc('/avatars/user.png')).toBe('/avatars/user.png')
    expect(getSafeImageSrc('https://example.com/image.png')).toBe(
      'https://example.com/image.png',
    )
    expect(getSafeImageSrc('blob:http://localhost/image-id')).toBe(
      'blob:http://localhost/image-id',
    )
  })

  it('rejects executable or inline payload URLs', () => {
    expect(getSafeImageSrc('javascript:alert(1)')).toBeUndefined()
    expect(getSafeImageSrc('data:image/svg+xml,<svg></svg>')).toBeUndefined()
  })
})
