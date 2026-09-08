# Music Metadata Specification

Read embedded metadata first: title, artist, album, album artist, track/disc number, year/date, genre, composer, conductor, compilation, copyright, label, catalog number, MusicBrainz IDs, and ReplayGain.

Preserve original values where useful, but create normalized values for search/grouping. Do not silently rewrite user metadata. Prefer album artist for album grouping; track artist determines track attribution. Support multiple artists when available and handle compilations without creating unrelated artist pages.

Artwork priority: embedded, cached external provider, explicitly supported filesystem artwork, placeholder. Cache artwork locally.

Metadata editing validates fields, shows affected files, requires explicit confirmation for file writes, writes atomically where possible, updates the database only after successful writes, and preserves clear errors.

Providers implement search/get methods for artist, album, track, and artwork. Provider failures must not break local functionality. Enrichment is opt-in, configurable, cached, and rate-limited.
