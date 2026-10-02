# Containerizing the stack

Our web stack can be built and run in containers. This document will explain why and how.

## Why?

Running in containers supports these developer use cases.

As a developer I can...

- Run the app quickly in a near-production environment
- Run all functional tests quickly, with assurance the same code will work as well in production
- Run the functional tests in the CI pipeline, protecting the codebase from every commit
- Lower the barrier to evaluation by colleagues

### Near-production environment locally

The container build generates the frontend as a static site. It runs the backend with production settings as a release build. Configuration is handled by environment variables, including CORS settings, in a production-like manner. All this without waiting for any cloud deployments, from the comfort of my desk, reliably and repeatably every time.

### Fast functional tests

If it takes too long to set up and run functional tests, or they're unreliable, we won't run them locally. This leads to the potential for
decay in the functional tests or last-minute surprises when CI builds fail.

Instead, with one script, we can quickly build the application into containers and run the entire suite of functional tests against it.

```powershell
./scripts/Run-FunctionalTestsVsContainer.ps1
```

> ![TODO] Functional tests are not implemented in the starter kit

### Functional tests in CI build

Rather than using additional cloud resources for a testing environment, and waiting for a deployment, we can build and run the entire functional
tests in the same container on the CI machine. This gives us high confidence that any new changes aren't going to break anything in production.

### Peer evaluation

When I want to send the app to a colleague for review, the barrier of entry is much lower to run in a docker container. Rather than having to
set up all the dependencies, they can simply run the docker compose
project, and they have a full version of the app running locally.

## How to run locally

First, copy [docker/.env.template](../docker/.env.template) to `docker/.env` and set `TESTPWORD` to a password for the seeded functional test user.

Then the simplest way to get started locally is to run the convenience scripts to build, and to run the compose project. Each of these operates on the [docker-compose-ci](../docker/docker-compose-ci.yml) project.

To run `docker compose build`:

```powershell
./scripts/Build-Container.ps1
```

To run `docker compose up --wait -d`, and bring up browser windows for the frontend and the Aspire Dashboard:

```powershell
./scripts/Start-Container.ps1
```

Finally, to run `docker compose down`:

```powershell
./scripts/Stop-Container.ps1
```

Once running, the frontend is at http://localhost:5400 and the backend API is at http://localhost:5401.

## How it's built

The [docker-compose-ci](../docker/docker-compose-ci.yml) project contains the orchestration needed to build, run, and (in the future) push and publish the containers. This project sets up the same build-time evironment variables that are used by the CD pipeline to deploy the production app. It runs them using a production-capable runtime configuration using environment variables.

Using a docker compose project to manage building, running, pushing, and publishing centralizes configuration in a single file, clarifying how application containerizing works across all phases.

> [!NOTE]
> In production, secrets are read from an Azure Key Vault via Managed Identity (see [SetupKeyVault.cs](../src/BackEnd/Startup/SetupKeyVault.cs)). The container environment instead bakes its test secrets into `appsettings.Container.json` and passes `TESTPWORD` as an environment variable, so there is a small variance from production in that way.

> [!TODO]
> Azure Key Vault secrets is not yet implemented in the starter kit

The backend [Dockerfile](../docker/Dockerfile) and the Frontend.Nuxt [Dockerfile](../src/FrontEnd.Nuxt/docker/Dockerfile) specify the needed build and run details.

The database runs as an ephemeral `postgres` container. I have not found a need for persistence across container runs, although that is easily accomplished by mounting a local volume.

Note that containers are not intended to replace a robust local development setup, which is orchestrated by .NET Aspire (see [ADR 0004](./adr/0004-aspire-development.md)).

```powershell
./scripts/Start-AppHost.ps1
```

To verify local development prerequisites and get a freshly-cloned repo ready to go, run:

```powershell
./scripts/Setup-Development.ps1
```

In the future, I plan to take advantage of coming capability in docker compose to package the entire project in a single OCI unit. This will
enable an evaluator to simply run one command to pull both containers locally and run the full project.

```bash
docker run jcoliz/listswebapp-v3:latest
```

## Observability

### Aspire Dashboard

The [docker-compose-ci](../docker/docker-compose-ci.yml) configuration includes the Aspire Dashboard for comprehensive observability. When containers are running, the dashboard is available at **http://localhost:18888**.

**Dashboard Features:**

- **Structured Logs** - Filter, search, and correlate logs with TraceIds (better than `docker logs`)
- **Distributed Traces** - Visualize request flows and timing breakdowns
- **Metrics** - Monitor ASP.NET Core, EF Core, and runtime performance
- **Resources** - View service health and configuration

**Automatic Access:**

- [Start-Container.ps1](../scripts/Start-Container.ps1) - Opens dashboard automatically

See [ADR 0004](./adr/0004-aspire-development.md) for the rationale behind using Aspire for observability only in this environment.

## Troubleshooting

If functional tests fail against the container, you have several options:

### 1. View Telemetry in Aspire Dashboard (Recommended)

Open http://localhost:18888 and check:

- **Structured Logs tab** - Filter by log level, search by content, view structured properties
- **Traces tab** - See request flows with timing information
- **Metrics tab** - Check for performance issues

This provides much richer diagnostics than `docker logs` alone and makes it easy to correlate logs with specific requests using TraceIds.

### 2. Re-run Failed Tests in Playwright Debug Mode

Change the `PWDEBUG` setting in [container-msedge.runsettings](../Tests/Functional/runsettings/container-msedge.runsettings) to `1` and re-run one test at a time. This usually shows the problem.

### 3. Check Docker Logs

The backend logs useful information to stdout:

```powershell
docker logs dnn_development_ci-backend-1
```

### 4. Increase Log Levels

If you need more detailed logs, raise the `DotNetNuxt` logging level in [appsettings.Container.json](../src/BackEnd/appsettings.Container.json), or override it with a `Logging__LogLevel__DotNetNuxt` environment variable in the docker-compose file. The logs will appear in both `docker logs` and the Aspire Dashboard.

### 5. Check Browser Console

For frontend issues, check the browser console logs. Occasionally useful notes are dropped there.

## Benchmarking performance

Both dockerfiles use intelligent layer caching. While the very first run can take 5+ minutes, it's much faster in subsequent iterations. In my experience, it takes about 30 seconds total to rebuild both containers in the worst case rebuilding all the backend projects and regenerating the frontend. Add about 2 minutes to run the functional tests locally. This compares with about 7.5 minutes on the CI machine to clone, build, publish and run functional tests a new version. On larger apps, I have seen an even greater savings.
