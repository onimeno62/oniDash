# oniDash Manga

The Manga plugin is a desktop-first manga discovery, library, download, update and reading experience. The UX takes inspiration from Kotatsu while the runtime boundary is Suwayomi, which executes Mihon/Tachiyomi-compatible extensions.

## Architecture

`oniDash.Web -> /api/manga/* -> oniDash.Manga -> Suwayomi GraphQL -> Mihon-compatible extensions -> source`

oniDash does **not** scrape individual manga sites itself. Source/runtime concerns stay behind Suwayomi and extension APIs.

The backend integration currently covers:

- Suwayomi connectivity/health
- library and continue-reading data
- update feed and library update trigger
- categories
- installed sources/extensions
- popular browsing and cross-source search
- manga/chapter metadata
- chapter page retrieval for the reader
- favorites via `updateManga`
- read/progress state via `updateChapter`
- chapter downloads via Suwayomi's downloader queue
- extension installation through Suwayomi

Suwayomi provides the underlying capabilities for extension installation/execution, source search/browsing, library/categories, automated updates/downloads, offline downloads, tracking, backups and OPDS. citeturn0search0turn1search1

## Configure Suwayomi

oniDash expects a running Suwayomi server at `http://127.0.0.1:4567` by default. Change this in `backend/oniDash.Api/appsettings.json`:

```json
"Manga": {
  "Suwayomi": {
    "BaseUrl": "http://127.0.0.1:4567",
    "AccessToken": "",
    "TimeoutSeconds": 30
  }
}
```

Suwayomi supports Windows and ships a WebUI. Its current server API is GraphQL; the integration uses the current schema rather than the deprecated pre-v1 REST API. citeturn0search0turn3search2

For extensions, configure an extension store/repository in Suwayomi, then install extensions from the Extensions UI/API. Current Suwayomi documentation explicitly requires users to configure an extension store rather than relying on a default bundled source repository. citeturn0search1

## Reader flow

1. oniDash loads the library from Suwayomi.
2. Selecting a manga loads its chapters.
3. Selecting a chapter calls `fetchChapterPages` through Suwayomi.
4. The reader displays the returned page URLs.
5. Page/chapter progress is written back with `updateChapter`.
6. Downloads are queued through Suwayomi's downloader, so the same offline files and queue remain visible to Mihon-compatible clients.

## Update/download behavior

The dashboard's **Check for updates** action starts Suwayomi's library update job. New chapters are then surfaced through the update feed. Automatic new-chapter downloads should be configured in Suwayomi's server settings; this keeps downloader policy in the runtime that actually owns the files. Suwayomi documents `autoDownloadNewChapters`, download paths and related policies. citeturn0search7

## Tracking, backup and sync

Tracking, backup/restore, SyncYomi and OPDS are intentionally delegated to Suwayomi rather than reimplemented in oniDash. This avoids creating a second, incompatible manga database and lets Mihon/Suwayomi clients share the same state. Suwayomi currently advertises tracking, Mihon-compatible backups, SyncYomi and OPDS support. citeturn0search0turn1search1

## Safety and legal boundary

oniDash does not ship copyrighted manga, bundled third-party source content, or source-specific scraping code. Users install/configure extensions and sources according to their own rights and applicable law. Extension code and licenses must be respected.
