# 0004. .NET Aspire for Development Orchestration

Date: 2026-10-01

## Status

Accepted

## Context

The stack has multiple components that run as separate processes: a .NET backend, a PostgreSQL database ([0005](0005-database-backend.md)), and a Node.js frontend. During development they must run together, find each other, and be debuggable.

Alternatives that proved cumbersome in the past:

* **Docker Compose** - Separate Dockerfiles and compose files to maintain, a slower feedback loop, and harder cross-container debugging.
* **Separate dev servers** - Running the frontend dev server and backend by hand, with manual proxy and environment setup. Error-prone and inconsistent between machines.

Requirements:

- Run the .NET backend, database, and Node.js frontend together
- Service discovery between components
- Debugging across components
- Fast iteration with hot reload
- Logs, traces, and metrics during development

## Decision

Use **.NET Aspire** for development orchestration. The AppHost project (`src/AppHost`) declares the components and their relationships in C#.

Today the AppHost ([AppHost.cs](../../src/AppHost/AppHost.cs)) starts:

- A PostgreSQL container with a `starterkit` database, on a fixed port
- The backend, with a reference to that database and the Application Insights connection string

The Nuxt frontend is included in `AppHost.cs` as a commented-out `AddJavaScriptApp` block, to be enabled when the frontend is implemented. It will receive the backend URL through `NUXT_PUBLIC_API_BASE_URL` (see [0007](0007-backend-proxy-or-direct.md)).

Shared telemetry configuration (OpenTelemetry, health checks) lives in `src/ServiceDefaults` and is referenced by the backend.

### Scope

Aspire is used for **development orchestration**, plus **observability only** in the container environment:

- **Development**: Aspire starts everything with `scripts/Start-AppHost.ps1` or `dotnet run` from the AppHost.
- **Container environment** (`docker/docker-compose-ci.yml`): Docker Compose orchestrates the containers. The standalone Aspire Dashboard image is added only to aggregate logs, traces, and metrics.
- **Production**: Aspire is not used. See [0006](0006-production-infrastructure.md).

## Consequences

### Easier

- **One command** starts the whole stack, with the Aspire Dashboard for logs, traces, and metrics.
- **Service discovery** - Connection strings and URLs are injected, so there is no hardcoded configuration or manual proxy setup.
- **Observability** - Structured logs, distributed tracing, and health checks via OpenTelemetry out of the box.
- **Adding dependencies** - Redis, a message broker, etc. are one line in the AppHost.
- **Debugging** - Attach a debugger to any service from Visual Studio or VS Code.
- **Onboarding** - The AppHost is self-documenting, which reduces "works on my machine" problems.

### More difficult

- **Learning curve** - Contributors must learn Aspire concepts and the dashboard.
- **Tooling** - A current .NET SDK and a container runtime are required, even for developers who mostly work on the frontend.
- **Maintenance** - The AppHost must be kept in sync with the real deployment shape.
- **Dev/production divergence** - Features must work both under Aspire and as standalone deployed services. Each service must stay runnable on its own, configured through environment variables.
- **.NET-centric** - Aspire's support for non-.NET services is good but not as deep.

### Migration path

The AppHost is a separate project and can be removed without touching application code. Each service stays runnable independently, so another orchestrator can replace it.

## Related Decisions

- [0005. Database Backend](0005-database-backend.md) - The PostgreSQL resource the AppHost provisions
- [0006. Production Infrastructure](0006-production-infrastructure.md) - Production does not use Aspire
- [0007. Backend Proxy or Direct](0007-backend-proxy-or-direct.md) - How the frontend learns the backend URL

## References

- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Aspire AppHost Concepts](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/app-host-overview)
