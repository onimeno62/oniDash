# Milestone 03 — Filesystem scanner (Phase 3)

## Scope
Recursive scan, filtering, identity/dedup groundwork, background job, progress, cancellation, and errors.

## Safety behavior
The scanner is read-only. Hidden/system/junk directories and reparse points are skipped. A missing root, inaccessible directory, or directory-enumeration failure fails the scan before reconciliation, so previously indexed files are not falsely marked missing. Individual files that disappear while being read are skipped and reconciled on a later completed scan.

A cancelled scan during discovery writes nothing. Cancellation during indexing may preserve completed writes from that pass, but it does not mark unobserved files missing or stamp the source as successfully scanned.

## Existing implementation
The scanner uses stable per-source/path identities, preserves media records when files disappear, and reports background progress with cancellation. See the repository history and tests for the full API and UI behavior.
