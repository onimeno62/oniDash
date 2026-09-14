# oniDash UI / Visual Design Specification

## Shared direction
oniDash is a premium desktop media application: dark-first, cinematic, calm, technical and personal. Use artwork heavily where it aids discovery; use dense information layouts where management matters. Light theme remains supported.

Avoid generic SaaS dashboard aesthetics, excessive glassmorphism, giant empty cards, decorative gradients and controls that do not represent working behavior.

## Shared shell
```text
Sidebar | Header / global search
        | Main content
        | Persistent player when music is active
```

1440px+: full navigation + content. 1024–1439px: compact navigation. Below 1024px: responsive navigation and layouts. Keyboard focus, semantic controls, sufficient contrast and reduced-motion support are mandatory.

## Music information architecture
```text
MUSIC
├── Home
├── Library
│   ├── Songs
│   ├── Albums
│   ├── Artists
│   ├── Genres
│   └── Folders
├── Collections
│   ├── Favorites
│   ├── Top Rated
│   ├── Most Played
│   ├── Recently Played
│   ├── Recently Added
│   └── Never Played
├── Playlists
│   ├── My Playlists
│   ├── Smart Playlists
│   └── Create Playlist
└── Tools
    ├── Scan Library
    ├── Metadata Manager
    ├── File Organizer
    ├── Lyrics
    ├── Duplicates
    └── Statistics
```

The global player is not a navigation page. It remains available while navigating the library.

## Music Home
Home is discovery-oriented and data-driven. It contains:
- Continue Listening with artwork, title, artist, album, progress, duration and resume action.
- Recently Played carousel.
- Recently Added carousel.
- Most Played carousel.
- Favorites carousel.
- Top Rated carousel.
- Album, artist and genre discovery.
- Playlist discovery.
- Library totals and useful health signals.

Do not build Home as a mockup before the underlying queries and player operations exist.

## Songs
Use a dense virtualized table for serious management. Support configurable columns, sorting, filtering, multi-selection, keyboard navigation, context menus and bulk actions. Candidate columns include title, artist, album, album artist, track/disc, year, genre, duration, format, bitrate, sample rate, play count, date added, last played and rating.

## Albums / Artists / Genres / Folders
Albums emphasize artwork and track order. Artists expose albums, popular tracks and all songs. Genres are database-backed facets. Folders expose the actual filesystem hierarchy and provide play, queue, rename, move, delete and Explorer actions.

## Track context menu
Every track surface uses the same action menu: Play, Play Next, Add to Queue, Add to Playlist, View Album, View Artist, Edit Metadata, Identify, Find/View Lyrics, Rename, Move, Open Location, Favorite, Rating, Track Information, Remove from Library and Delete File. Actions must call real domain operations; no dead controls.

## Player UX
Three levels:
1. Mini player: artwork, title/artist, previous/play-next, progress and queue.
2. Expanded player: large artwork, metadata, waveform, lyrics/visualizer and controls.
3. Full Now Playing: immersive artwork/lyrics/visualizer/waveform with queue and track information.

Queue supports reorder, remove, clear, shuffle, play-next and save-as-playlist. Playback controls include previous/next, play/pause, seek, shuffle, repeat, volume and output device.

## Lyrics UX
Lyrics can be plain, synced or word-synced. Show the active synced line prominently, allow clicking a line to seek, and provide a focused editor for timestamps, offsets and save/remove operations.

## Metadata UX
Single-track and bulk editors use focused dialogs/panels. External identification shows current versus proposed metadata before applying changes. File organization always provides a dry-run preview for bulk changes.

## Visualizer UX
Provide real visualization modes such as spectrum, bars, circular spectrum, oscilloscope and waveform. Visualizers consume playback analysis data and never become a separate playback implementation.

## States
Every music screen must have deliberate loading, empty, error, retry, unavailable/missing-file and partial-data states. Destructive operations show confirmation and the outcome.

## Motion
120–220ms for routine UI changes; 220–400ms for larger transitions; respect `prefers-reduced-motion`.
