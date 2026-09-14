# oniDash Master Task List

**Current milestone: Music Rebuild — Phase 4 next**

Agents execute the first applicable unchecked task in order. A task is complete only with implementation, relevant tests, handled UI states, documentation updates where needed, and actual verification evidence.

## 0 — Preparation / audit
- [x] Inventory current Music backend, database, API, frontend, player and tests.
- [x] Map existing Music functionality to the new Music contracts.
- [x] Identify broken/dead/duplicated code and mark it for replacement or removal.
- [x] Establish a regression baseline without claiming unverified existing tests pass.
- [x] Define migration strategy so existing catalogue/user state is preserved.

## 1 — Music domain and persistence
- [x] Define Track, Album, Artist, Genre, AlbumArtist, Disc and file relationships.
- [x] Define Favorite, Rating, PlayHistory and PlaybackState persistence.
- [x] Define Playlist, PlaylistItem and SmartPlaylist models.
- [x] Define Lyrics/LyricsVersion and Artwork models/provenance.
- [x] Add required indexes for library queries, history and search.
- [x] Add safe migration for the first Phase 1 persistence slice.
- [ ] Add migration regression tests against an existing Music database.
- [x] Define typed Music API DTOs; do not expose EF entities.
- [ ] Regenerate and verify the EF model snapshot after the next local EF migration run.

## 2 — Library sources and scanning
- [x] Add/remove/validate Music library sources through the shared Library/Source API.
- [x] Implement full recursive music scan through the shared scanner.
- [x] Implement incremental scan based on source-relative file identity and size/write-time change detection.
- [ ] Integrate filesystem watcher/reconciliation for near-real-time changes.
- [x] Reconcile uniquely identifiable renamed/moved files without creating duplicate tracks.
- [x] Mark missing files recoverably instead of deleting catalogue rows.
- [x] Add scan progress, cancellation, retry and per-file diagnostics through the shared scan-job system.
- [ ] Add dedicated Music scan idempotency/failure-isolation regression tests.

## 3 — Library queries
- [x] Songs endpoint with server-side pagination and total counts.
- [x] Albums endpoint with grouping/counts and sorting.
- [x] Artists endpoint with track/album counts.
- [x] Genres endpoint/facets.
- [ ] Folders endpoint/tree.
- [x] Shared filtering and sorting behavior across Songs/Albums/Artists.
- [x] Multi-field track search across title, artist and album artist.
- [ ] Virtualized Songs table.
- [ ] Multi-select and keyboard navigation.
- [ ] Loading/empty/error/retry/missing-file states.

## 4 — File management
- [ ] Safe single-track rename.
- [ ] Safe move.
- [ ] Explicit delete from disk.
- [ ] Remove from library without deleting file.
- [ ] Open file location in Windows Explorer.
- [ ] Bulk rename/move/delete with per-item results.
- [ ] Filename template engine.
- [ ] Organization dry-run preview.
- [ ] Duplicate detection: exact and potential duplicates.
- [ ] Tests for collisions, invalid paths, failures and rollback/reconciliation.

## 5 — Metadata and artwork
- [ ] Complete embedded tag reader/writer.
- [ ] Single-track metadata editor.
- [ ] Bulk metadata editor with replace/append/find-replace operations.
- [ ] Tag normalization rules.
- [ ] Metadata write verification.
- [ ] Metadata change/error reporting.
- [ ] MusicBrainz provider adapter.
- [ ] AcoustID fingerprint provider adapter.
- [ ] Candidate matching UI with current-vs-proposed preview.
- [ ] Embedded artwork extraction.
- [ ] Local folder artwork fallback where supported.
- [ ] Artwork cache and provenance.
- [ ] Provider failure/rate-limit tests.

## 6 — Playback engine
- [ ] Replace page-owned audio behavior with application-scoped PlayerService.
- [ ] Define player state machine and serialized commands.
- [ ] Implement decoder/output abstraction.
- [ ] Implement Windows WASAPI output/device enumeration.
- [ ] Play/pause.
- [ ] Seek.
- [ ] Previous/next.
- [ ] Queue add/remove/reorder/clear.
- [ ] Play Next.
- [ ] Shuffle.
- [ ] Repeat Off/All/One.
- [ ] Volume/mute/output device.
- [ ] Persist playback position and history.
- [ ] Handle load/seek/device/decode errors without corrupting queue state.
- [ ] Gapless playback where supported.
- [ ] Crossfade where supported.
- [ ] ReplayGain/loudness where supported.

## 7 — Player UX
- [ ] Persistent Mini Player.
- [ ] Expanded Player.
- [ ] Full Now Playing view.
- [ ] Queue panel.
- [ ] Waveform generation/rendering.
- [ ] Precise waveform seeking.
- [ ] Audio-analysis pipeline for FFT/spectrum data.
- [ ] Spectrum visualizer.
- [ ] Bars visualizer.
- [ ] Oscilloscope visualizer.
- [ ] Additional visualizer modes only after core visualizers are stable.
- [ ] Ensure player survives route changes and page unmounts.

## 8 — Lyrics
- [ ] Embedded lyrics extraction.
- [ ] Local `.lrc` and `.txt` discovery.
- [ ] Lyrics provider abstraction.
- [ ] LRCLIB provider.
- [ ] Search and candidate selection.
- [ ] Explicit save/download operation.
- [ ] Plain lyrics display.
- [ ] Synced lyrics display.
- [ ] Click lyric line to seek.
- [ ] Lyrics editor with timestamp editing.
- [ ] Offset/shift synchronization tools.
- [ ] Preserve source/provenance and local edits.
- [ ] Provider failure and no-match states.

## 9 — Collections and history
- [ ] Favorite/unfavorite track/album/artist behavior.
- [ ] 0–5 star rating behavior.
- [ ] Play history recording rules.
- [ ] Recently Added query.
- [ ] Recently Played query.
- [ ] Most Played query.
- [ ] Never Played query.
- [ ] Top Rated query.
- [ ] Normal playlists.
- [ ] Playlist reorder/remove/queue/play.
- [ ] Smart playlist rule model.
- [ ] Smart playlist rule builder.
- [ ] Save queue as playlist.

## 10 — Music Home
- [ ] Continue Listening backed by persisted playback state.
- [ ] Recently Played carousel.
- [ ] Recently Added carousel.
- [ ] Most Played carousel.
- [ ] Favorites carousel.
- [ ] Top Rated carousel.
- [ ] Album/artist/genre discovery sections.
- [ ] Playlist section.
- [ ] Library totals.
- [ ] Useful music-library health signals.
- [ ] Empty/new-library onboarding.
- [ ] Remove dead/placeholder dashboard controls.

## 11 — Windows integration and hardening
- [ ] Media-key integration boundary.
- [ ] Windows output-device change handling.
- [ ] Native notifications/Now Playing integration where appropriate.
- [ ] Drag/drop files and folders.
- [ ] Keyboard shortcut map.
- [ ] Large-library performance profiling.
- [ ] Crash/restart recovery for scanner and player.
- [ ] End-to-end Music regression suite.
- [ ] Accessibility pass.
- [ ] Final destructive-operation safety audit.

## Global acceptance criteria
- [ ] No visible Music control is fake or dead.
- [ ] No Music page directly touches SQLite or the filesystem.
- [ ] Playback is independent of React page lifecycle.
- [x] Library queries are server/database backed and bounded to server-side pages.
- [ ] External providers never silently overwrite local truth.
- [ ] Files are never silently renamed, moved, deleted or overwritten.
- [x] Long-running source scans are observable and cancellable through the shared scan-job system.
- [ ] Build, typecheck and relevant automated tests have actual evidence.
