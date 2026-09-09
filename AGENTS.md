# oniDash — AI Agent Operating Instructions

## Mission
Build oniDash as a local-first Windows media library platform with a beautiful modern web UI. The application manages music, movies, anime/TV, manga, books/e-books, and future catalogues through independent modules/plugins.

## Read before coding
The canonical project contract is:
1. `docs/PRODUCT-SPEC.md` — product behavior and non-goals.
2. `docs/ARCHITECTURE-SPEC.md` — system boundaries and technical contracts.
3. `docs/UI-SPEC.md` — visual and interaction rules.
4. `docs/TASKLIST.md` — ordered execution backlog.
5. `docs/ROADMAP.md` — milestone gates.

If an implementation conflicts with these documents, stop and resolve the conflict before coding.

## Non-negotiable architecture rules
1. Core must not contain catalogue-specific domain logic.
2. Catalogue features must be implemented as modules/plugins.
3. Frontend must not directly access SQLite or the filesystem.
4. Backend domain/application logic must not depend on React/UI code.
5. Plugins may depend on Core/Application contracts; Core never depends on plugins.
6. Prefer capability-based abstractions over type-specific conditionals.
7. Core library functions must work without Internet access.
8. External metadata providers are optional enrichment, never the source of truth.
9. Do not add speculative features before the current milestone is complete.
10. Every meaningful feature must have tests.
11. Never silently rename, move, delete, overwrite, or modify user media.
12. Preserve user data during schema migrations and upgrades.
13. API routes must never fall through to SPA HTML.
14. Never expose EF entities directly as public API contracts.
15. Long-running work must use observable, cancellable jobs.

## Workflow
1. Identify the current milestone in `docs/ROADMAP.md` and first unchecked tasks in `docs/TASKLIST.md`.
2. Read the relevant specs and skills.
3. Inspect existing code before designing new code.
4. Implement the smallest complete vertical slice.
5. Add/update backend, frontend and integration tests as appropriate.
6. Run formatter, build, typecheck and tests when tooling is available.
7. Update docs when behavior or architecture changes.
8. Report exactly what changed, what was verified, and remaining limitations.

## Git workflow
- Work from `main`.
- Create a focused feature/fix/docs branch.
- Keep commits coherent.
- Open a PR against `main`.
- Do not merge unless the user explicitly requests merging.
- Do not claim CI/build/test success without actual evidence.

## UI
The UI should feel like one premium application, not several cloned products: dark-first, artwork-heavy, rounded surfaces, subtle depth, strong typography, generous spacing, polished micro-interactions and complete loading/empty/error states. Use reference images only as inspiration; do not copy branding or exact layouts.

## Current execution priority
Platform foundation → unified scanner/indexer → metadata/artwork → global search/health → Music hardening → Movies/Anime → Manga → Books → unified dashboard → Windows productization.

## When blocked
Do not invent requirements. Choose the smallest reversible implementation, document the assumption, or ask when the decision materially affects architecture, data integrity, legal/safety boundaries, or user-visible behavior.
