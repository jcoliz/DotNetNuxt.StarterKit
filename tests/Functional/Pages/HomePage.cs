using Microsoft.Playwright;

namespace DotNetNuxt.StarterKit.Tests.Functional.Pages;

/// <summary>
/// Represents the HomePage type.
/// </summary>
public class HomePage : BasePage
{
    private const string UrlPath = "/";

    public ILocator WeatherCard => Page!.GetByTestId("weather-card");
    public ILocator WeatherCardButton => WeatherCard.GetByTestId("to-link");
    public ILocator ProfileCard => Page!.GetByTestId("profile-card");
    public ILocator ProfileCardButton => ProfileCard.GetByTestId("to-link");
    public ILocator AboutCard => Page!.GetByTestId("about-card");
    public ILocator AboutCardButton => AboutCard.GetByTestId("to-link");

    /// <summary>
    /// Constructor
    /// </summary>
    public HomePage(IPage _page) : base(_page)
    {
    }

    /// <summary>
    /// Navigates to the home page via address bar
    /// </summary>
    public async override Task<IResponse?> NavigateToUrlAsync()
    {
        var result = await Page!.GotoAsync(UrlPath);
        await WaitForPageReadyAsync();
        return result;
    }

    public override Task<bool> IsAtAsync() => IsAtPathAsync(UrlPath);

    /// <summary>
    /// Waits for the page to be ready for user interaction
    /// </summary>
    public async override Task WaitForPageReadyAsync(float timeout = 5000)
    {
        await WaitForEnabled(WeatherCardButton, timeout);
    }

    /// <summary>
    /// Selects a home page card by its displayed name.
    /// </summary>
    public Task SelectCardAsync(string card)
    {
        var button = card switch
        {
            "Weather" => WeatherCardButton,
            "Profile" => ProfileCardButton,
            "About" => AboutCardButton,
            _ => throw new ArgumentException($"Home page card '{card}' is not supported.", nameof(card))
        };

        return button.ClickAsync();
    }
}
