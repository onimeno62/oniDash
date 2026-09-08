import { apiFetch } from './client';

/** Lifecycle of a scan run (mirrors oniDash.Application.Scanning.ScanStatus). */
export type ScanStatus = 'Running' | 'Completed' | 'Failed' | 'Cancelled';

/** What the scanner is doing within a run (mirrors ScanPhase). */
export type ScanPhase = 'Discovering' | 'Indexing' | 'Reconciling' | 'Done';

/** Progress snapshot of a scan (mirrors ScanProgress). */
export interface ScanJobSnapshot {
  scanId: string;
  libraryId: string;
  sourceId: string;
  sourceName: string;
  status: ScanStatus;
  phase: ScanPhase;
  filesDiscovered: number;
  filesProcessed: number;
  filesIndexed: number;
  filesUpdated: number;
  filesUnchanged: number;
  filesMarkedMissing: number;
  startedAtUtc: string;
  completedAtUtc: string | null;
  error: string | null;
}

/** Starts a background scan for the source; 409 when one is already running. */
export function startScan(sourceId: string): Promise<ScanJobSnapshot> {
  return apiFetch<ScanJobSnapshot>(`/sources/${sourceId}/scans`, { method: 'POST' });
}

export function fetchScan(scanId: string, signal?: AbortSignal): Promise<ScanJobSnapshot> {
  return apiFetch<ScanJobSnapshot>(`/scans/${scanId}`, { signal });
}

export function cancelScan(scanId: string): Promise<{ cancelled: boolean }> {
  return apiFetch<{ cancelled: boolean }>(`/scans/${scanId}/cancel`, { method: 'POST' });
}

export function isTerminalStatus(status: ScanStatus): boolean {
  return status === 'Completed' || status === 'Failed' || status === 'Cancelled';
}
