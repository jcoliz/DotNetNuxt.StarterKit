import type { AuthorizedApiBase, IClientConfiguration } from '~/utils/AuthorizedApiBase'

/**
 * Type for API client constructor
 *
 * All NSwag-generated clients extend AuthorizedApiBase and have the same constructor signature
 */
type ApiClientConstructor<T extends AuthorizedApiBase> = new (
  configuration: IClientConfiguration,
  baseUrl?: string,
  http?: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> },
) => T

/**
 * A client whose calls resolve to `undefined` when they fail, because errors
 * are reported to problem details and then swallowed (see `throwOnError`).
 */
export type SwallowingClient<C> = {
  [K in keyof C]: C[K] extends (...args: infer A) => Promise<infer R>
    ? (...args: A) => Promise<R | undefined>
    : C[K]
}

/**
 * Composable to create pre-configured API client instances
 *
 * This wraps the generated NSwag API clients with the correct base URL and
 * auth-aware fetch, so components don't need to configure these manually.
 *
 * By default, failed calls are reported to problem details and resolve to
 * `undefined`, which the return type reflects. Pass `throwOnError: true` for
 * calls where the caller must know whether it succeeded (writes); the error is
 * still reported, then rethrown.
 *
 * @param ClientClass The API client class to instantiate (e.g., ItemsClient, ViewsClient)
 * @param config Optional configuration overrides
 * @returns A fully configured API client instance
 *
 * @example
 * // Reads: results may be undefined
 * const client = useApiClient(ItemsClient)
 *
 * @example
 * // Writes: rethrows after reporting
 * const client = useApiClient(ItemsClient, { throwOnError: true })
 *
 * @example
 * // With custom configuration
 * const client = useApiClient(ListsClient, { clearProblemDetails: false })
 */
export function useApiClient<T extends AuthorizedApiBase>(
  ClientClass: ApiClientConstructor<T>,
  config: Partial<IClientConfiguration> & { throwOnError: true },
): T
export function useApiClient<T extends AuthorizedApiBase>(
  ClientClass: ApiClientConstructor<T>,
  config?: Partial<IClientConfiguration>,
): SwallowingClient<T>
export function useApiClient<T extends AuthorizedApiBase>(
  ClientClass: ApiClientConstructor<T>,
  config?: Partial<IClientConfiguration>,
): T | SwallowingClient<T> {
  const runtimeConfig = useRuntimeConfig()
  //TODO: const authFetch = useAuthFetch(config?.useTokenCheck !== false)

  const defaultConfig: IClientConfiguration = {
    useTokenCheck: true,
    useProblemDetails: true,
  }

  return new ClientClass(
    { ...defaultConfig, ...config },
    runtimeConfig.public.apiBaseUrl,
    //authFetch,
  )
}
