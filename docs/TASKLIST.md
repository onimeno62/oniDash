# oniDash Master Task List

**Current milestone: Music Rebuild**

Agents execute the first applicable unchecked task in order. A task is complete only with implementation, relevant tests, handled UI states, documentation updates where needed, and actual verification evidence.

## 0 — Preparation / audit
- [ ] Inventory current Music backend, database, API, frontend, player and tests.
- [ ] Map existing Music functionality to the new Music contracts.
- [ ] Identify broken/dead/duplicated code and mark it for replacement or removal.
- [ ] Establish a regression baseline without claiming unverified existing tests pass.
- [ ] Define migration strategy so existing catalogue/user state is preserved.

## 1 — Music domain and persistence
- [ ] Define Track, Album, Artist, Genre, AlbumArtist, Disc and file relationships.
- [ ] Define Favorite, Rating, PlayHistory and PlaybackState persistence.
- [ ] Define Playlist, PlaylistItem and SmartPlaylist models.
- [ ] Define Lyrics/LyricsVersion and Artwork models/provenance.
- [ ] Add required indexes for library queries, history and search.
- [ ] Add safe migrations and migration tests.
- [ ] Define typed Music API DTOs; do not expose EF entities.

## 2 — Library sources and scanning
- [ ] Add/remove/validate Music library sources.
- [ ] Implement full recursive music scan.
- [ ] Implement incremental scan based on file identity/change detection.
- [ ] Integrate filesystem watcher/reconciliation.
- [ ] Reconcile renamed/moved files without creating duplicate tracks.
- [ ] Mark missing files recoverably.
- [ ] Add scan progress, cancellation, retry and per-file diagnostics.
- [ ] Test scan idempotency and failure isolation.

## 3 — Library queries
- [ ] Songs endpoint with server-side pagination.
- [ ] Albums endpoint with grouping and sorting.
- [ ] Artists endpoint.
- [ ] Genres endpoint/facets.
- [ ] Folders endpoint/tree.
- [ ] Shared filtering and sorting contract.
- [ ] Multi-field music search.
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
- [ ] Library queries are server/database backed and scalable.
- [ ] External providers never silently overwrite local truth.
- [ ] Files are never silently renamed, moved, deleted or overwritten.
- [ ] Long-running operations are observable and cancellable.
- [ ] Build, typecheck and relevant automated tests have actual evidence.
