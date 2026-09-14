# oniDash Music UX Workflows

## Design objective
The Music module should feel like a serious Windows music application: discovery is visual, management is dense and precise, editing is focused, and playback is always available.

## Home
Order content by usefulness rather than feature count:
1. Continue Listening.
2. Recently Played.
3. Recently Added.
4. Most Played.
5. Favorites / Top Rated.
6. Albums / Artists / Genres.
7. Playlists.
8. Library totals and health.

Carousels are used for discovery collections. Large datasets use tables or virtualized grids.

## Songs workflow
User can select one or many tracks. Selection enables queue, playlist, favorite, rating, metadata, rename, move, delete and other batch operations. Sorting/filtering never resets selection unexpectedly without clear UI feedback.

## Album workflow
Album header shows artwork, artist, year and track count with Play, Shuffle, Play Next, Queue and Favorite actions. Track list uses the same TrackRow/context menu used elsewhere.

## Artist workflow
Artist page provides artwork, album collection, popular tracks and all songs. Navigation into an album or track preserves the player/queue.

## Folder workflow
Folders represent the real local hierarchy. Users can scan, play, shuffle, queue, rename, move, delete and open the folder in Explorer.

## Metadata workflow
Open editor → inspect current metadata → edit → validate → preview write → confirm → execute → verify → report. Bulk editing follows the same pattern and never silently overwrites unrelated fields.

## Organization workflow
Select tracks → choose preset/custom filename template → generate proposed paths → show collision/errors → user confirms → execute → verify filesystem/database reconciliation.

## Identification workflow
Select track(s) → search MusicBrainz/AcoustID → show candidates and confidence → compare current/proposed metadata → select fields → apply explicitly. Provider failure leaves local data unchanged.

## Lyrics workflow
Track → check embedded/local lyrics → provider search when requested → select result → preview → save. When synced lyrics exist, current line follows playback and clicking a line seeks. Editor supports timestamp entry, shifting/offset and save.

## Player workflow
Play from any track surface. Mini player remains visible globally. Expanded player exposes queue and richer controls. Full player exposes artwork, lyrics, visualizer, waveform and track info. Route changes never reset playback.

## Queue workflow
Add to queue or play next from any track. Queue can be reordered by drag/keyboard, removed, cleared, shuffled or saved as a playlist. Queue errors never silently discard existing items.

## Search workflow
Search across title, artist, album, album artist, genre, filename, folder, playlist and lyrics where indexed. Results are grouped by type and use the same entity navigation/actions as normal library views.

## Error states
Show actionable messages for unavailable files, unsupported formats, provider failures, permission errors, path collisions, malformed tags, playback errors and lyrics no-match. Retry must repeat the failed operation rather than reload an unrelated page.

## Empty states
A new library explains how to add a source and scan it. Empty Favorites/Playlists/History explain how they become populated. Missing artwork/lyrics are informational and offer the relevant action.

## Accessibility and keyboard
All controls have labels and visible focus. Multi-selection, context menus, play/pause, search and common library actions must be keyboard reachable. Respect reduced-motion settings.
