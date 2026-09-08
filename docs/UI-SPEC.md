# oniDash UI / Visual Design Specification

## Direction
Premium dark media dashboard: large artwork, rounded cards, layered surfaces, subtle translucency/glass, restrained gradients, strong typography, compact navigation, persistent media controls, polished micro-interactions.

Use reference images as inspiration only. Do not reproduce third-party branding, logos, copy, or exact layouts.

## Identity
oniDash: cinematic, modern, calm, technical, personal. Dark-first, light theme supported. Default accent: restrained violet/indigo.

## Layout
1440px+: full sidebar + content + optional secondary rail. 1024–1439: compact sidebar + content. Below 1024: responsive navigation.

## Shell
```text
Sidebar | Header
        | Main content
        | Persistent player when active
```

## Design rules
Centralize colors, spacing, radii, typography, shadows, blur, and motion. Artwork is visually dominant. Avoid excessive blur/gradients.

## Components
AppShell, Sidebar, TopBar, Search, MediaCard, CompactMediaRow, HeroCard, Artwork, Badge, Tag, Tabs, Carousel, DataTable, DetailHeader, ProgressBar, PlayerBar, ContextMenu, Dialog, Toast, EmptyState, LoadingState, ErrorState.

## Motion
120–220ms for UI changes; 220–400ms for larger transitions; respect prefers-reduced-motion.

## Accessibility
Keyboard navigation, visible focus, semantic HTML, sufficient contrast, labels for icon-only buttons, reduced-motion support.

## Catalogue UI
Music emphasizes Artist → Album → Track. Movies: Movie → People → Credits → Files. Anime: Series → Seasons → Episodes. Manga: Series → Volumes → Chapters → Reader. Books: Book → Author → Series → Reader.
