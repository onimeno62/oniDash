# oniDash v1.0.0 Release Notes

**Release Date:** September 12, 2026  
**Commit:** [`766c6d4`](https://github.com/onimeno62/oniDash/commit/766c6d47103df78c90c0e06fffe9d99b1a9cd148)  
**Milestones Completed:** M0 through M9 (100% of Master Tasklist)

---

## Highlights

- **Multi-Database SQLite Isolation:** Isolated SQLite database boundaries (`oniDash.db`, `music.db`, `movies.db`, `manga.db`) preventing monolythic schema contention while ensuring clean plugin separation.
- **FTS5 Global Search Engine:** Incremental SQLite triggers automatically index tracks, videos, comics, books, and collections for instant cross-media search.
- **Byte-Range Streaming Pipeline:** Native HTTP 206 Partial Content support across media items, range-enabled EPUB readers, and on-the-fly CBZ image extraction.
- **Music & Audiophile Metadata:** Audio tag extraction, ReplayGain peak/loudness normalization persistence, playlisting, and embedded cover art extraction.
- **Movies & TV/Anime:** Full Series, Season, and Episode persistence with video probe inspection and unified continue-watching tracking.
- **Manga Platform:** Mihon/Suwayomi-compatible extension lifecycle, chapter download queue for offline reading, update notification dispatching, and external tracker sync (AniList / MyAnimeList).
- **Books & Documents:** Author and series classification, persistent bookmarks, table-of-contents extraction, and library health diagnostics (`/api/books/health`).
- **Unified Dashboard:** M8 multi-media hub featuring Continue Reading/Watching/Listening, Recently Added, Recently Played, Favorites across media, and local-only recommendations.
- **Windows Desktop Integration:** Background tray lifecycle, global hardware media key interceptors, OS file associations, and full zero-loss archive backup/restore.

---

## Verification & Testing

- Full integration test coverage via `Microsoft.AspNetCore.Mvc.Testing` across all API endpoint modules.
- Isolated test harnesses for audio metadata parsing, ZIP/CBZ reader pipelines, and SQLite persistence.
