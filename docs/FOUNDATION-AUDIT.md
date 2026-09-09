# Platform Foundation Audit

Date: 2026-09-10

## Scope

Audit of the existing Core, Application, Infrastructure, API and test boundaries against `docs/ARCHITECTURE-SPEC.md` and `docs/PRODUCT-SPEC.md`.

## Findings

### Core

- Core already owns the catalogue-neutral domain entities: `Library`, `LibrarySource`, `MediaItem`, `MediaFile`, `Artwork`, `Tag`, and `Collection`.
- Core has no EF Core dependency and contains no catalogue-specific Music, Movies, Anime, Manga, or Books models.
- `MediaType` was not previously represented as a canonical platform contract. This is added by this milestone.

### Application

- Application contains use-case contracts and repository abstractions and does not depend on Infrastructure implementations.
- Scanning is already isolated behind `IFileEnumerator`, `IScanService`, and per-file `IIndexedMediaHandler` capabilities.
- `PagedResult<T>` already establishes a 1-based page convention with a bounded page size.
- Existing scan jobs provide progress, cancellation, terminal status and diagnostics, but their lifecycle is scan-specific and in-memory. A generic persisted job model remains a later M0 task.

### Infrastructure

- SQLite/EF Core persistence and filesystem scanning are kept below Application.
- Filesystem paths are passed through application-level safety validation before persistence/use.

### API

- API endpoints are defined separately from the frontend and the SPA fallback is registered after API mappings.
- Existing endpoints use explicit response DTOs rather than exposing EF entities as public contracts.
- A canonical API error envelope is still required; this remains an unchecked M0 task.

### Tests

- The repository already has separate Core, Application, Infrastructure, API and catalogue test projects.
- API tests use ASP.NET Core integration infrastructure. A reusable temporary SQLite + temporary media-folder fixture is still required for the M0 integration-test harness task.

## Decisions

1. Keep the existing domain entities as the canonical persisted media contracts rather than introducing parallel DTO-like domain objects.
2. Introduce `MediaType` as a catalogue-neutral enum and use it from detection/indexing contracts; catalogue plugins must not own this taxonomy.
3. Keep the current scan service as the implementation of the filesystem/indexing boundary while generic job persistence is added separately.
4. Do not mark an M0 task complete merely because a related partial implementation exists; each checkbox requires acceptance evidence.
