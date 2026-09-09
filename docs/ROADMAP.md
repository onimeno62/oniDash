# oniDash Roadmap

This roadmap is a gated execution plan. A milestone is complete only when its acceptance criteria and relevant tasks are complete and verified.

## Phase 0 — Repository / architecture [COMPLETE]
Solution, documentation, build conventions, dependency rules.

## Phase 1 — Application shell [COMPLETE]
ASP.NET host, React app, design system, themes, shell, routing, settings and health.

## Phase 2 — Core library [COMPLETE]
SQLite, EF persistence, Library, Source, MediaItem, MediaFile, Artwork, Tag, Collection and basic APIs.

## Phase 3 — Filesystem indexing [COMPLETE / HARDEN]
Scanner, safe traversal, background jobs, progress, cancellation, identity-based indexing and missing-file handling exist. The next milestone must harden these foundations into one canonical indexing pipeline.

## Phase 4 — Search [FOUNDATION EXISTS / HARDEN]
SQLite FTS5, ranking, prefix/multi-term search and recovery exist. Next: canonical cross-media search contract, incremental indexing and health diagnostics.

## Phase 5 — Music [SUBSTANTIAL / HARDEN]
Music backend and premium workspace are established. Next: contract alignment, build/type hardening, health/duplicates, playback polish and Windows integration boundaries.

## Phase 6 — Movies [FOUNDATION EXISTS]
Movie detection, metadata, artwork, streaming and progress exist. Next: integrate fully with the canonical platform scanner/metadata pipeline.

## Phase 7 — TV / Anime [NEXT AFTER MOVIES]
Series, seasons, episodes, watch progress and optional anime metadata providers.

## Phase 8 — Manga [FOUNDATION EXISTS]
Dedicated UI/API concepts exist. Next: real plugin architecture and optional Mihon/Suwayomi-compatible source/extension integration, downloads and reader.

## Phase 9 — Books [FOUNDATION EXISTS]
Dedicated UI/API concepts exist. Next: canonical scanner integration, EPUB/PDF metadata, reader and complete persistence.

## Phase 10 — Unified platform experience
Cross-media dashboard, continue listening/watching/reading, activity, favorites, recommendations and library health.

## Phase 11 — Windows productization
Desktop wrapper, installer, associations, media keys, notifications, startup/tray and backup/restore.

## Phase 12 — Optional network mode
LAN access, authentication, responsive clients and multiple-client support. This phase must not compromise the local-first model.

## Current milestone: Platform Foundation Hardening

### Gate A — Canonical contracts
- [ ] Core media/source/file/artwork contracts audited and documented.
- [ ] Common API error and pagination contracts defined.
- [ ] Common background job contract defined.

### Gate B — Unified scanner/indexer
- [ ] One scanner pipeline owns filesystem discovery.
- [ ] Media handlers are capability-based.
- [ ] Upserts are idempotent.
- [ ] Missing files are recoverable.
- [ ] Scan progress/cancel/retry is observable.

### Gate C — Metadata/artwork
- [ ] Local metadata is separated from external enrichment.
- [ ] Provenance/confidence exists.
- [ ] Embedded artwork is extracted/cached.
- [ ] Providers are optional and cancellable.

### Gate D — Search/health
- [ ] Global cross-media search consumes canonical index.
- [ ] Incremental and recovery reindex work.
- [ ] Library health exposes actionable failures.

Do not begin a later feature because it looks visually complete. Platform correctness takes precedence over additional dashboard screens.
