# 0006. Production Infrastructure

Date: 2026-10-01

## Status

Accepted

## Context

The starter kit needs a production deployment target. Apps built from it are expected to deploy onto this infrastructure, so **making that deployment easy and simple is a goal of the starter kit**, not something each project works out for itself.

### Requirements

1. Low cost at low usage
2. Hosted in Azure
3. Custom domain with HTTPS certificate
4. A new app can go from the starter kit to a running production deployment with minimal Azure-specific work

### Composition

Two deployable components:
* Backend .NET API service (plus its PostgreSQL database, see [0005](0005-database-backend.md))
* Frontend Nuxt app, built as static files ([0003](0003-nuxt.md))

### Options considered

1. **Azure Container Apps (ACA)** - The default target for Aspire apps.
2. **Azure Kubernetes Service** - Like ACA, but more complicated.
3. **Static Web Apps for the frontend, ACA for the backend.**
4. **Static Web Apps for the frontend, App Service for the backend.**
5. **Blob Storage for the frontend.** Does not natively support a custom domain with HTTPS. That requires Azure Front Door, which is comparatively expensive.

## Decision

**Frontend: Azure Static Web Apps**
- Static output from `nuxt generate`
- Custom domain and HTTPS included
- Global CDN distribution
- The Free tier is sufficient, because the frontend calls the backend directly rather than through a linked backend ([0007](0007-backend-proxy-or-direct.md))

**Backend: Azure App Service** (Basic B1 tier as a starting point)
- Deployed as a published .NET app (`dotnet publish`), **not** as a container. `docker/Dockerfile` serves only the Container environment (see [ENVIRONMENTS](../ENVIRONMENTS.md)).
- Backed by Azure Database for PostgreSQL Flexible Server ([0005](0005-database-backend.md)), accessed with a managed identity
- Predictable, low cost for low, steady traffic

**Secrets**
- Azure Key Vault, read by the backend at startup using its managed identity
- Secrets are never stored in source control or pipeline definitions

**Deployment mechanism**
- Azure Pipelines is the **only** supported way to deploy to production

**Observability**
- Application Insights for telemetry, dependency tracking, and failure diagnostics
- A Log Analytics workspace for centralized logs

### Alternative considered: Azure Container Apps

ACA was rejected for cost. At low volumes, the consumption plan still has minimum charges, and App Service B1 gives more predictable pricing. Moving to ACA later is feasible, since the existing `docker/Dockerfile` is a starting point for a production container image.

## Deployment: easy by default

The starter kit should carry everything needed to stand this infrastructure up and deploy to it, so a new app only supplies names, a domain, and secrets. The kit should provide:

1. **Infrastructure as code** (Bicep) that provisions the whole footprint: the Static Web App, App Service plan and web app, PostgreSQL Flexible Server and database, Key Vault, Application Insights, the Log Analytics workspace, and the managed identity that grants the web app access to PostgreSQL and Key Vault. One parameter file per deployed environment.
2. **Secure defaults wired in the template**: managed identity access to PostgreSQL (no database passwords) and Key Vault, HTTPS only, and the frontend's origin set as the backend's allowed CORS origin ([0007](0007-backend-proxy-or-direct.md)).
3. **Azure Pipelines definitions** that, in order:
   - build and publish the backend with `dotnet publish`, stamping the version (`-p:SolutionVersion`)
   - apply database migrations to the target database
   - deploy the backend to App Service
   - run `nuxt generate` with `NUXT_PUBLIC_API_BASE_URL` set to the deployed backend URL (a pipeline variable), and deploy the static output to Static Web Apps
   - verify the deployed instance (e.g. `/health/db` and `/Version`)

   The backend deploys before the frontend ([0010](0010-backend-for-frontend.md)). Separate pipelines can target separate environments (e.g. UAT and production), triggered by version tags.
4. **Configuration by convention**: everything environment-specific is supplied through App Service app settings, using the same keys the backend already reads (see below). No code changes are needed per environment.
5. **Short documentation** covering the one-time steps: creating the Azure resources, configuring the custom domain, and first deployment.

### Settings the deployment must supply

| Setting | Purpose |
|---------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string, without a password. In Production the backend adds an Entra managed identity token ([0005](0005-database-backend.md)). |
| `Startup__AllowedCorsOrigins__0` | Origin of the deployed static site ([0007](0007-backend-proxy-or-direct.md)) |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Application Insights telemetry |
| `KeyVaultEndpoint` | URI of the Key Vault holding secrets. Set by the Bicep template. |
| `ASPNETCORE_ENVIRONMENT` | `Production` (selects the managed identity database path) |

`NUXT_PUBLIC_API_BASE_URL` is a **build-time** value for the frontend, supplied by the pipeline rather than an App Service setting.

Secrets live in Key Vault, not in app settings. They are mapped to configuration keys by a name convention, with `--` standing in for the `:` delimiter, so each app's secrets can be filtered by a prefix and shared vaults stay manageable.

The backend applies pending migrations at startup in every environment **except Production**. Production databases are migrated only by the deployment pipeline, so the pipeline must include a migration step. It should use the migration export tooling (`scripts/Export-PostgresMigration.ps1`) to produce a script and apply it to the production database before the new backend version starts serving.

### Starter kit status

Already in place: the `/health` and `/health/db` endpoints, managed identity database authentication, environment-specific CORS configuration, Application Insights support, the `/Version` endpoint, and the migration export script.

Not yet in place: the Bicep templates, the Azure Pipelines definitions, Key Vault configuration in the backend (a placeholder remains in `Program.cs`), and the deployment guide described above. They are the next step for this ADR, and this ADR is the specification for them.

## Consequences

### Easier

- Predictable, low monthly cost
- PostgreSQL is external to the app service, avoiding the persistence and locking problems of file-based databases
- HTTPS on a custom domain for both components
- Simpler than ACA or AKS
- Static generation keeps the frontend free of server runtime concerns
- Secrets stay out of source and pipelines, because the app reads them from Key Vault with its managed identity

### More difficult

- Less auto-scaling: App Service is scaled manually, and cannot scale to zero like ACA
- May need to migrate to ACA if traffic becomes highly variable
- Frontend and backend deploy separately, so deployment order and version compatibility matter ([0010](0010-backend-for-frontend.md))
- The starter kit takes on maintaining the infrastructure templates and pipeline steps, and must keep them in sync with the backend's configuration keys
- Templates and pipelines are Azure-specific, so projects that deviate from this infrastructure must adapt them

## Related Decisions

- [ENVIRONMENTS](../ENVIRONMENTS.md) - How Development, Container, and Production environments differ
- [0003. Nuxt](0003-nuxt.md) - Static generation of the frontend
- [0004. Aspire Development](0004-aspire-development.md) - Development orchestration differs from production deployment
- [0005. Database Backend](0005-database-backend.md) - PostgreSQL in all environments
- [0007. Backend Proxy or Direct](0007-backend-proxy-or-direct.md) - How the frontend reaches the backend
- [0010. Backend For Frontend](0010-backend-for-frontend.md) - Version compatibility across separate deployments
