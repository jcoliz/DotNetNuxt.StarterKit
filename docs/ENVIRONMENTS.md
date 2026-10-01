# Environments

The application is built to run in three distinct environments:

* Development: For local development
* Container: Primarily for quick build/execution of functional tests in CI pipeline
* Production: Running in Azure

**Environment Configuration:** Uses standard ASP.NET Core `ASPNETCORE_ENVIRONMENT` variable (Development, Container, Production).

## Development

As described in [ADR 0004](./adr/0004-aspire-development.md), local development is done using .NET Aspire.

**Architecture:**
- Frontend (`src/FrontEnd.Nuxt`) runs in the Nuxt dev server (via pnpm) with hot module replacement, on port 5284
- Backend (`src/BackEnd`) runs as a .NET API service
- PostgreSQL runs as an Aspire-managed container on port 5501
- Orchestrated by the Aspire AppHost ([AppHost.cs](../src/AppHost/AppHost.cs))

**Frontend-to-Backend Communication:**
- Aspire injects `NUXT_PUBLIC_API_BASE_URL` with the backend's resolved HTTP endpoint
- Frontend makes direct API calls to that base URL from browser JavaScript
- Service discovery handled by Aspire (`WithReference`)

**How to run:**
```powershell
.\scripts\Start-AppHost.ps1
```

Then open the Aspire Dashboard URL shown in the console.

## Container

The application can be packaged into containers and orchestrated with a [Docker Compose project](../docker/docker-compose-ci.yml). For more details on the container environment, please refer to [CONTAINER-ENVIRONMENT](./CONTAINER-ENVIRONMENT.md).

**Use cases:**
- Run functional tests in CI pipeline
- Run functional tests locally with ease
- Distribute the entire application via DockerHub for evaluation

**Architecture:**
- Frontend generated as static site using `nuxt generate`
- Frontend served by nginx
- Backend runs as containerized .NET API
- PostgreSQL runs as an ephemeral `postgres` container
- Aspire Dashboard container collects telemetry via OTLP
- Orchestrated via [docker compose](../docker/docker-compose-ci.yml)

**Frontend-to-Backend Communication:**
- Static site built with `NUXT_PUBLIC_API_BASE_URL` baked in at build time
- Configured via Docker build arg to point to backend container
- Direct API calls from browser JavaScript

**How to run:**
```powershell
.\scripts\Build-Container.ps1
.\scripts\Start-Container.ps1
```

Frontend available at http://localhost:5300, Backend at http://localhost:5301, Aspire Dashboard at http://localhost:18888.

Run functional tests with `.\scripts\Run-FunctionalTestsVsContainer.ps1`.

## Production

As described in [ADR 0006](./adr/0006-production-infrastructure.md) and [ADR 0007](./adr/0007-backend-proxy-or-direct.md), the production application is hosted on Azure services.

**Architecture:**
- Frontend: Azure Static Web Apps (static site from `nuxt generate`)
- Backend: Azure App Service (.NET Web API)
- Database: Azure Database for PostgreSQL Flexible Server (see [ADR 0005](./adr/0005-database-backend.md))
- Deployed via Azure Pipelines. Note this is the **ONLY** supported methodology to deploy bits.

Each deployment pipeline builds, deploys backend (with migrations) before frontend, then runs functional tests against the deployed instance.

**Frontend-to-Backend Communication:**
- Static site built with `NUXT_PUBLIC_API_BASE_URL` set to production backend URL
- Direct API calls from browser JavaScript
- CORS configured on backend to allow Static Web App origin

**Environment Variables:**
- `NUXT_PUBLIC_API_BASE_URL`: Set during build to the production backend URL, supplied by the pipeline variable `azureAppServiceUrl` (e.g., `https://api.chompr.net`)
- `KeyVaultEndpoint`: App Service setting (from [main.bicep](../.azure/deploy/main.bicep)) pointing at the shared Key Vault

**Secrets:**
- Read at startup from Azure Key Vault using Managed Identity ([SetupKeyVault.cs](../src/BackEnd/Startup/SetupKeyVault.cs))
- Secrets are filtered by a prefix derived from `Jwt:Audience` (e.g., `ListsWebApp.UAT` → `ListsWebApp-UAT--`), with `--` mapped to the `:` config delimiter
- Non-secret, per-site config comes from a private site config repo; see [.azure/site-config-template/README.md](../.azure/site-config-template/README.md)

## Summary Table

| Aspect | Development | Container | Production |
|--------|-------------|-----------|------------|
| Frontend host | Nuxt dev server | nginx (static) | Azure Static Web Apps |
| Backend host | .NET process | Docker container | Azure App Service |
| Database | Postgres container (Aspire) | Postgres container (ephemeral) | Azure PostgreSQL Flexible Server |
| Frontend build | `pnpm run dev` | `pnpm run generate` | `pnpm run generate` |
| API calls | Direct (URL injected by Aspire) | Direct (baked URL) | Direct (baked URL) |
| Orchestration | .NET Aspire | Docker Compose | Azure |
| Service discovery | Aspire | Docker network | DNS |
