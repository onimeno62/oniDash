# Music Phase 4: metadata editing

## Delivered

- Read-only metadata preview for a track through the local API.
- Explicit metadata write endpoint with a required confirmation flag.
- TagLib writer updates only fields supplied by the caller and saves the local file.
- Read failures and write failures return recoverable errors; the scanner remains independent.

## Safety boundary

Metadata writes are the only Music feature that modifies user media. UI work must show affected track/file context and require an explicit confirmation before sending `confirmed: true`. Never move, rename, delete, or rewrite artwork implicitly. External metadata providers remain opt-in enrichment.

## Follow-up

Add a dedicated metadata editor UI, atomic-write verification across supported formats, post-write reindexing, audit/history, and tests for malformed/locked/read-only files before calling this phase complete.
