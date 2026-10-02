# Area Paths

This document describes the area path structure used on work items in ADO. This structure is ALSO
used for commit scopes. The commit scope is always the **last segment only** — never the full path.
Use the lowercased form of that last segment. If there is a parenthesized term, use that instead.

For example:

* Application (app): Use "app".
* Tests/Unit: Use "unit" — not "tests/unit".
* FrontEnd/Interface (ui): Use "ui" — not "interface" or "frontend/interface".
* Controllers/Attributes: Use "attributes" — not "controllers/attributes".

Area Paths are typically allowed at any level of depth. e.g. "Controllers" by itself is OK (and most common),
or sometimes we're working in something more specific, so "Controllers/Attributes".

For broad cross-cutting initiatives that span multiple layers or sub-paths, favor writing a PRD so the
PRD slug can be used as the commit scope. This makes commit history far more scannable than a generic
layer scope.

## Architecture Layer Paths

Application (app) -- Changes to the organization or structure of the backend Application project. Typically if you're working here, look for a "Features" area path.
    /Exceptions

FrontEnd -- The FrontEnd.Nuxt project. Use this layer scope when changes span multiple FrontEnd sub-paths.
    /Setup (nuxt) -- Changes to structure, packages, config, head, etc.
    /Interface (ui) -- Changes which user can detect, but are independent from backend change
    /Composables
    /Components -- Visual/structural component changes with no user-visible behavior change
    /Pages -- Visual/structural page changes with no user-visible behavior change
    /Site Configuration (config) -- Environment and site-specific config.toml, secrets, and branding

BackEnd -- Also includes changes to ServiceDefaults project.
    /Logging -- Changes to logging infrastructure, or changes limited to specific log content

AppHost (aspire) -- The development-only Aspire host

Controllers
    /Attributes
    /API (api) -- Changes limited to how the backend presents an API to the frontend
    /TestControl -- Changes limited to test controller

Data
    /Postgres

Entities
    /Models
    /Abstractions

Tests
    /Unit
    /Endpoints -- As there may be multiple integration tests, any test that's not unit or functional can be assumed to be an integration test of the given variety.
    /Functional

Build
    /Azure Pipelines (ci) -- Includes GitHbb workflows
    /Infrastructure (infra) -- Bicep templates, deployment config & scripts
    /Docker
    /System -- DotNet build system, e.g. .slnx, .csproj, .editorconfig, .vscode, Directory.Build.props config
    /Scripts
    /Dependencies (deps) -- Dependabot standard. Only for bumping version numbers. Adding new packages should be included with the feature/layer being adjusted.

Docs
    /Readme
    /PRDs (prd)
    /Plans

## Feature Layer Paths

This is an example of a feature layer setup for an app. Each app needs to rewrite the following section of this document to correspond with its individual needs.

Features -- (not a valid path by itself; always use a child path)
    /Views
        /Browse
        /Prepare
        /Shop
    /Items
    /Action Log (actionlog) -- In-app log of user actions, surfaced to users (see ActionLog entity)
    /Manage
    /Import Export (impex)
        /Import
        /Export
    /Identity
        /Registration
        /Passwords
    /Sample Data (sample) -- Sample data for user consumption
    /Administration (admin) -- Site-wide powers for administrators (to come)
