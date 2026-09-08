# oniDash Agent Kit

This kit contains repository instructions, product requirements, architecture, UI/design specifications, UX flows, plugin rules, development/testing skills, roadmap, and an autonomous-agent bootstrap prompt.

Start with `AGENTS.md`, then `BOOTSTRAP.md` and `AGENT-PROMPT.md`.

## Repository layout

```text
backend/oniDash.Core            Generic domain primitives (no catalogue logic)
backend/oniDash.Application     Use cases / queries / DTOs
backend/oniDash.Infrastructure  EF Core + SQLite persistence
backend/oniDash.Api             ASP.NET Core host, serves the built SPA
frontend/oniDash.Web            React + TypeScript shell (Vite, Tailwind v4)
tests/                          Backend test projects (xUnit)
docs/                           Product/architecture/UI specifications
artifacts/                      Local build caches, NuGet feed, visual checks (gitignored)
```

## Running (Milestone 01)

Backend (API on http://localhost:5275):

```powershell
dotnet build oniDash.sln
dotnet run --project backend/oniDash.Api
```

The SQLite database is created at `%LOCALAPPDATA%\oniDash\onidash.db` on first start;
override with `ConnectionStrings:OniDash` or `OniDash:DataDirectory`.

Frontend development (Vite on http://localhost:5173, proxies `/api` to :5275):

```powershell
cd frontend/oniDash.Web
npm install
npm run dev
```

Production-style local run: `npm run build`, copy `dist/*` to
`backend/oniDash.Api/wwwroot/`, then start the API and open http://localhost:5275.

## Verification

```powershell
dotnet test oniDash.sln
cd frontend/oniDash.Web
npm run typecheck
npm run test
npm run build
```

## Milestone status

- **Milestone 01 — Foundation: complete.** Solution, health endpoint + SQLite wiring,
  React shell (AppShell/Sidebar/TopBar), routing, design tokens, dark/light themes,
  dashboard/library/search/settings/health pages, loading/empty/error states, tests.
- **Milestone 02 — Core library: complete.** EF Core + SQLite with `InitialCoreLibrary`
  migration, generic entities (Library, Source, MediaItem, MediaFile, Artwork, Tag,
  Collection), library/source/tag/collection APIs, functional library UI with read-only
  folder sources. No catalogue logic, no scanning yet.
- **Milestone 03 — Filesystem scanner: complete.** Read-only recursive enumerator
  (hidden/system/junk/extension filters, reparse-point safe), background scan jobs with
  live progress and cancellation, upsert-by-identity index (`IdentityKey` unique per
  source + path), vanished files marked missing (never deleted), scan start/status/
  cancel/list API, scan buttons + progress + media list in the library UI.
- **Milestone 04 — Search: complete.** SQLite FTS5 full-text search over media items
  (bm25 ranking, per-term prefix matching, multi-term AND), trigger-maintained live
  index plus a reindex recovery command, `GET /api/search` + `POST /api/search/reindex`,
  live search-as-you-type UI with library filter and ranked results. Foreign keys are
  now enforced (`Foreign Keys=True`) so model-declared cascades actually fire.
- **Milestone 05 — Music: complete.** First catalogue plugin (`oniDash.Music`): reads
  embedded tags with TagLibSharp via a new `IIndexedMediaHandler` scanner hook,
  groups tracks into per-library artists/albums, captures embedded cover art, and
  exposes artists/albums/tracks, cover, and ranged streaming endpoints. The UI adds a
  Music page (library picker, artist chips, album grid, track list) with an app-wide
  bottom player bar (play/pause, seek, stop) over HTTP range requests. Additive
  `InitialMusicCatalog` migration; core stays catalogue-free (rules 1, 2, 5, 6, 12).
- **Milestone 06 — Movies: complete.** Second catalogue plugin (`oniDash.Movies`):
  video detection by extension, offline metadata from a release-name parser plus an
  ffprobe duration/geometry read, embedded MP4 poster art, ranged video streaming, and
  a poster-grid UI with a full-surface player (resume-to-position, periodic progress
  saves, auto-watched at the end) plus Continue-watching, watched filters, and a
  reindex recovery command. Additive `InitialMovieCatalog` migration (rules 1, 2, 5,
  6, 11, 12).
- Next: **Phase 7 — TV/Anime** (see `docs/ROADMAP.md`).

## Offline NuGet bootstrap

`nuget.config` prefers the local feed in `artifacts/nuget-feed/` (a flat folder of
`.nupkg` files bootstrapped via node in restricted-network environments) and falls back
to nuget.org. On machines with normal access you can ignore the local feed; to refresh it,
run `node artifacts/fetch-nuget.js` after adding packages to its `ROOTS` list.

