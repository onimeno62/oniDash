# oniDash — AI Agent Operating Instructions

## Mission
Build oniDash as a local-first Windows media library platform with a beautiful modern web UI. The application manages music, movies, anime, manga, books/e-books, and future catalogues through independent modules/plugins.

## Non-negotiable architecture rules
1. The Core must not contain catalogue-specific domain logic.
2. Catalogue features must be implemented as modules/plugins.
3. The frontend must not directly access SQLite or the filesystem.
4. Backend domain/application logic must not depend on React/UI code.
5. Plugins may depend on Core contracts, but Core must never depend on plugins.
6. Prefer capability-based abstractions over type-specific conditionals.
7. Keep the application local-first: core library functions must work without Internet access.
8. External metadata providers are optional integrations, never the source of truth.
9. Do not add speculative features before the current milestone is complete.
10. Every meaningful feature must have tests.
11. Do not silently rename, move, delete, or modify user media files.
12. Preserve user data during schema migrations and upgrades.

## Workflow
1. Identify the current milestone in `docs/ROADMAP.md`.
2. Read its acceptance criteria and relevant skill files.
3. Inspect existing code before designing new code.
4. Implement the smallest complete vertical slice.
5. Add/update tests.
6. Run formatter, build, and tests.
7. Update documentation when behavior or architecture changes.
8. Report exactly what changed, what was tested, and remaining limitations.

## UI
The UI should feel like one premium application, not several cloned products. Use the supplied reference images as visual inspiration: dark-first, artwork-heavy, rounded surfaces, subtle gradients/glass, strong typography, generous spacing, and polished micro-interactions. Do not copy branding, logos, or exact layouts.

## Current priority
Foundation → shell/design system → core library → scanner → search → Music plugin → Movies → Anime → Manga → Books.

## When blocked
Do not invent requirements. Choose the smallest reversible implementation or ask when the decision materially affects architecture/data.
