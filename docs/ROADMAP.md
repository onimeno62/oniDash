# oniDash Roadmap

This is a gated execution plan. A milestone is complete only when its acceptance criteria and relevant tasks are implemented and verified with evidence.

## Phase 0 — Platform foundation
Existing shared Core/Application/Infrastructure, jobs, scanner/indexer, metadata/artwork contracts and global search are retained and hardened as needed.

## Phase 1 — Music rebuild [CURRENT]
Rebuild Music as a complete local music manager/player on top of the platform foundation. Do not begin later catalogue work until the Music gate passes.

### Gate A — Music domain and library foundation
- [ ] Canonical music entities and relationships are documented and mapped safely.
- [ ] Music library sources and scan/reconciliation lifecycle are reliable.
- [ ] Server-side songs/albums/artists/genres/folders queries work.
- [ ] Search/filter/sort/pagination work on large libraries.
- [ ] Missing/moved/changed files reconcile without duplicate rows.

### Gate B — File management
- [ ] Rename/move/delete/open-location operations are backend-owned and tested.
- [ ] Bulk operations validate and report per-file results.
- [ ] Organization templates support preview/dry-run.
- [ ] Duplicate detection is available without automatic destructive merging.

### Gate C — Metadata and artwork
- [ ] Single and bulk tag editing works.
- [ ] Tag writes are safe and verified.
- [ ] MusicBrainz/AcoustID enrichment is optional and preview-before-apply.
- [ ] Artwork extraction/cache and provider enrichment work with provenance.

### Gate D — Playback
- [ ] Application-scoped player survives navigation.
- [ ] Play/pause/seek/previous/next/queue/play-next/shuffle/repeat work.
- [ ] Volume/output-device handling works.
- [ ] Playback history and resume state persist.
- [ ] Gapless/crossfade/ReplayGain degrade cleanly when unsupported.

### Gate E — Lyrics and visualization
- [ ] Embedded/local/provider lyrics work.
- [ ] Plain and synchronized lyrics render correctly.
- [ ] Lyrics can be saved and edited safely.
- [ ] Click-to-seek synced lyrics work.
- [ ] Waveform and visualizer data come from the playback pipeline.

### Gate F — Collections and Music Home
- [ ] Favorites and ratings work.
- [ ] Recently added/played, most played, never played and top rated are real database queries.
- [ ] Normal and smart playlists work.
- [ ] Music Home uses working data rather than mock/placeholder controls.
- [ ] All Music actions have loading/error/empty states and tests.

## Phase 2 — Movies / TV / Anime
Resume only after Phase 1 passes. Align existing implementations with the canonical platform pipeline, then complete watch state, metadata, artwork and playback.

## Phase 3 — Manga
Complete real plugin/source architecture, reader, downloads, progress and optional integrations.

## Phase 4 — Books
Complete canonical scanner integration, metadata, covers, reading progress and readers.

## Phase 5 — Unified dashboard
Continue Listening/Watching/Reading, recent activity, favorites and cross-media discovery built on verified catalogue data.

## Phase 6 — Windows productization
Desktop wrapper, installer, file associations, media keys, notifications, startup/tray and backup/restore.

## Rule
Do not advance because a screen looks finished. Functional correctness, data safety and test evidence are the gate.
