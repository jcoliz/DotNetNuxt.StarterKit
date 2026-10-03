using DotNetNuxt.StarterKit.Tests.Functional.Pages;
using Gherkin.Generator.Utils;
using jcoliz.FunctionalTests;
using Microsoft.Playwright;

namespace DotNetNuxt.StarterKit.Tests.Functional.Steps;

/// <summary>
/// Step definitions for site launch, page navigation, page state, and
/// page-level assertions (loaded OK, redirected, screenshots).
/// </summary>
public class NavigationSteps(IBaseStepCapabilities context): BuiltInSteps((context as FunctionalTest)!)
{
    #region Site Launch

    /// <summary>
    /// When: User launches site
    /// </summary>
    [When("user launches the site")]
    [When("user navigates to the site index")]
    [Given("user has launched the site")]
    public new async Task UserLaunchesSite()
    {
        var result = await context.Page.GotoAsync("/", new() { WaitUntil = WaitUntilState.NetworkIdle } );
        context.ObjectStore.Add( result! );

        var homePage = context.GetOrCreatePage<HomePage>();
        await homePage.WaitForPageReadyAsync();
    }

    #endregion

    #region Assertions

    /// <summary>
    /// Then page loaded ok
    /// </summary>
    [Then("page loaded ok")]
    public new Task ThenPageLoadedOk() => base.ThenPageLoadedOk();

    #endregion
}
