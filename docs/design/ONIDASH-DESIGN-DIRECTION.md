# oniDash — Product Design Direction

## Design intent

oniDash should feel like a **serious personal media workstation**, not a template dashboard and not a collection of decorative cards.

The interface is designed around three qualities:

1. **Quiet confidence** — strong hierarchy, restrained surfaces, minimal ornament.
2. **Media-first** — artwork and content carry visual weight; chrome stays secondary.
3. **Fast to scan** — dense enough for a large library, but never visually noisy.

The visual language intentionally avoids generic SaaS patterns, excessive gradients, oversized hero typography, glassmorphism everywhere, floating blobs, arbitrary pills, and gratuitous animation.

## Visual system

### Palette

Dark mode is the primary experience.

- Background: near-black blue `#090B0F`
- Primary surface: `#101319`
- Elevated surface: `#151922`
- Hover surface: `#1B202A`
- Border: `#252B36`
- Primary text: `#F1F3F6`
- Secondary text: `#B9C0CB`
- Muted text: `#8F98A8`
- Accent: restrained violet `#8B7CF6`
- Positive: `#5BC58A`
- Warning: `#D7A85D`
- Danger: `#E16B78`

Accent is used for active navigation, primary actions, progress, focus states, and selected content. It should not become a background color for entire sections.

### Typography

Use the system UI stack for a native Windows feel:

`ui-sans-serif, system-ui, -apple-system, "Segoe UI", Roboto, sans-serif`

- Page title: 28–32px / 1.1, semibold
- Section title: 14–16px / 1.25, semibold
- Body: 13–14px / 1.5
- Metadata: 11–12px / 1.35
- Utility labels: 10–11px, uppercase only when genuinely useful

Avoid giant marketing-style headings. oniDash is an application, not a landing page.

### Shape

- Small controls: 8–10px radius
- Cards: 12–16px radius
- Large feature surfaces: 18–20px radius
- Pills: reserved for statuses, filters, and compact metadata
- Do not round every element independently

### Depth

Depth comes primarily from surface contrast and subtle borders. Shadows are reserved for drawers, menus, dialogs, and the persistent player.

Avoid heavy black shadows around ordinary cards.

## Layout

### Global shell

Desktop:

- 232–248px persistent sidebar
- 64–72px top bar
- centered content column with a practical max width
- persistent music player at the bottom when playback is active

Mobile:

- compact top bar
- bottom navigation or drawer navigation
- full-width content
- player collapses to essential controls

### Sidebar

Primary destinations:

- Overview
- Library
- Music
- Movies
- Manga
- Books

Secondary/system destinations:

- Search
- Settings
- Health/status where useful

The active destination uses a quiet surface plus an accent edge. Do not use bright filled navigation pills.

## Dashboard composition

The dashboard should answer four questions immediately:

1. What was I doing?
2. What can I resume?
3. What was added recently?
4. What is happening in my library?

Recommended order:

1. Greeting + compact context
2. One contextual continuation/resume module
3. Recently added
4. Recently played / reading / watching activity
5. Library statistics
6. Optional insights

Do not fill the first viewport with six equal statistic cards.

## Media cards

A media card is primarily an artwork surface with restrained metadata beneath it.

Rules:

- Artwork dominates.
- Metadata stays to one or two lines.
- Hover reveals actions rather than permanently showing a toolbar.
- Progress is a 2–3px line, not a large overlay.
- Status badges are compact and meaningful.
- Context menus hold destructive and file-management actions.

Music artwork may use square covers. Movies use portrait posters. Manga and books use portrait covers.

## Music workspace

Music is the flagship catalogue and should feel closer to a dedicated desktop player than a generic CRUD page.

The workspace should support:

- library/collection context
- search and filtering
- recently played
- albums and artists
- track list
- queue
- playlists
- favourites
- ratings
- metadata editing
- file actions
- lyrics
- playback

The persistent player should remain visually quiet. The current track, progress, transport controls, and volume are the priority. Advanced controls belong in the expanded now-playing surface.

## Metadata editing

Metadata editors should be functional and dense rather than decorative.

Group fields into:

- Identity: title, artist, album, album artist
- Classification: genre, year, track/disc number
- Credits: composer, conductor, publisher
- Artwork: embedded/external artwork
- Lyrics
- File: path, format, codec, bitrate, size

Support explicit save/cancel states. Never silently mutate metadata.

## Interaction principles

- Every interactive element has a visible hover/focus state.
- Keyboard navigation is first-class.
- Destructive actions require deliberate confirmation.
- Loading states preserve layout geometry.
- Empty states explain the next useful action.
- Errors state what failed and what can be done next.
- Avoid animation that delays interaction.
- Respect `prefers-reduced-motion`.

## No-AI-slop rules

Do not introduce:

- random gradient blobs
- neon purple everywhere
- excessive glassmorphism
- huge decorative numerals
- fake analytics charts with meaningless data
- generic "Welcome to your media journey" copy
- excessive rounded containers
- floating sparkle/star decorations
- arbitrary badges on every card
- stock-dashboard metric grids as the primary composition
- animation solely to make a static screen appear impressive

Every visual element must communicate hierarchy, content, state, or affordance.

## Reference prototype

A self-contained visual prototype is available at:

`docs/design/oniDash-dashboard.html`

Open the file directly in a browser to inspect the direction without running the full application.

## Implementation rule

The design direction must be implemented through shared tokens and reusable components. Individual pages should not invent independent colors, radii, shadows, typography scales, or interaction patterns.

The result should feel like one application even when switching between Music, Movies, Manga, Books, Library, and Settings.
