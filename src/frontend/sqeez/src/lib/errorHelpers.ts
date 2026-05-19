import { isAxiosError } from 'axios'
import type { AspNetProblemDetails } from '@/api/custom-axios'

/**
 * Extracts the most useful message from ASP.NET ProblemDetails.
 * Validation errors are flattened so callers can show a single toast message.
 */
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

/**
 * Returns a backend error message only for Axios ProblemDetails responses.
 * Non-Axios errors are ignored so callers can choose their own generic fallback.
 */
export function getErrorMessage(error: unknown): string | null {
  if (!isAxiosError<AspNetProblemDetails>(error)) return null

  return getAspNetProblemDetailsErrorMessage(error.response?.data)
}
