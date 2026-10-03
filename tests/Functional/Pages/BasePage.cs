using System.Text.RegularExpressions;
using jcoliz.FunctionalTests;
using Microsoft.Playwright;

namespace DotNetNuxt.StarterKit.Tests.Functional.Pages;

/// <summary>
/// Represents the common features and functionality shared by all page objects.
/// </summary>
public class BasePage(IPage _page) : PageObjectModel(_page)
{
    #region Properties

    /// <summary>
    /// Current page under test.
    /// </summary>
    public IPage? Page { get; set; } = _page;

    /// <summary>
    /// Page header locator.
    /// </summary>
    public ILocator Header => Page!.Locator("#PageHeader");
    /// <summary>
    /// Page title locator.
    /// </summary>
    public ILocator PageTitle => Header.Locator("h1");
    /// <summary>
    /// Page subtitle locator.
    /// </summary>
    public ILocator PageSubTitle => Header.GetByTestId("Subtitle");
    /// <summary>
    /// Problem details section locator.
    /// </summary>
    public ILocator ProblemDetails => Page!.GetByTestId("problem-details-viewer");
    /// <summary>
    /// Problem details detail display locator.
    /// </summary>
    public ILocator ProblemDetailsDetailDisplay => ProblemDetails.GetByTestId("detail-display");

    #endregion

    #region Navigation

    /// <summary>
    /// Navigate to this page using the browser address bar
    /// </summary>
    public virtual Task<IResponse?> NavigateToUrlAsync() => throw new NotImplementedException();

    #endregion

    #region Page State

    /// <summary>
    /// Waits for the page to be ready
    /// </summary>
    public virtual Task WaitForPageReadyAsync(float timeout = 5000) => throw new NotImplementedException();

    #endregion

    /// <summary>
    /// Gets the text content of the problem details section.
    /// Waits for the problem details to be visible before retrieving the text.
    /// </summary>
    public async Task<string> GetProblemDetailsTextAsync()
    {
        await ProblemDetails.WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible });
        return await ProblemDetailsDetailDisplay.InnerTextAsync();
    }

    #region API Helpers

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
