# Commit Conventions

This project follows a structured commit message format to maintain a clear and readable git history. Following these conventions helps with automated changelog generation, easier code reviews, and better collaboration.

## Format

All commit messages should follow this structure:

```
<type>(<scope>): <subject>

[optional body]

[optional footer]
```

## Types

Use one of the following types to categorize your commit:

- **feat**: A new feature for the user
- **fix**: A bug fix in application code
- **docs**: Documentation changes only
- **style**: Code style changes (formatting, missing semicolons, etc.) with no logic changes
- **refactor**: Code changes that neither fix a bug nor add a feature (includes renaming files if those files are application code)
- **perf**: Performance improvements
- **tests**: Adding, updating, fixing, or refactoring tests (use this for all test-related changes)
- **build**: Changes to build system, CI, dependencies, or project configuration (e.g., NuGet packages, npm dependencies, .csproj files)
- **revert**: Reverts a previous commit

### Notes

- Use `tests` type for all test-only changes: new tests, fixing broken tests, refactoring test code.
  The scope (`unit`/`functional`/`integration`) indicates which type of test.
  - **Exception — application code changes**: When a commit changes application code *and* updates companion tests,
    use the type that reflects the spirit of the change (e.g. `fix`, `feat`, `refactor`). The tests are
    just following along.
  - **Exception — test infrastructure**: Minor changes that exist solely to enable tests (e.g. adding
    test IDs to elements) can stay as `tests` even if they touch application code.
- We don't use `chore`. Most everything fits under `build`, unless it affects application code, in which case, look to `refactor` or `fix`.
- We use `ci` as a scope within build, e.g. `build(ci)`.

## Scopes

Use project-specific scopes as listed in [AREA-PATHS.md](./AREA-PATHS.md) to identify an area of change

### Architecture Layer Scopes

Use when changes are isolated to a single layer, for example:

- **frontend**: FrontEnd.Nuxt (Vue/Nuxt SPA)
- **backend**: BackEnd API service
- **data**: Data layer (EF Core, migrations, database context)

See area paths for a complete list

### Feature-Based Scopes

Use when changes span multiple layers for a single feature, or are within a single layer but
are narrowly scoped to only that feature. For example:

- **browse**: Item Browsing
- **logs**: Item logging

See area paths for a complete list

### PRD-specific Feature Scopes

When implementing a PRD, use the PRD filename (minus the actual `PRD` text) to identify all
changes related to that PRD, e.g. **transaction-record**: Transaction CRUD operations (PRD-TRANSACTION-RECORD.md)

**Note**: Feature scopes are preferred when implementing or modifying functionality that cuts across multiple architectural layers (e.g., entities, app layer, controllers, frontend). **When implementing a specific PRD, always use the PRD-specific feature slug** (e.g., `feat(transaction-record): add create endpoint`) rather than the generic scope (e.g., `feat(transactions): ...`). This makes commit history much more meaningful and easier to scan for specific feature work.

### Choosing the Right Scope

- **Use layer scopes** for isolated changes: `fix(data): correct migration rollback logic`
- **Use feature scopes** for cross-cutting work: `feat(auth): implement multi-factor authentication`
- **Scope is optional** but strongly recommended for clarity

Feel free to create new feature-based scopes as you implement major features in the application.
Please be sure to add them to the Area Paths document AND to area paths in ADO.

## Subject Line

The subject line should:

- Use imperative mood ("add" not "added" or "adds")
- Not capitalize the first letter (makes grep easier)
- Not end with a period
- Be limited to 72 characters
- Be concise but descriptive

### Examples

✅ **Good**:

```text
feat(ui): add item list display component
fix(ctrl): resolve null reference in lists controller
docs(contributing): move prerequisites to separate section
feat(basic-admin): add create user endpoint
feat(seed-invitations): implement invitation seeding logic
refactor(id): simplify token validation across all layers
```

✅ **Also Good** (generic scopes for non-PRD work):

```text
feat(impex): add list export utility
fix(bv): correct browse view filter edge case
```

❌ **Bad**:

```text
Added new feature.
Fixed bug
Update files
```

## Body (Optional)

Include a body when the commit needs additional explanation:

- Separate from subject with a blank line
- Wrap lines at 72 characters
- Explain **what** and **why**, not **how**
- Use bullet points for multiple items

### Example

```commit
refactor(data): simplify database context configuration

- Extract connection string logic to separate method
- Remove unused DbSet properties
- Add XML documentation for public members

This improves testability and makes the context easier to maintain.
```

## Footer (Optional)

Use the footer for:

### Breaking Changes

Prefix with `BREAKING CHANGE:` followed by a description:

```
feat(ent)!: redesign IDataProvider interface

BREAKING CHANGE: IDataProvider.GetItems() now returns Task<Result<T>>
instead of Task<T>. Update all callers to handle the Result pattern.
```

Note: The `!` after the type/scope is a visual indicator of a breaking change.

### Issue References

Reference issues that this commit addresses:

```
fix(ctrl): correct validation logic for list entitlements

Fixes #123
Closes #456
```

### Co-authors

Credit co-authors when pair programming:

```text
feat(frontend): implement user profile page

Co-authored-by: Jane Doe <jane@example.com>
```

## Complete Examples

### Simple Feature

```text
feat(ui): add browse view page
```

### Bug Fix with Details

```text
fix(data): prevent duplicate migration applications

Check for existing migrations before applying to avoid
database errors in production deployments.

Fixes #78
```

### Test Changes

```text
tests(unit): add validation tests for list model
tests(functional): fix flaky authentication test
tests(integration): refactor database setup for better performance
```

### Refactoring with Multiple Changes

```text
refactor(app): restructure views feature organization

- Move validation logic to separate validator class
- Extract data transformation to mapper
- Improve error handling with Result pattern
- Add comprehensive unit tests

This improves code maintainability and testability while
maintaining the same external API.
```

### Documentation Update

```text
docs(readme): update installation instructions for .NET 10
```

### Infrastructure Change

```text
build(ci): add automated deployment workflow

Implements continuous deployment to Azure on main branch merges.
Includes environment-specific configurations and approval gates.
```

## Best Practices

1. **Make atomic commits**: Each commit should represent a single logical change
2. **Commit early and often**: Don't wait until you have a massive changeset
3. **Write meaningful messages**: Future you (and your team) will thank you
4. **Use the body**: Don't be afraid to explain the context and reasoning
5. **Reference issues**: Link commits to issue tracking for better traceability
6. **Review before pushing**: Use `git log` to review your commit messages

## Tools

In the future, we will consider using these tools to enforce commit conventions:

- **[Commitizen](https://github.com/commitizen/cz-cli)**: Interactive commit message builder
- **[commitlint](https://commitlint.js.org/)**: Lint commit messages
- **[Husky](https://typicode.github.io/husky/)**: Git hooks to enforce conventions

## Resources

- [Conventional Commits Specification](https://www.conventionalcommits.org/)
- [Angular Commit Guidelines](https://github.com/angular/angular/blob/main/CONTRIBUTING.md#commit)
- [How to Write a Git Commit Message](https://chris.beams.io/posts/git-commit/)
