using Microsoft.Playwright;

namespace DotNetNuxt.StarterKit.Tests.Functional.Pages;

/// <summary>
/// Represents the WeatherPage type.
/// </summary>
public class WeatherPage : BasePage
{
    private const string UrlPath = "/weather";

    public ILocator ResultsTable => Page!.GetByTestId("results");
    public ILocator HasLoaded => Page!.GetByTestId("has-loaded");
    public ILocator Forecasts => ResultsTable.Locator("tbody tr");

    /// <summary>
    /// Constructor
    /// </summary>
    public WeatherPage(IPage _page) : base(_page)
    {
    }

    /// <summary>
    /// Navigates to the weather page via address bar
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
        await HasLoaded.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeout });
    }
}
