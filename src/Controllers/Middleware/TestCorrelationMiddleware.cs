using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DotNetNuxt.StarterKit.Controllers.Middleware;

/// <summary>
/// Enriches request logs and activity tags with test details supplied in HTTP headers.
/// </summary>
/// <remarks>
/// Domain-agnostic. Could be broken out into separate reusable middleware for test correlation.
/// </remarks>
/// <param name="next">The next middleware in the request pipeline</param>
/// <param name="logger">Logger used to scope downstream request logs</param>
public class TestCorrelationMiddleware(RequestDelegate next, ILogger<TestCorrelationMiddleware> logger)
{
    /// <summary>
    /// Adds test context for the duration of the downstream request pipeline.
    /// </summary>
    /// <param name="context">The current HTTP context</param>
    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-Test-Name", out var testName))
        {
            await next(context);
            return;
        }

        var activity = Activity.Current;
        activity?.SetTag("test.name", testName.ToString());
        activity?.SetTag("test.id", context.Request.Headers.TryGetValue("X-Test-Id", out var testId)
            ? testId.ToString() : null);
        activity?.SetTag("test.class", context.Request.Headers.TryGetValue("X-Test-Class", out var testClass)
            ? testClass.ToString() : null);
        activity?.SetTag("test.client", context.Request.Headers.TryGetValue("X-Test-Client", out var testClient)
            ? testClient.ToString() : null);

        using (logger.BeginScope(new Dictionary<string, object> { ["TestName"] = testName.ToString() }))
        {
            await next(context);
        }
    }
}