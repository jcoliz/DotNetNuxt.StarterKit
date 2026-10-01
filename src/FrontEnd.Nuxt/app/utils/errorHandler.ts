/**
 * API Error Handler Utility
 *
 * Provides a DRY helper function to consistently handle errors from API client calls.
 * Converts all error types (ApiException, ProblemDetails, or unexpected errors) into
 * IProblemDetails format for display with the ErrorDisplay component.
 */

import { FetchError } from 'ofetch'
import { ApiException, ProblemDetails, type IProblemDetails } from './apiclient'

/**
 * Converts any error from an API client call into IProblemDetails format
 *
 * @param err - The error caught from an API call
 * @param fallbackTitle - Optional custom title to use if API error has no title, or for unexpected errors
 * @param fallbackDetail - Optional custom detail message to prepend to API error details for better context
 * @returns IProblemDetails object ready for ErrorDisplay component
 *
 * @example Basic usage (uses API error details if available)
 * ```typescript
 * try {
 *   await apiClient.someMethod()
 * } catch (err) {
 *   error.value = handleApiErrorImpl(err)
 *   showError.value = true
 * }
 * ```
 *
 * @example With context-aware fallback messages (combines context with API error details)
 * ```typescript
 * try {
 *   await apiClient.deleteItem(id)
 * } catch (err) {
 *   // If API returns detail "Item does not exist", user sees:
 *   // "Could not delete the item. Item does not exist"
 *   error.value = handleApiErrorImpl(err, 'Delete Failed', 'Could not delete the item')
 *   showError.value = true
 * }
 * ```
 */
export function handleApiErrorImpl(
  err: unknown,
  fallbackTitle: string = 'Unexpected Error',
  fallbackDetail?: string,
): IProblemDetails {
  // Handle ApiException (most common case from auto-generated client)
  if (ApiException.isApiException(err)) {
    // The response body may contain a stringified ProblemDetails JSON
    const problemDetails = tryParseProblemDetails(err.response)
    if (problemDetails) {
      return {
        ...problemDetails,
        title: problemDetails.title || err.message || fallbackTitle,
        detail: combineDetails(
          fallbackDetail,
          problemDetails.detail,
          problemDetails.status ?? err.status,
        ),
      }
    }
    return {
      title: err.message || fallbackTitle,
      detail: combineDetails(fallbackDetail, undefined, err.status),
    }
  }

  // Handle direct ProblemDetails instance
  if (err instanceof ProblemDetails) {
    return {
      ...err,
      title: err.title || fallbackTitle,
      detail: combineDetails(fallbackDetail, err.detail, err.status),
    }
  }

  // Handle Fetch API errors (e.g., from ofetch / sidebase/nuxt-auth)
  // err.data contains the parsed JSON response body, which may be ProblemDetails
  if (err instanceof FetchError) {
    const data = err.data as IProblemDetails | undefined
    const status = data?.status ?? err.response?.status
    const title = data?.title || fallbackTitle
    const apiDetail = data?.detail
    return {
      ...data,
      title,
      detail: combineDetails(fallbackDetail, apiDetail, status),
    }
  }

  if (err instanceof Error) {
    return {
      title: err.name ?? fallbackTitle,
      detail: combineDetails(fallbackDetail, err.message, undefined),
    }
  }

  // Handle unexpected errors (network failures, etc.)
  return {
    title: fallbackTitle,
    detail:
      fallbackDetail ||
      (err instanceof Error
        ? err.message
        : 'An unexpected error occurred while performing the operation'),
  }
}

/**
 * Attempts to parse a raw response string as ProblemDetails JSON
 * @param response - The raw response body string from an ApiException
 * @returns Parsed IProblemDetails if valid, or undefined if not parseable
 */
function tryParseProblemDetails(response: string): IProblemDetails | undefined {
  try {
    const parsed = JSON.parse(response)
    // ProblemDetails always includes `type` and `title` fields per RFC 9457
    if (parsed && typeof parsed === 'object' && parsed.type && parsed.title) {
      return parsed as IProblemDetails
    }
  } catch {
    // Not valid JSON — ignore
  }
  return undefined
}

/**
 * Gets a friendly message based on HTTP status code
 * @param status - HTTP status code
 * @returns User-friendly message for the status code
 */
function getFriendlyMessageForStatus(status: number | undefined): string | undefined {
  if (!status) return undefined

  const friendlyMessages: Record<number, string> = {
    400: 'Please check the information you provided and try again.',
    401: 'You need to be logged in to access this resource.',
    403: 'You do not have permission to access this resource.',
    404: 'The requested resource could not be found.',
    409: 'This operation conflicts with the current state of the resource.',
    500: 'An internal server error occurred. Please try again later.',
    502: 'The server received an invalid response from an upstream server.',
    503: 'The service is temporarily unavailable. Please try again later.',
  }

  return friendlyMessages[status] || 'An error occurred while processing your request.'
}

/**
 * Combines fallback detail with API error detail and friendly status message
 * @param fallbackDetail - The context-specific fallback message
 * @param apiDetail - The detail from the API error
 * @param status - HTTP status code for friendly message
 * @returns Combined detail string with appropriate friendly messages
 */
function combineDetails(
  fallbackDetail: string | undefined,
  apiDetail: string | undefined,
  status: number | undefined,
): string {
  const friendlyMessage = getFriendlyMessageForStatus(status)

  // If we have an API detail, use it (it's the most specific)
  if (apiDetail) {
    // If we also have a fallback, prepend it
    return fallbackDetail ? `${fallbackDetail}. ${apiDetail}` : apiDetail
  }

  // No API detail - combine fallback and friendly message when both exist
  if (fallbackDetail && friendlyMessage) {
    return `${fallbackDetail}. ${friendlyMessage}`
  }

  // If we have a fallback, use it
  if (fallbackDetail) {
    return fallbackDetail
  }

  // If we have a friendly message based on status, use it
  if (friendlyMessage) {
    return friendlyMessage
  }

  // Last resort default
  return 'An error occurred while performing the operation'
}
