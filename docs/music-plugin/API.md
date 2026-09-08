# Music API Contract

Adapt naming to existing oniDash backend conventions.

## Core endpoints
- `GET /api/music/library`
- `GET /api/music/tracks`
- `GET /api/music/albums`
- `GET /api/music/artists`
- `GET /api/music/genres`
- `GET /api/music/search?q=&type=&limit=&offset=`

Track operations: `GET /api/music/tracks/:id`, `POST /api/music/tracks/:id/play`, `PATCH /api/music/tracks/:id`, `DELETE /api/music/tracks/:id`.

Album operations: `GET /api/music/albums/:id`, `POST /api/music/albums/:id/play`, `POST /api/music/albums/:id/queue`.

Artist: `GET /api/music/artists/:id`.

## Playlists
`GET/POST /api/music/playlists`, `GET/PATCH/DELETE /api/music/playlists/:id`, item add/remove/reorder endpoints under `/api/music/playlists/:id/items`.

## Favorites and history
`POST /api/music/favorites`, `DELETE /api/music/favorites/:entityType/:entityId`, `GET /api/music/favorites`, `GET /api/music/history`.

## Scanning
Library source CRUD under `/api/music/sources`, `POST /api/music/sources/:id/scan`, `POST /api/music/sources/:id/watch`, `GET /api/music/scans/:id`.

## Statistics
Overview, plays, top tracks/artists/albums, and genres under `/api/music/statistics`.

## Rules
Validate all input, paginate collection endpoints, use stable sorting and typed errors, never expose filesystem paths except to a trusted local UI, and do not perform expensive operations synchronously.
