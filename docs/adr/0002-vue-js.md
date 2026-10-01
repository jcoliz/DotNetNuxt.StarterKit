# 0002. Vue.js as Front-End Framework

Date: 2026-10-01

## Status

Accepted

## Context

Having chosen a SPA ([0001](0001-spa-web-app.md)), we need a frontend framework. The leading choices are:

| Framework | Notes |
|-----------|-------|
| **React** (Meta) | Largest ecosystem. JSX. Meta-frameworks: Next.js, Remix. |
| **Vue.js** (community) | Progressive framework with template syntax. Meta-frameworks: Nuxt, Quasar. |
| **Angular** (Google) | Full-featured, opinionated, TypeScript-first. |
| **Svelte/SvelteKit** | Compile-time framework, no virtual DOM. |
| **Solid.js, Qwik** | Newer, performance-focused. |

## Decision

Use **Vue.js 3** (Composition API, TypeScript) with Nuxt ([0003](0003-nuxt.md)).

### Primary reasons

- **Familiar to .NET developers.** Template syntax and single-file components (`.vue`) feel close to HTML/Razor.
- **Balanced.** Less opinionated than Angular, more structured than React. Official solutions exist for routing and state, but are not forced.
- **Good TypeScript support.** Vue 3 is written in TypeScript and the Composition API infers types well.
- **Component-friendly.** Single-file components with scoped styles make reusable component libraries easy, and work well with CSS frameworks such as Bootstrap.
- **Independent governance.** Not controlled by a single large corporation.
- **Good tooling.** Vite, Vue DevTools, Volar for VS Code, and Vitest/Vue Test Utils.

### Why not the others?

- **React** is excellent and has the largest ecosystem, but JSX is less familiar to ASP.NET developers and the ecosystem is more fragmented (many competing choices for routing and state).
- **Angular** is powerful for large enterprise apps, but has a steeper learning curve and more ceremony.
- **Svelte** is innovative, but has a smaller ecosystem and less community material.

## Consequences

### Easier

- Gentle learning curve for developers coming from ASP.NET
- Progressive adoption: start simple, add complexity as needed
- Composition API lets logic be extracted into reusable composables

```typescript
import { ref, computed } from 'vue'

export function useCounter() {
  const count = ref(0)
  const doubled = computed(() => count.value * 2)
  return { count, doubled }
}
```

### More difficult

- Smaller ecosystem than React: fewer third-party libraries and less community content
- Vue-specific knowledge is less transferable than React knowledge
- No major corporate sponsor; funding relies on community and sponsors
- TypeScript is optional in Vue, so the project must keep its own conventions consistent

## Related Decisions

- [0001. Single Page Web App](0001-spa-web-app.md) - Why a frontend framework is needed
- [0003. Nuxt](0003-nuxt.md) - Meta-framework built on Vue.js

## References

- [Vue.js Documentation](https://vuejs.org/)
- [Nuxt Documentation](https://nuxt.com/)
