using System;
using System.Reflection;
using DotNetNuxt.StarterKit.BackEnd.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DotNetNuxt.Hosting;
using DotNetNuxt.Hosting.Options;
using DotNetNuxt.Hosting.Logging;
using static DotNetNuxt.Hosting.Logging.BootLoggerExtensions;
using static DotNetNuxt.Hosting.VersionHelpers;

ILogger? logger = default;
try
{
    //
    // Set up Boot logger
    //

    logger = CreateStandardBootLogger();
    logger.LogInformation("Starting {App}", Assembly.GetExecutingAssembly().FullName);

    //
    // *** CREATE BUILDER ***
    //

    var builder = WebApplication.CreateBuilder(args);

    //
    // Set up configuration sources
    //

    // Add site-specific config option
    builder.Configuration.AddTomlFile("config.toml", optional: true, reloadOnChange: true);

    // TODO: Add Key Vault config source, if configured
    // builder.Configuration.SetupAzureKeyVault(logger);

    // Add the usual Startup options
    StartupOptions startupOptions = builder.AddStandardStartupOptions();

    //
    // Add more services
    //

    builder.AddServiceDefaults();

    builder.SetupDatabase(logger);

    builder.Logging.AddTerseConsoleLogFormatter(options => options.IncludeScopes = false);

    var version = GetVersion(typeof(Program).Assembly);
    logger.LogInformation("Application version: {Version}", version);
    builder.Services.AddWebApiServices(version);

    builder.Services.AddApplicationFeatures();

    builder.Services.AddSwagger();

    builder.Services.AddStandardCorsPolicy(startupOptions.AllowedCorsOrigins);

    var app = builder.Build();

    // Boot logger, signing off!
    logger.LogInformation("OK. Built {App}", Assembly.GetExecutingAssembly().FullName);

    // Switch to the app's logger so post-build logs go through the full pipeline
    // (OpenTelemetry, Azure Monitor, etc.) instead of just the startup console logger
    logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    try
    {
        await app.PrepareDatabaseAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database preparation failed. The app will start without seeded data. " +
            "Database-dependent API calls will fail until the database is available.");
    }

    app.UseWebApiServices();
    app.UseStatusCodePages(); // This enables ProblemDetails for status codes like 404

    // This is typically only done in the local and CI-built container
    if (startupOptions.AllowHttpInsecure)
    {
        logger.LogWarning("Allowing insecure HTTP connections");
    }
    else
    {
        if (!app.Environment.IsDevelopment())
        {
            // Note that in container we DON'T want dev server, but we also don't want https. So we can't use development mode
            // in container. Have to check the env var.

            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        app.UseHttpsRedirection();
    }

    logger.LogInformation("Startup Options: {Options}", System.Text.Json.JsonSerializer.Serialize(startupOptions));

    // Configure HTTP request pipeline 
    if (startupOptions.EnableSwaggerUi)
    {
        logger.LogInformation("Enabling Swagger UI");
        app.UseSwagger();
    }

    app.UseRouting();

    app.UseCors();

    app.MapControllers();

    app.MapDefaultEndpoints();

    logger.LogInformation("OK");

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
}


// Exposes the entry point to WebApplicationFactory<Program> in integration tests
public partial class Program;
