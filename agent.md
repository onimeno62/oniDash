# oniDash Agent Guide

oniDash is a Windows-first, local-first media library platform. Its UI is a modern React web application hosted locally by a .NET backend and eventually packaged as a Windows desktop application.

## Stack
- C# / .NET 10
- ASP.NET Core
- SQLite + Entity Framework Core
- React + TypeScript
- Tailwind CSS or equivalent utility-first styling
- xUnit; Vitest/Playwright where appropriate
- WebView2 or Tauri-style desktop wrapper later

## Repository shape
```text
backend/oniDash.Core
backend/oniDash.Application
backend/oniDash.Infrastructure
backend/oniDash.Api
backend/Plugins/*
frontend/oniDash.Web
desktop
tests
docs
```

## Dependency direction
```text
UI → API → Application → Core
             ↓
       Infrastructure
             ↓
           SQLite
```
Plugins depend on Core/Application contracts; Core never depends on plugins.

## First milestone
Launch shell, API connection, local settings shell, polished responsive UI, theme support, and health check. Do not implement catalogues yet.

## Discipline
One milestone at a time. One coherent feature at a time. Keep builds/tests green. Do not over-engineer. Never claim success without verification.
