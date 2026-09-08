# Music plugin agent guidance

Read the shared repository instructions in `/AGENTS.md` first, then read the complete music-plugin specification pack in [`docs/music-plugin/`](../../docs/music-plugin/).

Start with:

1. [`docs/music-plugin/AGENT.md`](../../docs/music-plugin/AGENT.md)
2. [`docs/music-plugin/ARCHITECTURE.md`](../../docs/music-plugin/ARCHITECTURE.md)
3. [`docs/music-plugin/ROADMAP.md`](../../docs/music-plugin/ROADMAP.md)
4. [`docs/music-plugin/TESTING.md`](../../docs/music-plugin/TESTING.md)

Keep the plugin modular, local-first, non-destructive, paginated, accessible, and testable. Do not put database or filesystem work in UI components, do not couple the player engine to the UI, and do not make external metadata providers a source of truth.
