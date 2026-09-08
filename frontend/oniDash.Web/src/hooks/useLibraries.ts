import { useCallback, useEffect, useRef, useState } from 'react';
import {
  addSource,
  createLibrary,
  deleteLibrary,
  fetchLibraries,
  fetchSources,
  removeSource,
  renameLibrary,
  type Library,
  type LibrarySource,
} from '../api/libraries';

interface LibrariesState {
  libraries: Library[];
  loading: boolean;
  error: string | null;
}

/**
 * Loads libraries once on mount and exposes create/rename/delete operations that keep
 * the local list in sync. Requests are aborted when the component unmounts.
 */
export function useLibraries(): {
  libraries: Library[];
  loading: boolean;
  error: string | null;
  reload: () => Promise<void>;
  create: (name: string) => Promise<void>;
  rename: (id: string, name: string) => Promise<void>;
  remove: (id: string) => Promise<void>;
} {
  const [state, setState] = useState<LibrariesState>({
    libraries: [],
    loading: true,
    error: null,
  });
  const controllerRef = useRef<AbortController | null>(null);

  const reload = useCallback(async () => {
    controllerRef.current?.abort();
    const controller = new AbortController();
    controllerRef.current = controller;

    setState((previous) => ({ ...previous, loading: true, error: null }));
    try {
      const libraries = await fetchLibraries(controller.signal);
      if (!controller.signal.aborted) {
        setState({ libraries, loading: false, error: null });
      }
    } catch (error) {
      if (controller.signal.aborted) return;
      setState({
        libraries: [],
        loading: false,
        error: error instanceof Error ? error.message : 'Unknown error.',
      });
    }
  }, []);

  useEffect(() => {
    void reload();
    return () => controllerRef.current?.abort();
  }, [reload]);

  const create = useCallback(
    async (name: string) => {
      const library = await createLibrary(name);
      setState((previous) => ({
        ...previous,
        libraries: [...previous.libraries, library].sort((a, b) =>
          a.name.localeCompare(b.name),
        ),
      }));
    },
    [],
  );

  const rename = useCallback(async (id: string, name: string) => {
    const updated = await renameLibrary(id, name);
    setState((previous) => ({
      ...previous,
      libraries: previous.libraries.map((library) => (library.id === id ? updated : library)),
    }));
  }, []);

  const remove = useCallback(async (id: string) => {
    await deleteLibrary(id);
    setState((previous) => ({
      ...previous,
      libraries: previous.libraries.filter((library) => library.id !== id),
    }));
  }, []);

  return { ...state, reload, create, rename, remove };
}

/**
 * Loads the folder sources of a library on demand (when a card expands), and manages
 * add/remove of sources with local optimistic-free refresh.
 */
export function useLibrarySources(libraryId: string | null): {
  sources: LibrarySource[];
  loading: boolean;
  error: string | null;
  reload: () => Promise<void>;
  add: (name: string, rootPath: string) => Promise<void>;
  remove: (sourceId: string) => Promise<void>;
} {
  const [sources, setSources] = useState<LibrarySource[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const controllerRef = useRef<AbortController | null>(null);

  const load = useCallback(async () => {
    if (!libraryId) return;
    controllerRef.current?.abort();
    const controller = new AbortController();
    controllerRef.current = controller;

    setLoading(true);
    setError(null);
    try {
      const result = await fetchSources(libraryId, controller.signal);
      if (!controller.signal.aborted) {
        setSources(result);
        setLoading(false);
      }
    } catch (err) {
      if (controller.signal.aborted) return;
      setError(err instanceof Error ? err.message : 'Unknown error.');
      setLoading(false);
    }
  }, [libraryId]);

  useEffect(() => {
    setSources([]);
    setError(null);
    if (libraryId) {
      void load();
    }
    return () => controllerRef.current?.abort();
  }, [load, libraryId]);

  const add = useCallback(
    async (name: string, rootPath: string) => {
      if (!libraryId) return;
      await addSource(libraryId, name, rootPath);
      await load();
    },
    [libraryId, load],
  );

  const remove = useCallback(
    async (sourceId: string) => {
      if (!libraryId) return;
      await removeSource(libraryId, sourceId);
      setSources((previous) => previous.filter((source) => source.id !== sourceId));
    },
    [libraryId],
  );

  return { sources, loading, error, reload: load, add, remove };
}
