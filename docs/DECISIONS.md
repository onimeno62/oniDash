# Architecture Decision Records

## ADR-001 — Local-first
The primary library index and core workflows are local and useful offline.

## ADR-002 — Modular catalogues
Music, Movies, TV/Anime, Manga, Books and future catalogues are modules/plugins. Shared infrastructure remains catalogue-agnostic.

## ADR-003 — React web UI
React/TypeScript is the primary UI for fast iteration and reuse across desktop/LAN clients.

## ADR-004 — .NET backend
C#/.NET provides strong Windows/filesystem/audio integration and mature tooling.

## ADR-005 — SQLite initially
SQLite provides a zero-configuration local database. Persistence remains behind application abstractions.

## ADR-006 — Shared visual language
Catalogue modules have specialized information architecture while using the shared oniDash design system.

## ADR-007 — Music player is application-scoped
Playback must not be owned by a React page. A long-lived PlayerService owns audio state, queue and playback lifecycle so navigation never interrupts playback.

## ADR-008 — Windows WASAPI for primary output
The Music playback pipeline should prefer Windows WASAPI for native output-device handling and low-latency desktop playback. Decoder/DSP/output remain capability-based.

## ADR-009 — Database-backed Music collections
Recently Added, Recently Played, Most Played, Top Rated, Favorites and similar collections are database queries, not frontend-derived arrays.

## ADR-010 — External enrichment is never local truth
MusicBrainz, AcoustID, lyrics and artwork providers return optional proposals with provenance/confidence. Applying external data is explicit and previewable.

## ADR-011 — Safe filesystem operations
Rename, move, organization and delete are backend operations with validation. Bulk organization uses a dry-run preview; no media mutation is silent.

## ADR-012 — Music Home comes last
The Music Home is a presentation layer over verified library/player capabilities. It must not be used to conceal incomplete backend functionality.
