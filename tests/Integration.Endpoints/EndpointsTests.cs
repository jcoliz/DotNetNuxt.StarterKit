using System.Diagnostics;
using System.Net;
using System.Text.Json;
using DotNetNuxt.StarterKit.Controllers.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace DotNetNuxt.Tests.Integration.Endpoints;

[TestFixture]
public class EndpointsTests
{
    private PostgreSqlContainer _postgres = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private readonly RequestTelemetryCapture _telemetry = new();

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await _postgres.StartAsync();

        // Program.cs reads the connection string eagerly, so it must be present before the host is built.
        // Environment variables are visible to WebApplication.CreateBuilder at that point.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _postgres.GetConnectionString());

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging =>
            {
                logging.AddProvider(_telemetry);
                logging.AddFilter<RequestTelemetryCapture>(null, LogLevel.Information);
            }));
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _client?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        _telemetry.Dispose();

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);

        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
    }

    [Test]
    public async Task Version_ReturnsOk()
    {
        var response = await _client.GetAsync("/version");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.Not.Empty);
    }

    [Test]
    public async Task Weather_ReturnsForecasts()
    {
        var response = await _client.GetAsync("/api/weather?offset=0&count=5");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetArrayLength(), Is.EqualTo(5));
    }

    [Test]
    public async Task Weather_OffsetOutOfRange_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/weather?offset=31");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [TestCase("/api/weather?offset=31", Description = "model validation")]
    [TestCase("/api/weather?offset=0&count=60", Description = "feature validation")]
    public async Task Weather_BadRequest_NamesTheOperation(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetProperty("detail").GetString(), Is.EqualTo("Failed to fetch weather forecasts"));
    }

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
        request.Headers.Add("X-Test-Class", nameof(EndpointsTests));
        request.Headers.Add("X-Test-Client", "BackEnd");

        using var response = await _client.SendAsync(request);
        var tags = await _telemetry.WaitForServerTagsAsync(traceId);
        var logs = _telemetry.Logs.Where(entry => entry.TraceId == traceId).ToArray();

        Assert.That(response.StatusCode, Is.EqualTo(expectedStatus));
        Assert.That(tags["test.name"], Is.EqualTo(nameof(TestCorrelation_enriches_request_telemetry)));
        Assert.That(tags["test.id"], Is.EqualTo(testId));
        Assert.That(tags["test.class"], Is.EqualTo(nameof(EndpointsTests)));
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

        using var response = await _client.SendAsync(request);
        var tags = await _telemetry.WaitForServerTagsAsync(traceId);
        var logs = _telemetry.Logs.Where(entry => entry.TraceId == traceId).ToArray();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(tags.Keys.Any(key => key.StartsWith("test.", StringComparison.Ordinal)), Is.False);
        Assert.That(logs, Is.Not.Empty);
        Assert.That(logs.Any(entry => entry.Scope.ContainsKey("TestName")), Is.False);
    }
}
