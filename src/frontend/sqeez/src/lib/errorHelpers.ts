import { isAxiosError } from 'axios'
import type { AspNetProblemDetails } from '@/api/custom-axios'

export function getAspNetProblemDetailsErrorMessage(
  problemDetails?: AspNetProblemDetails | null,
): string | null {
  if (!problemDetails) return null

  const validationMessage = problemDetails.errors
    ? Object.values(problemDetails.errors).flat().join(' ')
    : null

  return (
    problemDetails.error ||
    problemDetails.detail ||
    problemDetails.title ||
    validationMessage ||
    null
  )
}

export function getErrorMessage(error: unknown): string | null {
  if (!isAxiosError<AspNetProblemDetails>(error)) return null

  return getAspNetProblemDetailsErrorMessage(error.response?.data)
}
