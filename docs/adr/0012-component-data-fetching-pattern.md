# 0012. Component Data Fetching Pattern

Date: 2026-10-01

## Status

Accepted

## Context

Should Vue components make their own API calls, or should data fetching be centralized at the page level, with data passed to children via props?

> **Implementation status:** The frontend is not yet implemented. This ADR sets the convention for it. Examples use the starter kit's sample `Weather` API. The generated API client (`app/utils/apiclient.ts`, from `tools/ApiClientGenerator`) and the `AuthorizedApiBase` class exist. The `useApiClient()` and `useProblemDetails()` composables referenced below are planned.

### Two patterns

**Pattern A: Distributed**
```
Page
  ├── fetches main view data
  └── Child components
        └── action components fetch/mutate their own data
```

**Pattern B: Centralized**
```
Page
  ├── fetches ALL data
  ├── handles ALL mutations
  └── Child components (presentational only)
        └── receive data via props, emit events for mutations
```

### Constraints of this stack

1. **Static generation** - The frontend is built with `nuxt generate` ([0003](0003-nuxt.md)), so there is no request-time server rendering. Data is fetched in the browser.
2. **Generated API clients** - Typed clients are generated from the backend's OpenAPI document ([0010](0010-backend-for-frontend.md)).
3. **Centralized error handling** - A shared `useProblemDetails()` composable holds API errors in the form of RFC 9457 ProblemDetails, which the backend returns.
4. **Consistent client configuration** - A `useApiClient()` composable creates configured clients.

## Decision

**Use the distributed component pattern.** Components that perform a discrete user action may make their own API calls.

### Guidelines

1. **Pages** fetch the initial view data and pass it to children via props.
2. **Action components** (dialogs, forms, buttons) representing discrete user operations may call the API themselves.
3. **Error handling** goes through the shared `useProblemDetails()` composable.
4. **Mutations** emit events (e.g. `@created`, `@updated`) so parents can refresh.
5. **Inefficiencies** are fixed case by case.

### Why not centralized?

| Factor | Distributed | Centralized |
|--------|-------------|-------------|
| **SSR benefits** | N/A | N/A (static generation) |
| **Component reusability** | ✅ Self-contained | ❌ Requires parent wiring |
| **Testing** | ✅ Components test in isolation | ❌ Complex props to mock |
| **Code locality** | ✅ Related code together | ❌ Action logic split from UI |
| **Prop drilling** | ✅ Minimal | ❌ Deep prop chains for mutations |
| **State management** | ⚠️ Distributed | ✅ Single source of truth |
| **Network efficiency** | ⚠️ May have redundant calls | ✅ Can batch requests |

With a statically generated app, the main benefit of centralized fetching (server-side rendering of the data) does not apply. Reusability and testability outweigh the state management cost.

### Error handling stays centralized

Every component uses the same pattern, which makes distributed fetching practical:

```typescript
const errors = useProblemDetails()
const client = useApiClient(WeatherClient)

try {
  await client.get(0, 5)
} catch (error) {
  errors.handleApiError(error, 'Context-specific title')
}
```

This provides:
- Global reactive error state
- Automatic clearing of previous errors before an API call (in `AuthorizedApiBase.transformOptions`)
- Consistent error display across the app
- Context-specific titles for each operation

### When to consolidate

| Issue | Solution |
|-------|----------|
| Multiple calls for the same data | Add a combined endpoint, plus a caching composable |
| Repeated calls each time a dialog opens | Add in-memory caching |
| Waterfall requests | Use `Promise.all()` |

## Consequences

### Easier

- **Portability** - Components can move between pages without rewiring
- **Focused testing** - Each component tests its own data interactions
- **Clear responsibilities** - A dialog that edits an item makes the edit call
- **Incremental development** - New components do not require changes to page-level data code
- **Consistent errors** - One pattern everywhere

### More difficult

- **Reasoning about data flow** - It is spread across components
- **Network optimization** - Redundant calls must be spotted and fixed deliberately
- **Stale data** - Components can show old data after a sibling mutates it
- **Coordination** - Parents need refresh handling when children mutate

### Mitigations

1. Fix specific network inefficiencies as they are identified.
2. Use event emission to trigger parent refresh.
3. If many components share frequently accessed data, introduce a shared store (Pinia).

## Related Decisions

- [0003. Nuxt](0003-nuxt.md) - Static generation, which shapes this decision
- [0007. Backend Proxy or Direct](0007-backend-proxy-or-direct.md) - Components call the backend directly from the browser
- [0010. Backend For Frontend](0010-backend-for-frontend.md) - Generated clients and DTOs shaped for the UI
