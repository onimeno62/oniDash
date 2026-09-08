# oniDash Roadmap

## Phase 0 — Repository/architecture
Solution, docs, CI/build, conventions, dependency rules.

## Phase 1 — Application shell
ASP.NET host, React app, design system, themes, shell, routing, settings shell, health endpoint.

Acceptance: app launches, navigation/theme work, frontend/backend communicate.

## Phase 2 — Core library
SQLite, EF migrations, Library, Source, MediaItem, MediaFile, Artwork, Tag, Collection, basic API/UI.

## Phase 3 — Filesystem scanner
Recursive scan, filtering, identity/dedup groundwork, background job, progress, cancellation, errors.

## Phase 4 — Search
Abstraction, SQLite FTS5, global search, filters, ranking, indexing jobs.

## Phase 5 — Music
Artist, Album, Track, embedded metadata, artwork, UI, basic playback.

## Phase 6 — Movies
Movie, video detection, metadata, artwork, playback, watch progress.

## Phase 7 — TV/Anime
Series, seasons, episodes, anime metadata, watch progress.

## Phase 8 — Manga
Series, volumes, chapters, CBZ/PDF groundwork, reader, progress.

## Phase 9 — Books
Book, author, series, EPUB/PDF, reader, progress.

## Phase 10 — Enrichment
Metadata/artwork providers, duplicate detection, smart collections, advanced search.

## Phase 11 — Windows
Desktop wrapper, installer, associations, media keys, notifications, startup/tray.

## Phase 12 — Network
LAN, authentication, responsive mobile mode, multiple clients.

Do not start a later phase while the current phase has failing acceptance criteria.
