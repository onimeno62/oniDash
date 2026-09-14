# oniDash Master Task List

**Current milestone: Music Rebuild — hardening and native Windows playback**

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
- [x] Loading/empty/error/retry states on Music Home.
- [ ] Complete missing-file-specific browse states across all Music library views.

## 4 — File management
- [x] Safe single-track rename with source-root validation and index reconciliation.
- [x] Safe move inside the owning library source root.
- [x] Explicit delete from disk with confirmation and missing-file reconciliation.
- [x] Remove from library without deleting the physical file.
- [ ] Open file location in Windows Explorer.
- [ ] Bulk rename/move/delete with per-item results.
- [ ] Filename template engine.
- [ ] Organization dry-run preview.
- [ ] Duplicate detection: exact and potential duplicates.
- [ ] Tests for collisions, invalid paths, failures and rollback/reconciliation.

## 5 — Metadata and artwork
- [x] Embedded tag reader/writer foundation using TagLib.
- [x] Single-track metadata editor with explicit confirmation and catalogue reindex.
- [ ] Bulk metadata editor with replace/append/find-replace operations.
- [ ] Tag normalization rules.
- [ ] Metadata write verification.
- [ ] Metadata change/error reporting.
- [x] MusicBrainz read-only provider adapter.
- [ ] AcoustID fingerprint provider adapter.
- [ ] Candidate matching UI with current-vs-proposed preview.
- [x] Embedded artwork extraction into album artwork storage.
- [ ] Local folder artwork fallback where supported.
- [ ] Artwork cache and provenance.
- [ ] Provider failure/rate-limit tests.

## 6 — Playback engine
- [x] Replace page-owned audio behavior with application-scoped PlayerService.
- [x] Define player state machine and serialized command surface in the application-scoped service.
- [ ] Implement decoder/output abstraction.
- [ ] Implement Windows WASAPI output/device enumeration.
- [x] Play/pause.
- [x] Seek.
- [x] Previous/next.
- [x] Queue add/remove/reorder/clear API; UI reorder remains pending.
- [x] Play Next.
- [x] Shuffle.
- [x] Repeat Off/All/One.
- [x] Volume/mute/output volume control; device selection remains pending.
- [x] Persist playback position and history.
- [x] Handle load/seek/decode errors without corrupting queue state.
- [ ] Gapless playback where supported.
- [ ] Crossfade where supported.
- [ ] ReplayGain/loudness where supported.

## 7 — Player UX
- [x] Persistent Mini Player.
- [x] Expanded Player.
- [x] Full Now Playing view.
- [x] Queue panel.
- [ ] Waveform generation/rendering.
- [ ] Precise waveform seeking.
- [x] Audio-analysis pipeline for live FFT/spectrum data.
- [x] Spectrum visualizer.
- [x] Bars visualizer.
- [x] Oscilloscope visualizer component.
- [ ] Additional visualizer modes only after core visualizers are stable.
- [x] Ensure player survives route changes and page unmounts.

## 8 — Lyrics
- [ ] Embedded lyrics extraction.
- [x] Local `.lrc` and `.txt` discovery.
- [x] Lyrics provider abstraction.
- [x] LRCLIB provider.
- [ ] Search and candidate selection UI.
- [ ] Explicit save/download operation from provider results.
- [x] Plain lyrics display.
- [x] Synced lyrics display.
- [x] Click lyric line to seek.
- [x] Lyrics editor with timestamp editing via LRC text editing.
- [ ] Offset/shift synchronization tools.
- [x] Preserve source/provenance and local edits in MusicLyrics storage.
- [x] Provider failure and no-match states.

## 9 — Collections and history
- [x] Favorite/unfavorite track behavior and existing album/artist favorite API.
- [x] 0–5 star rating behavior.
- [x] Play history recording rules for natural track completion.
- [x] Recently Added query.
- [x] Recently Played query.
- [x] Most Played query.
- [x] Never Played query.
- [x] Top Rated query.
- [x] Normal playlist creation and persistence.
- [x] Playlist reorder/remove/queue/play API.
- [ ] Smart playlist rule model verification and rule builder UI.
- [ ] Smart playlist rule builder.
- [x] Save queue as playlist.

## 10 — Music Home
- [x] Continue Listening backed by persisted playback state, including direct track resolution.
- [x] Recently Played carousel.
- [x] Recently Added carousel.
- [x] Most Played carousel.
- [x] Favorites carousel.
- [x] Top Rated carousel.
- [x] Album/artist/playlist discovery sections.
- [x] Playlist section.
- [x] Library totals/overview statistics.
- [ ] Useful music-library health signals beyond basic totals.
- [x] Empty/new-library onboarding state.
- [x] Remove dead/placeholder dashboard retry and layout controls.
- [x] Standard and Bento dashboard layouts have distinct rendering behavior.

## 11 — Windows integration and hardening
- [x] Application keyboard shortcut map for player controls.
- [ ] Native Windows media-key integration boundary.
- [ ] Windows output-device change handling.
- [ ] Native notifications/Now Playing integration where appropriate; browser Media Session exists but is not native Windows integration.
- [ ] Drag/drop files and folders.
- [ ] Native Windows Explorer location integration.
- [ ] Large-library performance profiling.
- [ ] Crash/restart recovery for scanner and player.
- [ ] End-to-end Music regression suite.
- [ ] Accessibility pass.
- [ ] Final destructive-operation safety audit.
- [x] Add GitHub Actions CI workflow for backend restore/build/test and frontend install/build/test.
- [ ] Obtain and fix actual green CI verification evidence.

## Global acceptance criteria
- [ ] No visible Music control is fake or dead.
- [x] No Music page directly touches SQLite or the filesystem.
- [x] Playback is independent of React page lifecycle.
- [x] Library queries are server/database backed and bounded to server-side pages.
- [x] External metadata provider results are explicitly read-only proposals.
- [x] Files are never silently renamed, moved, deleted or overwritten by catalogue operations.
- [x] Long-running source scans are observable and cancellable through the shared scan-job system.
- [ ] Build, typecheck and relevant automated tests have actual evidence.
