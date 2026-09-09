# oniDash Architecture Specification

## 1. Target architecture
```text
React Web UI
    ↓ HTTP/JSON
ASP.NET Core API
    ↓
Application / Use Cases / DTOs / Contracts
    ↓
Core Domain
    ↓
Infrastructure ──→ SQLite / filesystem / local services

Plugins → Core + Application contracts
Core/Application → never depend on plugin implementations
```

## 2. Platform library pipeline
```text
Library Source
  → File Discovery
  → File Identity
  → Media Type Detection
  → Metadata Extraction
  → Normalization
  → Media Item/File persistence
  → Artwork extraction/cache
  → Search indexing
  → Plugin catalogue enrichment
```
Each stage must be independently testable and retryable.

## 3. Scanner requirements
- Recursive enumeration with configurable roots.
- Ignore configurable hidden/system/junk patterns.
- Never follow reparse points outside an allowed root without an explicit policy.
- Stable file identity should prefer path + size + timestamps and evolve toward stronger identity where useful.
- Upserts must be idempotent.
- Missing files are marked missing before destructive removal is considered.
- Scan jobs support cancellation, progress, retries and diagnostics.
- A failed file must not fail the entire scan.

## 4. Media detection
Detection is capability/handler based, not a giant media-type switch. Handlers declare supported extensions/signatures and produce normalized metadata candidates.

Initial handler families:
- audio
- video
- image/artwork
- ebook/document
- comic/archive

## 5. Metadata model
Keep three concepts separate:
1. **Local truth** — facts extracted from files or explicitly entered by the user.
2. **Normalized catalogue data** — canonical fields used by the application.
3. **External enrichment** — provider suggestions with provenance and confidence.

External metadata must never overwrite local truth implicitly.

## 6. Artwork
Artwork is an asset with provenance, type, dimensions and cache location. Support embedded artwork first; provider artwork is enrichment. Missing artwork is a health signal, not a fatal error.

## 7. Provider contracts
Use interfaces such as:
- `IMetadataProvider`
- `IArtworkProvider`
- `IMediaHandler`
- `ISearchIndexer`
- `ICataloguePlugin`

Providers must be optional, cancellable, rate-limit aware and diagnosable. No core feature may require a specific Internet provider.

## 8. Search
Global search is shared infrastructure. SQLite FTS5 is the baseline. Index normalized searchable fields plus media type/library identifiers. Search results return a common result envelope with type-specific navigation metadata.

## 9. Jobs
All long-running work uses a common job model:
- queued/running/completed/failed/cancelled
- progress and current item
- timestamps
- retry count
- error details
- correlation/job ID

Scanner, metadata enrichment, artwork fetching, search reindex and library maintenance use this model.

## 10. Persistence
SQLite + EF Core. Schema changes must be additive/safe where practical, use migrations, preserve user data, and include migration tests for risky changes.

## 11. API rules
- JSON contracts are explicit DTOs.
- Never expose EF entities directly as public API contracts.
- Return correct status codes and structured errors.
- Never let SPA fallback satisfy an API route.
- Every endpoint has happy-path and failure-path tests.

## 12. Frontend rules
The frontend talks only to APIs. Shared primitives handle loading/empty/error states. Media dashboards consume typed API clients. No media-specific dashboard should duplicate platform scanner/search infrastructure.

## 13. Security and safety
Local-by-default. Validate filesystem paths, prevent path traversal, avoid arbitrary command execution, constrain provider downloads, and treat downloaded metadata/content as untrusted input.
