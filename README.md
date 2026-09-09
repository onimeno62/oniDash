# oniDash

> **A local-first Windows media library for your entire collection.**
>
> Music. Movies. Anime. Manga. Books. One beautiful dashboard.

oniDash is a self-hosted, local-first media library platform designed for Windows. It brings personal media discovery, organization, metadata, playback, reading progress, search, and plugin-driven catalogues into one modern web dashboard backed by a local ASP.NET Core service and SQLite.

The project is intentionally designed around a simple principle:

**your media stays yours, your catalogue stays local, and every media type gets a purpose-built experience.**

---

## ✨ What is oniDash?

oniDash is not intended to be another generic file browser. It is a foundation for a unified personal media centre where each media type can provide its own specialized dashboard while sharing the same core library, search, persistence, navigation, and design system.

The application currently has dedicated experiences for:

| Media | Dashboard | Primary experience |
|---|---|---|
| 🎵 **Music** | `/music` | Artists, albums, tracks, playlists, categories, favourites, history, insights and playback |
| 🎬 **Movies** | `/movies` | Poster library, metadata, watch state, continue watching and playback |
| 📚 **Manga** | `/manga` | Library, reading progress, favourites, updates, categories and source/extension concepts |
| 📖 **Books** | `/books` | Bookshelf, reading progress, ratings, favourites, series, sorting and search |
| 🗂️ **Library** | `/library` | Shared media inventory, sources and collection management |
| 🔎 **Search** | `/search` | Cross-library search |
| ⚙️ **Settings** | `/settings` | Application and library configuration |

The long-term goal is to make oniDash feel like a single cohesive desktop media environment rather than a collection of unrelated plugins.

---

## 🎯 Project goals

### Local-first

oniDash is built for a local Windows installation. Your catalogue is backed by local storage, and media files remain under your control.

### One application, many media types

Music, movies, manga, books, anime and future media types should share infrastructure without forcing every catalogue into the same UI or domain model.

### Plugin-oriented architecture

Catalogue-specific behavior belongs to plugins/modules. The core library should remain generic and reusable.

### Beautiful by default

The frontend uses a dark-first visual language with elevated surfaces, rounded cards, responsive grids, clear typography, contextual navigation, loading/empty/error states, and media-focused layouts.

### Automation without losing control

Scanning, indexing, metadata extraction, playback progress, reading progress and catalogue updates are designed to be automated while remaining observable and recoverable.

### Windows-native practicality

The target environment is a personal Windows machine with local folders, local databases, local services, and media that may live across multiple disks or folders.

---

## 🖥️ Product experience

### Global shell

The application provides a shared shell with:

- Dashboard
- Library
- Music
- Movies
- Manga
- Books
- Search
- API Health
- Settings
- responsive sidebar navigation
- global top bar
- theme support
- shared loading, empty and error states

Media-specific features intentionally stay inside their respective dashboards. For example, playlists and music insights are part of the Music experience instead of being separate top-level navigation destinations.

### Music

The Music dashboard is the most mature catalogue experience and is designed as a unified music workspace.

It includes the concepts of:

- library selection
- filesystem reindexing
- artist browsing
- album browsing
- track browsing
- genre/category discovery
- search and filtering
- playlists
- smart/manual playlist concepts
- favourites
- listening history
- listening insights
- top tracks
- queue management
- playback controls
- ranged audio streaming
- metadata editing
- track/file actions
- embedded artwork

The Music architecture also provides provider boundaries and catalogue-specific persistence without moving music-specific concepts into the generic core.

### Movies

The Movies dashboard provides a visual poster-oriented experience with catalogue metadata and playback concepts including:

- movie library
- poster grid
- continue watching
- watched state
- resume playback
- playback position persistence
- duration and geometry information
- embedded poster artwork
- ranged video streaming
- reindex/recovery workflows

### Manga

The Manga dashboard establishes a foundation for a more advanced manga ecosystem inspired by applications such as Kotatsu and Suwayomi.

The planned direction includes:

- personal library
- reading progress
- favourites
- categories
- latest updates
- popular discovery
- source management
- extension/provider integration
- chapter downloads
- offline reading
- reader integration
- update notifications
- tracking/synchronization boundaries

The current frontend already models several of these concepts, but the manga API/backend integration is still under active development. It should not be mistaken for a fully operational manga source engine yet.

### Books

The Books dashboard provides a dedicated reading-library UI with:

