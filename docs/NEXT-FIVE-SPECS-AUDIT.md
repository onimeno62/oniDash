# Next five spec audit

Audited against `main` on 2026-09-11.

## 1. TV / Anime series domain
Not implemented. Movies has watch progress, but there are no canonical Series, Season, or Episode persistence contracts. Do not mark complete.

## 2. Manga plugin architecture
The frontend exposes source and extension concepts, but the backend implementation is not a real plugin runtime. Do not mark complete until the source boundary, installation lifecycle, and capability contracts are real.

## 3. Books author / series model
The Books metadata handler currently emits author and series values as normalized metadata fields. A typed persistence model and relationship queries are still missing. Do not mark complete.

## 4. Books reader integration
The dashboard supports progress controls, but there is no EPUB/PDF reader boundary or streaming contract. Do not mark complete.

## 5. Unified dashboard
The current dashboard is still a foundation welcome screen. Continue listening/watching/reading, recently added, activity, favorites, and local-only recommendations are not implemented as a unified data surface.

The next implementation should start with the TV/Anime canonical domain or the Books typed persistence model, not another cosmetic dashboard pass.
