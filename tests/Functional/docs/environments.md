# Environments, Configuration & Running Tests

The same functional tests run unchanged against three different deployments of the application.
What varies is a `.runsettings` file: where to point the browser, where to reach the API, how long
to wait, and which credentials to use.

This guide covers how that configuration works and how to run the tests against each target.

> **The canonical description of the environments themselves — architecture, hosting, how the front
> end reaches the back end — is [docs/ENVIRONMENTS.md](../../../docs/ENVIRONMENTS.md) at the
> repository root.** This document covers only what the functional tests need. Read that one first
> if you want to understand what you're testing against.

## Contents

- [Environments, Configuration \& Running Tests](#environments-configuration--running-tests)
  - [Contents](#contents)
  - [The Three Environments](#the-three-environments)
  - [Prerequisites](#prerequisites)
  - [Choosing a RunSettings File](#choosing-a-runsettings-file)
  - [Anatomy of a RunSettings File](#anatomy-of-a-runsettings-file)
    - [Why the timeouts differ](#why-the-timeouts-differ)
    - [`environment` is matched by string, so the name matters](#environment-is-matched-by-string-so-the-name-matters)
  - [Secrets and Placeholder Resolution](#secrets-and-placeholder-resolution)
    - [The `.env` file](#the-env-file)
    - [Placeholders, not secret runsettings copies](#placeholders-not-secret-runsettings-copies)
  - [Running Against Each Environment](#running-against-each-environment)
    - [Development](#development)
    - [Container](#container)
    - [Production](#production)
  - [Pipeline Integration](#pipeline-integration)
  - [Troubleshooting](#troubleshooting)
  - [See Also](#see-also)

## The Three Environments

| Environment | What it is | Brought up by |
| --- | --- | --- |
| **Development** | .NET Aspire orchestrates the API as a process and the front end as a Node dev server with HMR | `.\scripts\Start-AppHost.ps1` |
| **Container** | Front end statically generated and served by nginx; API in a container; Docker Compose orchestrates | `.\scripts\Build-Container.ps1` then `.\scripts\Start-Container.ps1` |
| **Production** | Azure Static Web Apps front end, Azure App Service back end | The release pipeline |

**The environment must already be running before you start the tests.** Nothing in the test project
launches the application. A run against a target that isn't up fails during the first navigation,
usually with a connection-refused error that looks more mysterious than it is.

See [docs/ENVIRONMENTS.md](../../../docs/ENVIRONMENTS.md) for what distinguishes them
architecturally — in particular, Development proxies `/api/**` through Nuxt, while Container and
Production bake an absolute API base URL into the static build. That difference is why `apiUrl` and
`webAppUrl` are separate parameters.

## Prerequisites

1. **.NET 10 SDK** — the test project targets `net10.0`.
2. **A browser for Playwright.**

For the browser you have two options, and this is what the `-msedge` runsettings variants are about:

**Option A — Playwright's bundled Chromium.** Build once, then run the generated install script:

```powershell
dotnet build
.\bin\Debug\net10.0\playwright.ps1 install chromium
```

This is needed only on first setup, and again whenever the Playwright package is upgraded. Use the
plain runsettings files (`development`, `container`, `production`).

**Option B — your installed Microsoft Edge.** No download needed. This is important for running
on Windows for Arm, because Playwright doesn't [build or provide browsers binaries for ARM devices](https://github.com/microsoft/playwright/issues/1243).

Use the `-msedge` files, which
launch Chromium through Edge's channel:

```xml
<Playwright>
  <BrowserName>chromium</BrowserName>
  <LaunchOptions>
    <Channel>msedge</Channel>
    <Headless>true</Headless>
  </LaunchOptions>
</Playwright>
```

The project defaults to Option B — `<RunSettingsFilePath>` in the `.csproj` points at
`runsettings/development-msedge.runsettings` — so a bare `dotnet test` works on a Windows machine
with Edge and no Playwright browser install.

## Choosing a RunSettings File

Pass one with `-s`:

```powershell
dotnet test .\ListsWebApp.Tests.Functional.csproj -s .\runsettings\container.runsettings
```

Omit `-s` and you get the project default, `runsettings/development-msedge.runsettings`.

All six files live in [`runsettings/`](../runsettings/):

| File | Environment | Front end | Back end | Timeout | Browser |
| --- | --- | --- | --- | --- | --- |
| `development.runsettings` | Development | `http://localhost:5284/` | `http://localhost:5030/` | 12s | Bundled Chromium |
| `development-msedge.runsettings` | Development | `http://localhost:5284/` | `http://localhost:5030/` | 12s | Edge, headless |
| `container.runsettings` | Container | `http://localhost:5300/` | `http://localhost:5301/` | 5s | Bundled Chromium |
| `container-msedge.runsettings` | Container | `http://localhost:5300/` | `http://localhost:5301/` | 5s | Edge, headless |
| `production.runsettings` | Production | `{WEBAPPURL_PROD}` | `{APIURL_PROD}` | 40s | Bundled Chromium |
| `production-msedge.runsettings` | Production | `{WEBAPPURL_PROD}` | `{APIURL_PROD}` | 30s | Edge, headless |

A `-msedge` file and its sibling differ **only** in browser selection. If a test passes under one and
fails under the other, you have found a genuine browser-behaviour difference, not a configuration
problem.

[`runsettings/README.md`](../runsettings/README.md) is the maintained reference for this folder and
should be updated alongside any change here.

## Anatomy of a RunSettings File

Every file supplies the same seven `TestRunParameters`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
	<TestRunParameters>
		<Parameter name="webAppUrl" value="http://localhost:5300/" />
		<Parameter name="apiUrl" value="http://localhost:5301/" />
		<Parameter name="defaultTimeout" value="5000" />
		<Parameter name="screenshotContext" value="container" />
		<Parameter name="userName" value="Tester" />
		<Parameter name="userPassword" value="{TESTPWORD}" />
		<Parameter name="environment" value="Container" />
	</TestRunParameters>
</RunSettings>
```

| Parameter | Meaning |
| --- | --- |
| `webAppUrl` | Base URL the browser navigates to |
| `apiUrl` | Base URL for direct API calls — see [`TestControlClient`](../Infrastructure/TestControlClient.cs) |
| `defaultTimeout` | Playwright default timeout, milliseconds |
| `screenshotContext` | Subfolder name for failure screenshots |
| `userName` | Seeded functional-test user; always `Tester` |
| `userPassword` | Always a `{PLACEHOLDER}`, never a literal |
| `environment` | Label the tests report to identify the target |

### Why the timeouts differ

`defaultTimeout` tracks how far away the work is, not how slow the test is:

- **Container — 5s.** Everything is local and pre-built. Static files off nginx.
- **Development — 12s.** The Nuxt dev server compiles routes on first request. The first navigation
  to a page is genuinely slow; later ones aren't.
- **Production — 30–40s.** Real network, plus possible cold starts on App Service.

Raising a timeout to fix a flaky test is almost always the wrong fix. The right fix is usually a
proper wait in the page object — see [The WaitForApi Pattern](./writing-pages.md#the-waitforapi-pattern).

### `environment` is matched by string, so the name matters

`environment` is not just a label. It surfaces as `TargetEnvironment` on the test context:

```csharp
public string? TargetEnvironment => _cachedTargetEnvironment ??= GetOptionalParameter("environment");
```

and step logic compares it **by exact string** to decide whether a scenario applies. `BuiltInSteps`
provides the gate used by `Given not running against {name} environment`:

```csharp
if (context.TargetEnvironment == environmentName)
{
    Assert.Ignore($"Test ignored because it is running against the '{environmentName}' environment.");
}
```

The three values are `Development`, `Container`, and `Production`, matching the names in
[docs/ENVIRONMENTS.md](../../../docs/ENVIRONMENTS.md). A mismatch doesn't produce an error — it
produces a scenario that silently fails to skip, or silently skips when it shouldn't.

> The value does **not** feed `ASPNETCORE_ENVIRONMENT`. It only tells the tests which target they're
> pointed at.

`Errors.feature` already uses this to separate behaviour that legitimately differs between targets:

```gherkin
Scenario: Page not found retains login state when home button clicked (in Local Development)
    Given running in "Development" environment
```

Note that `Given running in {string1} environment` in
[`NavigationSteps`](../Steps/NavigationSteps.cs) currently throws `NotImplementedException`, and
every scenario using it is tagged `@explicit:fails`, so none of this runs today. When that step is
implemented it will compare against `environment` the same way.

## Secrets and Placeholder Resolution

**No secrets are committed.** Values written `{NAME}` are resolved at run time by
`ResolveEnvironmentVariables` in the
[jcoliz.FunctionalTests](../../../submodules/jcoliz.FunctionalTests/src/FunctionalTests/FunctionalTest.cs)
submodule:

```csharp
foreach (Match match in EnvVarRegex().Matches(value))
{
    var envVarName = match.Groups[1].Value;
    var envVarValue = Environment.GetEnvironmentVariable(envVarName);

    if (envVarValue is null)
    {
        throw new InvalidOperationException(
            $"Environment variable '{envVarName}' referenced in test parameter '{contextName}' is not set. " +
            $"Original value: {value}"
        );
    }

    result = result.Replace(match.Value, envVarValue);
}
```

Two properties worth relying on:

- **Resolution reads real process environment variables.** Anything already in your shell works.
- **A missing variable fails loudly.** You get a named error rather than an empty password and a
  baffling login failure.

### The `.env` file

Before resolving anything, the base class loads `Tests/Functional/.env` into the process if it
exists — once per run, guarded by a static flag. It searches the current directory, the test
assembly directory, then three levels up from the assembly (which reaches the project root from
`bin/Debug/net10.0`). If no file is found it logs and continues; `.env` is optional.

To configure a local run, copy [`.env.template`](../.env.template) to `Tests/Functional/.env` and
fill in only the variables for the environment you're targeting:

```ini
# Development: local Aspire-orchestrated run (scripts/Start-AppHost.ps1)
# Used by: development.runsettings, development-msedge.runsettings
TESTPWORD_DEV=

# Container: docker compose run (scripts/Start-Container.ps1)
# Must match TESTPWORD in docker/.env, since that value seeds the "Tester" user.
TESTPWORD=

# Production: deployed Azure environment
TESTPWORD_PROD=
WEBAPPURL_PROD=
APIURL_PROD=
```

`.env` is git-ignored at the repository root, as is `*.secrets.runsettings`.

| Placeholder | Consumed by |
| --- | --- |
| `{TESTPWORD_DEV}` | `development.runsettings`, `development-msedge.runsettings` |
| `{TESTPWORD}` | `container.runsettings`, `container-msedge.runsettings` |
| `{TESTPWORD_PROD}`, `{WEBAPPURL_PROD}`, `{APIURL_PROD}` | `production.runsettings`, `production-msedge.runsettings` |

**`.env.template` is the authoritative list.** A new placeholder that isn't documented there will
strand the next person who clones the repo.

### Placeholders, not secret runsettings copies

Older instructions had you copy a runsettings file to a matching `*.secrets.runsettings` and edit
credentials in place. **Don't.** The `{PLACEHOLDER}` mechanism replaced it. The `*.secrets.runsettings`
gitignore entry survives only as a safety net; the current pattern keeps one committed file per
environment and moves the secret out to `.env`.

## Running Against Each Environment

### Development

```powershell
# Terminal 1 — from the repository root
.\scripts\Start-AppHost.ps1
```

Wait for the Aspire dashboard, then:

```powershell
# Terminal 2 — from Tests/Functional
dotnet test .\ListsWebApp.Tests.Functional.csproj
```

That uses the project default, `development-msedge.runsettings`. For bundled Chromium instead:

```powershell
dotnet test .\ListsWebApp.Tests.Functional.csproj -s .\runsettings\development.runsettings
```

Requires `TESTPWORD_DEV` — the password of the `Tester` user seeded in your local development
database.

### Container

The scripted path builds the image, brings up Compose, runs the tests, and tears down:

```powershell
pwsh .\scripts\Run-FunctionalTestsVsContainer.ps1
```

It reads `TESTPWORD` from your environment, falling back to the value in `docker/.env`, then runs:

```powershell
dotnet test .\ListsWebApp.Tests.Functional.csproj -s .\runsettings\container-msedge.runsettings
```

Against a container that's already up:

```powershell
dotnet test .\ListsWebApp.Tests.Functional.csproj -s .\runsettings\container.runsettings
```

**`TESTPWORD` must match `docker/.env`.** The same value seeds the `Tester` user *inside* the
container and authenticates the tests *against* it. A mismatch shows up as a login failure in the
first scenario, not as a configuration error.

### Production

```powershell
dotnet test .\ListsWebApp.Tests.Functional.csproj -s .\runsettings\production.runsettings
```

Requires `TESTPWORD_PROD`, `WEBAPPURL_PROD`, and `APIURL_PROD`.

This is fully supported — the tests sign in as a dedicated user and operate in their own list — but
it is still production. Confirm the `Tester` credentials by signing in manually before assuming a
failure is a real regression.

## Pipeline Integration

**Azure Pipelines never reads `.env`.** The step templates map pipeline variables into the same
environment variable names, so the committed runsettings files work unchanged.

[`steps/functional-test.yaml`](../../../.azure/pipelines/steps/functional-test.yaml) — validates the
password is set, builds, installs Chromium, runs:

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run functional tests'
  inputs:
    command: 'test'
    workingDirectory: $(Solution.FunctionalTestsDirectory)
    arguments: '-s ${{ parameters.runsettings }}'
  env:
    TESTPWORD: $(testPword)
```

[`steps/run-functional-test.yaml`](../../../.azure/pipelines/steps/run-functional-test.yaml) — assumes
a prior prepare step, and additionally maps the production URLs:

```yaml
  env:
    TESTPWORD: $(testPword)
    TESTPWORD_PROD: $(testPword)
    APIURL_PROD: $(azureAppServiceUrl)
    WEBAPPURL_PROD: $(azureFrontEndUrl)
```

Current consumers:

| Pipeline | Target | RunSettings |
| --- | --- | --- |
| [`v4.yaml`](../../../.azure/pipelines/v4.yaml) | Container, in CI | `runsettings/container.runsettings` |
| [`v4-uat.yaml`](../../../.azure/pipelines/v4-uat.yaml) | Deployed app | `runsettings/production.runsettings` |
| [`chompr-prod-v4.yaml`](../../../.azure/pipelines/chompr-prod-v4.yaml) | Deployed app | `runsettings/production.runsettings` |
| [`ci.yaml`](../../../.azure/pipelines/ci.yaml) | Container, in CI | `runsettings/docker.runsettings` — **this file does not exist**, see below |

The pipelines wait for readiness before testing — `wait-site-healthy.yaml` polls
`http://localhost:5300/version` — which is the CI equivalent of "bring the environment up first."

## Troubleshooting

| Symptom | Likely cause |
| --- | --- |
| `Environment variable 'X' referenced in test parameter 'Y' is not set` | No `.env`, or the variable is missing from it. The message names the variable. |
| `Required test parameter 'X' is not set in .runsettings file` | Wrong file passed to `-s`, or a parameter was dropped when copying a file. |
| Connection refused on first navigation | The target environment isn't running. |
| Login fails on every scenario | Password mismatch. For Container, compare `TESTPWORD` against `docker/.env`. |
| `[Environment] No .env file found (this is optional)` in output | Informational. Only a problem if you expected `.env` to be picked up — check it's at `Tests/Functional/.env`. |
| Tests pass locally, fail in CI | Timeout differences (5s vs 12s) often expose a missing wait in a page object. |
| Everything times out after a Playwright upgrade | Re-run `.\bin\Debug\net10.0\playwright.ps1 install chromium`, or switch to an `-msedge` file. |

If the symptom isn't a configuration problem — the environment is up, the credentials work, and a
scenario simply fails — see [debugging.md](./debugging.md).

## See Also

- **[docs/ENVIRONMENTS.md](../../../docs/ENVIRONMENTS.md)** — canonical description of the three environments
- [runsettings/README.md](../runsettings/README.md) — maintained reference for the runsettings folder
- [.env.template](../.env.template) — every variable and which file consumes it
- [debugging.md](./debugging.md) — what to do when a scenario fails and the configuration is fine
- [architecture.md](./architecture.md) — how the test infrastructure consumes these parameters
- [writing-pages.md](./writing-pages.md) — waiting properly instead of raising timeouts
- [sample-data.md](./sample-data.md) — seeding data once an environment is running
