# oniDash Music Plugin — UI Specification

## Visual language
Premium dark-first desktop media application: large artwork, restrained surfaces, subtle translucency, rounded cards, generous spacing, strong typography hierarchy, subtle motion, and atmospheric gradients only when they improve hierarchy. Avoid generic admin dashboards, excessive borders/cards/gradients, dense tables everywhere, and visual noise.

## Shell and screens
Navigation: Music, Home, Tracks, Albums, Artists, Genres, Playlists, Favorites, History, Statistics, Settings. Main content has contextual header, content, and persistent player.

Home includes continue listening, recently added/played, favorites, most played, discovery, and library health. Albums support grid/compact/list views with play, queue, and overflow actions. Artists support grid/list and hero/discography pages. Tracks use a dense virtualized list with track, artist, album, duration, year, codec, favorite, and context menu. Album detail includes artwork, metadata, grouped tracks, and play/shuffle/queue/playlist/favorite actions.

## Player
Persistent bottom bar: artwork, track/artist, previous, play/pause, next, progress, volume, queue, shuffle, repeat. Expanded player adds large artwork, timeline, controls, queue, and lyrics/provider area.

## Responsive and accessibility
At narrow widths collapse the sidebar, reduce metadata columns, adapt grids, preserve player controls, and keep touch targets usable. Every view defines loading, empty, error, populated, and unavailable-artwork states. Use keyboard navigation, visible focus, semantic buttons, tooltips for icon-only actions, accessible dialogs, sufficient contrast, and reduced-motion support.
