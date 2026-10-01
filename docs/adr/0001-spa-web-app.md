# 0001. Single Page Web App

Date: 2026-10-01

## Status

Accepted

## Context

A new web application built on this stack needs a user interface architecture. The two realistic choices are:

1. **Server-rendered web app** (e.g. ASP.NET Razor/MVC). Every navigation is a full-page round trip to the server, and UI and business logic live in the same deployable.
2. **Single Page Application (SPA)**. A client-side app loads once, then fetches only data (JSON) from an API.

## Decision

The starter kit is built as a **SPA with a separate .NET API backend**.

The frontend is a Vue/Nuxt app ([0002](0002-vue-js.md), [0003](0003-nuxt.md)) and the backend is an ASP.NET Core Web API (`src/BackEnd`). The two communicate over HTTP/JSON.

> **Implementation status:** The backend is implemented. The frontend (`src/FrontEnd.Nuxt`) is not yet implemented; this ADR describes the intended architecture.

## Consequences

### Easier

- **Responsive UI** - After the initial load, navigation fetches only data. Loading states, skeletons, and optimistic updates are straightforward.
- **Clear separation of concerns** - The API boundary is explicit. The backend owns data and business logic; the frontend owns presentation.
- **Independent deployment** - Frontend and backend deploy separately, and each can be hosted on infrastructure suited to it (see [0006](0006-production-infrastructure.md)).
- **Static hosting and CDN** - The built frontend is plain static files, which are cheap to host and fast to serve globally.
- **Modern tooling** - Hot module replacement, component dev tools, and the Vite ecosystem.
- **API reuse** - The same API can serve other clients later, though the starter kit does not optimize for this (see [0010](0010-backend-for-frontend.md)).

### More difficult

- **SEO** - Requires pre-rendering or SSR for public content. Less relevant for authenticated apps.
- **Initial load** - A larger JavaScript bundle must download before the app is interactive.
- **JavaScript required** - The app does not function without it.
- **More moving parts** - Client-side routing, state, and a second toolchain (Node.js) alongside .NET.
- **Version skew** - Frontend and backend are deployed separately, so they must tolerate running at different versions (see [0010](0010-backend-for-frontend.md)).

### When not to use this stack

A SPA is a poor fit for content-heavy, SEO-driven public sites, or for apps that must work without JavaScript. Projects like that should start from a server-rendered approach instead.

## Related Decisions

- [0002. Vue.js Frontend](0002-vue-js.md) - Choice of frontend framework for implementing this SPA
- [0003. Nuxt](0003-nuxt.md) - Meta-framework used to build the SPA
- [0006. Production Infrastructure](0006-production-infrastructure.md) - How the separately deployed pieces are hosted
