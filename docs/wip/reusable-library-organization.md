# Reusable Library and Template Organization

Date: 2026-10-03

Status: Working proposal. This document does not supersede the existing architecture decisions.

## Goal

Evolve the starter kit into two complementary products:

- A .NET template containing code downstream developers are expected to modify.
- A family of libraries containing reusable components maintained centrally and upgraded through package references.

Use a monorepo to develop and test these together, with separate packages published through a coordinated release.

## Organize Packages by Capability, Not Application Layer

The application's Clean Architecture layers describe where application-owned code belongs. Package boundaries describe capabilities consumers can adopt and upgrade independently. These are related, but should not be identical by default.

The organizing principle is:

> Libraries provide reusable mechanisms and explicit conventions; the template owns application policy and composition.

| Library responsibility | Template/application responsibility |
|---|---|
| Console formatter and registration | Whether and where to enable it |
| Problem response support | Application-specific messages and exception mappings |
| Version endpoint implementation | Which application version to report |
| Persistence contracts | Domain models and application-specific persistence requirements |
| Generic database connectivity check | Which context to check and readiness policy |
| CORS registration helpers | Origins, credential policy, and exposed headers |
| Telemetry configuration helpers | Application sources, exporters, and environment settings |

Domain-agnostic code is a useful extraction candidate, but may still contain policy that developers should control. Conversely, an intentionally opinionated convention can belong in a library if its behavior is documented and consumers can configure or opt out of it.

## Recommended Package Family

Names below are illustrative; choose stable package and namespace names before the first public release.

| Package | Contents | Recommendation |
|---|---|---|
| `DotNetNuxt.Data.Abstractions` | `IModel`, `IDataProvider`, and future shared persistence contracts | Extract initially: these contracts are already reused across applications |
| `DotNetNuxt.Logging` | Terse console formatter, options, and registration | Extract initially |
| `DotNetNuxt.AspNetCore` | Problem context integration, reusable exception handling, version endpoint, test correlation, optional CORS helpers | Extract initially |
| `DotNetNuxt.Hosting` | Telemetry, resilience, service discovery, and configurable health conventions | Extract once application policy has been separated |
| `DotNetNuxt.EntityFrameworkCore` | Reusable implementation of persistence contracts and generic EF health support | Extract as implementation behavior becomes complete and proven |
| `DotNetNuxt.Templates` | Installable template with application-owned code and package references | Add after the starter kit successfully consumes the libraries |

Do not create empty `Entities` or `Application` packages merely to mirror the application layers. Keep domain models, use cases, controllers for business features, database mappings, migrations, and application composition in generated source.

Do not split every middleware into a separate package. Split when a distinct dependency footprint, adoption choice, or release lifecycle provides a concrete benefit.

## Persistence Abstractions Are an Initial Extraction

The contracts in [Entities/Abstractions](../../src/Entities/Abstractions) are reused in every application in practice. That is sufficient evidence to treat them as an established shared capability, rather than waiting for another application to demonstrate reuse.

Package both [IModel](../../src/Entities/Abstractions/IModel.cs) and [IDataProvider](../../src/Entities/Abstractions/IDataProvider.cs) in `DotNetNuxt.Data.Abstractions`.

This package should:

- Depend only on the base .NET libraries, not EF Core, PostgreSQL, ASP.NET Core, or Aspire.
- Be referenced by application domain and feature projects as well as persistence adapters.
- Keep concrete models and application-specific contracts in the application's Entities project.
- Preserve the existing query-oriented approach rather than introduce a repository abstraction solely for packaging.

### Be Explicit About the Contract

Framework-independent signatures do not imply that every storage technology can implement every operation equally well.

`IDataProvider` exposes `IQueryable<T>`, expression-based navigation loading, tracked mutations, asynchronous execution, and bulk operations. It is a shared query-oriented persistence contract. Consumers still need to understand provider translation limits; arbitrary LINQ expressions are not guaranteed to work across different backends.

