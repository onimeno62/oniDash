import { useEffect, useRef, useState } from 'react';
import { searchMedia, type SearchResult } from '../api/search';

const DEBOUNCE_MS = 250;

/**
 * Search-as-you-type: debounces the query, aborts superseded requests, and exposes
 * results with loading/error state. A blank query clears results without a request.
 */
export function useSearch(libraryId?: string) {
  const [results, setResults] = useState<SearchResult[] | null>(null);
  const [searching, setSearching] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [query, setQuery] = useState('');
  const controllerRef = useRef<AbortController | null>(null);
  const timerRef = useRef<number | null>(null);
  const sequenceRef = useRef(0);

  const trimmed = query.trim();
  const activeLibraryId = libraryId ?? '';

  useEffect(() => {
    if (timerRef.current !== null) {
      window.clearTimeout(timerRef.current);
      timerRef.current = null;
    }
    controllerRef.current?.abort();
    controllerRef.current = null;

    if (trimmed === '') {
      setResults(null);
      setSearching(false);
      setError(null);
      return;
    }

    setSearching(true);
    timerRef.current = window.setTimeout(() => {
      const sequence = ++sequenceRef.current;
      const controller = new AbortController();
      controllerRef.current = controller;

      searchMedia(trimmed, {
        libraryId: activeLibraryId || undefined,
        limit: 100,
        signal: controller.signal,
      })
        .then((hits) => {
          if (sequence !== sequenceRef.current) return;
          setResults(hits);
          setError(null);
        })
        .catch((err: unknown) => {
          if ((err as { name?: string })?.name === 'AbortError') return;
          if (sequence !== sequenceRef.current) return;
          setError(err instanceof Error ? err.message : 'Search failed.');
          setResults([]);
        })
        .finally(() => {
          if (sequence === sequenceRef.current) {
            setSearching(false);
          }
        });
    }, DEBOUNCE_MS);

    return () => {
      if (timerRef.current !== null) {
        window.clearTimeout(timerRef.current);
        timerRef.current = null;
      }
    };
  }, [trimmed, activeLibraryId]);

  useEffect(
    () => () => {
      controllerRef.current?.abort();
      if (timerRef.current !== null) {
        window.clearTimeout(timerRef.current);
      }
    },
    [],
  );

  return { query, setQuery, results, searching, error };
}
