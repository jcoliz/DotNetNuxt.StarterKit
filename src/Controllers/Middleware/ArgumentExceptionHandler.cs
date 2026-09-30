using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace DotNetNuxt.StarterKit.Controllers.Middleware;

/// <summary>
/// Translates <see cref="ArgumentException"/> into an RFC 7807 validation problem response.
/// </summary>
/// <param name="problemDetailsFactory">Factory used to build a consistent problem response</param>
/// <param name="logger">Where to log handled exceptions</param>
public partial class ArgumentExceptionHandler(
    ProblemDetailsFactory problemDetailsFactory,
    ILogger<ArgumentExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ArgumentException argumentException)
        {
            return false;
        }

        LogHandled(argumentException);

        var errors = new Dictionary<string, string[]>();
        var key = argumentException.ParamName ?? string.Empty;
        errors[key] = [argumentException.Message];

        var problemDetails = problemDetailsFactory.CreateValidationProblemDetails(
            httpContext,
            new ModelStateDictionary(),
            statusCode: StatusCodes.Status400BadRequest);
        problemDetails.Errors = errors;

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Handled argument exception")]
    private partial void LogHandled(Exception exception);
}
