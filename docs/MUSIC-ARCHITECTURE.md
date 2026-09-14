# oniDash Music Architecture

## Boundary
Music is a catalogue module. Core provides generic media/file/library/job contracts; Music owns music-specific cataloging, queries, playback, metadata, lyrics, playlists and statistics.

## Services
- `MusicLibraryService`: sources, catalogue lifecycle and library operations.
- `MusicQueryService`: songs/albums/artists/genres/folders/collections/search with paging/filtering/sorting.
- `MusicScannerHandler`: turns indexed audio files into Music catalogue updates.
- `MusicFileService`: safe rename/move/delete/open-location and organization.
- `MusicMetadataService`: tag read/write, normalization and enrichment application.
- `MusicArtworkService`: extraction, caching and provider enrichment.
- `MusicLyricsService`: embedded/local/provider lyrics and persistence.
- `MusicPlaylistService`: normal and smart playlists.
- `MusicStatisticsService`: play history and derived insights.
- `MusicPlaybackService`: application-scoped player state and commands.

## Player
The player is not a React component. It is a long-lived application service/store that owns current track, queue, history, position, duration, volume, shuffle, repeat, output device and errors. UI components subscribe to state and issue commands.

Commands must be serialized to avoid races during rapid play/seek/next operations. Navigating away from Music must never stop playback unless the user explicitly stops it.

## Audio pipeline
```text
Track file
  → decoder
  → DSP / ReplayGain / volume
  → analyzer (FFT / waveform)
  → WASAPI output
```

The analyzer consumes samples from the active pipeline. It must not create a second independent playback path.

## Library queries
All large-library operations are server-side. Endpoints return typed DTOs with pagination metadata. React must not fetch all tracks to calculate Recently Played, Most Played, Top Rated or similar collections.

## Scanner integration
Use the platform scanner's indexed-file hook. Music claims only supported audio files, reads tags/technical data, and upserts by stable media/file identity. Rescans must not reset ratings, favorites, play history, playlists or playback state.

## Filesystem reconciliation
When a file path changes, first attempt identity reconciliation. When a file disappears, mark it missing instead of deleting catalogue state. When it returns, reconnect it. Rename/move operations initiated by oniDash update the catalogue only after filesystem success.

## Metadata provenance
Track local file metadata, normalized catalogue values and external proposals separately. A provider response contains source, timestamp and confidence. Applying it is an explicit command and should support a before/after preview.

## Lyrics model
A lyrics record contains track identity, kind (plain/synced/word-synced), source, timestamps/content, local-vs-provider provenance and update time. Embedded/local content is preferred before provider lookup unless the user explicitly searches.

## Provider adapters
Initial adapters:
- MusicBrainz for metadata identification.
- AcoustID for acoustic fingerprint lookup.
- LRCLIB for plain/synchronized lyrics.
- Artwork providers behind a generic artwork contract.

All providers are optional, cancellable and rate-limit aware.

## Events
Useful domain/application events include `TrackAdded`, `TrackUpdated`, `TrackMoved`, `TrackRenamed`, `TrackMissing`, `LyricsUpdated`, `ArtworkUpdated`, `PlaybackStarted`, `PlaybackCompleted`, `FavoriteChanged`, `RatingChanged`, `PlaylistChanged`, `ScanProgress` and `ScanCompleted`.

## Testing strategy
- Unit tests for parsing, normalization, query rules, player state transitions and filename templates.
- Integration tests using temporary SQLite and temporary media folders for scanning and file operations.
- Audio/player tests around command/state transitions and stream/device failures.
- API contract tests for status codes, pagination and errors.
- Frontend tests for user workflows and state rendering.
- End-to-end tests using generated/tagged audio fixtures and real filesystem operations in temporary directories.

## Safety invariants
1. Never silently mutate user files.
2. Never trust a provider response as local truth.
3. Never expose filesystem paths without authorization/validation.
4. Never let one bad media file abort a complete scan.
5. Never let a Music UI page own the player lifecycle.
6. Never make a large client-side collection the source of truth.
