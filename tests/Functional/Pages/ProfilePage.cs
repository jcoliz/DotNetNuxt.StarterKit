using Microsoft.Playwright;

namespace DotNetNuxt.StarterKit.Tests.Functional.Pages;

/// <summary>
/// Represents the Profile page.
/// </summary>
public class ProfilePage : BasePage
{
    private const string UrlPath = "/identity";

    public ProfilePage(IPage _page) : base(_page)
    {
    }

    /// <summary>
    /// Navigates to the Profile page via address bar.
    /// </summary>
    public async override Task<IResponse?> NavigateToUrlAsync()
    {
        var result = await Page!.GotoAsync(UrlPath);
        await WaitForPageReadyAsync();
        return result;
    }

    public override Task<bool> IsAtAsync() => IsAtPathAsync(UrlPath);

    /// <summary>
    /// Waits for the Profile page header to be visible.
    /// </summary>
    public override Task WaitForPageReadyAsync(float timeout = 5000)
    {
        return PageTitle.GetByText("Profile", new() { Exact = true }).WaitForAsync(
            new() { State = WaitForSelectorState.Visible, Timeout = timeout });
    }
}
