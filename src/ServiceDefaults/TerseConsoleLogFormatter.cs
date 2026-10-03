using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

namespace DotNetNuxt.StarterKit.ServiceDefaults;

/// <summary>
/// Formats logs with just the information we want in local debugging
/// </summary>
/// <remarks>
/// Domain-agnostic. Could be broken out into separate reusable library.
///
/// One thing to be aware of, we do use the TestName which is set by
/// the test correlation middleware during test execution.
/// </remarks>
public sealed partial class TerseConsoleLogFormatter : ConsoleFormatter, IDisposable
{
    [GeneratedRegex("(.+?) \\(.+?\\)$")]
    private static partial Regex ActionRegex();

    private readonly TerseConsoleLogOptions _options;

    /// <summary>
    /// Constructor
    /// </summary>
    public TerseConsoleLogFormatter(IOptions<TerseConsoleLogOptions> options)
        // Case insensitive
        : base("TerseConsole")
    {
        _options = options.Value;
    }

    /// <summary>
    /// Write a log message
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <param name="logEntry"></param>
    /// <param name="scopeProvider"></param>
    /// <param name="textWriter"></param>
    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var message =
            logEntry.Formatter?.Invoke(
                logEntry.State, logEntry.Exception);

        if (message is null)
        {
            return;
        }

        var category = logEntry.Category;
        var testName = default(string);

        var collectedScopes = new List<KeyValuePair<string, object>>();

        scopeProvider?.ForEachScope((x,t) => 
        {
            if (x is IEnumerable<KeyValuePair<string, object>> values)
            {
                // TODO: I don't think we're setting ActionName right now
                var found = values.Where(y => y.Key == "ActionName");
                if (found.Any())
                {
                    var kvp = found.FirstOrDefault();
                    var action = kvp.Value?.ToString() ?? string.Empty;

                    var match = ActionRegex().Match(action);
                    if (match.Success)
                    {
                        var first = match.Groups[1].Value;
                        var split = first.Split('.');
                        var chosen = split[^2..];
                        category = string.Join('.', chosen).Replace("Controller", "");
                    }
                }
                found = values.Where(y => y.Key == "TestName");
                if (found.Any())
                {
                    var kvp = found.FirstOrDefault();
                    testName = kvp.Value?.ToString();
                }

                collectedScopes.AddRange(values);
            }
        },typeof(TState));

        textWriter.Write
        (
            "{0} {1} [{2:D4}] {3}{4}: ",
            logEntry.LogLevel.ToString().ToUpper()[0..3],
            DateTimeOffset.Now.ToString("MM/dd HH:mm:ss"),
            category.StartsWith("Microsoft") 
                ? "****"
                : logEntry.EventId.Id.ToString("D4"),
            testName != null
                ? $"/{testName}/ "
                : string.Empty,
            category
        );
        textWriter.WriteLine(message);

        if (logEntry.Exception != null)
        {
            textWriter.WriteLine
            (
              // INF 02/21 02:02:48 [1003] Views.Shop: OK 0 items in 0 super groups
                "                EXCEPTION {0}: {1}",
                logEntry.Exception.GetType().Name,
                logEntry.Exception.Message
            );
            textWriter.WriteLine(logEntry.Exception.StackTrace);
            textWriter.WriteLine();
        }

        if (_options.IncludeScopes)
        {
            foreach (var kvp in collectedScopes)
            {
                textWriter.WriteLine("    {0}: {1}",kvp.Key, kvp.Value);            
            }        
        }
    }
    /*
     * EXAMPLE SCOPES:
        SpanId: b6e2eeaa4c526aa4
        TraceId: b4982c97c45f36e99f70f23fe7d78020
        ParentId: 0000000000000000
        ConnectionId: 0HN1F7CSLC00V
        RequestId: 0HN1F7CSLC00V:0000006D
        RequestPath: /api/Identity/refresh
        ActionId: a74f05e9-2d99-4455-9491-01c2f58cc868
        ActionName: Common.Identity.Controllers.IdentityController.Refresh (Common.Identity)     * 
     */
    private void CustomLogicGoesHere(TextWriter textWriter)
    {
    }

    /// <summary>
    /// Dispose object
    /// </summary>
    public void Dispose() { }
}

/// <summary>
/// Formatter options
/// </summary>
/// <remarks>
/// Seems like I can't have a formatter with NO options??
/// </remarks>
public sealed class TerseConsoleLogOptions : ConsoleFormatterOptions
{
}

/// <summary>
/// Extension to add and select the custom console formatter
/// </summary>
public static class ConsoleLoggerExtensions
{
    /// <summary>
    /// Add console logging and select the Terse Console Log formatter.
    /// </summary>
    /// <remarks>
    /// Configures the formatter in code; Logging:Console settings are not required.
    /// </remarks>
    /// <param name="builder"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static ILoggingBuilder AddTerseConsoleLogFormatter(
        this ILoggingBuilder builder,
        Action<TerseConsoleLogOptions> configure) =>
        builder.AddConsole(options => options.FormatterName = "TerseConsole")
            .AddConsoleFormatter<TerseConsoleLogFormatter, TerseConsoleLogOptions>(configure);
}
