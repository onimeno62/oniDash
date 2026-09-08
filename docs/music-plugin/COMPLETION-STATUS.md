# Music plugin completion status

## Completed in main

- Plugin specs and agent guidance
- Local scanning, TagLib metadata extraction, artwork, artists/albums/tracks
- Bounded catalogue APIs and global player
- Queue, next/previous, repeat, shuffle, persistence, and playback controls
- Favorites and listening history
- Manual and smart playlists with ordering
- Metadata preview/editor with explicit confirmation and post-write refresh
- Real MP3 metadata round-trip and malformed/missing-file coverage
- Statistics overview, genre breakdown, top-track API, and Music library health page
- Provider abstraction contract for opt-in external enrichment

## Remaining validation and hardening

- Run full backend and frontend builds/tests on Windows and in CI.
- Add locked/read-only Windows file tests and crash-interruption atomic replacement validation.
- Expand smart playlists to full AND/OR/NOT rule trees and add query evaluator tests.
- Add virtualization benchmarks for 50k-track libraries and accessibility audit.
- Implement provider adapters only after rate-limit/cache policy is approved.
