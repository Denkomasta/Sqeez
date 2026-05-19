/**
 * Derives a human-readable file name from a media asset URL.
 * If URL decoding fails, the original trailing segment is preserved.
 */
export function getMediaAssetName(locationUrl: string) {
  const name = locationUrl.split('/').pop() || locationUrl

  try {
    return decodeURIComponent(name)
  } catch {
    return name
  }
}
