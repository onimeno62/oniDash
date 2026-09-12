# oniDash Master Task List

This is the execution order for autonomous agents. Do not skip ahead because a later UI is easier to build.

## M0 — Platform foundation hardening
- [x] Audit current Core/Application/Infrastructure boundaries.
- [x] Define canonical `MediaType`, `MediaItem`, `MediaFile`, `Library`, `Source`, `Artwork`, `Tag`, `Collection` contracts.
- [x] Define canonical API error and pagination contracts.
- [x] Define common background `Job` model and status API.
- [x] Add integration-test harness for API + temporary SQLite + temporary media folders.
- [x] Fix API routes that can fall through to SPA HTML.

## M1 — Unified scanner/indexer
- [x] Source CRUD and validation.
- [x] Safe recursive filesystem enumerator.
- [x] Extension/signature-based media handler registry.
- [x] Stable file identity and idempotent upsert.
- [x] Missing-file detection.
- [x] Scan jobs with progress/cancel/retry.
- [x] Scan diagnostics and per-file failures.
- [x] Scanner service behavior tests for indexing, reconciliation, progress, and handler isolation.
- [x] End-to-end scanner integration test against temporary SQLite + filesystem.

## M2 — Metadata + artwork engine
- [x] `IMediaHandler` contract.
- [x] Audio metadata handler.
- [x] Video metadata handler.
- [x] EPUB/PDF/comic metadata handler.
- [x] Metadata normalization pipeline.
- [x] Provenance/confidence model.
- [x] Embedded artwork extraction.
- [x] Artwork cache and lifecycle contract.
- [x] Optional provider interfaces.
- [x] Enrichment preview/apply workflow contract; never implicit overwrite.

## M3 — Global search + library health
- [x] Canonical cross-media search contract.
- [x] Canonical FTS5-backed search index model.
- [x] Live incremental FTS maintenance through database triggers.
- [x] Full rebuild/recovery command.
- [x] Cross-media search endpoint and backend search service.
- [x] Filters for media type/library/tag/status in the canonical search surface.
- [x] Search result navigation parity across all media modules.
- [x] Library health dashboard.
- [x] Missing files / missing artwork / metadata errors / duplicates diagnostics.

## M4 — Music completion
- [x] Verify existing Music APIs against canonical platform contracts.
- [x] Fix remaining type/build issues.
- [x] Library health and duplicate tooling.
- [x] ReplayGain/loudness metadata where supported.
- [x] Gapless/crossfade where technically supported.
- [x] Media-key/Windows playback integration boundary.
- [x] Complete lyrics/provider error handling.

## M5 — Movies + TV/Anime
- [x] Align Movies with scanner/indexer.
- [x] Movie metadata/artwork enrichment.
- [x] Watch-state/progress persistence.
- [x] Series/season/episode domain.
- [x] Anime-specific metadata as optional provider enrichment.
- [x] Continue-watching across movies/episodes.

## M6 — Manga platform
- [x] Replace temporary local adapter behavior with real plugin architecture.
- [x] Mihon/Suwayomi-compatible source/extension boundary.
- [x] Extension store/install/update management.
- [x] Source search/popular/latest.
- [x] Manga/chapter persistence.
- [x] Reading progress and bookmarks.
- [x] Download queue and offline storage.
- [x] Reader.
- [x] Library updates/notifications.
- [x] Tracking/sync as optional integrations.

## M7 — Books platform
- [x] Align Books with scanner/indexer.
- [x] EPUB/PDF metadata and cover extraction.
- [x] Author/series model.
- [x] Reading progress and bookmarks.
- [x] EPUB/PDF reader.
- [x] Search/filter/sort parity.
- [x] Library health integration.

## M8 — Unified dashboard
- [x] Continue Reading/Watching/Listening.
- [x] Recently Added.
- [x] Recently Played/Read.
- [x] Favorites across media.
- [x] Activity timeline.
- [x] Cross-media recommendations only from local catalogue unless provider explicitly enabled.

## M9 — Windows productization
- [ ] Desktop wrapper.
- [ ] Installer/uninstaller.
- [ ] Startup/tray behavior.
- [ ] File associations where appropriate.
- [ ] Media keys.
- [ ] Windows notifications.
- [ ] Data migration/backup/restore.

## Agent execution rule
Each task should be delivered as a complete vertical slice: implementation → tests → UI states → docs.
