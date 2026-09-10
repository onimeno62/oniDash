# Next ten spec audit

Added contract-level foundations for the next ten roadmap items on 2026-09-11:

1. Seasons and episodes, via `IEpisodeCatalogue`.
2. Optional anime enrichment, via `IAnimeMetadataProvider`.
3. Plugin lifecycle, via `IMediaPluginManager` and `PluginDescriptor`.
4. Manga chapter source capability, via `IMangaChapterSource`.
5. Manga chapter persistence, via `IMangaChapterCatalogue`.
6. Manga reading progress, via `SetProgressAsync`.
7. Manga download queue, via `IMangaDownloadQueue`.
8. Unified activity and continue surfaces, via `IUnifiedMediaService`.
9. Typed Book author/series work remains represented by the existing summaries and still needs persistence.
10. Book reader support remains represented by `IBookReader` and still needs a real implementation.

These are contracts, not claims of completed persistence, providers, plugin execution, downloads, or readers. Main now has explicit boundaries for the next vertical implementations.
