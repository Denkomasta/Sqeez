export function getMediaAssetName(locationUrl: string) {
  const name = locationUrl.split('/').pop() || locationUrl

  try {
    return decodeURIComponent(name)
  } catch {
    return name
  }
}
