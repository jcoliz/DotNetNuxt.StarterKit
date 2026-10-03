# Writing Step Definitions

Step definitions are the glue between the Gherkin in [`Features/`](../Features/) and the C#
that actually drives the browser. Every line of a scenario resolves to exactly one step method, and
the generator emits a direct call to it — see [architecture.md](./architecture.md) for the pipeline.

This guide covers how to write and organize those methods. For *authoring the Gherkin* that calls
them, see [writing-features.md](./writing-features.md).

## Contents

- [Step Class Anatomy](#step-class-anatomy)
- [Attribute Binding](#attribute-binding)
- [Multiple Bindings](#multiple-bindings)
- [Using the ObjectStore](#using-the-objectstore)
- [Using Page Objects](#using-page-objects)
- [Using TestControlClient](#using-testcontrolclient)
- [Cleanup Pattern](#cleanup-pattern)
- [Assertions](#assertions)
- [Async Conventions](#async-conventions)
- [Example Walkthrough](#example-walkthrough)
- [Checklist](#checklist)

## Step Class Anatomy

Step classes live in [`Steps/`](../Steps/), one file per class, named `{Area}Steps.cs`. The shape is
always the same:

```csharp
using Gherkin.Generator.Utils;
using ListsWebApp.Tests.Functional.Infrastructure;
using ListsWebApp.Tests.Functional.Pages;

namespace ListsWebApp.Tests.Functional.Steps;

/// <summary>
/// Step definitions for item-level CRUD interactions and result assertions
/// shared across Browse and Manage views.
/// </summary>
public class ItemSteps(ITestCapabilitiesProvider context)
{
    #region Actions
    // ... [When] steps
    #endregion

    #region Assertions
    // ... [Then] steps
    #endregion
}
```

### Rules

| Rule | Why |
| --- | --- |
| Use a **primary constructor** taking `ITestCapabilitiesProvider context` | The generator instantiates step classes with `new(this)`, passing the test fixture. A parameterless or differently-shaped constructor will not compile. |
| The class must be `public` and non-abstract | The generated test class holds it as a `protected` property. |
| Put it in namespace `ListsWebApp.Tests.Functional.Steps` | This namespace is emitted into every generated file's using list. |
| Add a class-level `<summary>` describing the area | This is the only place a reader learns what the class is *for*. |

### Why `ITestCapabilitiesProvider` and not `FunctionalTest`

[`ITestCapabilitiesProvider`](../Infrastructure/FunctionalTestBase.cs) extends the base library's
`IBaseStepCapabilities` with this project's one addition:

```csharp
public interface ITestCapabilitiesProvider : IBaseStepCapabilities
{
    public TestControlClient TestControlClient { get; }
}
```

`IBaseStepCapabilities` gives you everything else a step needs:

| Member | Purpose |
| --- | --- |
| `IPage Page` | Raw Playwright page. **Never use this from a step** — go through a page object. |
| `ObjectStore ObjectStore` | Shared state between steps. |
| `T GetOrCreatePage<T>()` | Get or lazily construct a cached page object. |
| `void AddCleanupAction(string key, Func<Task> action)` | Register teardown work. |
| `string? TargetEnvironment` | The `environment` runsettings parameter, if set. |

Depending on the interface — rather than the concrete `FunctionalTestBaseV4` — keeps step classes
decoupled from NUnit and Playwright fixture plumbing.

### How the generator wires it up

You never construct a step class yourself. The generated fixture does it lazily, one instance per
test, for exactly the classes that test's scenarios reference:

```csharp
protected AuthenticationSteps AuthenticationSteps => _theAuthenticationSteps ??= new(this);
protected TestControlSteps TestControlSteps => _theTestControlSteps ??= new(this);
protected NavigationSteps NavigationSteps => _theNavigationSteps ??= new(this);

private AuthenticationSteps? _theAuthenticationSteps;
private TestControlSteps? _theTestControlSteps;
private NavigationSteps? _theNavigationSteps;
```

**Consequence:** step classes are cheap and stateless-by-convention. Do not put mutable instance
fields on them expecting them to persist — put shared state in the `ObjectStore` instead. (The one
sanctioned exception is a `private static` guard flag, as in
`TestControlSteps.isDataInitialized`, which deliberately spans the whole assembly run.)

### Region organization

Group methods by role, and name regions for what the reader is looking for. Existing practice:

| Class | Regions |
| --- | --- |
| `AuthenticationSteps` | Login, Logout, Register Navigation |
| `TestControlSteps` | Test Mode, Sample Data, Invitations, Cleanup |
| `NavigationSteps` | Site Launch, Environments, Page Navigation, Page State |
| `RegisterSteps` | Given: Invitation Setup, When: Navigation, When: Registration Actions, Then: Outcome Assertions |
| `ItemSteps`, `ManageSteps`, `StoreFilterSteps` | Actions, Assertions |
| `BrowseSteps` | Preconditions, Assertions |

Two acceptable schemes: **by workflow** (Login / Logout) or **by Gherkin role** (Actions /
Assertions). Small classes — `ListsSteps`, `SearchSteps` — skip regions entirely. Don't add regions
to a class with five methods.

### Inheriting from `BuiltInSteps`

The base library ships a handful of framework-level steps in `BuiltInSteps`: launching the site,
reloading the current page, taking screenshots, and environment gating. Only `NavigationSteps`
inherits from it, and it must adapt the interface:

```csharp
public class NavigationSteps(ITestCapabilitiesProvider context)
    : BuiltInSteps((context as FunctionalTest)!)
```

`BuiltInSteps` members are `protected`, so inheriting is how you expose them as bound steps.
**Only inherit if you are wrapping a built-in.** Everything else should be standalone.

## Attribute Binding

Bind a method to Gherkin text with `[Given]`, `[When]`, or `[Then]` from
`Gherkin.Generator.Utils`. The attribute text is the step **without** its keyword:

```csharp
[When("user expands the item create pane")]
public async Task UserExpandsTheItemCreatePane()
{
    var pageModel = context.GetOrCreatePage<ViewsPage>();
    await pageModel.ExpandItemCreatePane();
}
```

Matching is **case-insensitive** and anchored to the whole step text — partial matches do not count.

### Parameter capture

Wrap a segment in braces to capture it:

```csharp
[When("user navigates to {name} page")]
public async Task UserNavigatesToAnyPage(string name)
```

```gherkin
When user navigates to Manage page
```

generates `await NavigationSteps.UserNavigatesToAnyPage("Manage");`

Rules that matter:

1. **All captured parameters are `string`.** Parse inside the method if you need something else —
   e.g. `Guid.Parse(Key)` in `TestControlSteps.ListKeyIsEmpty`.
2. **Binding is positional, not by name.** The placeholder name is documentation only. `{Code}`
   happily binds to a parameter named `code`:
   ```csharp
   [When("user navigates to Register page with code {Code}")]
   public async Task UserNavigatesToRegisterPageWithCode(string code)
   ```
   Still — match the names. Mismatches read as bugs.
3. **A placeholder matches either a quoted string or a single unquoted token.** The generator
   compiles `{x}` to the regex group `((?:"[^"]*"|\S+))`. So this works:
   ```gherkin
   When changing the bulk store to "New Property"
   ```
   and this silently fails to match, because `New Property` is two tokens:
   ```gherkin
   When changing the bulk store to New Property
   ```
   **Always quote multi-word arguments.** The surrounding quotes are stripped before the value
   reaches your method.
4. **Scenario Outline placeholders pass through as variables.** `<Property>` in a step becomes the
   generated method's `string Property` parameter, fed by `[TestCase(...)]` attributes.

### Naming methods

The method name is what appears in generated code and in test-explorer stack traces, so make it
readable. Convention is a PascalCase restatement of the step text:

| Attribute text | Method name |
| --- | --- |
| `"selecting the first item"` | `SelectingTheFirstItem` |
| `"it is still marked selected"` | `ItIsStillMarkedSelected` |
| `"user clears the search"` | `UserClearsTheSearch` |
| `"applying bulk changes"` | `ApplyingBulkChanges` |
| `"changing the bulk store to {string1}"` | `ChangingTheBulkStoreTo` |

When a method carries several bindings, name it after the *primary* one and let the others alias in.

### Documenting the binding

Every step method gets an XML `<summary>` that restates the step **with** its keyword. This is the
line a reader sees on hover from the generated code:

```csharp
/// <summary>
/// When user edits the first item
/// </summary>
```

## Multiple Bindings

One method may carry any number of step attributes, across any keywords. This is the primary tool
for keeping feature files readable without duplicating implementation.

```csharp
/// <summary>
/// Upload a single item (OneItem.yaml) to the test tenant's list.
/// </summary>
[Given("one existing item in the current list")]
[Given("one existing item")]
public async Task OneExistingItem()
{
    await context.TestControlClient.UploadItemsAsync("OneItem");
    AddCleanupAction();
}
```

Both phrasings resolve to the same call. Use this for:

**Grammatical variants** — so the Gherkin reads naturally in whatever position it lands:

```csharp
[Given("user is on the {name} page")]
[When("user navigates to {name} page")]
[When("user navigates to the {name} page")]
[When("user visits the {name} page")]
public async Task UserNavigatesToAnyPage(string name)
```

**Tense shifts across keywords** — the same action is a `When` when it's the subject of the test and
a `Given` when it's setup:

```csharp
[Given("current user is logged out")]
[When("user logs out")]
public async Task UserLogsOut()
```

**Terse and explicit forms of the same precondition:**

```csharp
[Given("test mode has started")]
[Given("test data has been cleared")]
public async Task TestModeHasStarted()
```

### Delegation instead of duplication

When two steps are *almost* the same, don't add another attribute — delegate with an
expression-bodied method:

```csharp
[Given("user searches for the unique term")]
public Task UserSearchesForTheUniqueTerm() => SearchForTerm("Unique Match");

[Given("user searches for a term with zero matches")]
public Task UserSearchesForATermWithZeroMatches() => SearchForTerm("No Matching Items");
```

`SearchForTerm` here is a plain private helper with no attributes. **Helper methods without step
attributes are invisible to the generator** — use them freely.

### Composing steps

A step may call another step class directly. `AuthenticationSteps.GivenUserLoggedIn` builds a
compound precondition this way:

```csharp
/// <remarks>
/// Compound step: launches site, verifies it loaded, then logs in.
/// </remarks>
[Given("user is logged in as {userId}")]
[Given("user has logged in as {userId}")]
public async Task GivenUserLoggedIn(string userId)
{
    // Launch the site
    var navigationSteps = new NavigationSteps(context);
    await navigationSteps.UserLaunchesSite();

    // Log in
    await WhenUserLogsIn(userId);
}
```

Construct the other class with `new OtherSteps(context)` — passing the same `context` is what keeps
the `ObjectStore` and page cache shared. Note the `<remarks>` calling out that it's compound; a
reader debugging a failure needs to know the step does more than its name says.

## Using the ObjectStore

`ObjectStore` is the only sanctioned way to pass state between steps. Steps run as independent
method calls in the generated fixture — there are no local variables spanning them.

The generator now also understands explicit shared-state annotations on step methods and test
bases. Use those to declare the contract; use `ObjectStore` to move the actual runtime values.

### API

```csharp
store.Add<T>(T obj);              // key = typeof(T).Name
store.Add<T>(string key, T obj);  // explicit key
T Get<T>();                       // by type name
T Get<T>(string key);             // by explicit key
bool Contains<T>();
bool Contains<T>(string key);
```

`Get` throws `KeyNotFoundException` when the key is missing. That is intentional — a missing key
means an upstream step didn't run, and you want a loud failure at the point of use.

### When to use which overload

**Type-keyed** when there is only ever one instance of that type in flight:

```csharp
context.ObjectStore.Add(pageModel.ItemSelectedChecks.First);   // stores as "ILocator"
var it = context.ObjectStore.Get<ILocator>();
```

**String-keyed** when you need several of the same type, or the key is a domain concept:

```csharp
context.ObjectStore.Add("DefaultUser", userDetails);
context.ObjectStore.Add("InvitationCode", invite);
context.ObjectStore.Add("ResetCode", "INVALID_CODE");
context.ObjectStore.Add<BasePage>("CurrentPage", pageModel);
```

### Established keys

These keys are effectively part of the project's contract. Reuse them; don't invent parallel names.

| Key | Type | Provided by | Meaning |
| --- | --- | --- | --- |
| `Tester` | `UserDetails` | `FunctionalTestBaseV4.SetUp` | The pre-provisioned test account from runsettings. Always present. |
| `DefaultUser` | `UserDetails` | `FunctionalTestBaseV4.SetUp`, then overwritten by `TestControlSteps` when a fresh account is created | The account the current scenario acts as. |
| `CurrentPage` | `BasePage` | `NavigationSteps.UserNavigatesToAnyPage` | The page most recently navigated to, so `reloading the current page` reloads the right one. |
| `InvitationCode` | `string` | `RegisterSteps`, `TestControlSteps` | Invitation GUID under test. |
| `ResetCode` | `string` | `TestControlSteps`, `PasswordSteps` | Password-reset token extracted from a notification. |
| `NewPassword` | `string` | `PasswordSteps` | The password the user changed to. |

### `Requires`, `Provides`, and `BaseProvides`

Because `ObjectStore` dependencies are invisible to the compiler, declare them on the step so the
generator can surface the flow in generated code.

```csharp
/// <summary>
/// Given user is logged in with that account
/// </summary>
[Requires("DefaultUser", "a UserDetails object containing the username, email, and password of the created test user account.")]
[Given("user is logged in with that account")]
public Task UserIsLoggedInWithThatAccount()
    => WhenUserLogsIn("DefaultUser");
```

| Attribute | Meaning |
| --- | --- |
| `[Requires("<key>")]` | The step reads this state before it can run. The generator warns when the state is missing earlier in the scenario. |
| `[Provides("<key>")]` | The step writes this state after it completes successfully. |
| `[BaseProvides("<key>")]` | The generated test base provides this state before `Background:` and scenario steps run. |

Use the optional description to explain the state in test-author language, not implementation
language. When the state is type-derived, use `<see cref="..."/>` in the prose.

```csharp
/// <summary>
/// When item is selected
/// </summary>
[Provides("SelectedCheckbox", "<see cref=\"ILocator\"/> in ObjectStore — the selected item's checkbox locator, used by later 'it is still marked selected' assertions.")]
[When("item is selected")]
public async Task ItemIsSelected()
{
    var checkbox = await context.GetOrCreatePage<ManagePage>().ResolveSelectedCheckboxAsync();
    context.ObjectStore.Add("SelectedCheckbox", checkbox);
}
```

```csharp
[GeneratedTestBase(UseNamespace = "ListsWebApp.Tests.Functional.Features")]
[BaseProvides("Tester", "primary seeded test user account available before step execution")]
[BaseProvides("DefaultUser", "default seeded test user used for functional login")]
public abstract class FunctionalTestBaseV4 : FunctionalTest, ITestCapabilitiesProvider
{
}
```

`ObjectStore` prose comments are legacy. Prefer the attributes as the source of truth for the
generator, and remove the prose once the attribute carries the state contract cleanly.

> **Note:** some older methods still have prose comments next to the attributes. Feel free to remove
> those next time you're in the file.

These annotations are the source material for the generator's shared-state comments, warnings, and
step catalog output, so keeping them accurate has downstream value.

## Using Page Objects

**Steps do not touch Playwright.** A step method's job is to pick the right page object, call one or
two of its methods, and assert on what it returns. All locators, waiting, and DOM interaction belong
in [`Pages/`](../Pages/) — see [writing-pages.md](./writing-pages.md).

### `GetOrCreatePage<T>()`

```csharp
[When("applying bulk changes")]
public async Task ApplyingBulkChanges()
{
    var pageModel = context.GetOrCreatePage<ManagePage>();
    await pageModel.ApplyBulkChangesAsync();
}
```

The base library implements this as a cache over the `ObjectStore`:

```csharp
public T GetOrCreatePage<T>() where T : PageObjectModel
{
    if (_objectStore.Contains<T>())
    {
        return _objectStore.Get<T>();
    }

    var page = (T)Activator.CreateInstance(typeof(T), Page)!;
    _objectStore.Add(page);
    return page;
}
```

Two things follow from this:

1. **Page objects are constructed reflectively via a single-`IPage` constructor.** Every page class
   must offer one, or `GetOrCreatePage<T>` throws at runtime, not compile time.
2. **One instance per type per test.** Calling `GetOrCreatePage<ManagePage>()` from five different
   steps returns the same object, so any state the page object caches survives across steps.

Always assign to a local named `pageModel`. It's the house style and makes step bodies scannable.

### `Get<T>()` for polymorphic pages

Some steps must work against whichever items view the scenario landed on — Browse or Manage. Those
steps read the base type out of the store rather than creating a concrete one:

```csharp
[Requires("ItemsViewBasePage", "the page object for the current items-view page, which is required to perform the edit")]
[When("user edits the first item")]
public async Task UserEditsTheFirstItem()
{
    var pageModel = context.ObjectStore.Get<ItemsViewBasePage>();
    await pageModel.EditFirstItemAsync();
}
```

This only works because `NavigationSteps` registers the concrete page under the base type when it
navigates. Hence the mandatory `[Requires]` annotation — the generated code will surface a compiler
error if no navigation step ran first.

**Rule of thumb:** use `GetOrCreatePage<T>()` when the step knows exactly which page it needs; use
`ObjectStore.Get<T>()` when the step must operate on whatever page the scenario is currently on.

## Using TestControlClient

Setting up preconditions through the UI is slow and fragile. `Given` steps should instead push state
straight into the backend through
[`TestControlClient`](../Infrastructure/TestControlClient.cs), reached via
`context.TestControlClient`. It authenticates lazily on first use and resolves the test tenant from
the login response.

| Method | Backend call | Use for |
| --- | --- | --- |
| `BeginAsync(Guid? withList = null)` | `POST /api/tenant/{list}/Test/begin` | Enter test mode: clears items and deletes `__TEST__` users. The reset primitive. |
| `SeedAsync()` | `POST /api/tenant/{tenant}/Test/seed` | Load the standard demo data set. |
| `UploadItemsAsync(string filename, Guid? toList = null)` | `POST /api/tenant/{list}/Items/items` | Upload a YAML file from [`SampleData/`](../SampleData/). Pass the bare name — `"OneItem"`, not `"OneItem.yaml"`. |
| `CreateInvitationAsync(string? email, int? status, bool expired, string[]? actions)` | `PUT /api/tenant/{tenant}/Test/invite` | Mint an invitation in a specific state for registration tests. |
| `CreateUserAsync(string[]? actions)` | `POST /api/tenant/{tenant}/Test/user` | Create a throwaway `__TEST__` account with a given entitlement set. |
| `GetNotificationsAsync(string userName)` | `GET /api/tenant/{tenant}/Test/notifications` | Read the outbound email queue — how password-reset codes are recovered. |

### Typical shape

```csharp
/// <summary>
/// Upload three items (ThreeItems.yaml) to the test tenant's list.
/// </summary>
[Given("three existing items")]
public async Task ThreeExistingItems()
{
    await context.TestControlClient.UploadItemsAsync("ThreeItems");
    AddCleanupAction();
}
```

Three obligations for every data-creating step:

1. `<summary>` names the YAML file, so a reader can find the fixture without opening the client.
2. Register cleanup (see below).
3. Keep the step text about *outcome* (`three existing items`), not mechanism (`upload
   ThreeItems.yaml`). The feature file should not know YAML exists.

Available data files and their contents are catalogued in [sample-data.md](./sample-data.md).

### Guarding expensive setup

`SeedAsync()` is slow, and the demo data only needs loading once for the whole run. `TestControlSteps`
guards it with a static flag:

```csharp
[Given("user initialized tests with seed data, if no data exists")]
public Task UserInitializedTestsWithSeedDataIfNoDataExists()
{
    if (isDataInitialized)
        return Task.CompletedTask;
    isDataInitialized = true;

    return TestModeBeginWithSeedData();
}
private static bool isDataInitialized = false;
```

This is safe only because the assembly is `[assembly: NonParallelizable]`. Don't reach for static
state elsewhere.

## Cleanup Pattern

Any step that creates backend state must schedule its own removal, so the next test starts clean
regardless of whether the current one passed.

`IBaseStepCapabilities.AddCleanupAction` takes a key and an async action:

```csharp
void AddCleanupAction(string key, Func<Task> action);
```

The key exists to make registration **idempotent** — `TryAdd` semantics mean calling it from five
different `Given` steps in one scenario registers the cleanup once. This is why every sample-data
step can call it unconditionally without worrying about duplicates.

`TestControlSteps` wraps this in a private helper:

```csharp
public async Task Cleanup()
{
    await context.TestControlClient.BeginAsync();
}

public async Task CleanupWithKey(string key)
{
    await context.TestControlClient.BeginAsync(Guid.Parse(key));
}

/// <summary>
/// Add cleanup action to context, to ensure Cleanup is called after the test completes.
/// </summary>
private void AddCleanupAction(string? key = null)
{
    if (key != null)
    {
        // If a key is provided and exists in the object store, use it to create a unique cleanup action key
        context.AddCleanupAction($"testcontrol-cleanup-{key}", () => CleanupWithKey(key));
    }
    else
    {
        context.AddCleanupAction(key ?? "testcontrol-cleanup", Cleanup);
    }
}
```

The default list gets the key `testcontrol-cleanup`; a scenario touching a specific list gets
`testcontrol-cleanup-{listGuid}`, so multi-list scenarios clean up every list they touched.

### Choosing a key

| Situation | Key |
| --- | --- |
| One shared resource, cleaned the same way every time | A constant: `"testcontrol-cleanup"` |
| Per-instance resource | Interpolate the identity: `$"testcontrol-cleanup-{key}"` |

Never use a random or per-call unique key — that defeats deduplication and runs the same teardown
repeatedly.

### Declarative cleanup from the feature file

A scenario can also request teardown in Gherkin, which is clearer when the intent is explicit:

```gherkin
Given afterward the list is cleared
```

That binds to a step whose entire body is a registration:

```csharp
[Given("afterward test mode is ended")]
[Given("afterward default list is cleared")]
[Given("afterward the list is cleared")]
public Task TestModeIsEnded()
{
    AddCleanupAction();
    return Task.CompletedTask;
}
```

Note it registers rather than executes — nothing is torn down until the test finishes.

## Assertions

`Then` steps assert; `Given` and `When` steps do not. Use NUnit 4 constraint syntax with a message
that explains the *expectation*, since the failure text is often all a CI reader sees:

```csharp
[Then("it is still marked selected")]
public async Task ItIsStillMarkedSelected()
{
    var it = context.ObjectStore.Get<ILocator>();
    var pageModel = context.GetOrCreatePage<ManagePage>();

    var checkbox = await pageModel.ResolveSelectedCheckboxAsync(it);
    var isChecked = await checkbox.IsCheckedAsync();
    Assert.That(isChecked, Is.True, "Expected the item to still be marked as selected, but it was not.");
}
```

Guidelines:

- Always the three-argument form: `Assert.That(actual, constraint, message)`.
- Wrap related assertions in `Assert.Multiple(() => { ... })` so one failure doesn't mask the rest.
- Gather all state *before* asserting, so `Assert.Multiple` reports on complete data.
- `Assert.Ignore` is legitimate for environment gating — see
  `BuiltInSteps.GivenNotRunningAgainstNamedEnvironment`. `Assert.Fail` is for genuinely broken setup.
- `NUnit.Analyzers` is enabled; heed its warnings about classic-model or misused constraints.

## Async Conventions

Every step method returns `Task`. The generator always emits `await`, so a `void` method will not
compile against the generated call site.

**Real async work** — the common case:

```csharp
[When("clicking bulk delete")]
public async Task ClickingBulkDelete()
{
    var pageModel = context.GetOrCreatePage<ManagePage>();
    await pageModel.ClickBulkDeleteAsync();
}
```

**Pure delegation** — drop `async` and return the inner task:

```csharp
[Given("user searches for the unique term")]
public Task UserSearchesForTheUniqueTerm() => SearchForTerm("Unique Match");
```

**No async work at all** — return `Task.CompletedTask`, don't fake it with `await Task.CompletedTask`:

```csharp
[Given("an invalid password reset code")]
public Task AnInvalidPasswordResetCode()
{
    context.ObjectStore.Add("ResetCode", "INVALID_CODE");
    return Task.CompletedTask;
}
```

**Documentation-only steps** are a valid pattern. Some preconditions are satisfied by server
configuration rather than by anything the test does; bind them anyway so the Gherkin can state the
precondition, and explain in the body why there's nothing to do:

```csharp
[Given("user has access to {List}")]
public Task UserHasAccessToList(string List)
{
    // NOTE: This is set up by default seeders in appsettings.json,
    // so it will be available in ANY environment, regardless of
    // local configuration.

    return Task.CompletedTask;
}
```

### Global usings

[`GlobalUsings.cs`](../GlobalUsings.cs) exports exactly one namespace assembly-wide:

```csharp
global using NUnit.Framework;
```

**`Microsoft.Playwright` is deliberately *not* here.** It was removed so that any file needing to
name a Playwright type has to write the import itself — which makes the dependency visible in the
diff and in review. Import what you actually need:

| Using | When |
| --- | --- |
| `Gherkin.Generator.Utils;` | Always — this is where `[Given]`/`[When]`/`[Then]` live. |
| `ListsWebApp.Tests.Functional.Infrastructure;` | Always — `ITestCapabilitiesProvider`. |
| `ListsWebApp.Tests.Functional.Pages;` | When using page objects. |
| `ListsWebApp.Tests.Functional.Models;` | When touching `UserDetails` and friends. |
| `jcoliz.FunctionalTests;` | Only when referencing base-library types such as `PageObjectModel` or `FunctionalTest`. |
| `Microsoft.Playwright;` | **Treat as a smell.** See below. |

> **`using Microsoft.Playwright;` in a step file is a design question, not a formality.** Ask what
> the step is naming a Playwright type *for*. If the answer is "to drive the browser," that code
> belongs in a page object — see
> [Design Principle 4](./writing-pages.md#4-playwright-belongs-in-page-objects). The legitimate
> reason is moving an opaque `ILocator` or `IResponse` through the `ObjectStore`.

At present exactly three step files carry the import — `BrowseSteps`, `ManageSteps`, and
`NavigationSteps` — against sixteen files in [`Pages/`](../Pages/) and [`Components/`](../Components/),
where it belongs. If you are adding a fourth, justify it.

## Example Walkthrough

Suppose `Views.feature` needs a new step: after marking an item as a favorite, assert the favorites
count in the sidebar. Using [`AuthenticationSteps`](../Steps/AuthenticationSteps.cs) as the model:

### 1. Write the Gherkin first

```gherkin
Scenario: Favoriting an item updates the sidebar count
    Given one existing item
    And user is on the Browse page
    When user favorites the first item
    Then the favorites count is "1"
```

### 2. Build once and read the diagnostic

The generator emits `GHERKIN004` warnings for unmatched steps and stubs them out, marking the
scenario `[Explicit("steps_in_progress")]`. That warning list is your to-do list — it tells you
exactly which bindings are missing, and you get the suggested method signature for free from the
generated stub.

### 3. Find the right class — don't create a new one

Favoriting is item CRUD, so it belongs in `ItemSteps`, not a new `FavoritesSteps`. **Add to an
existing class unless the area genuinely has no home.** A new step class is justified when it maps to
a distinct page or workflow, not merely a distinct scenario.

### 4. Put the browser work in the page object

The step doesn't know about locators. Add to `ViewsPage` (see
[writing-pages.md](./writing-pages.md)):

```csharp
public async Task FavoriteFirstItemAsync() { /* locator work + waiting */ }
public Task<string> GetFavoritesCountAsync() { /* locator work */ }
```

### 5. Add the action step to the `Actions` region

```csharp
/// <summary>
/// When user favorites the first item
/// </summary>
/// <remarks>
/// REQUIRES: <see cref="ItemsViewBasePage"/> in ObjectStore (registered by navigation steps)
/// </remarks>
[When("user favorites the first item")]
public async Task UserFavoritesTheFirstItem()
{
    var pageModel = context.ObjectStore.Get<ItemsViewBasePage>();
    await pageModel.FavoriteFirstItemAsync();
}
```

It reads the polymorphic base page, because favoriting works from both Browse and Manage — so it
carries the `REQUIRES:` note.

### 6. Add the assertion step to the `Assertions` region

```csharp
/// <summary>
/// Then the favorites count is {count}
/// </summary>
[Then("the favorites count is {count}")]
public async Task TheFavoritesCountIs(string count)
{
    var pageModel = context.GetOrCreatePage<ViewsPage>();
    var actual = await pageModel.GetFavoritesCountAsync();

    Assert.That(actual, Is.EqualTo(count), $"Expected the sidebar to show {count} favorites.");
}
```

The parameter is `string` and compared as a string — no parsing needed, and the failure message
reports what was actually rendered.

### 7. Consider aliases

If a later scenario wants `Given user has favorited the first item`, add it as a second attribute on
the same method rather than writing a near-duplicate:

```csharp
[Given("user has favorited the first item")]
[When("user favorites the first item")]
public async Task UserFavoritesTheFirstItem()
```

### 8. Rebuild and confirm

The `GHERKIN004` warnings should be gone, and the scenario should no longer carry
`[Explicit("steps_in_progress")]`. Check the generated file under
`obj/GeneratedFiles/Gherkin.Generator/` to confirm the call site looks the way you expect — that's
the fastest way to catch a placeholder that failed to match.

The scenario's own `@explicit:wip` tag stays put through all of this. Implementing the step means
the scenario can now *run*; it does not mean it *passes*. Run it with `--filter`, watch it go green,
and only then remove the tag — see
[writing-features.md](./writing-features.md#explicitwip-and-the-generators-automatic-marking-are-two-different-things).

### 9. Confirm the step reads well from the outside

The generated step catalog is the browsable inventory for this repo. It is emitted beside the test
assembly when any test runs, so you do not maintain a separate file in source control. What the
generator cannot invent is the prose, so make sure the `<summary>` and the `[Requires]` /
`[Provides]` /
`[BaseProvides]` annotations say what a reader would need to know.

## Checklist

Before committing a new or changed step:

- [ ] Class uses a primary constructor taking `ITestCapabilitiesProvider context`
- [ ] Method returns `Task` and is `public`
- [ ] `<summary>` restates the step text including its keyword
- [ ] Attribute text is lowercase, keyword-free, and reads naturally in the feature file
- [ ] Multi-word arguments in feature files are quoted
- [ ] Placeholder names match the parameter names they bind to
- [ ] No raw Playwright calls — all DOM work delegated to a page object
- [ ] Any step that depends on scenario state uses `[Requires]`, `[Provides]`, or `[BaseProvides]`
- [ ] Backend state created by the step registers a keyed cleanup action
- [ ] `Then` steps assert with three-argument `Assert.That` and a useful message
- [ ] `Given`/`When` steps contain no assertions
- [ ] Method placed in the appropriate region, or the class is small enough not to need regions
- [ ] Reused an existing step or added an alias instead of writing a near-duplicate
- [ ] Build is clean — no `GHERKIN004` warnings for the new scenarios
- [ ] `<summary>` and state annotations are accurate — they feed the generated catalog and comments

## See Also

- [architecture.md](./architecture.md) — how steps are discovered, matched, and called
- [writing-features.md](./writing-features.md) — authoring the Gherkin that invokes these steps
- [writing-pages.md](./writing-pages.md) — where the Playwright code actually goes
- [`Steps/`](../Steps/) — every step currently available
- [sample-data.md](./sample-data.md) — the YAML fixtures used by `Given` steps
