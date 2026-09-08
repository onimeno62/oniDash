# oniDash Architecture

```text
┌──────────────────────────────────────┐
│ oniDash.Web — React / TypeScript     │
└──────────────────┬───────────────────┘
                   │ HTTP / JSON
┌──────────────────▼───────────────────┐
│ oniDash.Api — ASP.NET Core           │
└──────────────────┬───────────────────┘
┌──────────────────▼───────────────────┐
│ Application — use cases / queries    │
└──────────────────┬───────────────────┘
┌──────────────────▼───────────────────┐
│ Core — generic media domain          │
└──────────────────┬───────────────────┘
┌──────────────────▼───────────────────┐
│ Infrastructure — EF/SQLite/files     │
└──────────────────────────────────────┘
```

## Core owns
Library, LibrarySource, MediaItem, MediaFile, Artwork, Tag, Collection, Person, ExternalReference, Rating, Progress, History, plugin contracts.

## Core must not own
Artist, Album, Track, Movie-specific fields, Anime-specific fields, Manga chapters, ISBN-specific behavior.

## Plugin responsibilities
Domain models, persistence, file detectors, metadata adapters, application services, approved API/UI registration, optional migrations/jobs.

## Data ownership
Core owns core tables. Plugins own plugin tables. Plugins do not directly manipulate other plugins' tables.

## API
Expose application use cases and DTOs, not EF entities.

## Filesystem
Read-only by default. No automatic move/rename/delete in v1.

## Search
Start with SQLite FTS5 behind an abstraction.

## Background work
Scanning, artwork extraction, metadata enrichment, and indexing are background jobs with progress/cancellation.

## Offline
Browsing, search, cached artwork, and opening local files must work without Internet.