- library selection
- reindexing
- total/read/unread statistics
- continue reading
- all/unread/read/favourites views
- search by title, author, series and year
- sorting by title, author, recent reading, rating and page count
- cover grid
- book details
- read/unread state
- favourites
- 1–5 star ratings
- manual page progress
- series and format metadata

Like Manga, the Books UI currently defines a richer catalogue contract than the backend exposes. Backend implementation is part of the next integration stage.

---

## 🧱 Architecture

oniDash follows a layered architecture intended to keep the generic media library independent from catalogue-specific behavior.

```text
┌─────────────────────────────────────────────────────────────┐
│                         React Web UI                         │
│              Vite + React + TypeScript + Tailwind           │
├─────────────────────────────────────────────────────────────┤
│                     Media Dashboards                         │
│     Music │ Movies │ Manga │ Books │ Future Plugins          │
├─────────────────────────────────────────────────────────────┤
│                    HTTP API / Contracts                      │
│                     ASP.NET Core                             │
├─────────────────────────────────────────────────────────────┤
│                     Application Layer                        │
│             Use cases │ DTOs │ abstractions                  │
├─────────────────────────────────────────────────────────────┤
│                       Core Layer                             │
│       Generic library/domain primitives, no catalogue       │
├─────────────────────────────────────────────────────────────┤
│                   Infrastructure                             │
│              EF Core │ SQLite │ filesystem                   │
├─────────────────────────────────────────────────────────────┤
│                      Windows                                 │
│         Local folders │ media files │ local database         │
└─────────────────────────────────────────────────────────────┘
```

### Repository layout

```text
oniDash/
├── backend/
│   ├── oniDash.Core/             # Generic domain primitives
│   ├── oniDash.Application/      # Use cases, DTOs, abstractions
│   ├── oniDash.Infrastructure/   # EF Core, SQLite, persistence
│   ├── oniDash.Api/              # ASP.NET Core API host
│   ├── oniDash.Music/            # Music catalogue plugin
│   └── oniDash.Movies/           # Movies catalogue plugin
│
├── frontend/
│   └── oniDash.Web/              # React/Vite/Tailwind frontend
│
├── tests/                         # Backend and integration tests
├── docs/                          # Architecture, product, UI and roadmap docs
├── skills/                        # Agent/development skills
├── artifacts/                     # Local build/bootstrap artifacts
├── AGENTS.md                      # Repository agent instructions
├── AGENT-PROMPT.md                # Autonomous agent bootstrap prompt
├── BOOTSTRAP.md                   # Agent/project bootstrap workflow
├── agent.md                       # Agent operating guidance
├── oniDash.sln                    # .NET solution
└── README.md                      # This document
```

---

## 🔌 Plugin architecture

The central design rule is that **core infrastructure should know about media, not about every media catalogue.**

A plugin owns its specialized concerns:

```text
Core library
   │
   ├── Library
   ├── Source
   ├── MediaItem
   ├── MediaFile
   ├── Artwork
   ├── Tag
   └── Collection
          │
          ├── Music plugin
          │    ├── tracks
          │    ├── artists
          │    ├── albums
          │    ├── playlists
          │    ├── listening history
          │    └── playback/catalogue logic
          │
          ├── Movies plugin
          │    ├── movies
          │    ├── playback state
          │    └── poster/catalogue logic
          │
          ├── Manga plugin
          │    ├── manga
          │    ├── chapters
          │    ├── reading state
          │    └── sources/extensions
          │
          └── Books plugin
               ├── books
               ├── reading state
               ├── ratings
               └── series/metadata
```

This separation makes it possible to add future catalogues without contaminating the core domain with media-specific assumptions.

---

## 🗃️ Core library model

The shared library layer is responsible for generic concepts such as:

- libraries
- folder sources
- media items
- media files
- artwork
- tags
- collections
- search indexing
- scanning jobs
- filesystem identity

A key architectural principle is that the scanner can discover files without needing to understand every catalogue's complete domain model.

Catalogue plugins can consume indexed media through extension points such as media handlers and then build their own specialized catalogue tables and APIs.

---

## 🔍 Search

The core search implementation uses SQLite FTS5 for full-text search.

The design includes:

- ranked results
- `bm25` relevance ranking
- prefix matching
- multi-term matching
- trigger-maintained indexing
- explicit reindex/recovery support
- library filtering
- live search UI

