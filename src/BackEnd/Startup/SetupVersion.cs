using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace DotNetNuxt.StarterKit.BackEnd.Startup;

public static partial class __SetupVersion
{
    /// <summary>
    /// Get app version from the entry assembly
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="logger"></param>
    /// <returns>Informational version, or "unknown"</returns>
    public static string GetVersion(this WebApplicationBuilder builder, ILogger logger)
    {

        // Get app version from assembly attribute
        var assembly = Assembly.GetEntryAssembly();
        var version = assembly?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";

        LogVersion(logger, version);

        return version;
    }

    #region Logging

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "{Location}: Version: {Version}")]
    private static partial void LogVersion(ILogger logger, string version, [CallerMemberName] string location = "");

    #endregion
}
