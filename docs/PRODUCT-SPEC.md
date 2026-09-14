# oniDash Product Specification

**Status:** Canonical product contract
**Target:** Windows-first, local-first personal media library

## Product vision
oniDash is one premium personal media environment for Music, Movies, TV/Anime, Manga, Books and future media types. Each catalogue has specialized UX, while library infrastructure, search, jobs, artwork, health and design remain shared.

## Product principles
- Local-first: core catalogue and playback/management workflows work without Internet access.
- Non-destructive: user media is never silently changed.
- Observable: scans, failures, missing files and provider failures are visible.
- Recoverable: interrupted work can be retried safely.
- Contract-first: APIs and domain behavior are explicit and tested.
- Modular: media-specific logic stays in catalogue modules/plugins.
- Complete vertical slices: a visible control is not complete until its real operation works and is tested.

## Music product contract
Music must function as a complete local music library manager and player. It must support:

### Library
- Add/remove multiple music source folders.
- Full and incremental scans.
- Filesystem watching/reconciliation.
- Missing/moved/changed file detection.
- Songs, folders, albums, artists and genres.
- Recently added, recently played, most played, never played and top-rated collections.
- Favorites, ratings, playlists and smart playlists.
- Server-side search, filtering, sorting and pagination.

### File management
- Rename files safely.
- Move files safely.
- Delete files only after explicit confirmation.
- Open file location in Windows Explorer.
- Bulk operations.
- Duplicate detection.
- Template-driven organization with dry-run preview.

### Metadata and artwork
- Read and write embedded audio tags.
- Single-track and bulk metadata editing.
- Metadata normalization.
- MusicBrainz and AcoustID identification as optional enrichment.
- Embedded and local artwork extraction.
- Artwork provider enrichment with provenance.
- Preview before applying external metadata.

### Playback
- Persistent application-wide playback independent of page lifecycle.
- Play/pause, seek, previous/next, queue, play-next, shuffle and repeat.
- Volume and output-device selection.
- Playback history and resume position.
- Gapless playback and crossfade where technically supported.
- ReplayGain/loudness support where available.
- Windows media-key integration through a platform boundary.

### Lyrics
- Embedded lyrics.
- Local `.lrc` and `.txt` lyrics.
- Provider search and download.
- Plain and synchronized lyrics.
- Lyrics editing and timestamp adjustment.
- Click lyric line to seek.
- Save lyrics explicitly and preserve source/provenance.

### Visual/audio analysis
- Waveform seeking.
- Spectrum/FFT data.
- Spectrum, bars, oscilloscope and other visualizer modes.
- Visualizer data comes from the playback analysis pipeline, not a second decoder.

## Music Home
The Music Home is a presentation surface over working library/player services. It must not be implemented before those services work. It includes Continue Listening, Recently Played, Recently Added, Most Played, Favorites, Top Rated, album/artist/genre discovery and playlists.

## Non-goals
- Streaming subscriptions as a core requirement.
- Mandatory cloud accounts.
- Mandatory external metadata/lyrics providers.
- Silent metadata replacement.
- Silent filesystem mutation.
- Building the Music Home as a mockup while core music operations remain broken.

## Definition of done
A feature is complete only when its domain operation works end-to-end, API/UI contracts align, relevant tests exist, loading/empty/error states are handled, user data safety is preserved, documentation is current, and build/type/test verification has been run where tooling is available.