Search is deliberately implemented as shared infrastructure so every future media type can participate without implementing a completely separate search engine.

---

## 📁 Filesystem scanning

oniDash is designed to safely index local media directories without taking ownership of the files themselves.

The scanner is designed around:

- recursive enumeration
- hidden/system/junk filtering
- extension filtering
- reparse-point safety
- background scan jobs
- cancellation
- live progress reporting
- identity-based upserts
- missing-file detection
- non-destructive indexing

A vanished file is marked missing rather than immediately deleting catalogue information. This makes rescans and temporary disk disconnections much safer.

---

## 💾 Persistence

The default persistence stack is:

- **SQLite** for local storage
- **Entity Framework Core** for relational persistence
- additive migrations
- per-plugin schema ownership where appropriate
- local application data under `%LOCALAPPDATA%\\oniDash`

The default database location is:

```text
%LOCALAPPDATA%\oniDash\onidash.db
```

It can be overridden through the application's connection-string or data-directory configuration.

---

## 🎨 Frontend technology

The web application currently uses:

- React 19
- TypeScript 5.9
- Vite 7
- React Router 7
- Tailwind CSS 4
- Vitest
- Testing Library

The frontend is intentionally component-oriented and uses shared application primitives for:

- application shell
- navigation
- buttons and controls
- icons
- loading states
- empty states
- error states
- media cards
- player controls
- drawers/editors

The visual direction is a **dark-first media dashboard** with strong hierarchy, restrained borders, rounded surfaces, accent-driven actions, responsive layouts, and information-dense catalogue views.

---

## 🛠️ Requirements

For development on Windows, install:

- Windows 10/11
- .NET SDK compatible with the solution's target framework
- Node.js and npm
- Git

Verify the toolchain:

```powershell
dotnet --version
node --version
npm --version
git --version
```

---

## 🚀 Getting started

### 1. Clone the repository

```powershell
git clone https://github.com/onimeno62/oniDash.git
cd oniDash
```

### 2. Build the backend

```powershell
dotnet build oniDash.sln
```

### 3. Start the API

```powershell
dotnet run --project backend/oniDash.Api
```

The development API is configured to run on:

```text
http://localhost:5275
```

### 4. Start the frontend

In another terminal:

```powershell
cd frontend/oniDash.Web
npm install
npm run dev
```

The Vite development server runs on:

```text
http://localhost:5173
```

During development, the frontend proxies `/api` requests to the local ASP.NET Core service.

### 5. Production-style local run

Build the frontend:

```powershell
cd frontend/oniDash.Web
npm run build
```

Then copy the generated `dist` contents into:

```text
backend/oniDash.Api/wwwroot/
```

Start the API and open:

```text
http://localhost:5275
```

---

## 🧪 Verification

Backend:

```powershell
dotnet test oniDash.sln
```

Frontend type checking:

```powershell
cd frontend/oniDash.Web
npm run typecheck
```

Frontend tests:

```powershell
npm run test
```

Frontend production build:

```powershell
npm run build
```

For a complete verification pass:

```powershell
dotnet build oniDash.sln
dotnet test oniDash.sln
cd frontend/oniDash.Web
npm run typecheck
npm run test
npm run build
```

---

## ⚠️ Current integration status

The UI and backend are being developed in parallel. This distinction is important when running the current `main` branch.

### Fully established foundations

- generic library model
- SQLite persistence
- filesystem scanning
- search infrastructure
- shared API
- React application shell
- Music backend/plugin foundation
- Movies backend/plugin foundation

### Music

Music is the most complete specialized dashboard and includes a substantial backend/API foundation plus the unified frontend experience.

### Manga and Books

The current frontend includes dedicated dashboards and API clients for Manga and Books, but their backend endpoints are not yet registered in `oniDash.Api`.

That means a request such as:

```text
GET /api/manga/library?limit=500
```

or:

```text
GET /books?libraryId=...
```

can currently fall through to the SPA's `index.html` fallback in a packaged deployment. The frontend then attempts to parse the HTML document as JSON, producing the browser error:

```text
Unexpected token '<', "<!doctype ..." is not valid JSON
```

This is a **backend/API contract mismatch, not a React rendering problem**.

The correct next step is to implement and register the corresponding Books and Manga endpoints, services, persistence and tests rather than hiding the error in the frontend.

---

## 🧭 Roadmap

The project is evolving toward a complete personal media operating environment.

### Phase 1 — Foundation

