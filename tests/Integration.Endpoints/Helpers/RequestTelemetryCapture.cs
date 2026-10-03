using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DotNetNuxt.Tests.Integration.Endpoints.Helpers;

/// <summary>
/// Captures completed ASP.NET Core server activity tags and structured log scopes for integration tests.
/// </summary>
/// <remarks>
/// Register this helper as a logging provider on the test host. Its activity listener is process-wide,
/// so tests use a unique trace ID in the request's traceparent header to identify their own telemetry.
/// Dispose the helper to unregister the listener.
/// </remarks>
internal sealed class RequestTelemetryCapture : ILoggerProvider, ISupportExternalScope
{
    /// <summary>
    /// Coordinates activity completion with assertions, whether the activity finishes before or after a test waits.
    /// </summary>
    private readonly ConcurrentDictionary<ActivityTraceId, TaskCompletionSource<Dictionary<string, object?>>> _serverTags = new();

    /// <summary>
    /// Observes ASP.NET Core activities independently of any configured telemetry exporter.
    /// </summary>
    private readonly ActivityListener _listener;

    /// <summary>
    /// Provides the current logging scopes shared by the host's logging providers.
    /// </summary>
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    /// <summary>
    /// Gets captured log metadata and scope snapshots. Filter by trace ID to select a request's logs.
    /// </summary>
    public ConcurrentQueue<LogEntry> Logs { get; } = new();

    /// <summary>
    /// Starts listening for ASP.NET Core activities and snapshots server tags when each activity stops.
    /// </summary>
    /// <remarks>
    /// Sampling requests all activity data so tags are available even without an exporter.
    /// Both parent-context formats are supported. Waiter continuations run asynchronously to avoid
    /// running test assertions inside the request's activity-stop callback.
    /// </remarks>
    public RequestTelemetryCapture()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Microsoft.AspNetCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.Kind == ActivityKind.Server)
                {
                    _serverTags.GetOrAdd(activity.TraceId, _ => new(TaskCreationOptions.RunContinuationsAsynchronously))
                        .TrySetResult(activity.TagObjects.ToDictionary(tag => tag.Key, tag => tag.Value));
                }
            }
        };
        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>
    /// Waits for the server activity to finish so its final tags can be asserted.
    /// </summary>
    /// <param name="traceId">The trace ID supplied in the request's traceparent header.</param>
    /// <returns>A snapshot of the completed server activity's tags.</returns>
    /// <exception cref="TimeoutException">No matching server activity completes within ten seconds.</exception>
    public Task<Dictionary<string, object?>> WaitForServerTagsAsync(ActivityTraceId traceId)
        => _serverTags.GetOrAdd(traceId, _ => new(TaskCreationOptions.RunContinuationsAsynchronously))
            .Task.WaitAsync(TimeSpan.FromSeconds(10));

    /// <summary>
    /// Creates a logger that captures metadata and scopes rather than formatting log messages.
    /// </summary>
    /// <param name="categoryName">The category recorded with each log entry.</param>
    /// <returns>A logger writing to this helper's shared capture queue.</returns>
    public ILogger CreateLogger(string categoryName) => new CaptureLogger(this, categoryName);

    /// <summary>
    /// Receives the host's shared scope provider so scopes opened by other loggers are visible here.
    /// </summary>
    /// <param name="scopeProvider">The scope provider supplied by the logging infrastructure.</param>
    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

    /// <summary>
    /// Unregisters the process-wide activity listener, stopping further activity capture.
    /// </summary>
    public void Dispose() => _listener.Dispose();

    /// <summary>
    /// Holds the metadata needed to correlate a log entry with a request and inspect its structured scopes.
    /// </summary>
    /// <param name="Category">The originating logger's category.</param>
    /// <param name="EventId">The event identifier used to select a particular log operation.</param>
    /// <param name="TraceId">The current activity's trace ID, or the default value when no activity exists.</param>
    /// <param name="Scope">A snapshot of structured scope values at the time the entry was logged.</param>
    internal sealed record LogEntry(string Category, EventId EventId, ActivityTraceId TraceId,
        IReadOnlyDictionary<string, object?> Scope);

    /// <summary>
    /// Copies active structured scopes into the capture queue when downstream code writes a log entry.
    /// </summary>
    /// <param name="capture">The helper owning the scope provider and capture queue.</param>
    /// <param name="category">The originating logger's category.</param>
    private sealed class CaptureLogger(RequestTelemetryCapture capture, string category) : ILogger
    {
        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => capture._scopeProvider.Push(state);

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        /// <summary>
        /// Snapshots structured scope properties and associates them with the current activity's trace ID.
        /// </summary>
        /// <remarks>
        /// Non-structured scopes are ignored. Inner scopes overwrite matching keys from outer scopes.
        /// Copying values now preserves them after the request's scopes have been disposed.
        /// Message text, log state, and exceptions are not retained because assertions use metadata and scopes.
        /// </remarks>
        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var scopeValues = new Dictionary<string, object?>();
            capture._scopeProvider.ForEachScope((scope, values) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object?>> properties)
                {
                    foreach (var property in properties)
                    {
                        values[property.Key] = property.Value;
                    }
                }
            }, scopeValues);
            capture.Logs.Enqueue(new(category, eventId, Activity.Current?.TraceId ?? default, scopeValues));
        }
    }
}