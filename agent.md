# oniDash Agent Guide

## Mission
oniDash is a Windows-first, local-first media library platform. It is a modern local web application backed by .NET, SQLite and filesystem services, with eventual Windows desktop packaging.

## Canonical documents
Before implementing work, read:
- `docs/PRODUCT-SPEC.md`
- `docs/ARCHITECTURE-SPEC.md`
- `docs/UI-SPEC.md`
- `docs/TASKLIST.md`
- `docs/ROADMAP.md`
- `AGENTS.md`

These documents are the source of truth for product direction and agent behavior.

## Stack
- C# / .NET 10
- ASP.NET Core
- SQLite + Entity Framework Core
- React + TypeScript
- Tailwind CSS
- xUnit; Vitest/Playwright where appropriate
- Windows desktop wrapper later

## Dependency direction
```text
UI → API → Application → Core
             ↓
       Infrastructure
             ↓
           SQLite/filesystem
```
Plugins depend on Core/Application contracts. Core never depends on plugins.

## Execution discipline
One milestone at a time. Take the first applicable unchecked task in `docs/TASKLIST.md`. Deliver complete vertical slices: implementation, tests, UI states, documentation and verification.

Never:
- invent requirements to unblock yourself
- silently mutate user media
- make Internet access a core dependency
- hide API errors behind frontend fallbacks
- claim a build/test passed without running it
- mark a task complete without evidence

## Current priority
Platform foundation → scanner/indexer → metadata/artwork → global search/health → Music hardening → Movies/Anime → Manga → Books → unified dashboard → Windows productization.
