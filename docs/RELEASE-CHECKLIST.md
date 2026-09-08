# Stabilization release checklist

- [ ] Windows checkout builds the solution and runs backend tests.
- [ ] Frontend clean install passes typecheck, tests, and production build.
- [ ] E2E uses a dedicated data directory and never targets a normal user database.
- [ ] Path resolver tests cover traversal, rooted paths, sibling-prefix roots, and reparse points for audio and video.
- [ ] Inaccessible directories fail a scan before reconciliation; the index is not falsely marked missing.
- [ ] Cancellation, restart, re-scan, and file reappearance are verified.
- [ ] Movie progress completion sets watched state and timestamp consistently.
- [ ] Continue Watching and catalogue responses are bounded.
- [ ] Unsupported browser media displays a recoverable error.
- [ ] Phase 7 design is approved and mixed fixtures are defined.
- [ ] Windows packaging smoke test covers fresh install, loopback binding, ffprobe, upgrade, and data preservation.

This checklist is evidence-driven: do not mark an item complete from a historical milestone report alone.
