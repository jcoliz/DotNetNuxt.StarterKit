# Debugging a Failing Test

**Debugging is a human activity.** Every rung below needs a person watching a browser or sitting on a
breakpoint. None of it belongs in an unattended run, and none of it is work to hand to an agent.

This guide picks up where [environments.md](./environments.md) leaves off: the environment is up, the
credentials resolve, and a scenario simply will not pass.

Work the ladder in order. Each rung costs more setup than the one above it, and most failures never
get past the first.

## Contents

- [Debugging a Failing Test](#debugging-a-failing-test)
  - [Contents](#contents)
  - [Rung 1 — Read what the run already told you](#rung-1--read-what-the-run-already-told-you)
  - [Rung 2 — `PWDEBUG=1` and the Playwright Inspector](#rung-2--pwdebug1-and-the-playwright-inspector)
  - [Rung 3 — the VS Code debugger](#rung-3--the-vs-code-debugger)
  - [What is not on the ladder](#what-is-not-on-the-ladder)
  - [Failures in a Pipeline Run](#failures-in-a-pipeline-run)
    - [Capture the evidence into a bug](#capture-the-evidence-into-a-bug)
    - [Server logs — CI](#server-logs--ci)
    - [Server logs — Production](#server-logs--production)
    - [Why filtering by test name works](#why-filtering-by-test-name-works)
  - [See Also](#see-also)

## Rung 1 — Read what the run already told you

Before changing anything, look at what the failed run produced.

**The assertion message.** Steps assert with the three-argument `Assert.That(actual, constraint,
message)` form precisely so the message states the expectation in words. If it doesn't, that is a bug
in the step — see [writing-steps.md](./writing-steps.md#assertions).

**The failure screenshot.** `TearDownBase` captures one automatically on *any* failed test, with no
opt-in:

```csharp
if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
{
    var pageModel = _objectStore.Get<PageObjectModel>();
    await pageModel.SaveScreenshotAsync($"FAILED");
}
```

It lands at:

```
Screenshot/{screenshotContext}/{TestClass}/{TestName}-FAILED.png
```

relative to the test host's working directory — `bin/Debug/net10.0/` for a normal `dotnet test`. It is
also registered with `TestContext.AddTestAttachment`, so it appears as an attachment on the test
result, which is usually the faster way to open it.

This is why every runsettings file sets a distinct `screenshotContext`: a Development failure and a
Container failure of the same scenario land in different folders instead of overwriting each other.

The screenshot answers the single most common question immediately — **was the browser even where you
thought it was?** A shot of the login page means the failure is in `Background`, not in the scenario
you are staring at.

**The console output.** Page objects log the API requests they waited on through `TestContext.Out`,
so the last-logged URL tells you how far the scenario got.

## Rung 2 — `PWDEBUG=1` and the Playwright Inspector

When the screenshot doesn't explain it, run the one failing test under the Playwright Inspector and
step through it action by action.

Every `-msedge` runsettings file, plus `container.runsettings`, already carries the switch:

```xml
<RunConfiguration>
  <EnvironmentVariables>
    <PWDEBUG>0</PWDEBUG>
  </EnvironmentVariables>
</RunConfiguration>
```

Change it to `1` and run **only** the failing test:

```powershell
dotnet test .\ListsWebApp.Tests.Functional.csproj `
    -s .\runsettings\development-msedge.runsettings `
    --filter "FullyQualifiedName~MarkAnItemAsFavorite"
```

What changes:

| | Effect |
| --- | --- |
| Browser | Headed. `PWDEBUG=1` forces a visible browser regardless of `<Headless>true</Headless>`. |
| Inspector | The Playwright Inspector opens alongside, letting you step, resume, and see the locator each action resolved to. |
| Selector highlighting | The element a locator matched is highlighted live in the page. |

Four things worth knowing before you start:

1. **Always pair it with `--filter`.** `PWDEBUG` applies to the whole run. Without a filter you get an
   Inspector window per test and a suite that never finishes.

2. **`defaultTimeout` still applies — this project overrides Playwright's debug behaviour.** Normally
   `PWDEBUG=1` disables Playwright timeouts so you can take your time. Here it doesn't, because
   `SetUpBase` sets the timeout explicitly on every test, *after* `PWDEBUG` has been applied:

   ```csharp
   var defaultTimeoutParam = TestContext.Parameters["defaultTimeout"];
   if (Int32.TryParse(defaultTimeoutParam, out var val))
       Context.SetDefaultTimeout(val);
   ```

   So under Development you get **12 seconds** to inspect before the pending action times out. If that
   isn't long enough, raise `defaultTimeout` in the same runsettings file while you debug — and put it
   back afterward. See [Why the timeouts differ](./environments.md#why-the-timeouts-differ).

3. **A stalled action *is* the diagnosis.** When the Inspector sits on one step, the locator it is
   waiting for is the one that never resolved. That is normally a missing `data-test-id`, a locator
   scoped to the wrong root, or a page that hadn't finished rendering — see
   [writing-pages.md](./writing-pages.md).

4. **Set it back to `0` before you commit.** These runsettings files are tracked in git. `PWDEBUG=1`
   merged to `main` would hang the pipeline.

> `development.runsettings` and `production.runsettings` do not carry the `RunConfiguration` block at
> all. Add one if you need to debug through the bundled Chromium rather than Edge.

## Rung 3 — the VS Code debugger

When you need to see C# state — what a step actually received, what is in the `ObjectStore`, which
branch a page object took — the Inspector can't help. Go to breakpoints.

Two things must be true first, and both are easy to get wrong:

**1. VS Code must be open with `Tests/Functional` as the workspace root** — not the repository root.
The settings below are workspace-relative, and the test explorer discovers the project from that root.

**2. [`.vscode/settings.json`](../.vscode/settings.json) must name the runsettings file for the
environment you actually have running:**

```json
{
    "dotnet-test-explorer.testProjectPath": "**/*Test*.@(csproj|vbproj|fsproj)",
    "dotnet.unitTests.runSettingsPath": "runsettings/container-msedge.runsettings",
    "dotnet.testWindow.enableNextTest": false
}
```

That file is checked in and currently points at `container-msedge`. **If you are debugging against
Aspire, change it to `development-msedge`.** A mismatch here is silent — it surfaces as a
connection-refused on the first navigation, which reads like the app is down rather than like the
tests are aimed at the wrong port.

Then open the Test Explorer, find the failing test, and choose **Debug Test**. Breakpoints in step
classes and page objects are hit normally.

**Leave `PWDEBUG=1` set from Rung 2.** The combination is the whole point of getting this far: you
step through C# in the editor while watching the browser perform each action beside it, so you can see
the exact moment the rendered page diverges from what the page object expects. Note that a breakpoint
pause counts against `defaultTimeout` just like Inspector time does — raise it if you plan to sit on a
breakpoint.

## What is not on the ladder

**There is no `Task.Delay` rung.** If a test only passes once you add a sleep, you have not fixed it —
you have found a missing wait condition, and the fix belongs in the page object. See
[Wait for the operation to settle](./writing-pages.md#5-wait-for-the-operation-to-settle-not-just-for-the-api-to-respond).

**Raising `defaultTimeout` is a debugging aid, not a fix.** It is legitimate to raise it while you are
stepping through. Committing the raise to make a flaky test pass is the same mistake as the sleep.

## Failures in a Pipeline Run

A pipeline failure is a different job from the ladder above. You cannot attach a debugger to a build
agent, and by the time you look at the result the containers are gone and the log buffer is finite.

**So the first pass is capture, not diagnosis.** Get everything the run knows into a bug while it
still exists; reproduce and climb the ladder afterward, locally.

### Capture the evidence into a bug

1. **Find the failure in the build results**, and open the specific failed test.

2. **Use the "Create bug" button** on the test result. It pre-fills the title, the failing test, the
   stack trace, and a link back to the run — take that rather than opening a blank work item.

3. **Attach the screenshot.** It is on the test result as an attachment, put there by
   `TestContext.AddTestAttachment` ([Rung 1](#rung-1--read-what-the-run-already-told-you)). Download
   it and attach it to the bug — the build retains it only as long as the run is retained.

4. **Add the console output as a comment.** This is the page-object request log, so it establishes
   how far the scenario got.

5. **Add the server logs as a comment.** Where they come from depends on the environment — see below.

The screenshot plus console output plus server logs answer, between them, "what did the browser see,
what did the test ask for, and what did the server do about it." A bug with all three can be worked
later. A bug with only a stack trace usually can't, because the run it came from has aged out.

### Server logs — CI

The CI pipeline publishes the container logs as a build artifact regardless of outcome
([`publish-docker-logs.yaml`](../../../.azure/pipelines/steps/publish-docker-logs.yaml) runs with
`condition: always()`).

Neither [`ci.yaml`](../../../.azure/pipelines/ci.yaml) nor [`v4.yaml`](../../../.azure/pipelines/v4.yaml)
overrides the template's `artifact` parameter, so it lands as **`docker-logs-1`** containing
**`docker-logs.log`**.

Download it, open the log in a text editor, and pull every line tagged with the failing test's name —
see [below](#why-filtering-by-test-name-works) for why that works.

> **Match the artifact to the attempt.** The artifact name carries a `$(System.JobAttempt)` suffix, so
> a rerun publishes a *second* artifact. If the job was retried, make sure you have the logs from the
> attempt that actually failed.

### Server logs — Production

Production logs go to Log Analytics. Sign in to the Azure Portal, open the workspace, and run:

```kusto
AppTraces
| where Properties.TestName == "<test-name>"
| where TimeGenerated > ago(1h)
| project TimeGenerated, Message, Properties.RequestPath
| order by TimeGenerated asc
```

Then use **Share → Copy results** and paste that into a comment on the bug.

> The `ago(1h)` clause is doing real work: `TestName` is not unique across runs, so without a time
> window you get every execution of that scenario in the retention period. Widen or narrow it to
> bracket the run you care about.

### Why filtering by test name works

Both log searches above depend on the same piece of infrastructure, which is worth knowing about
because it makes server-side triage possible at all.

Every request a test makes — Playwright navigation *and* `TestControlClient` setup calls — carries
correlation headers built by `TestCorrelationContext`: `traceparent`, `X-Test-Name`, `X-Test-Id`,
`X-Test-Class` and `X-Test-Client`. On the server, the `ExtractDetails` middleware lifts `X-Test-Name`
into the logging scope and onto the current `Activity`:

```csharp
scope["TestName"] = testName;
activity?.SetTag("test.name", testName);
```

That scope entry is what surfaces as `Properties.TestName` in `AppTraces`. In container logs it is
rendered inline by `CustomLogFormatter`, which writes the test name between slashes on **every** line
emitted during that request:

```
INF 02/21 02:02:48 [1003] /MarkAnItemAsFavorite/ Views.Shop: OK 0 items in 0 super groups
```

So searching a `docker` log for `/MarkAnItemAsFavorite/` yields exactly the server-side activity for
that scenario, interleaved with nothing else — even though the log contains the whole suite.

Two details worth knowing when a search comes up empty:

- **The header value is URL-encoded.** Generated test method names are plain identifiers, so this
  normally makes no difference, but it explains an unexpected `%20` or `%2B` in a log line.
- **`X-Test-Id` is unique per execution** where `TestName` is not. It isn't in the formatted console
  line, but it is on the trace as `test.id`, which is the way to separate two runs of the same test
  when the time window can't.

## See Also

- [environments.md](./environments.md) — getting an environment up, runsettings, secrets, and the
  configuration-level [troubleshooting table](./environments.md#troubleshooting)
- [writing-pages.md](./writing-pages.md) — locators, page readiness, and waiting properly
- [writing-steps.md](./writing-steps.md) — assertion conventions and the `ObjectStore`
- [architecture.md](./architecture.md) — what happens during a test run
