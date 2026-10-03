using DotNetNuxt.StarterKit.Tests.Functional.Pages;
using Gherkin.Generator.Utils;
using jcoliz.FunctionalTests;
using Microsoft.Playwright;

namespace DotNetNuxt.StarterKit.Tests.Functional.Steps;

/// <summary>
/// Step definitions for site launch, page navigation, page state, and
/// page-level assertions (loaded OK, redirected, screenshots).
/// </summary>
public class NavigationSteps(IBaseStepCapabilities context) : BuiltInSteps((context as FunctionalTest)!)
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
        var result = await context.Page.GotoAsync("/", new() { WaitUntil = WaitUntilState.NetworkIdle });
        context.ObjectStore.Add(result!);

        var homePage = context.GetOrCreatePage<HomePage>();
        await homePage.WaitForPageReadyAsync();
    }

    #endregion

    #region Page Navigation

    /// <summary>
    /// When user navigates to any page
    /// </summary>
    [Given("user is on the {name} page")]
    [When("user navigates to {name} page")]
    [When("user navigates to the {name} page")]
    [When("user visits the {name} page")]
    [Provides("CurrentPage", "the page object which the user is currently working with in the test context")]
    public async Task UserNavigatesToAnyPage(string name)
    {
        BasePage model = name switch
        {
            "Home" => context.GetOrCreatePage<HomePage>(),
            "Weather" => context.GetOrCreatePage<WeatherPage>(),
            _ => throw new NotImplementedException($"Navigation to page '{name}' is not implemented.")
        };

        context.ObjectStore.Add("CurrentPage", model);
        var result = await model.NavigateToUrlAsync();
        context.ObjectStore.Add(result!);
    }

    #endregion

    #region Assertions

    /// <summary>
    /// Then page loaded ok
    /// </summary>
    [Then("page loaded ok")]
    public new Task ThenPageLoadedOk() => base.ThenPageLoadedOk();

    /// <summary>
    /// Then {count} forecasts are visible
    /// </summary>
    [Then("{count} forecasts are visible")]
    public async Task ResultsHasBodyRows(int count)
    {
        var pageModel = context.ObjectStore.Get<WeatherPage>();

        var actual = await pageModel.Forecasts.CountAsync();

        Assert.That(actual, Is.EqualTo(count), $"Expected {count} forecasts, but found {actual}.");
    }

    #endregion
}