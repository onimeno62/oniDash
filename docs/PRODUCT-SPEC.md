# oniDash Product Specification

**Status:** Canonical product contract
**Audience:** all human and AI agents
**Target:** Windows-first, local-first personal media library

## 1. Product vision
oniDash is a single premium personal media environment for Music, Movies, TV/Anime, Manga, Books and future media types. It combines one local catalogue, one search system, one design language and purpose-built experiences per media type.

## 2. Non-goals
- Do not become a generic file manager.
- Do not require cloud services for core library operation.
- Do not bundle copyrighted media or hard-code piracy sources.
- Do not make external metadata providers mandatory.
- Do not silently mutate, move, rename or delete user media.

## 3. Core user journeys
1. Add one or more local folders as library sources.
2. Scan/index media safely in the background.
3. See accurate library health and scan progress.
4. Browse each media type using a specialized dashboard.
5. Search the entire collection from one global search.
6. Enrich metadata/artwork when desired while retaining local truth.
7. Play/watch/read media and persist progress.
8. Recover from disconnected drives, moved files and failed metadata providers without losing catalogue data.

## 4. Product principles
- Local-first: catalogue and core workflows work offline.
- Non-destructive: never alter user media without an explicit user action.
- Observable: jobs, errors, missing files and provider failures are visible.
- Recoverable: scans and enrichment can be retried safely.
- Modular: media-specific logic belongs in plugins/modules.
- Consistent: shared shell and design system, specialized media UX.
- Contract-first: API DTOs and behavior are explicit and tested.
- Incremental: complete vertical slices before expanding scope.

## 5. Media capabilities
### Music
Artists, albums, tracks, genres, playlists, favourites, history, insights, metadata editing, artwork, playback, queue and lyrics.

### Movies
Poster library, metadata, watch state, continue watching, resume playback, artwork and video streaming.

### TV/Anime
Series, seasons, episodes, watch progress, continue watching, metadata and artwork. Anime-specific provider integration is optional enrichment.

### Manga
Series, chapters, categories, favourites, reading progress, reader, downloads/offline state, updates and optional Mihon/Suwayomi-compatible provider integration.

### Books
Books, authors, series, formats, covers, reading progress, ratings, favourites and EPUB/PDF reader integration.

## 6. Platform capabilities
The platform must eventually provide:
- Library/source management
- Unified scanner/indexer
- Metadata normalization
- Artwork management/cache
- Global search
- Background jobs
- Library health/diagnostics
- Settings
- Backup/restore of catalogue state
- Provider/plugin management
- Windows integration

## 7. Definition of done
A feature is not complete until its behavior is implemented, API/UI contracts are aligned, relevant tests exist, error/loading/empty states are handled, documentation is updated, and build/type/test verification has been run where tooling is available.
