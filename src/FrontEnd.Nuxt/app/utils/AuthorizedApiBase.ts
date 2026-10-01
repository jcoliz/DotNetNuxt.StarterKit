/**
 * Configuration details for the specific client instance
 */
export interface IClientConfiguration {
  /**
   * True if the client should check and refresh auth token before every call
   */
  useTokenCheck?: boolean
  /**
   * True if the client should report any errors to useProblemDetails
   */
  useProblemDetails?: boolean
  /**
   * True or undefined if the client should clear the problem details,
   * false if we should leave it alone.
   *
   * Most of the time, if we are using problem details, we do want to clear
   * the previous problems first. However, there are cases where we do
   * two API calls quickly, and wouldn't want to lose problems shown from
   * the first call, so we have this option to NOT clear the old problem
   * first.
   */
  clearProblemDetails?: boolean
  /**
   * True to rethrow errors to the caller after they are reported.
   * False or undefined to swallow them: the call then resolves to `undefined`
   * (despite the generated return type), so callers that use the result
   * should set this to true.
   */
  throwOnError?: boolean

  /**
   * Force this problem to be returned in the result. (Optional, obviously!)
   * This is for error testing
   */
  // Causes circular reference
  //problem?: ProblemDetails
}

/**
 * Base class for all API client classes
 *
 * The initial purpose of this class is to inject the auth token into each
 * request, if exists. We have been extending it ever since!
 *
 * Now it...
 *  * Injects auth token if exists
 *  * Updates the refresh token, if caller has opted in
 *  * Directly posts any reported error to global problem details, if caller has opted in
 *  * Rethrows errors to the caller only if `throwOnError` is set
 *
 * @see https://github.com/RicoSuter/NSwag/wiki/TypeScriptClientGenerator#inject-an-authorization-header
 */
export class AuthorizedApiBase {
  /**
   * Configuration details for this client instance
   */
  private configuration: IClientConfiguration

  /** Constructor
   *
   * Called by derived class
   *
   * @param configuration Configuration details for this client instance
   */
  constructor(configuration: IClientConfiguration) {
    this.configuration = configuration
  }

  /**
   * Transform options on upcoming request from derived class
   *
   * Derived API clients call this just before making a request. It's our chance to
   * hook into the pipeline before requests are made.
   *
   * @param options Initial HTTP request options
   * @returns Updated request options
   */
  protected async transformOptions(options: RequestInit): Promise<RequestInit> {
    if (
      this.configuration != undefined &&
      this.configuration.useProblemDetails &&
      this.configuration.clearProblemDetails !== false
    ) {
      const problem = useProblemDetails()
      problem.clearError()
    }

    return options
  }

  /**
   * Transform result received from the server
   *
   * Derived API client classes will call this after receiving a response
   * from the server, but before parsing it. By design, this is our chance
   * to modify the response before or after parsing. We use it to report
   * any error (non-success responses surface as exceptions from the
   * processing function) to useProblemDetails, if the caller opted in.
   *
   * @param _url_ The request URL that produced this response
   * @param _response The exact response from the server
   * @param arg2 The derived class's processing function
   * @returns The processed response, or `undefined` if an error was swallowed
   * @throws The original error, only when `throwOnError` is set
   */
  protected async transformResult<T>(
    _url_: string,
    _response: Response,
    arg2: (response: Response) => Promise<T>,
  ): Promise<T> {
    try {
      return await arg2(_response)
    } catch (error) {
      if (this.configuration?.useProblemDetails) {
        await useProblemDetails().handleApiError(error, 'Request failed')
      }
      if (this.configuration?.throwOnError) {
        throw error
      }
      return undefined as T
    }
  }
}
