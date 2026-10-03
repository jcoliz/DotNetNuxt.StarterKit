# Writing Feature Files

Feature files are the entry point to the whole test system. Every NUnit test in this project is
generated from a Gherkin scenario — see [architecture.md](./architecture.md) for how that works.
This guide covers how to author `.feature` files that the generator understands.

## Contents

- [File Location and Naming](#file-location-and-naming)
- [Feature File Anatomy](#feature-file-anatomy)
- [Step Ordering](#step-ordering-never-given-or-when-after-a-then)
- [Tagging Conventions](#tagging-conventions)
- [Step Reuse](#step-reuse)
- [Parameterization](#parameterization)
- [Background and Cleanup](#background-and-cleanup)
- [Example Walkthrough](#example-walkthrough)

## File Location and Naming

**Feature files must live in [`Features/V4/`](../Features/V4/).** That directory is the glob in the
`AdditionalFiles` item group, and it is the only input the generator sees:

```xml
<ItemGroup>
  <AdditionalFiles Include="Features\V4\*.feature" />
</ItemGroup>
```

A `.feature` file anywhere else — including directly in [`Features/`](../Features/) — produces no
tests at all. The `*TODO.feature` files in the parent directory are deliberate parking lots for
scenarios not yet ported; they are inert.

### Naming

| Rule | Example |
| --- | --- |
| PascalCase, one word where possible, named for the *area under test* | `Manage.feature`, `Registration.feature`, `Passwords.feature` |
| The file name becomes the generated class name | `Views.feature` → `Views_Feature_Tests` |
| No suffix like `Tests` or `Feature` in the file name | `Lists.feature`, not `ListsFeature.feature` |

Current files: `Views`, `Manage`, `Lists`, `Registration`, `Passwords`, `Smoke`, `NoAccess`,
`Errors`.

Prefer adding scenarios to an existing feature file over creating a new one. Create a new file only
when the area is genuinely distinct — a new file means a new class, and therefore a new `Background`
that must be maintained separately.

## Feature File Anatomy

```gherkin
Feature: (Manage) [User can] manage items in bulk        <- title

The Manage view displays all items with selection checkboxes,    <- description
supports search and filtering, individual item editing, and
bulk operations (edit fields, delete) on selected items.

Background: Setup                                        <- runs before every scenario
    Given user has logged in as "Tester"
    And test mode has started

Rule: Bulk operations on selected items                  <- grouping

Scenario: Select an item, then return later to find it still selected
    Given one existing item
    And user is on the Manage page
    When selecting the first item
    And reloading the page
    Then it is still marked selected
```

### `Feature:` title

Titles follow a consistent shape so the test list reads well:

```
(Area) [Actor Can] goal
```

Real examples:

- `(Manage) [User can] manage items in bulk`
- `(Registration) [Administrator Can] Invite a small number of initial users`
- `(Lists) [User can] switch between multiple Lists`
- `Errors are handled gracefully` — the bracketed prefixes are optional when the area is obvious

The title becomes the `<summary>` XML doc on the generated class.

### Description

Free text between the `Feature:` line and the first `Background:`/`Rule:`/`Scenario:` becomes the
class-level `<remarks>`. This is the place to record scope and non-obvious constraints. From
[Smoke.feature](../Features/V4/Smoke.feature):

```gherkin
Feature: Selected set of pages load successfully

As a developer
I want to verify that the test infrastructure is working and that key pages load correctly

One requirement of this test is that we not have any Background steps, which could complicate the test.
The point of this test is to verify that the most basic functionality (loading pages) is working at all.
So we want to keep the setup as minimal as possible while still covering the top-level scenarios.
```

Scenarios can carry descriptions too, placed between the `Scenario:` line and the first step. These
become `<remarks>` on the generated test method:

```gherkin
Scenario Outline: Single item shown on unfiltered views

These views show all items, so a single item should be shown on each of them.
Other views automatically filter out items, so they will be tested separately below.

    Given one existing item
    When user visits the <View> page
    Then Results contains "Name 01"
```

generates:

```csharp
/// <summary>
/// Single item shown on unfiltered views
/// </summary>
/// <remarks>
/// These views show all items, so a single item should be shown on each of them.
/// Other views automatically filter out items, so they will be tested separately below.
/// </remarks>
[TestCase("Browse")]
[TestCase("Manage")]
[Test]
public async Task SingleItemShownOnUnfilteredViews(string View)
```

Use `#` comments for notes that should *not* appear in generated code — build annotations, TODOs,
and reminders about flakiness:

```gherkin
# This test has been flaky on containers. The screen shot shows sitting on the login page
# Only shop, browse and prepare ever fail
```

### `Rule:`

Rules group related scenarios into a `#region Rule: <name>` in the generated class. They are purely
organizational — they do not affect execution — but they make both the feature file and the
generated code navigable. Use them once a feature file exceeds a handful of scenarios.

```gherkin
Rule: Bulk operations on selected items
...
Rule: Server-scoped bulk selection
...
Rule: Access guards
```

A feature file with no `Rule:` lines is fine for small features
([Errors.feature](../Features/V4/Errors.feature), [Smoke.feature](../Features/V4/Smoke.feature)).

### `Scenario:`

Each becomes one `[Test] public async Task` method. The scenario name is converted to a PascalCase
method name, so **make names descriptive and unique within the file** — that name is what you will
type into `--filter`.

Structure scenarios as Given (arrange) / When (act) / Then (assert), using `And` to continue the
previous keyword. The generator normalizes `And`/`But` to whatever keyword preceded it.

### Step ordering: never `Given` or `When` after a `Then`

**A scenario must move through its keywords in one direction only: all `Given` steps, then all
`When` steps, then all `Then` steps. Once a `Then` appears, no `Given` or `When` may follow it.**

```gherkin
# CORRECT
Scenario: Search narrows the results
    Given three existing items
    And user is on the Browse page
    When searching for "Name 03"
    Then Results has 1 body row
    And Results contains "Name 03"
```

```gherkin
# WRONG - a second act/assert cycle bolted on
Scenario: Search then clear
    Given three existing items
    And user is on the Browse page
    When searching for "Name 03"
    Then Results has 1 body row
    When user clears the search     <- not allowed
    Then Results has 3 body rows
```

Use `And` to extend the current phase; never restart an earlier phase.

**Why this matters.** A scenario that acts again after asserting is really two scenarios sharing a
setup, and it costs you on every axis:

- **Diagnosis.** When the run fails at "Results has 3 body rows", the failure message tells you
  nothing about which of the two acts broke. Two separate scenarios name their own failure.
- **Independence.** The second act silently depends on the first having succeeded, so one defect
  reports as one failure instead of two — and fixing it tells you nothing about whether the other
  path works.
- **Selective execution.** You cannot `--filter` your way to just the half you are debugging.
- **Readability.** The Given/When/Then shape is what makes a scenario scannable. A scenario with
  three `When` blocks has no shape at all.

**Split instead.** The wrong example above becomes two scenarios, and the shared arrangement moves
into a `Given` — usually by reusing a step that already performs the earlier action:

```gherkin
Scenario: Search narrows the results
    Given three existing items
    And user is on the Browse page
    When searching for "Name 03"
    Then Results has 1 body row

Scenario: Clearing the search restores all results
    Given three existing items
    And user is on the Browse page
    And searched for "Name 03"
    When user clears the search
    Then Results has 3 body rows
```

Note how the second scenario arranges with `Given searched for "Name 03"` rather than repeating
`When`. Step classes commonly provide exactly this pairing for the purpose — for example
[`SearchSteps`](../Steps/SearchSteps.cs) binds one method to both `[Given("searched for {term}")]`
and `[When("searching for {term}")]`. If the `Given` phrasing you need does not exist yet, add the
attribute to the existing step method; you do not need a new method.

### `Scenario Outline:` and `Examples:`

Each `Examples:` row becomes a `[TestCase(...)]` attribute; each column becomes a `string` parameter
on the test method. Reference columns with `<AngleBrackets>`.

Single column — the most common case ([Views.feature](../Features/V4/Views.feature)):

```gherkin
Scenario Outline: Empty page shows no items
    Given no existing items
    When user visits the <View> page
    Then Results has 0 body rows

Examples:
    | View    |
    | Browse  |
    | Prepare |
    | Shop    |
    | Manage  |
```

A column reused in more than one step ([Manage.feature](../Features/V4/Manage.feature)):

```gherkin
Scenario Outline: Bulk edit properties on selected items
    Given three existing items
    And user is on the Manage page
    And selected the first 2 items
    When expanding the bulk edit card
    And changing the bulk <Property> to "New Property"
    And applying bulk changes
    Then the first 2 items have <Property> set to "New Property"

Examples:
    | Property |
    | Store    |
    | Location |
    | Aisle    |
```

Multiple columns ([Lists.feature](../Features/V4/Lists.feature)):

```gherkin
Examples:
    | Key                                  | List                           |
    | f6a1c48d-959b-4cc1-ac0c-e92e4cf49133 | Functional Test Secondary List |
```

Reach for a Scenario Outline when the *same* assertions apply across variants. When the assertions
differ per variant — as in the filtered-versus-unfiltered view cases in `Views.feature` — write
separate outlines rather than adding conditional columns.

## Tagging Conventions

Tags go on the line immediately above `Scenario:` or `Scenario Outline:`.

### `@explicit:<reason>` — exclude from unattended runs

This is the workhorse tag. It generates `[Explicit("<reason>")]`, which means NUnit skips the
scenario in a normal run but will execute it when it is selected directly by filter. The reasons
in use carry specific meanings:

| Tag | Meaning |
| --- | --- |
| `@explicit:wip` | Actively being worked on right now |
| `@explicit:not-started` | Scenario is written and agreed; implementation has not begun |
| `@explicit:not-implemented` | The application feature or the supporting steps do not exist yet |
| `@explicit:fails` | Known failure, deliberately left in place to document the defect |

```gherkin
# TODO: Double-check! I thought this was implemented, BUT indeed the test is failing.
@explicit:not-started
Scenario: Bulk delete selected items
    Given three existing items
    ...
```

A bare `@explicit` with no reason works and generates `[Explicit]`, but always give a reason — the
reason is what tells the next reader whether this is a defect or unfinished work.

#### `@explicit:wip` and the generator's automatic marking are two different things

Both produce an `[Explicit]` attribute, and it is easy to assume one makes the other redundant. It
does not. They guard different failure modes and they are added and removed by different hands.

| | `[Explicit("steps_in_progress")]` | `@explicit:wip` |
| --- | --- | --- |
| Added by | The generator, automatically | **You**, by hand, as you write each scenario |
| Added when | The scenario contains a step with no binding | Always, on every new scenario |
| Removed by | The generator, automatically | **You**, by hand |
| Removed when | The last missing step gets implemented | You have watched the scenario pass |
| Protects against | A scenario that would throw `NotImplementedException` | A scenario that compiles and runs but has never been seen green |

The manual tag exists precisely because the automatic one lapses too early. The moment you implement
the final step, the generator's marking disappears — and the scenario joins the unattended run
immediately, before anybody has confirmed it actually passes. In practice a freshly-implemented
scenario rarely passes first try: a locator is wrong, a wait is missing, an assertion is off by one.
Without `@explicit:wip` those failures land in everyone else's test run.

So the rule is simple:

> **Tag every scenario `@explicit:wip` when you write it. Remove the tag only after you have run
> that scenario with `--filter` and seen it pass.**

Selecting a test by filter runs it even while it is marked explicit, so the tag never gets in the way
of your own iteration loop. It only keeps unfinished work out of everybody else's.

The other reasons in the table are for scenarios that should stay excluded *after* you are done —
most importantly `@explicit:fails`, which documents a real defect.

### `@ab:NNNN` — work item linkage

Links a scenario to its Azure Boards work item. **The generator ignores this tag** — it produces no
attribute and has no runtime effect. It exists so that a reader of the feature file can trace a
scenario back to the requirement that motivated it.

```gherkin
@ab:1873
Scenario: User registers account and logs in
```

### Tags supported by the generator but not used here

| Tag | Effect | Why unused |
| --- | --- | --- |
| `@category:<name>` | `[Category("<name>")]` | Feature-file granularity has been sufficient for selecting subsets |
| `@order:<n>` | `[Order(<n>)]` | Tests are designed to be order-independent; needing this is a smell |
| `@hidden` | Scenario generates no code at all | Deleting or `@explicit`-tagging is clearer |
| `@namespace:` / `@baseclass:` / `@using:` | Feature-level overrides of the generated class's namespace, base class and usings | This project has exactly one test base ([`FunctionalTestBaseV4`](../Infrastructure/FunctionalTestBase.cs)), so these must not appear in `Features/V4/` |

## Step Reuse

Scenarios are cheap to add precisely because most steps already exist. Before writing a step line,
open the generated step catalog at `../bin/Debug/net10.0/ListsWebApp.Tests.Functional.StepCatalog.md`
after running any functional test. That file is the current, browsable inventory of phrases already
bound to steps.

> The generator writes this catalog automatically beside the test assembly. The [`Steps/`](../Steps/)
> classes are still the implementation source, but the catalog is the place to check for existing
> phrasing before adding a new step.

### One method, many phrasings

Step methods commonly carry several attributes so the same behaviour reads naturally in different
grammatical positions. From [`NavigationSteps`](../Steps/NavigationSteps.cs):

```csharp
[Given("user is on the {name} page")]
[When("user navigates to {name} page")]
[When("user navigates to the {name} page")]
[When("user visits the {name} page")]
public async Task UserNavigatesToAnyPage(string name)
```

All four phrasings are equivalent. Pick the one that reads correctly in context — `Given user is on
the Manage page` when it is arrangement, `When user visits the Browse page` when it is the action
under test.

Valid `{name}` values for these navigation steps are `Login`, `Lists`, `ImportExport`, `Logs`,
`Profile`, `Browse`, `Manage`, `Forgot`, `Register`, `Prepare` and `Shop`. Anything else throws
`NotImplementedException` at runtime rather than failing to generate — so typos here surface as a
red test, not a build error.

Similarly, from [`ItemSteps`](../Steps/ItemSteps.cs), singular and plural variants avoid awkward
English:

```csharp
[Then("Results has {count} body rows")]
[Then("Results has {count} body row")]
public async Task ResultsHasBodyRows(int count)
```

### Choosing wording for a new step

**Match existing phrasing exactly when the meaning is the same.** `user is on the Manage page` and
`user is at the Manage page` would be two bindings for one concept — a maintenance liability.

**Keep wording reusable, but not artificially generic.** Scenario-specific phrasing is correct when
it encodes meaningful domain setup. These steps from `Manage.feature` are deliberately specific,
and that is a feature, not a flaw:

```gherkin
Given three existing items with only 2 containing a unique term
Given three existing items where 2 contain a unique term across different stores
```

Over-generalising these into something like `Given {count} items with {n} matching {term} in
{store}` would produce a step with five parameters, no clear intent, and a setup path nobody can
read. The specific version communicates the *purpose* of the fixture.

**When no step matches, just write the line you want.** Build, and the generator produces a stub
plus warning `GHERKIN004` naming exactly what is missing. See [writing-steps.md](./writing-steps.md)
for implementing it.

## Parameterization

### Placeholders

A step binding declares parameters with `{placeholder}` in its attribute text. The generator matches
the pattern against your step line and passes the captured text as an argument. **The placeholder
name is documentation only** — arguments bind positionally, and the *type* comes from the C# method
signature.

| Feature file line | Binding | Generated call |
| --- | --- | --- |
| `Then Results has 0 body rows` | `[Then("Results has {count} body rows")]` with `int count` | `ResultsHasBodyRows(0)` |
| `When searching for "Name 03"` | `[When("searching for {term}")]` with `string term` | `SearchForTerm("Name 03")` |
| `Given selected the first 2 items` | `[Given("selected the first {count} items")]` with `int count` | `SelectedTheFirstItems(2)` |
| `Then the first 2 items have Store set to "New Property"` | `[Then("the first {count} items have {Property} set to {value}")]` | `TheFirstItemsHavePropertySetTo(2, "Store", "New Property")` |

### Quoting

Wrap a value in double quotes when it contains spaces or when the quoting aids readability:

```gherkin
Then Results contains "Name 01"
Then a "Page Not Found" error is displayed
And changing the bulk <Property> to "New Property"
```

Bare values are fine for single tokens and numbers:

```gherkin
Given user has logged in as "Tester"
When user navigates to Lists page
Then Results has 0 body rows
```

### Combining outline columns with literals

Outline columns and quoted literals mix freely in one step. From `Manage.feature`:

```gherkin
And changing the bulk <Property> to "New Property"
```

generates:

```csharp
await ManageSteps.ChangingTheBulkPropertyTo(Property, "New Property");
```

`Property` is the test method's parameter; `"New Property"` is a literal.

### The `%20` note — legacy

[`ItemSteps.ResultsContains()`](../Steps/ItemSteps.cs) carries this remark:

```csharp
/// The name parameter is URL-decoded to work around a Gherkin Generator bug
/// that misparses quoted strings containing spaces.
/// Use %20 in feature files to represent spaces (e.g. "Name%2001").
[Then("Results contains {name}")]
public async Task ResultsContains(string name)
{
    var decoded = Uri.UnescapeDataString(name);
```

**This is historical.** The current generator handles quoted multi-word values correctly —
`Then Results contains "Name 01"` generates `ResultsContains("Name 01")`. The `Uri.UnescapeDataString`
call is harmless and keeps any older `%20`-encoded feature text working, but:

- Write natural spaces inside quotes. Do not encode.
- Do not copy the decoding pattern into new steps.

If you ever see a parameter arrive truncated at its first space, that is the symptom this workaround
addressed — report it rather than re-introducing encoding.

## Background and Cleanup

### `Background:`

Background steps are generated into a `[SetUp] SetupAsync()` method that runs before **every**
scenario in the file. The standard opening for a feature that needs a logged-in user and a clean
slate:

```gherkin
Background: Setup
    Given user has logged in
    And test mode has started
```

`Manage.feature` uses the explicit-user variant, which is equivalent for the default case but states
the identity outright:

```gherkin
Background: Setup
    Given user has logged in as "Tester"
    And test mode has started
```

Keep Background minimal. Every step there costs time on every scenario in the file, and it obscures
what a given scenario actually depends on. `Smoke.feature` has *no* Background at all, and documents
why — the whole point of that feature is to prove basic page loading works without any setup
machinery in the way.

### Test isolation steps

| Step text | Effect |
| --- | --- |
| `test mode has started` | Clears all items and deletes `__TEST__` users via `TestControlClient.BeginAsync()` |
| `test data has been cleared` | Identical — same method, different phrasing for mid-scenario use |
| `test mode has started with seed data` | Clears, then loads demo data |

These call the backend test control API directly rather than driving the UI, so they cost one HTTP
round trip. See [architecture.md](./architecture.md#infrastructure-layer).

### Cleanup

**Data-seeding steps register their own cleanup.** `one existing item`, `three existing items`,
`a new test account…`, `an invitation…` and their variants all call `AddCleanupAction()` internally,
so the data they create is removed after the test. You do not normally need to write a cleanup step.

For the cases where you do want to force it, these bindings exist:

```gherkin
Given afterward the list is cleared
Given afterward default list is cleared
Given afterward test mode is ended
```

All three map to the same method. Use them when a scenario dirties state through the UI rather than
through a seeding step.

### Login state

Logging in happens through the browser, so it is not free. Scenarios that need a *different* user
log the current one out first:

```gherkin
Scenario: User without Manage rights does not see Manage in sidebar
    Given current user is logged out
    And a new test account with read-write list access
    And user is logged in with that account
    When user navigates to Browse page
    Then the sidebar does not show "Manage"
```

## Example Walkthrough

Suppose we are adding coverage for marking items as favorites. Here is the full loop.

### 1. Write the scenarios first — all of them

Resist the urge to write one scenario, implement it, then write the next. Write the complete set,
then build. **Tag every one of them `@explicit:wip`** — they are not going to pass yet, and you do
not want them in anyone's run until they do. Create `Features/V4/Favorites.feature`:

```gherkin
Feature: (Favorites) [User can] mark items as favorites

Favorites are a per-item flag surfaced on the Browse and Manage views.
Marking an item favorite should persist across page loads and should be
reflected in the item edit dialog.

Background: Setup
    Given user has logged in
    And test mode has started

Rule: Marking favorites

@explicit:wip
Scenario: Mark an item as favorite
    Given one existing item
    And user is on the Browse page
    When marking the first item as favorite
    Then the first item is shown as favorite

@explicit:wip
Scenario: Favorite state persists across reload
    Given one existing item
    And user is on the Browse page
    And marked the first item as favorite
    When reloading the page
    Then the first item is shown as favorite

Rule: Displaying favorites

@explicit:wip
Scenario Outline: Favorite items appear on views that show all items
    Given one existing favorite item
    When user visits the <View> page
    Then Results contains "Name 01"

Examples:
    | View   |
    | Browse |
    | Manage |
```

Note the reuse: `user has logged in`, `test mode has started`, `one existing item`,
`one existing favorite item`, `user is on the Browse page`, `reloading the page`,
`user visits the <View> page` and `Results contains "…"` are all existing bindings. Only the
favorite-specific steps are new.

### 2. Build and read the warnings

```pwsh
dotnet build ListsWebApp.Tests.Functional.csproj
```

The generator emits `GHERKIN004` naming `Favorites` and the count of unimplemented steps, and writes
stubs into
`obj/GeneratedFiles/Gherkin.Generator/Gherkin.Generator.GherkinSourceGenerator/Favorites.feature.g.cs`:

```csharp
#region Stubs for Unimplemented Steps

/// <summary>
/// When marking the first item as favorite
/// </summary>
[When("marking the first item as favorite")]
public async Task MarkingTheFirstItemAsFavorite()
{
    throw new NotImplementedException();
}

/// <summary>
/// Then the first item is shown as favorite
/// </summary>
[Then("the first item is shown as favorite")]
public async Task TheFirstItemIsShownAsFavorite()
{
    throw new NotImplementedException();
}

#endregion
```

The two scenarios with missing steps now carry `[Explicit]` twice over: once from your
`@explicit:wip` tag and once from the generator's `steps_in_progress` marking. That is expected and
harmless — the outline in the second `Rule:` needs no new steps, so it is held back by your tag
alone. Either way the suite still runs green while you work.

If the steps you implement read or write scenario state, check the generated comments as well: the
generator now reflects `[Requires]`, `[Provides]`, and `[BaseProvides]` in the emitted test body so
you can spot missing setup before you run the browser.

**Read the stub list carefully before implementing anything.** If a step you expected to reuse
appears here, your wording did not match an existing binding — fix the feature file rather than
adding a duplicate binding. This is the cheapest correction point in the whole loop.

### 3. Implement the steps

Move the stubs into a step class — see [writing-steps.md](./writing-steps.md) — and any new locators
or actions into the page object — see [writing-pages.md](./writing-pages.md).

If a scenario needs a data fixture that does not exist, add a YAML file per
[sample-data.md](./sample-data.md).

### 4. Run scenarios individually as they come online

```pwsh
dotnet test --settings runsettings/development-msedge.runsettings `
            --filter "FullyQualifiedName~MarkAnItemAsFavorite"
```

Selecting a test directly by filter runs it even while it is still marked explicit, so you get a
tight loop on one scenario without waiting for the suite.

**When a scenario passes, remove its `@explicit:wip` tag — and not before.** One scenario, one tag,
at the moment you have evidence it works. Resist untagging the whole file at the end; that is how a
scenario nobody ever ran green ends up in the nightly.

### 5. Finish

Once every step is implemented, rebuild. The `GHERKIN004` warning disappears and the automatic
`[Explicit("steps_in_progress")]` markings vanish on their own. **Your `@explicit:wip` tags do not** —
they are yours to remove, and by this point each should already be gone, retired as its scenario went
green in step 4.

Run the whole feature to confirm:

```pwsh
dotnet test --settings runsettings/development-msedge.runsettings `
            --filter "FullyQualifiedName~Favorites_Feature_Tests"
```

## Checklist

- [ ] File is in `Features/V4/`, PascalCase, named for the area
- [ ] `Feature:` title follows `(Area) [Actor Can] goal`
- [ ] Description explains scope and any non-obvious constraints
- [ ] `Background:` contains only what *every* scenario needs
- [ ] Scenario names are descriptive and unique within the file
- [ ] No `Given` or `When` step appears after a `Then` step in any scenario
- [ ] Existing step wording reused wherever the meaning is the same
- [ ] Scenario Outline used only where the assertions are genuinely identical across rows
- [ ] Every newly written scenario is tagged `@explicit:wip`
- [ ] No `@explicit:wip` tag remains on a scenario you have not personally watched pass
- [ ] Any other `@explicit:` tag carries a reason
- [ ] `@ab:NNNN` added if a work item motivated the scenario
- [ ] Build is clean — no unexpected `GHERKIN004` steps remaining

## See Also

- [architecture.md](./architecture.md) — how feature files become tests
- [`Steps/`](../Steps/) — every available step, by class
- [writing-steps.md](./writing-steps.md) — implementing a step binding
- [sample-data.md](./sample-data.md) — adding a YAML fixture
- [environments.md](./environments.md) — running what you wrote
