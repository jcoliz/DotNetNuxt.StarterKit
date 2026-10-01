# 0007. Proxy to Backend or Make Direct Calls?

Date: 2026-10-01

## Status

Accepted

## Context

With a statically hosted frontend and a separately hosted backend ([0006](0006-production-infrastructure.md)), there are two ways for the browser to reach the API:

1. **Proxy**: the static host forwards `/api` calls to the backend (e.g. the Azure Static Web Apps [linked backend](https://learn.microsoft.com/en-us/azure/static-web-apps/apis-app-service) feature).
2. **Direct**: JavaScript in the browser calls the backend at its own URL.

## Decision

The frontend **calls the backend directly**. The backend enables CORS for the frontend's origin.

> **Implementation status:** The backend side (CORS) is implemented. The frontend is not yet implemented, so the `NUXT_PUBLIC_API_BASE_URL` wiring is described here but not yet in place.

## Consequences

### Easier

* **Lower cost** - Static Web Apps linked backends require the Standard tier. Direct calls work on the Free tier, which still includes custom domains and HTTPS.
* **Truly static frontend** - `nuxt generate` output needs no server-side proxying.
* **Independent deployment** - Frontend and backend have no hosting-level coupling.
* **Simple CORS** - Configured in one place in the .NET backend.
* **Observability** - Requests go straight to the backend, which makes tracing easier.
* **CDN-friendly** - The static host serves only static content.

### More difficult

* **The frontend build is tied to a backend URL.** The built frontend embeds the backend's address, so a build is specific to its target environment and is less portable.
* **CORS must be configured per environment**, so a production backend does not accept requests from development clients.
* **No single URL.** Users can see the backend URL in network requests. This is a minor concern.
* **Client-side error handling** - Unreachable backends and CORS failures show up in the browser and need client-side error handling and telemetry.

## Implementation

### Frontend: backend URL

Set `NUXT_PUBLIC_API_BASE_URL` **when generating** the static site, to point to the backend API URL. The Aspire AppHost sets it for development ([0004](0004-aspire-development.md)). In the container and production builds, the build step sets it, for example through a Docker build `ARG` or a pipeline variable.

### Backend: CORS

Allowed origins are explicit, per-environment configuration:

* [`StartupOptions.AllowedCorsOrigins`](../../src/BackEnd/Options/StartupOptions.cs) - the configuration property (section `Startup`)
* [`SetupCors.cs`](../../src/BackEnd/Startup/SetupCors.cs) - builds the CORS policy from those origins and exposes the `content-disposition` header so the browser can read download filenames
* [`appsettings.Development.json`](../../src/BackEnd/appsettings.Development.json) - development origins (empty by default; add the local frontend dev server origin, e.g. `http://localhost:5173`)
* [`docker-compose-ci.yml`](../../docker/docker-compose-ci.yml) - sets `STARTUP__ALLOWEDCORSORIGINS__0` for the container environment
* Production - set `Startup__AllowedCorsOrigins__0` in the App Service configuration to the static site's origin

Security properties:
* No wildcard origins. Every origin is listed explicitly per environment.
* A development backend accepts only local development origins.
* A production backend accepts only the production frontend origin.
* Preflight requests are handled by ASP.NET Core middleware.

### Open considerations

These do not change the decision, but each project should decide how to handle them:

* What the UI shows when the backend is unreachable
* How to monitor and debug CORS failures in production
* How to report client-side errors (e.g. with Application Insights)
* How to handle a partial deployment, or roll back both components. These problems exist with a proxy too. See [0010](0010-backend-for-frontend.md) for version compatibility.

## Related Decisions

- [0004. Aspire Development](0004-aspire-development.md) - Supplies the backend URL in development
- [0006. Production Infrastructure](0006-production-infrastructure.md) - The hosting this decision is made for
- [0010. Backend For Frontend](0010-backend-for-frontend.md) - Version compatibility between separately deployed components
