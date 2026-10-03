using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DotNetNuxt.StarterKit.Application;
using DotNetNuxt.StarterKit.Controllers.Attributes;
using DotNetNuxt.StarterKit.Entities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DotNetNuxt.StarterKit.Controllers;

[Route("api/[controller]")]
[ApiController]
//[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public partial class WeatherController(WeatherForecastFeature feature, ILogger<WeatherController> logger) : ControllerBase
{
    /// <summary>
    /// Retrieve current weather forecasts
    /// </summary>
    /// <param name="offset">Number of days from today to start listing forecasts</param>
    /// <param name="count">Number of forecasts to include</param>
    /// <returns>Current weather forecasts</returns>
    [HttpGet()]
    [ProblemContext("Failed to fetch weather forecasts")]
    [ProducesResponseType(typeof(IReadOnlyCollection<WeatherForecast>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [Range(-30, 30)] int offset = 0,
        [Range(1, 60)] int count = 5)
    {
        var forecasts = await feature.ListForecasts(offset, count);

        var result = forecasts;

        LogOkCount(result.Count);

        return Ok(result);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "{Location}: OK {Count} returned")]
    private partial void LogOkCount(int count, [CallerMemberName] string location = "");

}
