using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;
using System.Text.Json;
using DotNetNuxt.Abstractions;
using DotNetNuxt.StarterKit.Application;
using DotNetNuxt.StarterKit.Controllers;
using DotNetNuxt.Tests.Integration.Endpoints.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetNuxt.Tests.Integration.Endpoints;

[TestFixture]
public class ExceptionLoggingTests
{
    [Test]
    public async Task Weather_query_failure_returns_problem_details_and_logs_original_exception()
    {
        var exception = new InvalidOperationException("Simulated forecast query failure");
        using var telemetry = new RequestTelemetryCapture();
        using var output = new StringWriter();
        using var host = await new HostBuilder().ConfigureWebHost(builder => builder
            .UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddWebApiServices("test")
                    .AddApplicationPart(typeof(WeatherController).Assembly);
                services.AddSingleton<IDataProvider>(new FailingDataProvider(exception));
                services.AddSingleton(TimeProvider.System);
                services.AddTransient<WeatherForecastFeature>();
            })
            .ConfigureLogging(logging =>
            {
                logging.AddProvider(telemetry);
                logging.AddFilter<RequestTelemetryCapture>(null, LogLevel.Information);
                logging.AddProvider(new NUnitLoggerProvider(TextWriter.Synchronized(output)));
                logging.AddFilter<NUnitLoggerProvider>(null, LogLevel.Information);
            })
            .Configure(app =>
            {
                app.UseWebApiServices();
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            })).StartAsync();
        using var client = host.GetTestClient();
        var traceId = ActivityTraceId.CreateRandom();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/weather?offset=0&count=5");
        request.Headers.Add("traceparent", $"00-{traceId}-{ActivitySpanId.CreateRandom()}-01");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        await telemetry.WaitForServerTagsAsync(traceId);
        var exceptionLogs = telemetry.Logs
            .Where(entry => entry.TraceId == traceId && ReferenceEquals(entry.Exception, exception))
            .ToArray();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        using var problem = JsonDocument.Parse(body);
        Assert.That(problem.RootElement.GetProperty("status").GetInt32(), Is.EqualTo(500));
        Assert.That(problem.RootElement.GetProperty("detail").GetString(),
            Is.EqualTo("Failed to fetch weather forecasts"));
        Assert.That(body, Does.Not.Contain(exception.Message));
        Assert.That(exceptionLogs, Has.Length.EqualTo(1), "The original exception should be logged exactly once.");
        Assert.That(exceptionLogs[0].Level, Is.EqualTo(LogLevel.Error));
        Assert.That(exceptionLogs[0].Category,
            Is.EqualTo("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware"));
        Assert.That(exceptionLogs[0].Message, Is.Not.Empty);
        Assert.That(exceptionLogs[0].Exception!.StackTrace, Does.Contain(nameof(WeatherForecastFeature.ListForecasts)));
        Assert.That(output.ToString(), Does.Contain(exception.ToString()));
    }

    private sealed class FailingDataProvider(Exception exception) : IDataProvider
    {
        public IQueryable<T> Get<T>() where T : class, IModel => Array.Empty<T>().AsQueryable();

        public Task<List<T>> ToListNoTrackingAsync<T>(IQueryable<T> query) where T : class
            => Task.FromException<List<T>>(exception);

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
