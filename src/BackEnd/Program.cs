using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

ILogger? logger = default;
ILoggerFactory? startupLoggerFactory = default;
try
{
    //
    // Set up Startup logger
    //

    startupLoggerFactory = LoggerFactory.Create(builder =>
    {
        builder.SetMinimumLevel(LogLevel.Debug);
        // TODO: Add Terse logger
        // builder.AddTerseConsoleLogFormatter(options => options.IncludeScopes = false);
        // builder.AddConsole(x => x.FormatterName = "TerseConsole");
    });
    logger = startupLoggerFactory.CreateLogger("Startup");
    logger.LogInformation("Starting {App}",Assembly.GetExecutingAssembly().FullName);

    //
    // *** CREATE BUILDER ***
    //

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddControllers();

    var app = builder.Build();

    app.UseHttpsRedirection();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Environment.ExitCode = 1;

    if (logger != null)
    {
        logger.LogCritical(ex, "Startup Failed");
    }
    else
    {
        Console.WriteLine("*** STARTUP FAILED *** {0}: {1}", ex.GetType().Name, ex.Message);
    }
}
finally
{
    startupLoggerFactory?.Dispose();
}

