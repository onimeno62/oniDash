import { useCallback, useEffect, useRef, useState } from 'react';
import { fetchHealth, type AppHealthReport } from '../api/health';

interface HealthState {
  data: AppHealthReport | null;
  loading: boolean;
  error: string | null;
}

export interface UseHealthResult extends HealthState {
  refresh: () => Promise<void>;
}

/**
 * Loads the API health report on mount and optionally polls it. Concurrent loads are
 * collapsed by aborting the in-flight request; component unmount aborts as well.
 */
export function useHealth(pollMs?: number): UseHealthResult {
  const [state, setState] = useState<HealthState>({ data: null, loading: true, error: null });
  const inFlight = useRef<AbortController | null>(null);

  const load = useCallback(async () => {
    inFlight.current?.abort();
    const controller = new AbortController();
    inFlight.current = controller;

    setState((previous) => ({ ...previous, loading: true, error: null }));

    try {
      const report = await fetchHealth(controller.signal);
      if (!controller.signal.aborted) {
        setState({ data: report, loading: false, error: null });
      }
    } catch (error) {
      if (controller.signal.aborted) {
        return;
      }
      setState({
        data: null,
        loading: false,
        error: error instanceof Error ? error.message : 'Unknown error.',
      });
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    if (!pollMs) {
      return;
    }
    const id = window.setInterval(() => void load(), pollMs);
    return () => window.clearInterval(id);
  }, [load, pollMs]);

  useEffect(() => () => inFlight.current?.abort(), []);

  return { ...state, refresh: load };
}
