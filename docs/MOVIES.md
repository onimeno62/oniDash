# oniDash Movies

The Movies plugin is a local-first cinema dashboard for Windows. It treats the filesystem index as the source of truth for media files while keeping playback state in the oniDash catalogue.

## Dashboard

- Hero cinema header with library selector and reindex action.
- Collection KPIs: total movies, watched, unwatched, and total runtime.
- Continue Watching rail with resume and details actions.
- All Movies, Unwatched, and Watched views.
- Instant title/year filtering and sorting by title, newest, runtime, or recently watched.
- Responsive poster grid with lazy artwork loading and hover playback affordances.

## Movie details

- Poster, title, release year, runtime, container, watch state, and resume percentage.
- One-click play and mark watched/unwatched.
- Detail drawer designed as the home for future rich metadata fields and management actions.

## Playback

- Native browser video playback with HTTP range streaming.
- Resume from saved position.
- Progress saved while pausing and closing the player.
- Automatic watched state when playback finishes.
- Full-surface dark player with codec/container failure feedback.

## Library operations

- Reindex the current library from the dashboard.
- Watch progress survives rescans because it belongs to the movie catalogue entity.
- Missing or unsupported browser codecs remain indexed rather than being silently removed.

## Planned enrichment

The plugin boundary is intentionally ready for richer movie metadata without coupling the core catalogue to a provider: genres, synopsis, cast, crew, studios, countries, certifications, external IDs, alternate titles, collections/franchises, trailers, subtitles, audio tracks, artwork management, duplicate detection, rename/organize templates, favorites, ratings, comments, and smart collections.

## Safety

Filesystem operations should always be explicit and previewable. Metadata enrichment should be non-destructive by default. Deleting a catalogue record must remain distinct from deleting the underlying file.
