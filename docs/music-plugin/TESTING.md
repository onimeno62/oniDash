# Music Plugin Testing Strategy

## Unit
Test metadata normalization, artist/album grouping, playlist rules, smart-playlist evaluation, queue transitions, repeat/shuffle, history thresholds, search ranking, and scanner change classification.

## Integration
Test fixture-library scanning, persistence, metadata parsing, artwork caching, playlist CRUD, search indexing, and playback service integration.

## UI
Test navigation, album/artist opening, track playback, queue operations, playlist creation, favorites, search, and loading/empty/error states.

## Fixtures
Include MP3, FLAC, M4A, OGG/Opus, WAV, multi-disc albums, compilations, missing metadata, embedded artwork, and malformed files.

## Performance
Benchmark initial and incremental scans, search latency, album-grid rendering, 50k-track lists, and statistics queries. The UI must not freeze during background operations.