- [x] .NET solution
- [x] local API
- [x] SQLite
- [x] React shell
- [x] navigation
- [x] themes
- [x] dashboard/library/search/settings/health foundations

### Phase 2 — Core library

- [x] generic library entities
- [x] folder sources
- [x] media files
- [x] artwork
- [x] tags
- [x] collections
- [x] library APIs

### Phase 3 — Filesystem indexing

- [x] recursive scanner
- [x] safe filesystem traversal
- [x] background jobs
- [x] progress reporting
- [x] cancellation
- [x] identity-based indexing
- [x] missing-file handling

### Phase 4 — Search

- [x] SQLite FTS5
- [x] ranking
- [x] prefix matching
- [x] multi-term search
- [x] live frontend search
- [x] recovery reindex

### Phase 5 — Music

- [x] audio catalogue plugin
- [x] embedded metadata extraction
- [x] artists
- [x] albums
- [x] tracks
- [x] artwork
- [x] ranged streaming
- [x] player
- [x] queue
- [x] playlists
- [x] favourites
- [x] history
- [x] insights
- [x] metadata editing
- [x] unified Music dashboard
- [x] categories/genre discovery

### Phase 6 — Movies

- [x] movie catalogue plugin
- [x] video detection
- [x] release-name metadata parsing
- [x] duration/geometry extraction
- [x] poster artwork
- [x] ranged streaming
- [x] resume playback
- [x] watch state
- [x] continue watching
- [x] poster dashboard

### Phase 7 — Manga

- [x] dedicated Manga dashboard foundation
- [x] library UI
- [x] reading-progress UI
- [x] favourites UI
- [x] categories UI
- [x] updates UI
- [x] source/extension UI foundation
- [ ] Manga backend endpoints
- [ ] persistent manga catalogue
- [ ] Mihon-compatible provider runtime
- [ ] source installation/update lifecycle
- [ ] chapter reader
- [ ] offline downloads
- [ ] update notifications
- [ ] tracking/sync integrations

### Phase 8 — Books

- [x] dedicated Books dashboard foundation
- [x] reading-progress UI
- [x] favourites
- [x] ratings
- [x] search/sorting
- [x] series metadata model
- [ ] Books backend endpoints
- [ ] EPUB/PDF/CBZ indexing
- [ ] cover extraction/cache
- [ ] reader integration
- [ ] metadata providers
- [ ] collections/categories
- [ ] series management

### Phase 9 — Anime / TV

Planned capabilities include:

- series and seasons
- episode indexing
- episode metadata
- watch state
- continue watching
- subtitle handling
- playback
- anime-specific metadata
- season/episode organization
- collections
- external metadata providers

### Phase 10 — Platform polish

- desktop packaging
- background service lifecycle
- richer notifications
- backup/restore
- configurable metadata providers
- richer dashboards
- keyboard navigation
- accessibility improvements
- performance profiling
- larger-library optimization
- automated integration testing

---

## 🔐 Privacy and security

oniDash is designed around local use.

Recommended deployment principles:

- bind the API to localhost unless remote access is explicitly required
- do not expose the service directly to the public internet
- protect remote access behind an appropriate VPN or authenticated reverse proxy
- treat metadata-provider credentials as secrets
- avoid logging sensitive filesystem information unnecessarily
- keep backups of the oniDash database when catalogue state matters

The default architecture assumes the API and frontend run on the same machine.

---

## 📦 Offline NuGet bootstrap

The repository contains a local NuGet bootstrap mechanism for restricted-network development environments.

`nuget.config` prefers the local feed under:

```text
artifacts/nuget-feed/
```

and can fall back to nuget.org when network access is available.

The repository also contains tooling for refreshing the local package cache when required.

---

## 🤖 AI-assisted development

oniDash is intentionally structured to support autonomous coding agents.

Before modifying the repository, agents should read:

1. `AGENTS.md`
2. `BOOTSTRAP.md`
3. `AGENT-PROMPT.md`
4. `agent.md`
5. relevant files under `docs/`
6. relevant skills under `skills/`

The repository treats architecture, product requirements, UI rules, testing practices and plugin boundaries as first-class project artifacts rather than relying solely on source code conventions.

For an AI agent, the expected workflow is:

```text
Understand repository rules
        ↓
Inspect current main
        ↓
Inspect existing plugin/API contracts
        ↓
Plan the smallest coherent change
        ↓
Implement on a feature branch
        ↓
Run build/typecheck/tests
        ↓
Review the diff
        ↓
Open a PR against main
```

