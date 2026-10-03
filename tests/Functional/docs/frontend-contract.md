# Front-End Testing Contract

This document defines the contract between front-end implementation and the functional test suite.

Scope:
- This is a testability contract for front-end markup and behavior.
- Front-end developers are also responsible for maintaining the related page object models in functional tests.

## 1. Naming Convention for data-test-id

- kebab-case is required for new data-test-id values.
- Existing data-test-id values are grandfathered and may optionally be migrated opportunistically when related code is touched.
- There is no automated lint rule for data-test-id casing; compliance is by review convention.

Terminology:
- page root id: the HTML `id` on the page's main root element, used to scope page-object locators.
- component root data-test-id: the `data-test-id` on a reusable component's root element.

## 2. Component data-test-id API Convention

This project treats component test IDs as a public testing API.

- Every new reusable component must define a default root data-test-id derived from the component name in kebab-case.
- Reusable components may expose an optional dataTestId prop to override only the root data-test-id.
- Internal child data-test-id values must remain semantic and local to the component.
- Callers should pass dataTestId only when needed to disambiguate sibling instances in the same locator chain.
- Tests should prefer chained locators from a known parent context, not global uniqueness assumptions.
- Changing or removing an established component data-test-id contract is a breaking test change and must be reflected in the corresponding page object before merging into upstream main.

Reference implementation pattern:
- [FrontEnd.Nuxt/app/components/IdTester.vue](../../../FrontEnd.Nuxt/app/components/IdTester.vue)

## 3. Uniqueness Rules

- Page-global uniqueness of data-test-id is not required.
- A data-test-id is sufficiently unique when, from the intended page root, the page object can build a stable locator chain that resolves to exactly one target element for the action or assertion being performed.
- If sibling instances are ambiguous in the same parent chain, add a parent identifier or override the component root data-test-id.

## 4. Page Root IDs

- Each routable page must define a unique route meta title such that its normalized page root id is unique across all pages.
- The page root id is derived only by usePageId and follows its normalization behavior.
- Changing route meta title is a test-contract breaking change unless tests and page models are updated accordingly.
- Any new layout must preserve this behavior by binding the derived page root id onto the layout main element.

References:
- [FrontEnd.Nuxt/app/composables/usePageId.ts](../../../FrontEnd.Nuxt/app/composables/usePageId.ts)
- [FrontEnd.Nuxt/app/layouts/default.vue](../../../FrontEnd.Nuxt/app/layouts/default.vue)
- [FrontEnd.Nuxt/app/layouts/guest.vue](../../../FrontEnd.Nuxt/app/layouts/guest.vue)

## 5. Readiness Contract

Each page defines its own readiness criteria in WaitForPageReadyAsync, based on the page UX and loading behavior.

Requirements:
- Every page object must override WaitForPageReadyAsync.
- Readiness must be based on deterministic, observable UI state, not fixed delays.
- Readiness may be page-specific, but must represent a state where user actions are safe and meaningful.
- All navigation entry points must call WaitForPageReadyAsync before returning.

References:
- [Tests/Functional/Pages/BasePage.cs](../Pages/BasePage.cs)
- [Tests/Functional/Pages/ViewsPage.cs](../Pages/ViewsPage.cs)
- [writing-pages.md](./writing-pages.md)

## 6. Planning and Review Checklist

For substantial front-end changes, plans should include:
- New or changed data-test-id values needed by scenarios.
- Any new component-level dataTestId override needs.
- Any route title change that affects page root id values.
- Readiness signal definition and page object update plan.
