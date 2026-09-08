# oniDash Music Plugin — AI Agent Instructions

## Mission
Build a production-quality, modern, local-first music library dashboard for oniDash.

The Music plugin supports artists, albums, tracks, genres, playlists, favorites, listening history, metadata, artwork, folders, scanning, search, statistics, audio playback, queue management, smart playlists, and library health.

## Principles
- Local-first: local files are the source of truth.
- Non-destructive: scanning, indexing, and artwork extraction are read-only unless explicitly requested.
- Fast: support 10,000–100,000+ tracks with pagination, virtualization, indexes, and incremental queries.
- Beautiful: cinematic, image-driven, polished desktop UI.
- Accessible: keyboard navigation, semantic controls, and clear focus states.

## Architecture
Keep Presentation → Application → Domain → Infrastructure separate. Do not put database or filesystem operations in UI components. Do not couple the audio engine to UI components. External metadata providers are optional enrichment services.

Separate server/application state, player state, and UI state. Use stable IDs, transactions for multi-record operations, appropriate indexes, and avoid N+1 queries.

## Filesystem and providers
Normalize Windows paths and handle renames, moves, deletions, inaccessible folders, locked files, permission errors, and malformed media files. One bad file must not abort a scan. External providers are enrichment only, cached and rate-limited; core functionality must work offline.

## Implementation order
1. plugin shell
2. domain models
3. database schema
4. library sources
5. scanner
6. metadata extraction
7. artwork
8. library queries
9. artist/album/track UI
10. search
11. player
12. queue
13. playlists
14. favorites/history
15. statistics
16. metadata enrichment
17. smart playlists
18. library health
19. polish/performance

## Definition of done
Every feature needs loading, empty, and error states, appropriate tests, accessibility, performance validation, and UI consistency.
