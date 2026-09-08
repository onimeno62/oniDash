# Milestone 02 — Core library

## Objective
Persist real data: SQLite via EF Core migrations, generic library entities, a management API surface, and a functional library UI. No catalogue-specific logic (Architecture rule 1) and no file scanning yet.

## Deliverables
Backend: Core entities (Library, LibrarySource, MediaItem, MediaFile, Artwork, Tag, Collection + join entities), Application use cases with repository abstractions and ProblemDetail-mapped exceptions, Infrastructure EF model (NOCASE name columns, DateTimeOffset-to-binary converter, cascade FKs, unique indexes), `InitialCoreLibrary` migration, endpoints for libraries/sources/items/tags/collections, `IFileSystemProbe` existence checks.

Frontend: typed libraries API client with RFC 7807 error surfacing, `useLibraries`/`useLibrarySources` hooks, functional Library page (create, rename with inline edit, delete with confirmation, expandable folder sources with add/remove and API-driven validation errors), ProblemDetails-aware `apiFetch`/`apiDelete`.

## Acceptance criteria
1. `dotnet ef migrations` script creates all tables with FK cascades and unique indexes.
2. Libraries can be created, renamed, listed, and deleted through the API and the UI.
3. Sources validate that the folder exists (read-only; nothing is scanned or modified).
4. Duplicate library/tag/collection names and duplicate source paths are rejected (409) — case-insensitively, at service and database level.
5. Tag assignment and collection membership endpoints enforce same-library rules.
6. Unknown resources return 404 problem details; invalid input returns 400 validation problems.
7. Deleting a library cascades to its sources, items, tags, and collections — user files are never touched.
8. Tests pass: Application unit (fakes), Infrastructure integration (real SQLite), API integration (WebApplicationFactory).
9. The library UI works end-to-end against the running API (verified by automated browser checks at 1280×800, 1920×1080, 2560×1440).
10. Core still contains no catalogue-specific logic.

## Not in this milestone
File scanning (Phase 3), search (Phase 4), catalogue plugins (Music, Movies, Anime, Manga, Books), external metadata, playback.
