# Catalogue spec audit

Audited on 2026-09-11 against the current main implementation.

## Movies

- Scanner alignment: `MovieIndexedMediaHandler` consumes canonical scan contexts, skips unchanged/non-video files, and isolates enrichment failures.
- Metadata and artwork: movie cataloguing uses filename parsing, local video probing, and embedded poster extraction with local-only fallback behavior.
- Watch-state persistence: movie progress and watched state are persisted with completion threshold handling, timestamps, and cancellation-aware database writes.
- Continue watching: `/api/movies/continue` returns unfinished items with saved progress and optional library filtering.

## Books

- EPUB/PDF metadata and cover extraction: the Books handler reads local EPUB/PDF metadata and identifies embedded EPUB cover candidates without network access or media mutation.

These are contract/spec synchronization items, not a claim that the full Movies, Books, or TV/Anime roadmaps are complete. Remaining gaps include series/episode modeling, reader integration, provider enrichment, and full verification in the repository toolchain.
