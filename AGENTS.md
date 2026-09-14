# oniDash — AI Agent Operating Instructions

## Mission
Build oniDash as a local-first, Windows-first personal media library. Music is the next priority and must be treated as a complete music manager/player, not a decorative dashboard. Other catalogues (Movies, TV/Anime, Manga, Books) remain modular and must not be expanded while the current milestone is incomplete.

## Canonical documents
Read these before coding:
1. `docs/PRODUCT-SPEC.md` — product contract.
2. `docs/ARCHITECTURE-SPEC.md` — platform architecture and boundaries.
3. `docs/UI-SPEC.md` — shared visual/interaction rules.
4. `docs/MUSIC-SPEC.md` — complete Music product contract.
5. `docs/MUSIC-ARCHITECTURE.md` — Music domain, player, scanner and provider architecture.
6. `docs/MUSIC-UX.md` — Music information architecture and UX behavior.
7. `docs/TASKLIST.md` — ordered executable backlog.
8. `docs/ROADMAP.md` — milestone gates.
9. `docs/DECISIONS.md` — architecture decisions.

If implementation conflicts with these documents, stop and resolve the conflict before coding.

## Non-negotiable rules
1. Inspect existing code before changing it; reuse sound infrastructure and replace broken abstractions instead of layering patches.
2. Core must not contain catalogue-specific Music logic.
3. Frontend never accesses SQLite or the filesystem directly.
4. EF entities are never public API contracts.
5. Player state and audio playback must live independently of React page lifecycle.
6. Library queries, sorting, filtering and pagination are server/database operations, not large client-side arrays.
7. External metadata and lyrics providers are optional enrichment, never local truth.
8. Never silently rename, move, delete, overwrite, or modify user media.
9. Destructive/bulk file operations require validation, preview where applicable, explicit confirmation, execution reporting and recoverable failure handling.
10. Long-running scans, metadata enrichment, lyrics lookup and maintenance use observable cancellable jobs.
11. Every meaningful feature needs backend and/or frontend tests appropriate to its boundary.
12. No fake controls, dead buttons, placeholder success states or UI-only features.
13. Do not mark work complete without actual build/typecheck/test evidence.
14. Do not claim CI success without CI evidence.
15. Do not add future catalogue features while the Music rebuild milestone is incomplete.

## Music implementation order
Follow `docs/TASKLIST.md` exactly. The required order is:
1. Music domain/library foundation.
2. Scanner and filesystem reconciliation.
3. Library browsing, queries and search.
4. Safe file management and organization.
5. Metadata/artwork and external identification.
6. Playback engine and queue.
7. Player UX, waveform and visualizers.
8. Lyrics and synchronized lyrics.
9. Favorites, ratings, history and playlists.
10. Music Home/dashboard.
11. Advanced Windows/audio features and hardening.

Do not build the Music Home first. It is the final presentation layer over working capabilities.

## Workflow
1. Read the canonical documents.
2. Inspect relevant existing implementation and tests.
3. Select the first applicable unchecked task.
4. Implement one complete vertical slice.
5. Add regression tests before moving on.
6. Run formatter, build, typecheck and tests when available.
7. Update documentation if contracts change.
8. Report changed files, verification performed, failures and remaining limitations.

## UI direction
oniDash should feel like a premium desktop music application: calm, modern, cinematic and information-rich. Use artwork where it improves discovery, dense tables where management matters, focused editors for metadata, and a persistent player. Avoid generic SaaS-dashboard patterns, excessive glassmorphism, oversized empty cards and decorative controls without functionality.

## Git workflow
Work from `main` unless a task explicitly requires a focused branch/PR workflow. Keep commits coherent. Never merge a PR unless the user explicitly requests it.
