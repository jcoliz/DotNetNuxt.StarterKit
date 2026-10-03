using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DotNetNuxt.Tests.Integration.Endpoints;

internal sealed class RequestTelemetryCapture : ILoggerProvider, ISupportExternalScope
{
    private readonly ConcurrentDictionary<ActivityTraceId, TaskCompletionSource<Dictionary<string, object?>>> _serverTags = new();
    private readonly ActivityListener _listener;
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    public ConcurrentQueue<LogEntry> Logs { get; } = new();

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

    public Task<Dictionary<string, object?>> WaitForServerTagsAsync(ActivityTraceId traceId)
        => _serverTags.GetOrAdd(traceId, _ => new(TaskCreationOptions.RunContinuationsAsynchronously))
            .Task.WaitAsync(TimeSpan.FromSeconds(10));

    public ILogger CreateLogger(string categoryName) => new CaptureLogger(this, categoryName);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

    public void Dispose() => _listener.Dispose();

    internal sealed record LogEntry(string Category, EventId EventId, ActivityTraceId TraceId,
        IReadOnlyDictionary<string, object?> Scope);

    private sealed class CaptureLogger(RequestTelemetryCapture capture, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => capture._scopeProvider.Push(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

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