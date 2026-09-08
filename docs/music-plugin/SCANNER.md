# Music Scanner Specification

## Pipeline
Discover → identify audio files → parse metadata → normalize → resolve entities → extract artwork → persist → update search index.

Recursively scan sources while ignoring temporary files, hidden/system files where appropriate, unsupported formats, and known cache folders.

## Incremental behavior
Use path, size, modified timestamp, and where useful a content hash. Do not fully parse unchanged files. Detect new, modified, moved, renamed, and deleted files. Preserve identity after moves/renames only when confidently matched.

## Safety and reliability
One malformed file produces a per-file error and does not abort the scan. Never delete originals. Retain missing database records long enough for recovery/relinking. Scans expose discovered, processed, failed, current path, elapsed time, and estimated progress; cancellation leaves the database consistent.

Watch mode, when supported, debounces filesystem-event bursts.

## Performance
Support large libraries with batch processing, bounded memory, bulk operations where safe, pagination, virtualization, and indexed queries.
