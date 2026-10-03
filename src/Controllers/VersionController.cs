using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace DotNetNuxt.StarterKit.Controllers;

/// <summary>
/// Public version endpoint
/// </summary>
/// <remarks>
/// Domain-agnostic. Could be broken out into separate reusable library.
/// 
/// This is a controller and not a simple endpoint so it gets picked up by the frontend API generator.
/// </remarks>
/// <param name="options">Where to get version from</param>
/// <param name="logger">Where to log</param>
[Route("[controller]")]
[Produces("text/plain")]
public partial class VersionController(IOptions<VersionOptions> options, ILogger<VersionController> logger) : Controller
{
    /// <summary>
    /// The the current application version
    /// </summary>
    /// <returns>Version identifier as simple text</returns>
    [HttpGet]
    [ActionName("Index")]
    public IActionResult Get()
    {
        var version = options.Value.Version;

        if (version != null)
        {
            LogVersionOK(logger, version);
            return Ok(version);
        }
        else
        {
            LogVersionNotFound(logger);
            return NoContent();
        }
    }

    #region Logging

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "{Location}: OK {Version}")]
    private static partial void LogVersionOK(ILogger logger, string version, [CallerMemberName] string location = "");

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "{Location}: Not Found")]
    private static partial void LogVersionNotFound(ILogger logger, [CallerMemberName] string location = "");

    #endregion
}