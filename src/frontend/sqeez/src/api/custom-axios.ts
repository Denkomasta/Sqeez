import axios, {
  type AxiosError,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from 'axios'
import { useAuthStore } from '@/store/useAuthStore'
import { toast } from 'sonner'
import i18n from '@/i18n'

interface RetryQueueItem {
  resolve: (value: unknown) => void
  reject: (error: unknown) => void
}

interface CustomAxiosRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean
}

/** Problem Details shape returned by ASP.NET endpoints, including validation errors. */
export interface AspNetProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  traceId?: string
  errors?: Record<string, string[]>
  error?: string
}

const baseURL =
  typeof import.meta !== 'undefined'
    ? import.meta.env.VITE_API_BASE_URL
    : 'http://localhost:5000'

/** Shared API client used by generated Orval hooks. */
export const AXIOS_INSTANCE = axios.create({
  baseURL,
  withCredentials: true,
})

let isRefreshing = false
let failedQueue: RetryQueueItem[] = []

const processQueue = (error: Error | AxiosError | null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error)
    } else {
      prom.resolve(null)
    }
  })
  failedQueue = []
}

const AUTH_ENDPOINTS = [
  '/api/auth/login',
  '/api/auth/register',
  '/api/auth/refresh',
  '/api/auth/verify-email',
]

AXIOS_INSTANCE.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<AspNetProblemDetails>) => {
    if (axios.isCancel(error)) {
      return Promise.reject(error)
    }

    const originalRequest = error.config as CustomAxiosRequestConfig

    if (!error.response || !originalRequest) {
      if (error.request) {
        toast.error(i18n.t('errors.networkTitle'), {
          description: i18n.t('errors.networkDescription'),
        })
      }
      return Promise.reject(error)
    }

    const isUnauthorized = error.response.status === 401

    const isAuthRequest = AUTH_ENDPOINTS.some((url) =>
      originalRequest.url?.includes(url),
    )

    // Logic: Only attempt refresh if it's a 401 on a NON-auth endpoint
    if (isUnauthorized && !originalRequest._retry && !isAuthRequest) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject })
        })
          .then(() => AXIOS_INSTANCE(originalRequest))
          .catch((err) => Promise.reject(err))
      }

      originalRequest._retry = true
      isRefreshing = true

      try {
        await AXIOS_INSTANCE.post('/api/auth/refresh')
        processQueue(null)
        return AXIOS_INSTANCE(originalRequest)
      } catch (refreshError) {
        processQueue(refreshError as AxiosError)

        // Use the store's state directly for a clean logout
        useAuthStore.getState().setUser(null)

        // toast.error(i18n.t('errors.sessionExpiredTitle'), {
        //   description: i18n.t('errors.sessionExpiredDescription'),
        // })

        return Promise.reject(refreshError)
      } finally {
        isRefreshing = false
      }
    }

    if (error.response) {
      const status = error.response.status
      const data = error.response.data

      let errorMessage = data?.detail || data?.title

      // If there are specific DTO validation errors, flatten them into a single string
      if (data?.errors) {
        const validationMessages = Object.values(data.errors).flat().join(' ')

        if (validationMessages) {
          errorMessage = validationMessages
        }
      }

      switch (status) {
        case 500:
          toast.error(i18n.t('errors.serverErrorTitle'), {
            description:
              i18n.t('errors.serverErrorDescription') || errorMessage,
          })
          break
        default:
          break
        // toast.error(i18n.t('errors.defaultErrorTitle'), {
        //   description:
        //     i18n.t('errors.defaultErrorDescription') || errorMessage,
        // })
      }
    }

    return Promise.reject(error)
  },
)

/**
 * Orval-compatible request wrapper around the shared Axios instance.
 * It attaches a cancel method so TanStack Query can cancel in-flight requests.
 *
 * @param config - Generated endpoint request configuration.
 * @param options - Optional per-call Axios overrides.
 */
export const customInstance = <T>(
  config: AxiosRequestConfig,
  options?: AxiosRequestConfig,
): Promise<T> => {
  const source = axios.CancelToken.source()
  const promise = AXIOS_INSTANCE({
    ...config,
    ...options,
    cancelToken: source.token,
  }).then(({ data }) => data)

  // @ts-expect-error - We are adding a custom cancel method to the promise
  promise.cancel = () => {
    source.cancel('Query was cancelled')
  }

  return promise
}

export type ErrorType<Error> = AxiosError<Error>
