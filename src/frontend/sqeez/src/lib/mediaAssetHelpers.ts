/**
 * Derives a human-readable file name from a media asset URL.
 * If URL decoding fails, the original trailing segment is preserved.
 *
 * @param locationUrl - Media asset location or file URL from the API.
 * @returns Decoded trailing file name, or the original trailing segment on malformed encoding.
 */
export function getMediaAssetName(locationUrl: string) {
  const name = locationUrl.split('/').pop() || locationUrl

  try {
    return decodeURIComponent(name)
  } catch {
    return name
  }
}
