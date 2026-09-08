# oniDash Music Plugin — Architecture

## Style
Use modular domain-oriented architecture:

Presentation → Application → Domain → Infrastructure

Suggested modules include domain entities for artist, album, track, playlist, playback, metadata, library, and statistics; application services for library, search, playlists, playback, metadata, statistics, and scanning; infrastructure for database, filesystem, audio, metadata, artwork, search, and providers; and presentation routes, components, player, hooks, and state.

## Plugin boundary
The plugin exposes navigation entries, routes, commands/actions, player integration, global search, dashboard widgets, settings, and library services. Avoid direct coupling to other media plugins.

Reuse oniDash's theme system, component primitives, routing, notifications, command palette, global search, database abstractions, plugin lifecycle, settings, logging, and filesystem abstractions. Do not duplicate shared infrastructure.

## Data flow
Filesystem → scanner → parser → normalized metadata → database → search index.

User action → application service → domain rules → repository → database/filesystem → UI update.

Playback: UI → player service → audio backend → playback events → player state → UI.

## Background jobs
Scanning, artwork extraction, metadata enrichment, and expensive statistics run as background jobs with ID, status, progress, timestamps, error count, cancellation, and resumability where practical.