`IModel` deliberately establishes an integer identity convention. Preserve that proven convention for the initial extraction rather than speculatively introducing generic key types. However, distinguish the API guarantee from database implementation details: an `int Id` property does not itself guarantee increasing values, clustering, or any particular index layout.

Before freezing a public API, document and verify:

- Whether each mutation is staged until `SaveChangesAsync`, or executes immediately.
- Tracking and no-tracking behavior.
- Cancellation behavior; currently only `SaveChangesAsync` exposes a cancellation token.
- Bulk update column naming and selection semantics.
- Whether bulk operations bypass tracking, and how callers handle already-tracked entities afterward.
- What affected-row counts mean for clear, delete, update, and save operations.
- How unsupported operations are reported by each implementation.

The current [ApplicationDbContext](../../src/Data/Postgres/ApplicationDbContext.cs) throws `NotImplementedException` for clear and bulk operations. Extracting the interfaces is still appropriate, but do not publish that implementation as a complete reusable adapter without implementing those operations or explicitly defining supported capabilities and failure behavior.

Do not redesign the interfaces merely to extract them. Resolve any necessary contract changes deliberately before a stable release, and preserve behavior with contract tests.

### Separate Contracts from EF Implementation

If the generic `IDataProvider` implementation is also reused across applications, place that implementation in `DotNetNuxt.EntityFrameworkCore`, not in the abstractions package.

A reusable context base class or adapter can own generic query, mutation, and execution behavior. The application should retain its concrete context, entity sets, model configuration, migrations, and DI registration. Choose the base-class or adapter approach based on existing application usage rather than introducing both.

Provider-specific dependencies and bulk-operation implementations should remain outside the framework-independent contracts package. A separate PostgreSQL package is warranted only when there is enough reusable provider-specific behavior to justify it.

## Dependency Direction

The library family must not reference generated application projects.

```text
Application.Entities ------> Data.Abstractions
Application.Features ------> Application.Entities
Application.Features ------> Data.Abstractions (where directly used)
Application.Data ----------> Application.Entities
Application.Data ----------> EntityFrameworkCore ------> Data.Abstractions
Application.Controllers ---> Application.Features
Application.Controllers ---> AspNetCore
Application.Host ----------> application projects and selected support packages
```

If the EF implementation has not yet been extracted, the application's Data project implements `Data.Abstractions` directly.

Avoid a dependency from Logging to AspNetCore merely to read the test-name logging scope. The formatter and middleware can share a documented scope convention without requiring that dependency. Introduce a shared contracts package only if the shared surface grows enough to warrant it.

## Existing Extraction Candidates and Caveats

### Logging

Extract [TerseConsoleLogFormatter](../../src/ServiceDefaults/TerseConsoleLogFormatter.cs), its options, and registration independently of Aspire.

Treat its test-name scope support as a documented convention. Applications choose when to enable the formatter.

### ASP.NET Core Support

Extract these related components together:

- [ProblemContextAttribute](../../src/Controllers/Attributes/ProblemContextAttribute.cs) and its problem-details integration in [ControllersExtensions](../../src/Controllers/Extensions.cs).
- [ArgumentExceptionHandler](../../src/Controllers/Middleware/ArgumentExceptionHandler.cs), with explicit opt-in to its argument-exception-to-400 convention.
- [VersionController](../../src/Controllers/VersionController.cs) and [VersionOptions](../../src/Controllers/VersionOptions.cs).
- [TestCorrelationMiddleware](../../src/Controllers/Middleware/TestCorrelationMiddleware.cs), with the host choosing whether to enable it.

Preserve focused `Add...` and `Use...` methods. A convenience method may compose them, but should not make replacing one convention or controlling middleware order difficult.

Explicitly register the packaged version controller's MVC application part. Verify discovery both in the backend and in [ApiClientGenerator](../../tools/ApiClientGenerator/Program.cs), including generated frontend API output.

[GetVersion](../../src/BackEnd/Startup/SetupVersion.cs) currently uses `Assembly.GetExecutingAssembly()`. Moving it into a package would report the library's version. Have the application supply its assembly or version explicitly.

### Host Policy

