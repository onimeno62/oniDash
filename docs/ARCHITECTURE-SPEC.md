# oniDash Architecture Specification

## Platform target
```text
React / TypeScript UI
        ↓ HTTP / JSON
ASP.NET Core API
        ↓
Application / use cases / DTOs / contracts
        ↓
Core domain
        ↓
Infrastructure
   ├── SQLite / EF Core
   ├── filesystem
   └── Windows services

Catalogue plugins → Core/Application contracts
Core/Application → never depend on plugin implementations
```

## Shared library pipeline
```text
Library Source
 → File Discovery
 → File Identity / Reconciliation
 → Media Type Detection
 → Handler Metadata Extraction
 → Normalization
 → Media File / Item Persistence
 → Artwork Extraction / Cache
 → Search Index
 → Catalogue-specific enrichment
```
Every stage must be independently testable, retryable and observable.

## Music architecture
Music is a catalogue module. It owns music-specific domain models, queries, metadata behavior and music UI contracts without putting music concepts into Core.

```text
Music UI
  ├── library views / search / editors
  ├── player views
  └── lyrics / visualizers
          ↓
Typed Music API
          ↓
Music Application Services
  ├── Library / Query Service
  ├── Scan Integration
  ├── File Management
  ├── Metadata / Artwork
  ├── Lyrics
  ├── Playlists / Collections
  └── Statistics
          ↓
Music Domain + Persistence

Global Player Store
          ↓
Music Playback Service
          ↓
Audio Pipeline
  ├── Decoder
  ├── DSP / ReplayGain / volume
  ├── Analyzer / FFT / waveform
  └── WASAPI output
```

The player is application-scoped and must not depend on a mounted Music page. React components observe player state; they do not own the audio engine.

## Scanner
- Recursive source enumeration with configurable roots/exclusions.
- Reparse-point/path traversal protection.
- Stable file identity and idempotent upsert.
- Changed, moved and missing file reconciliation.
- Background jobs with progress, cancellation, retry and per-file diagnostics.
- Catalogue handlers claim files by capability/extension/signature.
- A failed handler/file must not abort the complete scan.

## Music data boundaries
Separate:
1. **File/local truth** — tags and filesystem facts.
2. **Normalized catalogue state** — fields used for application behavior.
3. **External enrichment** — MusicBrainz, AcoustID, artwork and lyrics suggestions with provider/provenance/confidence.

External enrichment never implicitly overwrites local truth.

## File operations
All rename/move/delete/organization operations go through a backend service that validates paths, detects collisions, records intended changes, executes safely, verifies the result, updates catalogue references and emits an operation result/event. Bulk organization requires a preview/dry run and explicit confirmation.

## Queries
Library views are database-backed. Sorting, filtering, grouping, search and pagination are performed server-side. The browser must not load the entire library into memory to render a page.

## Player state
Player state includes current track, queue, history, position, duration, volume, mute, shuffle, repeat, output device and error/loading state. Playback commands are centralized and serialized to avoid races during rapid track changes/seeks.

## Audio pipeline
Prefer native Windows audio output through WASAPI. Decoder, DSP and analyzer are separate stages. The visualizer consumes analysis data from the active playback pipeline rather than decoding a second copy of the file. Gapless/crossfade/ReplayGain are capabilities and must degrade cleanly when unsupported.

## Lyrics
Lyrics are assets with type (plain/synced/word-synced), source/provenance and optional local/embedded representation. Provider lookup is optional and cancellable. Saving lyrics is an explicit operation.

## Providers
Use interfaces such as `IMetadataProvider`, `IArtworkProvider`, `ILyricsProvider`, `IAudioFingerprintProvider`, `IMediaHandler` and `ICataloguePlugin`. Providers are optional, rate-limit aware, cancellable and diagnosable.

## API rules
- Explicit DTO contracts; never expose EF entities.
- Correct HTTP status codes and structured errors.
- No API route may fall through to SPA HTML.
- Every endpoint has success and failure-path tests.
- Long-running work returns observable job state.

## Persistence
SQLite + EF Core. Use safe migrations, preserve existing user data, and test risky migrations. Music database tables may reference shared media identities only through stable contracts; Core must not depend on Music.

## Security and safety
Validate every filesystem path, prevent path traversal, constrain provider downloads, treat external metadata/content as untrusted input, and never execute arbitrary commands supplied by media metadata.
