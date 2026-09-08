# oniDash Component Specification

## Groups
Foundation: Button, IconButton, Input, Badge, Avatar, ProgressBar.
Layout: AppShell, Sidebar, TopBar, PageContainer, Section.
Media: Artwork, MediaCard, MediaRow, HeroCard, PersonCard, TrackRow.
Navigation: Tabs, Breadcrumbs, Search.
Feedback: EmptyState, LoadingState, ErrorState, Toast, Skeleton.
Overlay: Dialog, Drawer, ContextMenu, Popover.
Playback: PlayerBar, Queue, VolumeControl.

## Rules
AppShell owns global navigation and viewport, never catalogue business logic. Sidebar supports dynamic plugin entries and responsive navigation. TopBar supports page context, global search, actions. MediaCard uses variants (poster/square/compact/landscape), not duplicated catalogue cards. Every data screen has loading/populated/empty/error states. Interactive components support keyboard/focus/accessibility.
