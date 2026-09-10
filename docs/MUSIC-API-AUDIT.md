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

## Fixed in this pass

- Lyrics GET read the same `.lrc` file twice. It now reads once and derives synchronization from the same content.

## Remaining gaps

- Music endpoints still return several ad-hoc `{ error }` payloads instead of the platform error envelope.
- Music DTOs are not yet unified with the canonical cross-media result envelope.
- Full build/type verification requires the repository toolchain and is not claimed here.
