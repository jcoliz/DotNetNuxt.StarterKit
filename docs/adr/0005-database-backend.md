# 0005. Database Backend

Date: 2026-10-01

## Status

Accepted

## Context

Apps built on this stack need a database for persistent data.

### Requirements

1. Low cost in production at low volumes
2. Easy to use during development
3. Runs in CI pipelines
4. Easy to use from Azure App Service
5. Scales up when volumes grow
6. One data implementation, not one per environment

### Options considered

#### 1. SQLite (rejected)

**Pros:** A single implementation that works everywhere, no cost, no server, trivial in CI, and full EF Core support.

**Cons:**
- No reliable concurrent writes at scale
- Not safe to share across multiple app instances
- On Azure App Service, the file must live on network-backed storage (Azure Files). SQLite assumes local file semantics and behaves badly there: file locking problems, high latency, and risk of corruption during restarts and deployments.
- [Migration limitations](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations#migrations-limitations) and feature gaps (e.g. server-side generation of GUID values)

SQLite is fine for prototypes and isolated development, but it is not a durable choice for Azure App Service or anywhere that needs multiple instances, restarts, or real concurrency.

#### 2. PostgreSQL everywhere (chosen)

**Pros:**
- One implementation across development, CI, and production
- Available as Azure Database for PostgreSQL Flexible Server
- Runs as a container locally, and works well with Aspire and Docker Compose
- Strong feature set and reliability
- Multiple app environments can share one server

**Cons:**
- Needs a container or managed service in local development
- Slightly more setup than SQLite

#### 3. Azure SQL Server (fallback)

**Pros:** Proven on Azure App Service, with a low-cost basic tier.

**Cons:** In practice it needs a different database for local development and CI, which means maintaining two data implementations and configuration to select between them.

## Decision

1. **Use PostgreSQL in every environment**: development, CI/CD, and production.
2. **Local development**: Aspire starts a PostgreSQL container ([0004](0004-aspire-development.md)). The container environment uses Docker Compose with a PostgreSQL image.
3. **Production**: Azure Database for PostgreSQL Flexible Server ([0006](0006-production-infrastructure.md)).
4. **No per-environment providers**: PostgreSQL is the single supported database. SQLite is not supported.

### Implementation

- The `src/Data/Postgres` project holds the EF Core `ApplicationDbContext`, migrations, and registration (`Extensions.cs`). See its README for migration workflow.
- `ApplicationDbContext` implements the storage-agnostic `IDataProvider` abstraction ([0011](0011-clean-architecture.md)).
- In Production, the app authenticates to PostgreSQL with an Azure Entra managed identity token, refreshed periodically. In all other environments, the connection string is used as-is.
- Scripts in `scripts/` (`Add-Migration.ps1`, `Export-PostgresMigration.ps1`) support creating and exporting migrations. `tools/Postgres.MigrationsMain` supports design-time tooling.

## Consequences

### Easier

- One data model and one database technology everywhere, so behavior is consistent from local to production
- Simpler operations and fewer deployment surprises
- A good fit for concurrency, scaling, and Azure-hosted workloads
- One migration path for all environments

### More difficult

- A database container or service is required in local development
- A managed PostgreSQL service must be provisioned and maintained in Azure
- Developers need a container runtime installed

### Mitigation

- Aspire and Docker Compose keep local setup simple and repeatable
- Use a single migration and deployment path for all environments

## Related Decisions

- [0004. Aspire Development](0004-aspire-development.md) - Provisions the local PostgreSQL container
- [0006. Production Infrastructure](0006-production-infrastructure.md) - Azure-hosted PostgreSQL in production
- [0011. Clean Architecture](0011-clean-architecture.md) - The `IDataProvider` abstraction in front of the database
