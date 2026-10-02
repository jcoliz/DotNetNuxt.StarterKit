# 0008. Identity and API Authentication

Date: 2026-10-02

## Status

Accepted

## Context

The frontend is a statically generated Nuxt SPA and the backend is a separately hosted ASP.NET Core API. The browser calls the API directly ([0001](../0001-spa-web-app.md), [0003](../0003-nuxt.md), [0007](../0007-backend-proxy-or-direct.md)). Applications built from this starter may need user accounts, but authentication is not required by every application and is not currently implemented in the starter.

Authentication must work without a Nuxt server at request time. Authorization must remain a backend responsibility: hiding a control or route in the browser is not an access check.

## Decision

For applications that need local user accounts:

- Use **ASP.NET Core Identity** for account and credential management. Do not implement password storage, password verification, or account recovery from scratch.
- Authenticate API requests with **short-lived bearer access tokens**. The API validates the token's signature, issuer, audience, and lifetime, and requires HTTPS outside local development.
- Use a maintained Nuxt-compatible authentication integration for browser sign-in and session handling. The intended reference integration is [Nuxt Identity](https://github.com/jcoliz/NuxtIdentity) with [Nuxt Auth](https://auth.sidebase.com/); verify that the selected releases support the current Nuxt version and static deployment model before adopting them.
- Keep token issuance, validation, refresh, revocation, and key management on the backend or in the chosen authentication integration. Follow that integration's documented secure storage model; do not put long-lived credentials or refresh tokens in `localStorage`.
- Enforce authorization in the API for every protected operation. Global roles may be represented as claims; access to application-owned resources must be checked against current server-side ownership or membership data (see [0009](0009-accounts-and-tenancy.md)).
- Treat external identity providers as an application decision. When required, prefer a standard OpenID Connect integration over custom provider protocols.

The frontend may contain public configuration such as the API base URL, but never signing keys, client secrets, or other credentials. Authentication does not change the direct API and explicit CORS model in [0007](../0007-backend-proxy-or-direct.md).

> **Implementation status:** Authentication and authorization are not wired into this starter yet. The choice of ASP.NET Core Identity and the Nuxt integration is the intended reference path, not a claim that these protections are already present. A production implementation must include integration tests for sign-in, token lifecycle, unauthorized requests, and authorization boundaries.

## Consequences

### Easier

- Account and credential handling use established framework components rather than application-specific password code.
- The API remains the authority for protected data while the frontend can be hosted as static files.
- A standard token boundary keeps the API independent of Nuxt's client-side session state.

### More difficult

- The application must configure token issuance and validation correctly; ASP.NET Core Identity alone does not provide a secure API token system.
- Browser token lifecycle, refresh, revocation, and cross-origin requests require careful design and testing.
- Authorization policies and resource ownership checks are application-specific and must be maintained with the data model.

## Related Decisions

- [0001. Single Page Web App](../0001-spa-web-app.md) - Separate frontend and API
- [0003. Nuxt](../0003-nuxt.md) - Static frontend architecture
- [0007. Backend Proxy or Direct](../0007-backend-proxy-or-direct.md) - Direct API calls and CORS
- [0009. Tenant and Workspace Data Boundaries](0009-accounts-and-tenancy.md) - Optional data isolation and membership authorization
