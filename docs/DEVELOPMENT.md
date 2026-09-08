# Development Rules

Enable nullable reference types and strict TypeScript. Treat warnings seriously.

Backend: thin endpoints, DTOs, async I/O, cancellation tokens, path validation, deliberate paginated queries, business logic outside endpoint handlers, migrations for schema changes.

Frontend: typed API client, separate server/local UI state, composable components, no large business-rule page components, loading/error/empty states.

Tests: Core unit, Application unit/integration, scanner temp-directory tests, API integration, important UI behavior, critical E2E flows.

Filesystem tests: nested folders, duplicates, inaccessible files, cancellation, long names, unsupported extensions.

Performance: never scan on UI thread, paginate large views, thumbnail artwork, avoid N+1 queries.

Suggested commits: feat(core): add library source; feat(scanner): add cancellable scan; feat(ui): add library grid.
