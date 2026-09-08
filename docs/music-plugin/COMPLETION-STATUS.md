# Music plugin completion status

## Completed in main

- Plugin specs and agent guidance
- Local scanning, TagLib metadata extraction, artwork, artists/albums/tracks
- Bounded catalogue APIs and global player
- Queue, next/previous, repeat, shuffle, persistence, and playback controls
- Favorites and listening history
- Manual and smart playlists with ordering
- Nested AND/OR/NOT smart-playlist rules with validation and bounded evaluation
- Metadata preview/editor with explicit confirmation and post-write refresh
- Staged metadata writes with original-file preservation on failed parsing/writes
- Real MP3 metadata round-trip, malformed/missing-file, and failed-write preservation coverage
- Statistics overview, genre breakdown, top-track API, and Music library health page
- Provider abstraction contract for opt-in external enrichment

## Remaining validation and product work

- Run full backend and frontend builds/tests on Windows and in CI.
- Validate locked/read-only Windows replacement behavior and crash interruption on real NTFS volumes.
- Add 50k-track virtualization and query benchmarks, then tune based on measurements.
- Complete an accessibility audit with keyboard/focus/screen-reader checks for Music, Playlists, Insights, and the player.
- Add concrete provider adapters only after credentials, cache, privacy, and rate-limit policy are approved.

## Release gate

Do not mark the Music plugin release-ready from source review alone. The Windows build, full test suite, isolated E2E run, performance benchmark, and accessibility audit must produce evidence first.
