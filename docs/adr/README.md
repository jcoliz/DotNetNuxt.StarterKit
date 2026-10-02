# Architecture Decision Records

These records explain the major choices behind the starter kit's web stack and architecture.

## Decisions

### [0001. Single Page Web App](0001-spa-web-app.md)

The starter kit uses a single-page application with a separate ASP.NET Core API. This creates a clear HTTP/JSON boundary and allows independent deployment, while accepting additional JavaScript tooling, client-side complexity, and reduced suitability for SEO-heavy or no-JavaScript sites.

### [0002. Vue.js as Front-End Framework](0002-vue-js.md)

Vue 3 with the Composition API and TypeScript is the frontend framework. Its HTML-like templates, progressive structure, component model, and TypeScript support are approachable for .NET developers; the trade-offs include a smaller ecosystem and framework-specific skills.

### [0003. Using Nuxt](0003-nuxt.md)

Nuxt provides the Vue application structure, routing, tooling, and static generation. The frontend is intended to deploy as static files without a runtime Node.js server, communicating with the .NET API; this excludes request-time server rendering and Nuxt server routes.

### [0004. .NET Aspire for Development Orchestration](0004-aspire-development.md)

.NET Aspire orchestrates the local development stack and provides service discovery and observability. Docker Compose remains responsible for the container environment, while production uses Azure infrastructure directly; Aspire is not the production deployment platform.

### [0005. Database Backend](0005-database-backend.md)

PostgreSQL is the sole supported database across development, CI, and production, avoiding provider-specific behavior and migrations. Aspire and Docker Compose provide local instances; Azure Database for PostgreSQL is the production target. SQLite and per-environment database providers are explicitly excluded.

### [0006. Production Infrastructure](0006-production-infrastructure.md)

Production targets Azure Static Web Apps for the generated frontend and Azure App Service for the .NET API, backed by PostgreSQL. Key Vault, managed identity, Application Insights, and Azure Pipelines complete the intended secure deployment; infrastructure templates and pipelines remain planned work.

### [0007. Proxy to Backend or Make Direct Calls?](0007-backend-proxy-or-direct.md)

The browser calls the API directly at its own URL, and the backend allows explicitly configured frontend origins through CORS. This keeps the frontend static and supports lower-cost hosting, at the cost of environment-specific URLs and CORS configuration.

### [0010. Backend For Frontend Pattern](0010-backend-for-frontend.md)

The backend is tailored to its own frontend rather than designed as a reusable public API. Business logic and UI-shaped data contracts stay in .NET; generated TypeScript clients keep contracts aligned. Separate deployments follow an N-1 compatibility policy, with backend deployment first.

### [0011. Clean Architecture Pattern](0011-clean-architecture.md)

The backend follows Clean Architecture: dependencies point inward from the host and controllers through application logic to entity abstractions, while data infrastructure implements those abstractions. Thin controllers, testable features, and provider-independent business logic guide organization and dependencies.

### [0012. Component Data Fetching Pattern](0012-component-data-fetching-pattern.md)

Pages load initial view data, while discrete action components may make their own API calls and emit mutation events. Generated clients report errors through shared handling. This favors component reuse and isolated testing over centralized state, accepting that redundant calls and stale data need case-by-case attention.