using Microsoft.Playwright;

namespace DotNetNuxt.StarterKit.Tests.Functional.Pages;

/// <summary>
/// Represents the HomePage type.
/// </summary>
public class HomePage : BasePage
{
    public ILocator WeatherCard => Page!.GetByTestId("weather-card");
    public ILocator WeatherCardButton => WeatherCard.GetByTestId("to-link");
    public ILocator ProfileCard => Page!.GetByTestId("profile-card");
    public ILocator AboutCard => Page!.GetByTestId("about-card");

    /// <summary>
    /// Executes HomePage.
    /// </summary>
    public HomePage(IPage _page) : base(_page)
    {
    }

    /// <summary>
    /// Executes NavigateToUrlAsync.
    /// </summary>
    public async override Task<IResponse?> NavigateToUrlAsync()
    {
        var result = await Page!.GotoAsync("/");
        await WaitForPageReadyAsync();
        return result;
    }

    /// <summary>
    /// Waits for the page to be ready
    /// </summary>
    public async override Task WaitForPageReadyAsync(float timeout = 5000)
    {
        await WaitForEnabled(WeatherCardButton, timeout);
    }
}
