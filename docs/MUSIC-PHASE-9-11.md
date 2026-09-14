# Music Rebuild — Phases 9–11

## Phase 9 — Collections and history
Implemented:
- Server-side Continue Listening collection from persisted playback state.
- Recently Played collection using bounded SQL grouping over play history.
- Recently Added collection using persisted `AddedAtUtc`.
- Most Played collection using play-count and played-time aggregation.
- Favorites collection for tracks.
- Top Rated collection from persisted 0–5 ratings.
- Never Played collection using play-history exclusion.
- Normal playlist creation, item management and reorder support retained.
- Smart playlist rule model/evaluation retained.
- Save-current-queue-to-playlist endpoint, with missing-track and duplicate protection.

## Phase 10 — Music Home
Implemented:
- Music Home now consumes one bounded server-side collection API instead of loading the library into the browser.
- Continue Listening uses persisted playback position.
- Recently Played, Recently Added, Most Played, Favorites, Top Rated and Never Played sections.
- Album, artist and playlist discovery remains bounded by server-side limits.
- Empty-library and loading/error states.
- Existing player remains persistent across navigation.

The previous year-based Recently Added calculation is no longer used by the new Music Home.

## Phase 11 — Windows integration and hardening
Implemented at the web/player boundary:
- Media Session integration for play/pause, next/previous and seek controls exposed by supported browsers/Windows media surfaces.
- System Now Playing metadata and artwork.
- Keyboard playback shortcuts: Space, Left/Right, Shift+Left/Right.
- CI workflow covering .NET restore/build/test and frontend install/build/test.

Still intentionally pending:
- Native WASAPI decoder/output layer and device enumeration.
- Native Windows Explorer integration and native notifications.
- Drag/drop folder ingestion.
- Gapless/crossfade/ReplayGain.
- Full end-to-end regression suite and large-library profiling.

These native Windows items are not represented as complete merely by browser APIs.
