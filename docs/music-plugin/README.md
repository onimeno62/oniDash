# oniDash Music Plugin Specification Pack

This folder is the source of truth for Music plugin product, architecture, API, data model, scanner, metadata, artwork, player, search, UI, settings, testing, smart playlists, roadmap, and agent guidance.

Read [`AGENT.md`](AGENT.md) before changing the plugin. The implementation must also follow the repository-level [`AGENTS.md`](../../AGENTS.md), existing oniDash architecture, and current milestone acceptance criteria.

The current codebase already has the foundational Music plugin. These documents describe the target direction and must be reconciled with existing contracts before adding schema or API surface. Prefer small vertical slices, additive migrations, typed DTOs, bounded queries, offline-first behavior, and regression tests.
