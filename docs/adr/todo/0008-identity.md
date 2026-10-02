# 0008. Identity system

Date: 2025-11-13

## Status

Accepted

## Context

### Question

What software components and/or possible identity providers should we use for identity authentication and authorization?

In ListsWebApp.V3, I rolled my own custom system. While I am happy with this, I've also received feedback that this is an unsafe practice, and should attempt to use as many off-the-shelf components as possible which have more security reviewers looking at them.

Some questions that come to mind:

* For the front-end, should I use NuxtAuth? This was recommended.
* ASP.NET includes some identity components. Can I leverage those? I know them well. Can I combine them with NuxtAuth??
* Should I use an external identity providers?
* How do I handle authorization? Certain users will have access to certain lists, and this is information that is unique to the app, so I'll have to store it here.
* Are there any examples of good existing systems that combine Nuxt frontend with ASP.NET backend for identity?

### Analysis

Based on the existing architecture (Nuxt 4 + ASP.NET Core + direct API calls) and the infrastructure decision to use static site generation ([ADR 0006](./0006-production-infrastructure.md)), we need a client-side authentication solution that works with static hosting.

### Experience to date

I have been using @sidebase/nuxt-auth and NuxtIdentity during development on YoFi.V3, and I have been happy with it.

## Decision

I created a new library, [Nuxt Identity](https://github.com/jcoliz/NuxtIdentity) to collect the needed glue to bring together these battle-tested auth components while maintaining the current direct API call architecture.

- **[@sidebase/nuxt-auth](https://auth.sidebase.com/) with local provider** - Client-side authentication
- **ASP.NET Core Identity** with JWT tokens for stateless authentication

Nuxt Identity aims to be a thin library, focused on moving data between .NET Identity and @sidebase/nuxt-auth. Here's what it's doing:

- JWT handling: Setting up JWT token creating and validation with security best practices.
- API endpoints: Supplying the expected endpoints, translating those requests into .NET Identity system calls, and returning the results in the expected form.
- Error handling: Surfacing RFC 7807 compliant error responses with ProblemDetails middleware for better API consistency.
- Role/claim visibility: Surfacing user's roles and claims in auth tokens and in the user session.
- Refresh tokens: .NET Identity doesn't handle refresh tokens at all, so a big part of this libraries work is storing and validating those with automatic rotation.

### Addressing Specific Questions

1. **NuxtAuth?** → Use **@sidebase/nuxt-auth with local provider** (modern, works with static generation)

2. **ASP.NET Identity?** → **Yes, absolutely**. It's mature, well-tested, and provides the API endpoints for the frontend

3. **External providers?** → **Later**. Start with built-in, add external providers when needed using ASP.NET Core's external login system

4. **Authorization?** → Use **ASP.NET Core's policy-based authorization** with custom claims for list access. Store user-to-list mappings in your database using the three-role model (Owner/Editor/Viewer) as defined in ADR 0009.

5. **Examples?** → The local provider pattern is common for SPA + API architectures. JWT tokens bridge frontend and backend auth

### Why This Approach Works Well

✅ **Security**: ASP.NET Core Identity is battle-tested with regular security updates  
✅ **Familiar**: You already know ASP.NET Core Identity well  
✅ **Stateless**: JWT tokens work perfectly with your direct API call architecture  
✅ **Static Hosting Compatible**: No server-side session handling required  
✅ **Cost Effective**: Works with Azure Static Web Apps infrastructure  
✅ **Extensible**: Easy to add external providers later  
✅ **Single Database**: User data stays in your database with your list data
✅ **Authorization Ready**: Built-in support for roles and custom claims for list-level permissions

## Consequences

### What becomes easier:

- **Security**: ASP.NET Core Identity is battle-tested with regular security updates
- **Familiarity**: Leverages existing ASP.NET Core Identity knowledge
- **Stateless Architecture**: JWT tokens work perfectly with direct API call architecture
- **Static Hosting**: Compatible with Azure Static Web Apps cost-effective infrastructure
- **Extensibility**: Easy to add external providers later
- **Single Database**: User data stays in database with list data
- **Authorization**: Built-in support for roles and custom claims for list access control as defined in ADR 0009

### What becomes more complex:

- **Client-Side Token Management**: All authentication state managed in browser
- **Token Refresh**: Need to implement JWT token refresh logic client-side
- **Custom Claims**: List-level authorization requires custom implementation
- **Security Considerations**: Tokens stored client-side (though mitigated by secure cookies and short expiry)
- **Testing**: Authentication flows need to be tested across both frontend and backend

### Future Considerations:

- External identity providers can be added incrementally
- Session management handled by @sidebase/nuxt-auth local provider
- Authorization policies can be extended for more granular permissions
- Consider token refresh strategies for long-lived sessions

## Architecture Compatibility

This identity design is fully compatible with ADR 0006 (Production Infrastructure):

- ✅ **Static Site Generation**: Local provider works without server-side handlers
- ✅ **Azure Static Web Apps**: Client-side authentication compatible
- ✅ **Cost Effective**: No additional hosting costs for authentication
- ✅ **Direct API Calls**: Maintains the established architecture pattern

The frontend will authenticate against the App Service backend using standard REST API calls, keeping the architecture simple, secure, and cost-effective.

## Related Decisions

- [ADR 0009: Multi-tenancy and List Sharing Model](0009-accounts-and-tenancy.md) - Defines the list structure, roles, and authorization policies that this identity system implements
- [ADR 0006: Production Infrastructure](0006-production-infrastructure.md) - Azure infrastructure decisions
