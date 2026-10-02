# 0009. Multi-tenancy and Workspace model

Date: 2025-11-13

## Status

Accepted

## Context

### Question

How should YoFi.V3 handle multiple users and financial data isolation? What is the relationship between users and their financial data boundaries?

### Background

YoFi.V3 is a rewrite of the [YoFi personal finance application](https://github.com/jcoliz/yofi). YoFi is single-tenant. This is a constraint I would like to improve upon with this rewrite.

### Use Case Analysis

**Primary Use Cases:**
- **Personal Finance**: Individual manages their own financial data
- **Household Finance**: Family members share access to household financial data
- **Small Business**: Business owner + bookkeeper access business financial data
- **Multi-Workspace**: User manages both personal and business finances separately

## Decision

### Terminology: Implementation vs. User Interface

**Implementation Layer**: Uses "Tenant" terminology throughout the codebase
- Database: `Tenants`, `UserTenantRoles`, `TenantId` columns
- Code: `Tenant`, `ITenantService`, `TenantContext` classes
- APIs: `/api/tenant/{tenantId}/transactions`

**User Interface Layer**: Uses "Workspace" terminology for end users
- UI components: Workspace selector, workspace switcher
- All user-facing text and documentation

**Rationale**: Separates domain-agnostic implementation from user experience, allowing UI terminology to evolve based on user testing while keeping code reusable.

### Tenant Model: Logical Financial Boundary

A "Workspace" (UI) / "Tenant" (implementation) represents a complete set of financial records (transactions, budgets, categories, etc.) managed as a unit.

Examples:
- "Smith Family Finances" (household)
- "John's Personal Finances" (individual)
- "ABC Consulting Business" (business)

### Multi-Tenancy: Role-Based Access Control

**User-to-Tenant Relationships:**
- **One User → Multiple Tenants**: Personal + business finances
- **Multiple Users → One Tenant**: Family members, business owner + bookkeeper

**Role Levels:**
1. **Owner** - Full control (edit data, manage users, delete tenant)
2. **Editor** - Edit financial data, view reports (cannot manage users)
3. **Viewer** - Read-only access to data and reports

**Rules:**
- Tenant creation makes creator the Owner
- Only Owners can invite/remove users and change roles
- Each tenant must have at least one Owner
- Financial data is completely isolated by tenant

### Data Architecture

**Core Entities:**
- **Tenants** - Container for financial data (called "Workspace" in UI)
- **UserTenantRoles** - User-to-Tenant relationships with role assignments
- **Financial Data** - All entities (Transactions, Categories, Budgets) are tenant-scoped

**Key Principles:**
- Complete data isolation by tenant
- All financial queries must be tenant-scoped
- Role-based authorization on tenant resources

**Implementation Details:** See [`TENANCY.md`](../TENANCY.md) for complete implementation documentation including database schema, API structure, JWT claims format, authorization policies, and usage patterns.

## Consequences

### Easier:
- Clear data boundaries prevent leaks
- Supports personal, household, and business scenarios
- Role-based access scales to complex use cases
- Domain-agnostic code reusable for other applications
- UI terminology can evolve without code changes

### More Complex:
- All queries must be tenant-scoped
- Users need tenant selection/switching UI
- Email invitation and acceptance workflow required
- Tenant access denied error handling

## Related Decisions

- [ADR 0008: Identity System](0008-identity.md) - Authentication foundation
- [ADR 0005: Database Backend](0005-database-backend.md) - SQLite storage

## Implementation

The tenancy system has been implemented using a lightweight pattern that provides superior security and maintainability compared to the original design. Key implementation highlights:

- **Single Enforcement Point:** All tenant-scoped queries use a base query pattern that automatically filters by tenant
- **Security-First Authorization:** Returns 403 for both "tenant not found" and "access denied" to prevent tenant enumeration
- **Claims-Based Access:** JWT tokens contain `tenant_role` claims in format `"{tenantKey}:{role}"`
- **Middleware Pipeline:** Authorization → Tenant Context → Controller flow ensures proper isolation
- **Production-Ready:** Comprehensive integration test coverage validates cross-tenant isolation

**Complete Documentation:** See [`TENANCY.md`](../TENANCY.md) for:
- Architecture and component overview
- Data model and database configuration
- Authorization policies and role-based access
- Tenant context management and isolation patterns
- Exception handling and security strategy
- Implementation examples and usage patterns
- Testing strategy and key file references

**Outstanding Work:** See [`wip/TENANCY-TODO.md`](../wip/TENANCY-TODO.md) for remaining features and enhancements.
