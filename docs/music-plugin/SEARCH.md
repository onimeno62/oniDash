# Music Search Specification

Search targets: tracks, albums, artists, genres, and playlists.

Support exact, prefix, token, fuzzy, and normalized matching. Queries such as `daft punk`, `daftp`, and `random access` should return useful results.

Rank exact title/name first, then prefix, exact artist, token matches, and fuzzy similarity. Support filters for artist, album, genre, year, format, bitrate, favorite, and played/unplayed.

Search must be indexed; normal search must not scan whole tables. Global results are grouped into Tracks, Albums, Artists, and Playlists with keyboard navigation and enter-to-open/play behavior.
