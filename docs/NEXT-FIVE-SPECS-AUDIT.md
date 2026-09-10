# Next five spec audit

Audited against `main` on 2026-09-11.

## 1. TV / Anime series domain
Added the first platform-neutral `ISeriesCatalogue` contract and a read-only `/api/catalogue/series` boundary. Persistence and episode indexing remain future work.

## 2. Manga plugin architecture
Added `IMediaPlugin` and `IMangaSource` capability contracts. Runtime installation and execution remain future work.

## 3. Books author / series model
Added typed `BookAuthorSummary` and `BookSeriesSummary` contracts. Books persistence and relationship endpoints remain future work.

## 4. Books reader integration
Added the cancellable `IBookReader` boundary. No reader implementation is registered yet, so the UI must treat it as unsupported.

## 5. Unified dashboard
Added `/unified`, a loading/error/empty-state dashboard backed by the series contract. Continue media/activity/favorites aggregation remains future work.
