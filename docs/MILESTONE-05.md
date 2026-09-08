# Milestone 05 — Music (Phase 5)

Scope from `docs/ROADMAP.md`: Artist, Album, Track, embedded metadata, artwork,
UI, basic playback.

## What was built

### Music plugin (`backend/oniDash.Music/`)
A separate catalogue plugin project (rules 2, 5, 6): Core and Application know nothing
about music; the plugin owns its own `MusicDbContext`, its own EF migration
(`InitialMusicCatalog`, additive — rule 12), and its own initializer that runs in a
separate try/catch at startup so a music failure can never take the core down.

- **Cross-catalogue relation by convention**: a keyless `MediaItemAnchor` maps to the
  existing core `MediaItems` table; `Tracks.MediaItemId → MediaItems` cascades on
  delete (scans own discovery; catalogue rows follow), while artist/album links are
  `SetNull`. The migration was hand-edited so the music context never creates or drops
  `MediaItems` (core owns the table).
- **Tag reading** (`Tagging/`): `IAudioTagReader` with a TagLibSharp implementation →
  `AudioTags` (title, track/album artist, album, numbers, year, duration, genre, cover
  bytes + content type); any parse failure returns `null` rather than throwing. Front
  cover is preferred when a file carries multiple pictures.
- **Grouping** (`Cataloging/MusicCatalogService`): `IndexFileAsync` upserts one track per
  `MediaItemId`; albums are grouped by `(album, albumArtist ?? trackArtist)`, per-library
  unique on normalized names, case-insensitive artist merge, album-artist wins for
  grouping, cover captured once (first file with art), title falls back to the file stem.
  Resolution checks *both* the database and entities added during the same pass, so
  two files of one album in one scan can never collide (a real bug the tests caught).
- **Reindex** (`MusicReindexService`): recovery command that finds audio files whose
  track row is missing or older than the file (`POST /api/music/reindex` →
  `{ indexedTracks }`), never touching non-audio items.
- **Streaming** (`IMediaFileLocator`/`AudioFileLocator`): resolves the track's current
  file path through core repositories, guards against root escapes and files marked
  missing, and maps content types (mp3 → `audio/mpeg`, m4a → `audio/mp4`, flac, ogg…).

### Capability hook (Application — rule 6)
`ScanService` now exposes `IIndexedMediaHandler` — after each file row is persisted the
scanner hands every registered handler an `IndexedMediaContext` (source, file row, media
item, change kind New/Updated/Unchanged). The scanner stays catalogue-agnostic; the
music plugin registers `MusicIndexedMediaHandler` (skips non-audio and unchanged files,
never lets a catalogue error break a scan). Extension-based audio detection lives in the
plugin (`MusicCatalogService.AudioExtensionsMatch`).

### API (`Api` — endpoints mapped by the plugin)
- `GET /api/music/artists?libraryId=` → `[{ id, name }]`
- `GET /api/music/albums?libraryId=&artistId=` → `[{ id, title, artistName, year, hasCover }]`
- `GET /api/music/tracks?libraryId=&albumId=&artistId=` → ordered track summaries
  (disc/track number, year, duration, genre, album linkage)
- `GET /api/music/albums/{id}/cover` → embedded artwork bytes (404 when none)
- `GET /api/music/tracks/{id}/stream` → ranged audio stream (`Accept-Ranges`, 206 on
  partial requests) so the browser can seek
- `POST /api/music/reindex` → `{ indexedTracks }`

### UI (`MusicPage` + app-wide player)
- Music nav entry; page with library picker, artist chips (incl. "All"), album grid
  with embedded covers (gradient placeholder when absent), and ordered track rows.
- `PlayerProvider` (single `<audio>` element, context state) + fixed bottom
  `PlayerBar`: play/pause per track, seek, position/duration readout, stop. Streams
  come from the API with range requests — the frontend never touches SQLite or the
  filesystem (rule 3).
- The stale sidebar footer version text was corrected while touching navigation.

## Tests
- Backend **139/139** (+17, 0 warnings): new `oniDash.Music.Tests` — 12 catalog tests
  (tag extraction to track/album/artist, filename fallback, two-file album grouping,
  case-insensitive artist merge, album-artist grouping, albumless tracks, single cover
  capture, album separation across libraries, same-row reindex, unreadable files write
  nothing, extension matching, stale/missing reindex candidates) plus 5 real-file
  TagLib# round-trips (text tags, embedded cover, front-cover preference, garbage and
  missing files → null). Application tests cover the scan→handler hook (change kinds,
  non-audio skipped, handler failures never fail the scan).
- Frontend **36/36** (+5, typecheck clean, build OK): empty state, catalogue rendering
  with durations, play→player-bar flow, artist filter request params, retryable error
  state.

## Verification
- E2E `artifacts/visual/verify-m05.js`: **19/19** against the live app — generates real
  tagged MP3s (ID3v2.3 frames + silent MPEG frames written byte-wise in node), scans
  them through the API, then checks artists/albums/tracks/cover/duration/album-order,
  HTTP 206 ranged streaming, reindex counter, artist filtering + cover rendering +
  playback-initiated ranged request in the UI, no overflow at 1280/1920/2560, and
  M01/M02/M04 API/UI regressions.
- Regressions: M01, M02, M03, M04 scripts all pass.
- API v0.5.0 health `Healthy db=Ok`; `InitialMusicCatalog` applied to the local
  database on startup — additive only, existing data intact (rule 12).

## Known limitations
- Playback is single-track manual play: no queue, autoplay-next, shuffle, or volume
  control yet; the player state lives in memory and resets on reload.
- Only embedded artwork is used — no folder-image fallback (`cover.jpg`) yet.
- Catalogue updates only on scan (via the handler) or the reindex command; renames of
  already-indexed files are not re-read until a rescan marks them changed.
- Genres/years are stored per track but not yet faceted in the UI (no genre browsing).
- Music search stays with M04's media-item search; tag-level search comes later.
