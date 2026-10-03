using System.Collections.Generic;
using System.Text.RegularExpressions;
using jcoliz.FunctionalTests;
using Microsoft.Playwright;

namespace DotNetNuxt.StarterKit.Tests.Functional.Pages;

/// <summary>
/// Represents the common features and functionality shared by all page objects.
/// </summary>
public class BasePage(IPage _page): PageObjectModel(_page)
{
    #region Properties

    /// <summary>
    /// Gets a value.
    /// </summary>
    public IPage? Page { get; set; } = _page;

    /// <summary>
    /// Executes Locator.
    /// </summary>
    public ILocator Header => Page!.Locator("#PageHeader");
    /// <summary>
    /// Executes Locator.
    /// </summary>
    public ILocator PageTitle => Header.Locator("h1");
    /// <summary>
    /// Executes GetByTestId.
    /// </summary>
    public ILocator PageSubTitle => Header.GetByTestId("Subtitle");
    /// <summary>
    /// Executes GetByTestId.
    /// </summary>
    public ILocator ProblemDetails => Page!.GetByTestId("problem-details-viewer");
    /// <summary>
    /// Executes GetByTestId.
    /// </summary>
    public ILocator ProblemDetailsDetailDisplay => ProblemDetails.GetByTestId("detail-display");

    #endregion

    #region Navigation

    /// <summary>
    /// Navigate to this page using the browser address bar
    /// </summary>
    public virtual Task<IResponse?> NavigateToUrlAsync() => throw new NotImplementedException();

    /// <summary>
    /// Navigate to this page using the browser address bar, but don't confirm successful
    /// </summary>
    public virtual Task<IResponse?> TryNavigateToUrlAsync() => throw new NotImplementedException();

    /// <summary>
    /// Executes ReloadPageAsync.
    /// </summary>
    public async override Task ReloadPageAsync()
    {
        await Page!.ReloadAsync();
        await WaitForPageReadyAsync();
    }

    #endregion

    #region Page State

    /// <summary>
    /// Waits for the page to be ready
    /// </summary>
    public virtual Task WaitForPageReadyAsync(float timeout = 5000) => throw new NotImplementedException();

    /// <summary>
    /// Determines whether we are currently at this page, by checking for the presence of a unique element or other heuristic. Used to determine whether navigation was successful.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public virtual Task<bool> IsAtAsync() => throw new NotImplementedException();

    /// <summary>
    /// Executes WaitUntilLoaded.
    /// </summary>
    public async Task WaitUntilLoaded()
    {
        await Page!.GetByTestId("BaseSpinner").WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Hidden });
    }

    #endregion

    /// <summary>
    /// Executes GetProblemDetailsTextAsync.
    /// </summary>
    public async Task<string> GetProblemDetailsTextAsync()
    {
        await ProblemDetails.WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible });
        return await ProblemDetailsDetailDisplay.InnerTextAsync();
    }

    #region API Helpers

    // TODO: Work out duplication with base functional test versions of these!
    /// <summary>
    /// Executes WaitForApi.
    /// </summary>
    public async Task WaitForApi(Func<Task> action, string? endpoint = null)
    {
        var response = await Page!.RunAndWaitForResponseAsync(action, endpoint ?? "/api/**");
        TestContext.Out.WriteLine("API request {0}", response.Url);
        Assert.That(response!.Ok, Is.True);
    }

    /// <summary>
    /// Executes an action and waits for a matching API response (regex variant)
    /// </summary>
    /// <param name="action">Action that triggers the API call</param>
    /// <param name="regex">Regex pattern to match the API endpoint URL</param>
    public new async Task WaitForApi(Func<Task> action, Regex regex)
    {
        await base.WaitForApi(action, regex);
    }

    #endregion
}
