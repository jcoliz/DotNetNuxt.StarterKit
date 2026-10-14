using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;
using System.Text.Json;
using DotNetNuxt.Abstractions;
using DotNetNuxt.StarterKit.Application;
using DotNetNuxt.Tests.Integration.Endpoints.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace DotNetNuxt.Tests.Integration.Endpoints;

[TestFixture]
[NonParallelizable]
public class ExceptionLoggingTests : EndpointTestFixture
{
    private readonly RequestTelemetryCapture _telemetry = new();
    private readonly InvalidOperationException _exception = new("Simulated forecast query failure");
    private FailingDataProvider _provider = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _provider = new FailingDataProvider(_exception);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDataProvider>();
            services.AddSingleton<IDataProvider>(_provider);
        });
        builder.ConfigureLogging(logging =>
        {
            logging.AddProvider(_telemetry);
            logging.AddFilter<RequestTelemetryCapture>(null, LogLevel.Information);
        });
    }

    [OneTimeTearDown]
    public void DisposeTelemetry() => _telemetry.Dispose();

    [Test]
    public async Task Weather_query_failure_returns_problem_details_and_logs_original_exception()
    {
        var testId = TestContext.CurrentContext.Test.ID;
        var traceId = ActivityTraceId.CreateRandom();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/weather?offset=0&count=5");
        request.Headers.Add("traceparent", $"00-{traceId}-{ActivitySpanId.CreateRandom()}-01");

        using var response = await Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        await _telemetry.WaitForServerTagsAsync(traceId);
        var exceptionLogs = _telemetry.Logs
            .Where(entry => entry.TraceId == traceId && ReferenceEquals(entry.Exception, _exception))
            .ToArray();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        using var problem = JsonDocument.Parse(body);
        Assert.That(problem.RootElement.GetProperty("status").GetInt32(), Is.EqualTo(500));
        Assert.That(problem.RootElement.GetProperty("detail").GetString(),
            Is.EqualTo("Failed to fetch weather forecasts"));
        Assert.That(body, Does.Not.Contain(_exception.Message));
        Assert.That(exceptionLogs, Has.Length.EqualTo(1), "The original exception should be logged exactly once.");
        Assert.That(exceptionLogs[0].Level, Is.EqualTo(LogLevel.Error));
        Assert.That(exceptionLogs[0].Category,
            Is.EqualTo("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware"));
        Assert.That(exceptionLogs[0].Message, Is.Not.Empty);
        Assert.That(exceptionLogs[0].Exception!.StackTrace, Does.Contain(nameof(WeatherForecastFeature.ListForecasts)));
        Assert.That(_provider.TestId, Is.EqualTo(testId),
            "Request logging must retain NUnit's test context so TestContext.Progress reaches the runner.");
    }

    private sealed class FailingDataProvider(Exception exception) : IDataProvider
    {
        public string? TestId { get; private set; }

        public IQueryable<T> Get<T>() where T : class, IModel => Array.Empty<T>().AsQueryable();

        public Task<List<T>> ToListNoTrackingAsync<T>(IQueryable<T> query) where T : class
        {
            TestId = TestContext.CurrentContext.Test.ID;
            return Task.FromException<List<T>>(exception);
        }

        public IQueryable<TEntity> GetIncluding<TEntity, TProperty>(
            Expression<Func<TEntity, TProperty>> navigationPropertyPath) where TEntity : class, IModel
            => throw new NotSupportedException();

        public void Add(object item) => throw new NotSupportedException();
        public void AddRange(IEnumerable<object> items) => throw new NotSupportedException();
        public void Update(object item) => throw new NotSupportedException();
        public void UpdateRange(IEnumerable<object> items) => throw new NotSupportedException();
        public void Remove(object item) => throw new NotSupportedException();
        public void RemoveRange(IEnumerable<object> items) => throw new NotSupportedException();
        public Task<int> ClearAsync<T>() where T : IModel => throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<List<T>> ToListAsync<T>(IQueryable<T> query) => throw new NotSupportedException();
        public Task<int> CountAsync<T>(IQueryable<T> query) where T : IModel => throw new NotSupportedException();
        public Task<bool> AnyAsync<T>(IQueryable<T> query) where T : IModel => throw new NotSupportedException();
        public Task BulkInsertAsync<T>(IList<T> items) where T : IModel => throw new NotSupportedException();
        public Task<int> BulkDeleteAsync<T>(IQueryable<T> items) where T : IModel => throw new NotSupportedException();
        public Task<int> BulkUpdateAsync<T>(IQueryable<T> items, T newValues, List<string> columns) where T : IModel
            => throw new NotSupportedException();
    }
}
