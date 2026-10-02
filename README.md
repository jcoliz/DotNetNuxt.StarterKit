# DotNetNuxt Starter Kit

DotNetNuxt Starter Kit is a reference implementation of our web application stack: a Nuxt frontend with an ASP.NET Core backend, PostgreSQL, and .NET Aspire for local orchestration. It provides a working foundation and conventions for applications built on this stack.

## Technology Stack

- **Frontend:** Nuxt 4, Vue 3, TypeScript, Bootstrap
- **Backend:** .NET 10, ASP.NET Core, Entity Framework Core
- **Database:** PostgreSQL
- **Local orchestration:** .NET Aspire
- **Package management:** pnpm

## Project Structure

- `src/FrontEnd.Nuxt` - Nuxt application
- `src/BackEnd` - ASP.NET Core host and startup configuration
- `src/Controllers` - HTTP API endpoints
- `src/Application` - Application features
- `src/Entities` - Domain models and abstractions
- `src/Data/Postgres` - PostgreSQL data access
- `src/AppHost` - Aspire orchestration for the frontend, backend, and database
- `tests` - Automated tests

## Getting Started

Prerequisites: .NET 10 SDK, Node.js 24+, pnpm, and Docker Desktop.

Set up dependencies and verify the build:

```powershell
./scripts/Setup-Development.ps1
```

Start the local application:

```powershell
./scripts/Start-AppHost.ps1
```

## Architecture

See [docs/adr/README.md](docs/adr/README.md) for summaries of the architecture decisions and links to each record.

## Work to Come

This is an evolving reference implementation. Planned work, including functional tests, production infrastructure, CI/release pipelines, and authentication and authorization, is tracked in [docs/TODO.md](docs/TODO.md).
