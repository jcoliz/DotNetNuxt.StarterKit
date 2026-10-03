using Microsoft.Playwright;

namespace DotNetNuxt.StarterKit.Tests.Functional.Pages;

/// <summary>
/// Represents the About page.
/// </summary>
public class AboutPage : BasePage
{
    private const string UrlPath = "/about";

    public ILocator VersionTable => Page!.GetByTestId("app-version-table");
    public ILocator BackendVersion => VersionTable.GetByTestId("service-version");
    public ILocator FrontendVersion => VersionTable.GetByTestId("front-end-version");

    public AboutPage(IPage _page) : base(_page)
    {
    }

    /// <summary>
    /// Navigates to the About page via address bar.
    /// </summary>
    public async override Task<IResponse?> NavigateToUrlAsync()
    {
        var result = await Page!.GotoAsync(UrlPath);
        await WaitForPageReadyAsync();
        return result;
    }

    public override Task<bool> IsAtAsync() => IsAtPathAsync(UrlPath);

    /// <summary>
    /// Waits for the backend version to finish loading.
    /// </summary>
    public async override Task WaitForPageReadyAsync(float timeout = 5000)
    {
        await BackendVersion.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeout });
    }

    public Task<bool> IsBackendVersionVisibleAsync() => BackendVersion.IsVisibleAsync();

    public Task<bool> IsFrontendVersionVisibleAsync() => FrontendVersion.IsVisibleAsync();

    public async Task<string> GetBackendVersionAsync() => await BackendVersion.InnerTextAsync();

    public async Task<string> GetFrontendVersionAsync() => await FrontendVersion.InnerTextAsync();
}
