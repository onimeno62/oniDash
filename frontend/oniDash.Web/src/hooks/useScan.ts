import { useCallback, useEffect, useRef, useState } from 'react';
import {
  cancelScan,
  fetchScan,
  isTerminalStatus,
  startScan,
  type ScanJobSnapshot,
} from '../api/scans';

const POLL_INTERVAL_MS = 500;

/**
 * Drives one scan at a time: starts it, polls its snapshot until a terminal status, and
 * notifies the caller on completion (success, failure, or cancellation). Polling stops
 * automatically when the component unmounts.
 */
export function useScan(onCompleted?: (job: ScanJobSnapshot) => void) {
  const [job, setJob] = useState<ScanJobSnapshot | null>(null);
  const [starting, setStarting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const timerRef = useRef<number | null>(null);
  const onCompletedRef = useRef(onCompleted);
  onCompletedRef.current = onCompleted;

  const stopPolling = useCallback(() => {
    if (timerRef.current !== null) {
      window.clearInterval(timerRef.current);
      timerRef.current = null;
    }
  }, []);

  const poll = useCallback(
    (scanId: string) => {
      stopPolling();
      timerRef.current = window.setInterval(() => {
        void (async () => {
          try {
            const snapshot = await fetchScan(scanId);
            setJob(snapshot);
            if (isTerminalStatus(snapshot.status)) {
              stopPolling();
              onCompletedRef.current?.(snapshot);
            }
          } catch {
            // Transient poll errors (e.g. the tab was backgrounded): keep polling and
            // leave the last known snapshot on screen.
          }
        })();
      }, POLL_INTERVAL_MS);
    },
    [stopPolling],
  );

  const start = useCallback(
    async (sourceId: string) => {
      stopPolling();
      setStarting(true);
      setError(null);
      try {
        const snapshot = await startScan(sourceId);
        setJob(snapshot);
        if (isTerminalStatus(snapshot.status)) {
          onCompletedRef.current?.(snapshot);
        } else {
          poll(snapshot.scanId);
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'The scan could not be started.');
      } finally {
        setStarting(false);
      }
    },
    [poll, stopPolling],
  );

  const cancel = useCallback(async () => {
    if (!job || isTerminalStatus(job.status)) return;
    try {
      await cancelScan(job.scanId);
    } catch {
      // Cancellation is best-effort; polling reports the final state either way.
    }
  }, [job]);

  useEffect(() => stopPolling, [stopPolling]);

  return { job, starting, error, start, cancel, stopPolling };
}
