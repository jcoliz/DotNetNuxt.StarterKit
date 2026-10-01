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
   * True if the client show throw an error when problems are reported
   * from server. False or undefined to handle errors entirely in client.
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
 *
 * Soon, it will...
 *  * Standardize the errors so that all exceptions are problem details
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
   * to modify the response before or after parsing. However, we just use
   * it to capture any errors thrown by parsing.
   *
   * @param _url_ The request URL that produced this response
   * @param _response The exact response from the server
   * @param arg2 The derived class's processing function
   * @returns The processed response
   * @throws Lots of different errors based on the situation
   */
  protected async transformResult(
    _url_: string,
    _response: Response,
    arg2: (response: Response) => Promise<any>,
  ): Promise<any> {
    //
    // We don't actually do anything here. Just process the pipeline.
    // We care about the errors
    //
    const result = await arg2(_response)
    return result
    //
    // The original design here was to capture errors and convert them
    // to ProblemDetails, but that hasn't been implemented yet.
    //
    // Right now, we have this boilerplate after every API call in the generated clients to post errors to the global problem details:
    //
    // catch (error) {
    // errors.handleApiError(error, 'Loading failed', 'Failed to fetch items')
    //
    // This does have the advantage of allowing each call to customize the error message, but it is a lot of boilerplate, and it's easy for developers to forget to do it.
    // It would be worth considering to move this logic here, so that all errors are standardized and automatically posted, and developers don't have to worry about it at all.
    // We could still allow for custom error messages by allowing the caller to pass in a custom
    // error handler in the configuration, or by allowing the caller to pass in a custom error message in the configuration that we use when posting the error.
    //
  }
}
