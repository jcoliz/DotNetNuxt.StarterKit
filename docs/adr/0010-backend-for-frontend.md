# 0010. Backend For Frontend Pattern

Date: 2026-10-01

## Status

Accepted

## Context

What philosophy governs the interaction between frontend and backend?

### Technical constraint

This stack assumes stronger depth in C#/.NET than in TypeScript/Vue. Concentrating business logic and complexity in the backend leverages that expertise and gives more robust, better-tested code. This favors putting as much logic as possible in the backend Application layer ([0011](0011-clean-architecture.md)).

## Decision

The backend is a dedicated service **tailored to the needs of its frontend**, following the Backend For Frontend (BFF) pattern.

- The backend exists to serve the project's own frontend
- Its API shape is optimized for the frontend's UX
- It evolves at the pace of the frontend
- It is not intended for reuse by other clients

### Details

- Business logic and complexity are concentrated in the backend, ideally the Application layer.
- DTOs are tailored to what the user needs to see or send at any given moment.
- Application logic shapes database queries directly into (or out of) the DTOs the frontend sees.

### Excluded concerns

- This is not a public API for third-party consumption
- It is not designed for mobile apps, CLI tools, or other clients
- API stability guarantees beyond N-1 compatibility are minimal

## Consequences

### Easier

- **Simpler frontend** - Presentation logic only, with no duplicated business rules
- **Optimized data shapes** - Less client-side transformation
- **Centralized validation** - Business rules are enforced in one place
- **Faster iteration** - No API contract negotiation between teams
- **Testability** - Complex logic is tested in C# with mature tooling
- **Generated client** - The TypeScript API client is generated from the backend's OpenAPI document (`tools/ApiClientGenerator`), keeping both sides in sync

### More difficult

- **Tight coupling** - Frontend and backend evolve together
- **Reusability** - If multiple frontends are needed later, logic may need extraction or duplication
- **Coordinated changes** - Most features touch both sides

## Alternatives considered

- **GraphQL** - Typically pays off when the frontend team is strong and wants flexible fetching while the backend stays simple. This stack has the opposite emphasis.
- **Generic, reusable REST API** - Prone to over-fetching and bloat, and optimizes for reuse that may never happen. Start specific, and generalize only when real second use cases appear.
- **Microservices** - Adds operational complexity with no benefit at this scale.
- **Shared API for multiple client types** - Speculative. Refactor when there are concrete requirements.

## Versioning and compatibility

Because frontend and backend deploy separately ([0006](0006-production-infrastructure.md)), they can briefly run at different versions. The recommended policy is asymmetric.

### Version support policy

- **N-1 compatibility**: The backend must stay compatible with the previous frontend release (N-1).
- **Never break N-1**: A backend deployment must not break N-1 frontend functionality.
- **No forward compatibility**: The backend need not support frontends newer than itself. A frontend can assume the backend is the same version or newer.

**Deployment order**: Deploy the backend first, then the frontend. Never deploy a frontend ahead of its backend.

### Breaking changes

A breaking change is anything that prevents a frontend from delivering a scenario. For example: removing a DTO field, removing an endpoint, or changing required parameters. Adding fields or optional parameters is not breaking.

**Migration pattern**:
1. **Release N**: Mark the field or endpoint obsolete, but keep it working.
2. **Release N+1**: Remove it. N-1 frontends no longer use it.

**Version-aware behavior**: If necessary, the backend can adapt its behavior based on the frontend version in a request header. Use this sparingly. Additive changes that work for both N and N-1 are preferred.

**Database migrations** must also be compatible with N-1, since the old backend may briefly run against a newer schema during rollout.

### Starter kit status

The backend exposes a public `/Version` endpoint (`VersionController`) that returns the application version, which is read from the assembly's informational version. Frontend version detection, request header exchange, and automatic frontend refresh are not yet implemented, and will arrive with the frontend.

## Related Decisions

- [0001. Single Page Web App](0001-spa-web-app.md)
- [0006. Production Infrastructure](0006-production-infrastructure.md) - Separate deployments make version compatibility necessary
- [0011. Clean Architecture](0011-clean-architecture.md) - Where the concentrated business logic lives
