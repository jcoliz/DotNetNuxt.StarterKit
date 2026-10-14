using DotNetNuxt.Tests.Integration.Endpoints.Helpers;
using Microsoft.Extensions.Logging;

namespace DotNetNuxt.Tests.Integration.Endpoints;

[TestFixture]
public class NUnitLoggerProviderTests
{
    [Test]
    public void Logger_writes_formatted_message_level_and_category()
    {
        using var output = new StringWriter();
        using var provider = new NUnitLoggerProvider(output);
        var logger = provider.CreateLogger("Server.Request");

        logger.LogInformation("Handling {Path}", "/version");

        Assert.That(output.ToString(),
            Is.EqualTo($"[Information] Server.Request: Handling /version{Environment.NewLine}"));
    }

    [Test]
    public void Logger_includes_exception_details()
    {
        using var output = new StringWriter();
        using var provider = new NUnitLoggerProvider(output);
        var logger = provider.CreateLogger("Server.Request");
        var exception = new InvalidOperationException("Database unavailable");

        logger.LogError(exception, "Request failed");

        Assert.That(output.ToString(),
            Is.EqualTo($"[Error] Server.Request: Request failed{Environment.NewLine}{exception}{Environment.NewLine}"));
    }

    [Test]
    public void Provider_filter_enables_information_and_excludes_debug_despite_global_warning_filter()
    {
        using var output = new StringWriter();
        using var factory = LoggerFactory.Create(logging =>
        {
            logging.AddProvider(new NUnitLoggerProvider(output));
            logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
            logging.AddFilter<NUnitLoggerProvider>(null, LogLevel.Information);
        });
        var logger = factory.CreateLogger("Microsoft.AspNetCore.Hosting.Diagnostics");

        logger.LogDebug("Debug details");
        logger.LogInformation("Request starting");
        logger.Log(LogLevel.None, "Disabled");

        Assert.That(output.ToString(),
            Is.EqualTo($"[Information] Microsoft.AspNetCore.Hosting.Diagnostics: Request starting{Environment.NewLine}"));
    }
}
