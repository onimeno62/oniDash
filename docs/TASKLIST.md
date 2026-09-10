# oniDash Master Task List

This is the execution order for autonomous agents. Do not skip ahead because a later UI is easier to build.

## M0 — Platform foundation hardening
- [x] Audit current Core/Application/Infrastructure boundaries.
- [x] Define canonical `MediaType`, `MediaItem`, `MediaFile`, `Library`, `Source`, `Artwork`, `Tag`, `Collection` contracts.
- [x] Define canonical API error envelope and pagination conventions.
- [x] Define common background `Job` model and status API.
- [x] Add integration-test harness for API + temporary SQLite + temporary media folders.
- [x] Fix any API routes that can fall through to SPA HTML.

## M1 — Unified scanner/indexer
- [x] Source CRUD and validation.
- [x] Safe recursive filesystem enumerator.
- [x] Extension/signature-based media handler registry.
- [x] Stable file identity and idempotent upsert.
- [x] Missing-file detection.
- [x] Scan jobs with progress/cancel/retry.
- [x] Scan diagnostics and per-file failures.
- [x] Scanner service behavior tests for indexing, reconciliation, progress, and handler isolation.
- [ ] End-to-end scanner integration test against temporary SQLite + filesystem.

## M2 — Metadata + artwork engine
- [x] `IMediaHandler` contract.
- [ ] Audio metadata handler.
- [ ] Video metadata handler.
- [ ] EPUB/PDF/comic metadata handler.
- [ ] Metadata normalization pipeline.
- [x] Provenance/confidence model.
- [ ] Embedded artwork extraction.
- [x] Artwork cache and lifecycle contract.
- [x] Optional provider interfaces.
- [x] Enrichment preview/apply workflow contract; never implicit overwrite.

## M3 — Global search + library health
- [x] Canonical cross-media search contract.
- [x] Canonical FTS5-backed search index model.
- [x] Live incremental FTS maintenance through database triggers.
- [x] Full rebuild/recovery command.
- [x] Cross-media search endpoint and backend search service.
- [ ] Filters for media type/library/tag/status in the canonical search surface.
- [ ] Search result navigation parity across all media modules.
- [ ] Library health dashboard.
- [ ] Missing files / missing artwork / metadata errors / duplicates diagnostics.

## M4 — Music completion
- [ ] Verify existing Music APIs against canonical platform contracts.
- [ ] Fix remaining type/build issues.
- [ ] Library health and duplicate tooling.
- [ ] ReplayGain/loudness metadata where supported.
- [ ] Gapless/crossfade where technically supported.
- [ ] Media-key/Windows playback integration boundary.
- [ ] Complete lyrics/provider error handling.

## M5 — Movies + TV/Anime
- [ ] Align Movies with scanner/indexer.
- [ ] Movie metadata/artwork enrichment.
- [ ] Watch-state/progress persistence.
- [ ] Series/season/episode domain.
- [ ] Anime-specific metadata as optional provider enrichment.
- [ ] Continue-watching across movies/episodes.

## M6 — Manga platform
- [ ] Replace temporary local adapter behavior with real plugin architecture.
- [ ] Mihon/Suwayomi-compatible source/extension boundary.
- [ ] Extension store/install/update management.
- [ ] Source search/popular/latest.
- [ ] Manga/chapter persistence.
- [ ] Reading progress and bookmarks.
- [ ] Download queue and offline storage.
- [ ] Reader.
- [ ] Library updates/notifications.
- [ ] Tracking/sync as optional integrations.

## M7 — Books platform
- [ ] Align Books with scanner/indexer.
- [ ] EPUB/PDF metadata and cover extraction.
- [ ] Author/series model.
- [ ] Reading progress and bookmarks.
- [ ] EPUB/PDF reader.
- [ ] Search/filter/sort parity.
- [ ] Library health integration.

## M8 — Unified dashboard
- [ ] Continue Reading/Watching/Listening.
- [ ] Recently Added.
- [ ] Recently Played/Read.
- [ ] Favorites across media.
- [ ] Activity timeline.
- [ ] Cross-media recommendations only from local catalogue unless provider explicitly enabled.

## M9 — Windows productization
- [ ] Desktop wrapper.
- [ ] Installer/uninstaller.
- [ ] Startup/tray behavior.
- [ ] File associations where appropriate.
- [ ] Media keys.
- [ ] Windows notifications.
- [ ] Data migration/backup/restore.

## Agent execution rule
Each task should be delivered as a complete vertical slice: implementation → tests → UI states → docs. Never mark a task complete without evidence. When repository CI is unavailable, distinguish source-level evidence from executed verification.
