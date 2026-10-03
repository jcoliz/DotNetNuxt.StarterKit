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
        var model = GetPage(name);

        context.ObjectStore.Add("CurrentPage", model);
        var result = await model.NavigateToUrlAsync();
        context.ObjectStore.Add(result!);
    }

    /// <summary>
    /// When user selects a home page card
    /// </summary>
    [When("user selects the {card} card")]
    public async Task UserSelectsTheCard(string card)
    {
        var model = context.ObjectStore.Get<HomePage>();
        await model.SelectCardAsync(card);
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

    /// <summary>
    /// Then backend version is visible
    /// </summary>
    [Then("backend version is visible")]
    public async Task BackendVersionIsVisible()
    {
        var pageModel = context.ObjectStore.Get<AboutPage>();

        Assert.That(await pageModel.IsBackendVersionVisibleAsync(), Is.True);
    }

    /// <summary>
    /// Then frontend version is visible
    /// </summary>
    [Then("frontend version is visible")]
    public async Task FrontendVersionIsVisible()
    {
        var pageModel = context.ObjectStore.Get<AboutPage>();

        Assert.That(await pageModel.IsFrontendVersionVisibleAsync(), Is.True);
    }

    /// <summary>
    /// Then both versions match
    /// </summary>
    [Then("both versions match")]
    public async Task BothVersionsMatch()
    {
        var pageModel = context.ObjectStore.Get<AboutPage>();

        Assert.That(
            await pageModel.GetBackendVersionAsync(),
            Is.EqualTo(await pageModel.GetFrontendVersionAsync()));
    }

    /// <summary>
    /// Then the user lands on the expected page
    /// </summary>
    [Then("the user lands on the {page} page")]
    public async Task TheUserLandsOnThePage(string page)
    {
        var model = GetPage(page);
        context.ObjectStore.Add("CurrentPage", model);

        await model.WaitForPageReadyAsync();
        Assert.That(await model.IsAtAsync(), Is.True, $"Expected to land on the {page} page.");
    }

    #endregion

    private BasePage GetPage(string name) => name switch
    {
        "Home" => context.GetOrCreatePage<HomePage>(),
        "Weather" => context.GetOrCreatePage<WeatherPage>(),
        "Profile" => context.GetOrCreatePage<ProfilePage>(),
        "About" => context.GetOrCreatePage<AboutPage>(),
        _ => throw new NotImplementedException($"Navigation to page '{name}' is not implemented.")
    };
}