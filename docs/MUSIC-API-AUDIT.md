# Music API contract audit

Audited against the platform rules on 2026-09-11.

## Verified

- Artist, album, and track listings use bounded `limit`/`offset` pagination.
- Catalogue queries accept library and parent-entity filters.
- Streaming uses server-side file location and range processing.
- Reindex is cancellable and isolated behind the catalogue service.
- Rating writes validate the 0–5 range and update timestamps.
- Rename and file deletion are explicit endpoint actions, not implicit scanner behavior.
- Lyrics reads and writes use cancellation tokens and stay local-first.
- Lyrics writes enforce a bounded payload and normalize line endings.
- Lyrics reads avoid duplicate I/O, reject oversized files, and return structured 403/413/503 failures.
- Duplicate diagnostics are read-only, library-scoped, bounded, and return the candidate track IDs for review.

## Remaining gaps

- Music endpoints still return several ad-hoc `{ error }` payloads instead of the platform error envelope.
- Music DTOs are not yet unified with the canonical cross-media result envelope.
- ReplayGain/loudness and Windows media-key boundaries remain unimplemented.
- Full build/type verification requires the repository toolchain and is not claimed here.
