# Implementation gate

The application and platform contract pass is complete enough to begin vertical implementation work. Further interface-only specs would add ceremony without value.

## Required next work

1. Implement persisted TV/Anime series, seasons, episodes, and progress with migrations and API tests.
2. Implement typed Books author/series relationships and reader streaming with fixture coverage.
3. Replace Manga placeholder state tables with a real plugin/source boundary and chapter persistence.
4. Implement unified activity, favorites, and continue surfaces from real catalogue data.
5. Implement Windows host, installer, notifications, media keys, and backup/restore behind platform adapters.
6. Resolve ReplayGain through supported TagLib field APIs, migration, and round-trip tests.
7. Run formatter, build, typecheck, unit tests, API tests, and frontend tests before marking any item complete.

Do not add more empty interfaces until at least one of these vertical slices has executable persistence, endpoint behavior, tests, and failure states.
