using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace DotNetNuxt.Tests.Integration.Endpoints;

[TestFixture]
public class EndpointsTests
{
    private PostgreSqlContainer _postgres = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await _postgres.StartAsync();

        // Program.cs reads the connection string eagerly, so it must be present before the host is built.
        // Environment variables are visible to WebApplication.CreateBuilder at that point.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _postgres.GetConnectionString());

        _factory = new WebApplicationFactory<Program>();
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
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("development"));
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
}
