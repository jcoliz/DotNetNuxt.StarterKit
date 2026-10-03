using System.Diagnostics;
using System.Net;
using DotNetNuxt.StarterKit.Controllers.Middleware;
using DotNetNuxt.Tests.Integration.Endpoints.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetNuxt.Tests.Integration.Endpoints;

[TestFixture]
[NonParallelizable]
public class TestCorrelationMiddlewareTests : EndpointTestFixture
{
    private readonly RequestTelemetryCapture _telemetry = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.ConfigureLogging(logging =>
        {
            logging.AddProvider(_telemetry);
            logging.AddFilter<RequestTelemetryCapture>(null, LogLevel.Information);
        });

    [OneTimeTearDown]
    public void DisposeTelemetry() => _telemetry.Dispose();

    [TestCase("/version", HttpStatusCode.OK)]
    [TestCase("/api/weather?offset=0&count=60", HttpStatusCode.BadRequest)]
    public async Task TestCorrelation_enriches_request_telemetry(string url, HttpStatusCode expectedStatus)
    {
        var traceId = ActivityTraceId.CreateRandom();
        var testId = Guid.NewGuid().ToString();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("traceparent", $"00-{traceId}-{ActivitySpanId.CreateRandom()}-01");
        request.Headers.Add("X-Test-Name", nameof(TestCorrelation_enriches_request_telemetry));
        request.Headers.Add("X-Test-Id", testId);
        request.Headers.Add("X-Test-Class", nameof(TestCorrelationMiddlewareTests));
        request.Headers.Add("X-Test-Client", "BackEnd");

        using var response = await Client.SendAsync(request);
        var tags = await _telemetry.WaitForServerTagsAsync(traceId);
        var logs = _telemetry.Logs.Where(entry => entry.TraceId == traceId).ToArray();

        Assert.That(response.StatusCode, Is.EqualTo(expectedStatus));
        Assert.That(tags["test.name"], Is.EqualTo(nameof(TestCorrelation_enriches_request_telemetry)));
        Assert.That(tags["test.id"], Is.EqualTo(testId));
        Assert.That(tags["test.class"], Is.EqualTo(nameof(TestCorrelationMiddlewareTests)));
        Assert.That(tags["test.client"], Is.EqualTo("BackEnd"));
        Assert.That(logs.Any(entry => entry.Scope.TryGetValue("TestName", out var testName)
            && Equals(testName, nameof(TestCorrelation_enriches_request_telemetry))), Is.True);

        if (expectedStatus == HttpStatusCode.BadRequest)
        {
            var handledLog = logs.Single(entry => entry.Category == typeof(ArgumentExceptionHandler).FullName
                && entry.EventId.Id == 1);
            Assert.That(handledLog.Scope["TestName"], Is.EqualTo(nameof(TestCorrelation_enriches_request_telemetry)));
        }
    }

    [Test]
    public async Task TestCorrelation_without_headers_does_not_enrich_request_telemetry()
    {
        var traceId = ActivityTraceId.CreateRandom();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/weather?offset=0&count=60");
        request.Headers.Add("traceparent", $"00-{traceId}-{ActivitySpanId.CreateRandom()}-01");

        using var response = await Client.SendAsync(request);
        var tags = await _telemetry.WaitForServerTagsAsync(traceId);
        var logs = _telemetry.Logs.Where(entry => entry.TraceId == traceId).ToArray();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(tags.Keys.Any(key => key.StartsWith("test.", StringComparison.Ordinal)), Is.False);
        Assert.That(logs, Is.Not.Empty);
        Assert.That(logs.Any(entry => entry.Scope.ContainsKey("TestName")), Is.False);
    }
}