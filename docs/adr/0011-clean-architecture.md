# 0011. Clean Architecture Pattern

Date: 2026-10-01

## Status

Accepted

## Context

The backend needs an architectural pattern that provides:

- Separation of business rules from infrastructure
- Testability at every layer
- Independence from UI frameworks and databases
- The ability to defer infrastructure decisions

### Alternatives

1. **N-Tier / Layered** - Strict layering, but no dependency inversion. Business logic depends on the data layer, which couples it to the database and makes testing hard.
2. **Hexagonal (Ports and Adapters)** - Conceptually similar to Clean Architecture, but with terminology that is less familiar to .NET developers.
3. **Vertical Slice** - Each feature has its own controller, logic, and data. Duplicates shared concerns, and suits microservices better than a monolith.
4. **Onion** - Nearly identical to Clean Architecture, with fewer .NET examples.
5. **Feature folders** - Flat organization by feature, with no dependency rules and no built-in testability.

## Decision

Adopt **Clean Architecture**, with this layering:

```
BackEnd (host) → Controllers → Application → Entities ← Data
```

Dependencies flow inward. Inner layers know nothing about outer layers.

### Layers

#### 1. Entities (`src/Entities`)

The core domain layer.

- Domain models (e.g. `WeatherForecast`)
- Abstractions for infrastructure, notably `IDataProvider` and `IModel`
- Domain exceptions

**Rules:** No dependencies on other layers. No knowledge of databases, frameworks, or UI. Only domain concepts and contracts.

#### 2. Application (`src/Application`)

The business logic layer.

- Feature classes that implement business capabilities and use cases (e.g. `WeatherForecastFeature`)
- DTOs, workflows, and validation

**Rules:** Depends only on Entities. No knowledge of controllers, databases, or UI frameworks. Uses `IDataProvider` for queryable data access.

#### 3. Controllers (`src/Controllers`)

The API boundary layer.

- HTTP endpoints, request/response handling, and status code mapping
- Logging at the API boundary
- Error handling middleware (e.g. mapping argument exceptions to ProblemDetails)
- OpenAPI documentation

**Rules:** Depends on Application and Entities. Thin: delegates to Application features. No business logic.

#### 4. Data (`src/Data/Postgres`)

The infrastructure layer.

- Entity Framework `DbContext` and configuration
- Database migrations
- The `IDataProvider` implementation, which exposes `IQueryable<T>` for each entity

**Rules:** Implements interfaces from Entities. No knowledge of Controllers or Application.

Currently the only implementation is PostgreSQL ([0005](0005-database-backend.md)).

#### 5. BackEnd (`src/BackEnd`)

The host and composition root.

- Startup and configuration
- Dependency injection registration
- Middleware pipeline
- CORS, Swagger, health checks
- Environment-specific settings

**Rules:** References all other projects to wire them together. Contains no business logic.

#### Supporting projects

- `src/ServiceDefaults` - Shared telemetry and health check configuration (Aspire)
- `src/AppHost` - Development orchestration ([0004](0004-aspire-development.md))
- `tests/Unit` - Unit tests for Application features
- `tools/` - Tooling for API client generation and migrations

### Dependency inversion

> High-level modules should not depend on low-level modules. Both should depend on abstractions.

1. Application features depend on `IDataProvider` (defined in Entities), not on Entity Framework.
2. Controllers depend on Application features, not on data access.
3. Entities has no dependencies. It is the stable core.

### Combined with BFF

Clean Architecture is combined with the Backend For Frontend pattern ([0010](0010-backend-for-frontend.md)): DTOs are shaped for the frontend, business logic is in the Application layer, and controllers stay thin.

## Key patterns

**Data access, not the Repository pattern**
- `IDataProvider` exposes `IQueryable<T>` so features write LINQ directly.
- Execution helpers (`ToListNoTrackingAsync`, `CountAsync`, etc.) are on the interface too, so features need no EF reference.
- This avoids repository boilerplate. If a feature needs an operation that doesn't fit, add a narrowly scoped interface in Entities for it.

**Feature pattern**
- Each business capability is a feature class.
- Features get dependencies (`IDataProvider`, `TimeProvider`) through constructor injection.
- Features return DTOs or models, not data-layer types.

**Dependency injection**
- Each layer has an `Extensions.cs` that registers its own services. The BackEnd host calls them in turn.

## Consequences

### Easier

- Features are testable in isolation by substituting `IDataProvider` and `TimeProvider`
- Business logic has no Entity Framework dependency
- The database provider can be swapped behind the Data layer
- Each layer has a single responsibility, and code is easy to locate
- The Application layer can be reused from CLI tools or background jobs
- Infrastructure decisions can be deferred

### More complex

- Developers must understand layer boundaries and dependency rules
- More files: interfaces, DTOs, and feature classes
- More layers to trace when debugging
- A simple CRUD feature needs more ceremony than it would in a flat structure

## Extending the starter kit

- **External services** (email, storage, etc.): add each as its own project (e.g. `Services.Email`), implementing an interface from Entities. Keep them separate rather than in one monolithic Infrastructure project, so they stay independently testable and swappable.
- **Other databases**: add another `Data.*` project implementing `IDataProvider`. The kit supports PostgreSQL only by decision ([0005](0005-database-backend.md)).

## Related Decisions

- [0005. Database Backend](0005-database-backend.md) - The Data layer implementation
- [0010. Backend For Frontend](0010-backend-for-frontend.md) - Why logic and DTOs are shaped this way

## References

- Robert C. Martin, *Clean Architecture: A Craftsman's Guide to Software Structure and Design*
- [The Clean Architecture (blog post)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Microsoft: Common web application architectures](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture)
