# Milestone 03 — Filesystem scanner (Phase 3)

Scope from `docs/ROADMAP.md`: recursive scan, filtering, identity/dedup groundwork,
background job, progress, cancellation, errors.

## What was built

### Read-only enumeration (Infrastructure)
- `FileEnumerator` (`Infrastructure/Scanning/`): manual stack walk from the source root.
  Skips hidden and system entries by attribute, well-known junk directories
  (`$recycle.bin`, `system volume information`, `windows`, `program files`,
  `node_modules`, `.git`, …) plus per-options excludes, and never follows reparse points
  (junctions/symlinks). Unreadable subtrees are skipped with the scan continuing. A
  vanished root throws `DirectoryNotFoundException` so the scan reports an error instead
  of silently marking everything missing. Strictly read-only (AGENTS.md rule 11): it
  never creates, renames, moves, or deletes anything.
- Discovered timestamps are quantized to whole milliseconds because SQLite stores
  `DateTimeOffset` coarser than NTFS; without quantization every re-scan would report
  every file as changed.

### Scan use case (Application)
- `ScanService`: three phases — **discover** (enumerate fully before any write, so a
  cancelled/failed pass leaves the index untouched), **index** (upsert), **reconcile**
  (mark missing). A successful pass stamps `Source.LastScannedAtUtc`.
- Identity/dedup groundwork: each `MediaFile` row carries an `IdentityKey`
  (`<sourceId N-form>/<lowercased relative path>`), unique per database (index
  `IX_Files_IdentityKey`). Re-scans match on identity, not row id: identical files are
  unchanged, changed size/timestamp rows are updated in place (keeping their media item,
  and therefore any future user tags/collections), and files that disappeared are
  marked `MissingSinceUtc` — never deleted.
- `PlaceholderMediaItemResolver` (`IMediaItemResolver` capability): one placeholder
  media item per new file, named after the file stem. Catalogue plugins re-group these
  in Phase 5+ (rule 1: Core stays catalogue-free; rule 6: capability abstraction).
- `ScanJobManager` (`IScanJobManager`): singleton background job runner. One scan per
  source at a time (`StartScanResult` rejection otherwise), thread-safe immutable
  snapshots, cancellation by scan id, per-run DI scope resolution. Job history is
  in-memory (single-user local app; restart clears it, index persists).

### API (`Api/Endpoints/ScanEndpoints.cs`)
- `POST /api/sources/{sourceId}/scans` → 202 Accepted + `ScanProgress` snapshot;
  409 problem+json (with `scanId` extension) when a scan is already running for that
  source; 404 unknown source.
- `GET /api/scans/{scanId}` → snapshot or 404.
- `POST /api/scans/{scanId}/cancel` → `{ cancelled }` (false for unknown/finished).
- `GET /api/scans?sourceId=&limit=` → recent runs.

### Persistence
- Migration `ScannerGroundwork`: `Files.IdentityKey` (required, max 1100, unique index),
  `Files.MissingSinceUtc`, `Sources.LastScannedAtUtc`. Additive only; existing rows get
  their `IdentityKey` backfilled by SQL in the migration (rule 12).
- `MediaFileRepository`: add/update (detached re-attach), bulk `MarkMissingAsync` via
  `ExecuteUpdateAsync`; scans never delete rows.
- `MediaItemRepository.AddPlaceholderAsync`.

### UI (`LibraryPage`)
- Per-source **Scan** button; while a scan runs all scan buttons disable (409s cannot
  happen through the UI, and are surfaced if they do).
- Live progress card: phase (Discovering/Indexing), processed/total counter, progress
  bar, Cancel button.
- Terminal summaries: completed counts (`N new, N updated, N unchanged, N missing`),
  cancelled, or failure message; transient start errors too.
- **Media** panel in the expanded library: paged item list with total count and an
  empty state; refetches automatically when a scan completes.
- `useScan` hook owns start/poll(500ms)/cancel with cleanup on unmount.

## Tests (backend 104 total, all green; 0 warnings)
- Core 5 (unchanged), Application 52 (+15: indexing/upsert/unchanged/missing/reappear/
  case-insensitive identity/cancellation-writes-nothing/unknown-source/root-path passed
  to enumerator; manager: completion counts, running rejection, cancel transition,
  unknown-id, history order, failure reporting), Infrastructure 22 (+12: enumerator
  recursion/hidden/junk/extension/empty/missing-root/utc/quantization; repository
  add/update/mark-missing/unique-identity), Api 25 (+10: 202+location, completed counts,
  409 with scanId, cancel path, unknown source/scan 404, missing marking, unchanged
  rescan, changed detection, scan history).

## Frontend tests (28 total, all green)
- Library page: 12 (+5: scan start→progress→completed summary, cancel flow, failure
  message, 409 message, indexed items list; items fetch mocked for all existing tests).

## Verification
- E2E `artifacts/visual/verify-m03.js`: 10/10 browser checks against the real API —
  scan control, live progress card, completed summary matching backend counts (41 files
  incl. a nested `covers/` folder), media list + count, disabled scan buttons while
  running, API 409 with running scan id, UI cancel → cancelled state, scan history.
- Regression: `verify-m02.js` 14/14, `verify.js` (M01) all checks pass.
- API v0.3.0 health: `Healthy`, DB `Ok`; migration applied to the local database on
  startup with existing data intact.

## Known limitations
- Scan progress/history is in-memory: restarting the API loses running-job state
  (persisted index is untouched; a re-scan re-converges it).
- One scan at a time per source (by design); concurrent scans of different sources are
  allowed.
- Files moved *within* a source appear as missing + new (new placeholder item) until
  catalogue plugins adopt identity heuristics (Phase 5+).
- One placeholder media item per file; grouping into albums/movies is catalogue-plugin
  territory (Phase 5+), explicitly not done here (rule 9).
- Extension filtering defaults to "index everything except hidden/system/junk"; a
  per-source extension allowlist is deferred until a real catalogue needs it.
