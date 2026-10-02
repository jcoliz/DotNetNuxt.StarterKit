# 0009. Tenant and Workspace Data Boundaries

Date: 2026-10-02

## Status

Accepted

## Context

Applications with multiple users may need to group data so that one person can work in more than one group, and several people can share access to the same group. This is an application-level data and authorization concern; the starter kit does not require every application to be multi-tenant.

The stack uses PostgreSQL ([0005](../0005-database-backend.md)) and places business rules in the backend ([0011](../0011-clean-architecture.md)). If an application introduces shared data boundaries, every read and write must enforce membership on the server. A tenant identifier supplied by a route, query, or request body is a selector, not proof of access.

## Decision

For applications that require shared data boundaries:

- Model the boundary explicitly as a **tenant** in the backend and database. Use **workspace** in the user interface only when that is the clearest term for the product's users. Do not force tenant/workspace terminology on applications whose domain has a better concept.
- Store user-to-tenant membership and any application-specific role in the database. A user may belong to multiple tenants, and a tenant may have multiple users.
- Resolve the requested tenant and verify the authenticated user's current membership and permission on every protected operation. Do not rely on a tenant ID or membership claim supplied by the client, and avoid treating long-lived token claims as the authoritative source for changeable membership.
- Scope all tenant-owned data access and mutations by the authorized tenant on the server. Keep this enforcement close to the data-access boundary where practical, and add tenant identifiers to relevant keys and uniqueness constraints so the database model preserves the boundary.
- Test isolation directly: users must not read or modify another tenant's records by changing identifiers, filters, or request payloads. Include these checks in integration tests.
- Define roles from the product's actual operations. Owner, Editor, and Viewer are one possible model, not defaults for every application. Invitation flows, ownership transfer, tenant deletion, and minimum-owner rules are also product decisions.

This ADR does not prescribe database-per-tenant isolation. A shared database with tenant-scoped records is the default for this stack; choose separate databases only when compliance, operational isolation, scale, or customer requirements justify the additional cost and complexity.

> **Implementation status:** Authentication and tenant isolation are not implemented in this starter. These guidelines describe the required security boundary for applications that add multi-user, tenant-scoped data; they are not evidence that existing API endpoints enforce it.

## Consequences

### Easier

- The data boundary is explicit and can support personal, household, team, or business workspaces when those concepts fit the product.
- Membership and permissions can evolve without hard-coding one role model into the shared starter.
- Server-side enforcement and cross-tenant tests make isolation requirements reviewable.

### More difficult

- Every tenant-owned query and mutation must apply the authorized tenant boundary.
- Membership changes, invitations, and role management add workflows and security-sensitive cases.
- A shared database does not provide isolation automatically; query filters and tests must be maintained as features are added.

## Related Decisions

- [0005. Database Backend](../0005-database-backend.md) - PostgreSQL data storage
- [0008. Identity and API Authentication](0008-identity.md) - Authenticated API requests
- [0011. Clean Architecture](../0011-clean-architecture.md) - Backend application and data boundaries
