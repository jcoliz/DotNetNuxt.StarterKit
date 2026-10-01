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
 * Composable to create pre-configured API client instances
 *
 * This wraps the generated NSwag API clients with the correct base URL and
 * auth-aware fetch, so components don't need to configure these manually.
 *
 * @param ClientClass The API client class to instantiate (e.g., ItemsClient, ViewsClient)
 * @param config Optional configuration overrides
 * @returns A fully configured API client instance
 *
 * @example
 * // Basic usage
 * const client = useApiClient(ItemsClient)
 *
 * @example
 * // With custom configuration
 * const client = useApiClient(ListsClient, { clearProblemDetails: false })
 */
export function useApiClient<T extends AuthorizedApiBase>(
  ClientClass: ApiClientConstructor<T>,
  config?: Partial<IClientConfiguration>,
): T {
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
