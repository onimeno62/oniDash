# Milestone 06 — Movies (Phase 6)

Scope from `docs/ROADMAP.md`: Movie, video detection, metadata, artwork, playback,
watch progress.

## What was built

### Movies plugin (`backend/oniDash.Movies/`)
The second catalogue plugin, mirroring the Music architecture (rules 2, 5, 6): its own
`MoviesDbContext`, its own additive EF migration (`InitialMovieCatalog` — rule 12), and
its own startup initializer in a separate try/catch. A keyless anchor maps to the core
`MediaItems` table; `Movies.MediaItemId → MediaItems` cascades on delete, so removing a
library removes its movies. The migration never creates or drops core-owned tables.

- **Video detection**: `MovieCatalogService.VideoExtensionsMatch` (mp4, m4v, mkv, avi,
  mov, wmv, webm, mpg/mpeg, ts, flv); the plugin's `IIndexedMediaHandler` receives every
  scanned file and claims only videos — the scanner itself stays catalogue-free (rule 6).
- **Metadata** (offline-first, rule 7/8):
  - *Filename parser* (`MovieNameParser`, pure string transform): `Title (Year)`,
    scene-style dotted names, quality/edition noise tokens, release-group tails anchored
    by codec tokens (`...x264-GRP`), multi-part markers (`CD1/Part 2`). The bare-year
    match takes the *last* 19xx/20xx group so `2001.A.Space.Odyssey.1968.1080p` parses
    correctly.
  - *Probe* (`IVideoProbeReader`/`FfprobeVideoProbeReader`): duration + geometry from the
    local ffprobe (read-only, rule 11). When ffprobe is absent or the file is unreadable
    the probe returns null and cataloguing degrades to filename metadata — the catalogue
    still fills in.
- **Artwork** (`IVideoArtworkReader`/`TagLibVideoArtworkReader`): embedded poster atoms
  (MP4/M4V cover art) via TagLib#; unsupported containers yield null and the UI shows a
  generated placeholder. Posters are captured once, never overwritten by re-scans.
- **Watch progress**: `SaveProgressAsync` (resume position) and `SetWatchedAsync`
  (mark/unmark; either direction resets the resume position). Re-scans and reindex
  passes never overwrite watch state.

### API (`Api` — endpoints mapped by the plugin)
- `GET /api/movies?libraryId=&watched=` → summaries (title, year, runtime, container,
  poster flag, watched state, resume position), ordered by normalized title.
- `GET /api/movies/continue?libraryId=` → partially watched, unwatched movies.
- `GET /api/movies/{id}/poster` → embedded artwork bytes (404 when none).
- `GET /api/movies/{id}/stream` → ranged video stream (206 on partial requests) for
  seeking.
- `POST /api/movies/{id}/progress` → save resume position; reaching ≥ 95 % of the
  runtime counts as watched and clears the position.
- `POST /api/movies/{id}/watched` → mark/unmark watched (resume resets).
- `POST /api/movies/reindex` → `{ indexedMovies }` recovery command.

### UI (`MoviesPage`)
- Movies nav entry; page with library picker, watched filter (all/unwatched/watched),
  poster grid (2:3 cards, runtime/year captions, "Watched" badge, resume progress bar),
  a Continue-watching row, and an empty state explaining what fills the page.
- `MoviePlayer` full-surface overlay: `<video controls autoplay>` streaming from the
  API with range requests, resume-to-position on open, progress saved every ~10 s of
  playback, auto-mark-watched on `ended`, manual "Mark watched" button, Escape/close.
- The frontend never touches SQLite or the filesystem (rule 3). Sidebar footer version
  text updated.

## Tests
- Backend **159/159** (+20, 0 warnings): new `oniDash.Movies.Tests` — 10 parser cases
  (parenthesized/bare/scene-style names, `1941 (1979)` and `2012 (2009)` title-vs-year
  traps, codec tails, multi-part, no-year), 9 catalog tests (probe metadata, non-video
  rejection, same-row reindex, unprobeable degradation, poster captured once, watch
  state surviving reindex and being clearable, unknown-id rejection, extension match),
  1 reindex test (only missing/stale candidates), all against the real migration in an
  in-memory SQLite database with a faithful core-schema shim.
- Frontend **41/41** (+5, typecheck clean, build OK): empty state, grid rendering with
  runtime formatting, continue-watching cards, watched-filter request params, player
  open → mark-watched → close flow.

## Verification
- E2E `artifacts/visual/verify-m06.js`: **23/23** against the live app — generates real
  fixtures with the local ffmpeg build (a VP9/WebM test video, an MP4 with an embedded
  PNG cover atom, and an unprobeable placeholder), scans them through the API, then
  checks detection, filename parsing (incl. release-style + codec tail), probe duration,
  poster extraction/serving, 206 ranged streaming, progress save, continue-watching,
  watched marking (manual + reaching-the-end in the browser player), watched filter,
  reindex counter, grid/poster rendering, player overlay with ranged stream request,
  and M01/M02/M04/M05 regressions.
- Regressions: M01–M05 scripts all pass (M01/M05 version assertions loosened to
  semver-shape so they stay milestone-independent).
- Two real defects found and fixed by the E2E pass:
  1. `GET /movies/continue` crashed — SQLite cannot `ORDER BY DateTimeOffset`; now
     ordered by deepest progress with a title tie-break.
  2. The watched *endpoint* still kept the resume position on mark-watched (the service
     had been fixed but the endpoint had its own copy of the rule); both now clear it.
- API v0.6.0 health `Healthy db=Ok`; `InitialMovieCatalog` applied to the local database
  on startup — additive only, existing data intact (rule 12).

## Known limitations
- Metadata is filename + probe only; no online providers (Phase 10) and no local
  NFO/poster-file sidecar support yet.
- Artwork comes from embedded MP4 cover atoms only; mkv/avi attachments and external
  poster images are placeholders for now.
- No duplicate merging: two rips of one film stay separate rows (Phase 10 groundwork).
- No subtitle/audio-track selection, transcoding, or TV/anime structures (Phase 7).
- Watch progress saves every ~10 s of active playback; closing the tab mid-scene can
  lose up to that much position.
