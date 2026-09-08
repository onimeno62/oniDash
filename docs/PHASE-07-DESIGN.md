# Phase 7 design guardrail: TV and Anime

This design must be approved before schema work starts. The first slice stays offline-first and additive.

## Ownership
A video file is claimed by exactly one catalogue handler. Movies keeps its claim when a filename is confidently movie-shaped. TV/Anime claims files only when it can identify a series plus episode number. Ambiguous files remain in Core and are surfaced for a user override; they must not silently duplicate into both catalogues.

## Identity
Episode identity is `(library_id, normalized_series_key, season_number, episode_number, file_identity_key)`. Re-scans update the same row by scanner file identity and preserve progress, tags, and collections. A renamed or moved file is a new scan identity until a later duplicate/identity feature explicitly links it.

## Naming rules
Support common forms such as `Show S01E02`, `Show 1x02`, and absolute episode forms only when the series key is unambiguous. Parse specials as season 0. Multi-episode files store an ordered episode range and one backing file. Unknown season/episode values remain unclassified rather than guessed.

## Playback
Reuse the safe file locator and range streaming contract. The first slice supports browser-compatible local files only, resume progress, watched state, and next-episode navigation. No transcoding, online metadata, subtitle selection, or automatic file organization.

## Migration and regression rules
TV/Anime owns its tables and migration history. Core and Movies schemas are not rewritten. Existing Music and Movies E2E fixtures must remain green. Every episode upsert, parser rule, progress transition, and mixed movie/TV/anime classification case needs unit or integration coverage.

## Acceptance gate
Before implementation, add mixed fixtures covering movie, series episode, special, multi-episode file, ambiguous filename, and re-scan. Expected ownership must be deterministic. The design is not complete until these cases and user override behavior are agreed.
