using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace DotNetNuxt.Tests.Integration.Endpoints.Helpers;

public abstract class EndpointTestFixture
{
    private PostgreSqlContainer _postgres = null!;
    private WebApplicationFactory<Program> _factory = null!;

    protected HttpClient Client { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await _postgres.StartAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _postgres.GetConnectionString());

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging =>
            {
                logging.AddProvider(new NUnitLoggerProvider(TestContext.Progress));
                logging.AddFilter<NUnitLoggerProvider>(null, LogLevel.Information);
            });
            ConfigureWebHost(builder);
        });
        // NUnit's immediate output needs the calling test's async-local context during requests.
        _factory.Server.PreserveExecutionContext = true;
        Client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        Client?.Dispose();
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

    protected virtual void ConfigureWebHost(IWebHostBuilder builder) { }
}