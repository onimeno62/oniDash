# Music Phase 4: metadata editing

## Delivered

- Read-only metadata preview for a track through the local API.
- Explicit metadata write endpoint with a required confirmation flag.
- TagLib writer updates only fields supplied by the caller and trims text values.
- Post-write catalogue refresh keeps the Music index aligned with the file.
- Real MP3 round-trip tests cover successful writes, preservation of omitted fields, malformed files, and missing files.

## Safety boundary

Metadata writes are the only Music feature that modifies user media. UI work shows affected track/file context and requires explicit confirmation before sending `confirmed: true`. Never move, rename, delete, or rewrite artwork implicitly. External metadata providers remain opt-in enrichment.

TagLib# owns the container-specific save behavior. The current writer does not claim crash-safe atomic replacement; locked/read-only and process-interruption behavior must be validated before promising atomic writes.

## Follow-up

Add locked/read-only Windows file tests, post-write audit/history, and provider-backed enrichment only after local editing remains reliable.
