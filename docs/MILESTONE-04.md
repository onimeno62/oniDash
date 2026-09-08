# Milestone 04 — Search (Phase 4)

Scope from `docs/ROADMAP.md`: abstraction, SQLite FTS5, global search, filters,
ranking, indexing jobs.

## What was built

### Abstraction (Application)
- `ISearchService` + `SearchResult` DTO (`Application/Search/`): search all indexed
  media items (all whitespace terms must match, per-term prefix matching, optional
  single-library filter, most-relevant-first) and a `ReindexAsync` recovery command.
  Core stays catalogue-free — results are generic media items (rule 1); no
  catalogue-specific fields (rule 9).

### SQLite FTS5 (Infrastructure)
- `FtsSearchService`: queries the `MediaItemsFts` virtual table with `MATCH` and ranks
  with `bm25()` (lower-is-better; deterministic display-name tie-break). User input is
  compiled into a safe expression — each term double-quoted with embedded quotes
  doubled and suffixed with `*` for prefix search — so FTS5 syntax (`OR`, `NOT`,
  parentheses) in user text is neutralized, parameterized through `@match`.
- **Live indexing** (the roadmap's "indexing jobs"): the migration creates
  `AFTER INSERT/UPDATE/DELETE` triggers on `MediaItems` maintaining the FTS table, so
  scans, renames, and deletes — including bulk `ExecuteDeleteAsync` and FK cascades —
  are searchable immediately without a separate indexing pass. `POST /api/search/reindex`
  rebuilds from persisted items as a recovery/consistency command.
- Migration `SearchIndexFts`: creates the FTS5 table (DisplayName indexed; ItemId/
  LibraryId unindexed payload), the three triggers, and one backfill of existing items.
  Insert trigger is idempotent per ItemId (the backfill and triggers coexist safely).
  Additive only (rule 12).

### Integrity fix (found by search tests)
- Microsoft.Data.Sqlite defaults **foreign keys off**; cascade deletes declared in the
  model were not actually enforced. The production connection string now sets
  `Foreign Keys=True` (test connection strings match). With FKs on, cascaded deletes
  fire the FTS triggers correctly.

### API (`Api/Endpoints/SearchEndpoints.cs`)
- `GET /api/search?q=&libraryId=&limit=` → ranked `SearchResult[]` (itemId, displayName,
  libraryId, libraryName); blank `q` → `[]`; limit clamped 1–200 (default 50).
- `POST /api/search/reindex` → `{ indexedItems }`.

### UI (`SearchPage` + `useSearch`)
- Live search-as-you-type (250 ms debounce, superseded requests aborted, stale
  responses dropped by sequence check). Results ranked with per-item library badges
  and a result count; library filter dropdown (All libraries / each library); loading,
  no-results, and retryable error states; shell TopBar search routes here with `?q=`.
- Frontend never touches SQLite/filesystem directly (rule 3) — everything via the API.

## Tests
- Backend **122/122** (+18, 0 warnings): Infrastructure 34 (+12 FTS: case-insensitive
  word search, multi-term AND, prefix, library filter, bm25 term-frequency ranking,
  blank/unknown queries, FTS-syntax injection safety, trigger sync on rename/delete,
  cascade keeps index consistent, reindex, limit cap) and Api 31 (+6: ranked results
  with library names, blank query, library filter via query string, limit, live index
  without manual reindex, reindex count).
- Frontend **31/31** (+5, typecheck clean, build OK): pre-query hint, live ranked
  results with library badges, no-results state, library filter request params,
  retryable error state.

## Verification
- E2E `artifacts/visual/verify-m04.js`: **12/12** against the live app — empty state,
  scan-fed live results, deterministic relevance order, multi-term AND narrowing,
  library filter, no-results, no overflow at 1280/1920/2560 widths, reindex count,
  and a second scan's files searchable immediately (live index, no reindex).
- Regressions: M01, M02, M03 scripts all pass. Visual-check libraries are cleaned
  before and after runs (scripts idempotent).
- API v0.4.0 health `Healthy db=Ok`; `SearchIndexFts` applied to the local database on
  startup, additive with data intact.

## Known limitations
- Search covers media item display names only — file paths, tags, and collection names
  are not indexed yet (tag/collection search arrives with catalogue phases that fill
  those tables).
- No snippet/highlighting and no pagination — results are capped (default 50, max 200)
  and ranked; a single-user local index makes paging unnecessary for now.
- Ranking is bm25 over the FTS index; no recency or library-weight boosts (deferred
  until a real catalogue gives those meaning).
- `ReindexAsync` is exposed as an API command but has no UI button (recovery path;
  triggers keep the index live in normal operation).