Keep the aggregate [StartupOptions](../../src/BackEnd/Options/StartupOptions.cs) in the template initially. It combines HTTPS, Swagger UI, and CORS decisions belonging to the application. Libraries can expose focused options without owning the entire startup configuration schema.

[SetupCors](../../src/BackEnd/Startup/SetupCors.cs) contains reusable registration mechanics but also credential, header, and method policy. Make those choices explicit if packaging the helper.

[ServiceDefaults](../../src/ServiceDefaults/Extensions.cs) combines reusable infrastructure with a starter-kit-specific tracing source pattern and health policy. Make application sources and health endpoint conventions configurable, or leave those choices in generated startup code.

The backend now uses EF Core's built-in `AddDbContextCheck<ApplicationDbContext>()` support rather than a custom health-check type. If this behavior moves into a reusable library, keep it generalized over `DbContext`; the application should still decide registration, tags, routes, and whether database availability affects readiness.

[AddApplicationFeatures](../../src/Application/Extensions.cs) should remain application-owned: its registration of `WeatherForecastFeature` changes as applications add features. A generally useful default such as `TimeProvider.System` does not justify packaging the application's feature registration method.

## Monorepo Layout

One possible eventual layout:

```text
src/
  Libraries/
    Data.Abstractions/
    Logging/
    AspNetCore/
    Hosting/
    EntityFrameworkCore/
templates/
  WebApp/
tests/
  Libraries/
  PackageIntegration/
  TemplateSmoke/
samples/
  ReferenceApp/
tools/
```

This is an eventual shape, not a prerequisite for extraction. Keep the current starter kit runnable as the reference consumer while introducing package projects. Avoid a large directory rearrangement at the same time as changing assembly boundaries.

Use project references during normal monorepo development, but also test packed NuGet artifacts so project references do not conceal missing package dependencies, content, or registration behavior.

## Coordinated Releases

Initially use one version for the package family:

1. Build and test all libraries and the reference application.
2. Pack all artifacts from the same commit and version.
3. Validate package-consuming and template-generated applications against those artifacts.
4. Publish library packages in dependency order and verify their availability.
5. Publish the template with a pinned, tested set of package versions.

This intentionally permits no-op package releases in exchange for simpler compatibility management. Move to independent versions only if components develop meaningfully different release lifecycles.

NuGet publication is not atomic across packages. Retain the tested artifacts so interrupted publication can resume without rebuilding different contents for the same version. Never overwrite an existing version.

Use semantic versioning for public APIs and documented behavior. Changes to HTTP responses, startup defaults, persistence semantics, and health conventions can be breaking changes even when method signatures remain unchanged.

## Incremental Extraction and Verification

1. Extract Data.Abstractions, Logging, and AspNetCore without changing intended behavior.
2. Make the existing starter kit consume those projects; retain application composition.
3. Exercise the packages in another existing application, especially the shared persistence contracts.
4. Test packaged consumption, controller discovery, problem responses, version reporting, logging scopes, and API generation.
5. Extract Hosting and EF implementation support when policy boundaries and operation semantics are established.
6. Add template packaging and smoke-test generation, restore, build, tests, and API generation.

For persistence implementations, run common contract tests against each supported provider. Tests should verify actual mutation, tracking, query, and bulk-operation semantics, not just that the interfaces compile.

Template updates affect newly generated applications. Library updates can benefit existing applications through package upgrades. Existing applications continue to own their copied startup code, configuration, models, migrations, and business features; template upgrades do not automatically merge changes into those files.

## Recommendation

Adopt a monorepo with separate capability-oriented packages and coordinated releases. Start with **Data.Abstractions, Logging, and AspNetCore**. The persistence abstractions are already proven reusable and belong in the first extraction, while application-specific Entities and Application code remain in the template.

Keep the composition root visible, make conventions explicit, and validate both the reference application and actual package consumers before publishing a template.

## Related Architecture

- [Clean Architecture](../adr/0011-clean-architecture.md)
- [Database Backend](../adr/0005-database-backend.md)
- [Aspire Development](../adr/0004-aspire-development.md)