Agents should never claim tests passed unless they actually ran them.

---

## 🧩 Design principles

### 1. Core stays generic

Do not move Music, Movies, Manga or Books catalogue rules into the generic core unless the concept is genuinely shared.

### 2. Plugins own specialized data

A catalogue plugin should own its specialized persistence, services, metadata logic and API surface.

### 3. Dashboards own the experience

A media plugin should feel like a complete application within oniDash. Its related features belong together instead of fragmenting the global navigation.

### 4. APIs are explicit contracts

Frontend API clients should correspond to real backend endpoints. Placeholder contracts must be clearly documented until implemented.

### 5. Files are not disposable records

Indexing should be non-destructive. A catalogue should be able to survive temporary disks, renamed folders, and missing files without immediately losing user state.

### 6. Recovery matters

Scans, metadata extraction and catalogue updates should have explicit reindex/recovery mechanisms.

### 7. UI states are part of the product

Every dashboard should handle loading, empty, error, unavailable and populated states gracefully.

### 8. Performance is a feature

Large media collections require pagination, bounded queries, efficient indexing, lazy artwork loading and careful filesystem traversal.

---

## 🧪 Testing strategy

Testing should happen at multiple levels.

### Backend

- domain/application unit tests
- persistence tests
- API integration tests
- scanner tests
- search tests
- plugin catalogue tests

### Frontend

- component tests
- API/client contract tests
- interaction tests
- routing tests
- dashboard state tests

### End-to-end

Future desktop packaging should verify workflows such as:

```text
Create library
  → add folder source
  → scan
  → index media
  → open dashboard
  → search
  → play/read/watch
  → update progress
  → rescan
  → preserve state
```

---

## 📐 API conventions

The API is organized around explicit endpoint modules in `backend/oniDash.Api/Endpoints`.

Core areas include:

- health
- libraries
- tags
- collections
- scanning
- search
- music
- movies

Future catalogue APIs should follow the same pattern:

```text
backend/oniDash.Api/Endpoints/
    MangaEndpoints.cs
    BooksEndpoints.cs
```

with specialized services and persistence remaining outside the API host.

---

## 🖼️ Visual direction

The target UI is a modern media dashboard rather than a traditional admin panel.

Important characteristics include:

- dark-first presentation
- strong visual hierarchy
- generous spacing
- rounded cards and panels
- subtle borders
- high-quality artwork as a primary visual element
- responsive grids
- contextual actions
- compact but readable metadata
- clear primary actions
- persistent media controls where appropriate
- minimal global navigation
- feature-rich internal dashboard navigation

The application should feel closer to a polished desktop media centre than a CRUD database interface.

---

## 🤝 Contributing

Contributions are welcome, especially around:

- catalogue plugins
- Windows integration
- filesystem indexing
- metadata providers
- media playback
- reader integrations
- search performance
- accessibility
- automated testing
- desktop packaging
- UI/UX refinement

Before opening a change:

1. read the repository agent/development instructions
2. understand the existing architecture
3. avoid duplicating functionality already present
4. keep generic code generic
5. add or update tests where appropriate
6. verify the affected frontend/backend builds
7. document new API contracts
8. open a focused pull request

---

## 📄 License

A license has not yet been declared for the repository. Until a license is added, treat the source as **all rights reserved** and do not assume permission to redistribute or reuse it.

---

## 🔗 Project

**Repository:** https://github.com/onimeno62/oniDash

**Default branch:** `main`

**Project:** oniDash

**Target platform:** Windows

**Architecture:** Local-first web dashboard + local ASP.NET Core service + SQLite

---

## ⭐ Vision

oniDash is ultimately intended to become a single, beautiful home for a personal digital collection:

```text
                    ┌───────────────┐
                    │    oniDash    │
                    └───────┬───────┘
                            │
          ┌─────────────────┼─────────────────┐
          │                 │                 │
       Discover          Organize           Enjoy
          │                 │                 │
     ┌────┼────┐       ┌────┼────┐       ┌────┼────┐
     │    │    │       │    │    │       │    │    │
   Search Browse Scan  Tags Lists Stats  Play Read Watch
     │    │    │       │    │    │       │    │    │
     └────┴────┴───────┴────┴────┴───────┴────┴────┘
                            │
                 Your personal collection
```

**One dashboard. Every medium. Completely yours.**
