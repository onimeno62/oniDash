# oniDash Manga

The Manga plugin is designed as a desktop-first manga discovery, library, download, update and reading experience. Its UX takes inspiration from Kotatsu while using a provider boundary that can consume Mihon-compatible extensions through a compatible runtime such as Suwayomi.

## Reference projects

- Kotatsu: https://github.com/KotatsuApp/Kotatsu
- Suwayomi: https://github.com/Suwayomi/Suwayomi
- Mangayomi: https://github.com/kodjodevf/mangayomi
- Yomihon: https://github.com/yomihon/yomihon

Kotatsu is used as UX inspiration rather than copied UI/code. Its repository is archived, so oniDash should avoid coupling itself to Kotatsu internals.

## Feature scope

- Home dashboard with continue reading, latest chapter updates and popular/discovery rows
- Library with search, filters, favorites, unread counts and reading status
- User-defined categories
- Mihon-compatible extension/source management
- Source browsing/search and source metadata
- Install/enable/disable extension lifecycle through the backend
- Library update checks and update feed
- Per-title unread/read chapter state
- Reading progress and continue-reading queue
- Chapter download/offline queue
- Bulk download and automatic download of new chapters
- Manga detail drawer with metadata, genres, author/artist, source and progress
- Favorites and reading status: reading, completed, on hold, plan to read, dropped
- Ratings/bookmarks/history and incognito mode at the reader/backend layer
- Configurable manga/webtoon reader with RTL/LTR, long-strip and paged modes
- Page prefetch/cache, resume position and offline reading
- Tracking integrations (AniList, MyAnimeList, Kitsu, MangaUpdates) behind adapters
- Backup/restore compatible with the selected backend format
- Desktop notifications for new chapters and download failures
- OPDS export where supported

## Source architecture

The frontend talks to `/api/manga/*`. The backend should provide a provider abstraction with a Mihon-compatible extension runtime. Suwayomi is the preferred integration boundary because it explicitly supports Mihon/Tachiyomi extensions and exposes library, categories, updates, downloads and tracking capabilities.

The application must not hard-code individual source websites. Extension identifiers, source metadata, authentication and scraping/runtime concerns belong to the provider layer.

## Safety and legal boundary

oniDash should act as a reader/library client. It does not ship copyrighted manga or bundled third-party content. Users install/configure extensions and sources according to their own rights and applicable law. Extension code and licenses must be respected.
