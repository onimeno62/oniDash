import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { PageHeader } from '../components/PageHeader';
import { EmptyState } from '../components/states/EmptyState';
import { ErrorState } from '../components/states/ErrorState';
import { LibraryIcon, SearchIcon } from '../components/icons';
import { fetchLibraries } from '../api/libraries';
import { useSearch } from '../hooks/useSearch';

/**
 * Global search across every indexed library: live results as you type, ranked by
 * relevance (SQLite FTS5 + bm25), filterable to a single library.
 */
export function SearchPage() {
  const [searchParams] = useSearchParams();
  const [libraryId, setLibraryId] = useState(searchParams.get('library') ?? '');
  const { query, setQuery, results, searching, error } = useSearch(libraryId || undefined);

  return (
    <div className="space-y-8">
      <PageHeader
        title="Search"
        subtitle="Global search across every library — indexed media, ranked by relevance."
      />

      <form role="search" onSubmit={(event) => event.preventDefault()} className="mx-auto w-full max-w-2xl space-y-3">
        <label htmlFor="library-search" className="sr-only">
          Search query
        </label>
        <div className="relative">
          <SearchIcon className="pointer-events-none absolute left-4 top-1/2 size-5 -translate-y-1/2 text-muted" />
          <input
            id="library-search"
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search your library…"
            className="w-full rounded-2xl border border-border bg-surface py-3.5 pl-12 pr-4 text-[15px] text-primary shadow-card placeholder:text-muted focus:border-accent focus:outline-none"
          />
        </div>
        <LibraryFilter value={libraryId} onChange={setLibraryId} />
      </form>

      {error && (
        <ErrorState
          title="Search failed"
          message={error}
        />
      )}

      {!error && query.trim() === '' && (
        <EmptyState
          icon={<SearchIcon className="size-6" />}
          title="Type to search your library"
          description="Results appear as you type — every indexed media item, ranked most relevant first."
        />
      )}

      {!error && query.trim() !== '' && searching && results === null && (
        <p role="status" className="text-center text-sm text-secondary">
          Searching…
        </p>
      )}

      {!error && query.trim() !== '' && results !== null && results.length === 0 && (
        <EmptyState
          icon={<SearchIcon className="size-6" />}
          title="No results"
          description={`Nothing in your indexed libraries matches “${query.trim()}”.`}
        />
      )}

      {!error && results !== null && results.length > 0 && (
        <section aria-label="Search results" className="space-y-2">
          <p role="status" className="text-sm text-secondary" data-testid="result-count">
            {results.length} {results.length === 1 ? 'result' : 'results'}
          </p>
          <ul className="space-y-2">
            {results.map((result) => (
              <li
                key={result.itemId}
                className="flex items-center gap-3 rounded-xl border border-border bg-surface px-4 py-3 shadow-card"
              >
                <span
                  aria-hidden
                  className="grid size-9 shrink-0 place-items-center rounded-lg bg-accent-soft text-accent"
                >
                  <LibraryIcon className="size-4" />
                </span>
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-sm font-medium">{result.displayName}</span>
                  <span className="block truncate text-xs text-muted">{result.libraryName}</span>
                </span>
              </li>
            ))}
          </ul>
        </section>
      )}
    </div>
  );
}

function LibraryFilter({ value, onChange }: { value: string; onChange: (value: string) => void }) {
  const [libraries, setLibraries] = useState<{ id: string; name: string }[] | null>(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    const controller = new AbortController();
    fetchLibraries(controller.signal)
      .then((all) => {
        setLibraries(all);
        setFailed(false);
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setFailed(true);
        }
      });
    return () => controller.abort();
  }, []);

  return (
    <div className="flex items-center justify-center">
      <label htmlFor="library-filter" className="sr-only">
        Filter by library
      </label>
      <select
        id="library-filter"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="rounded-lg border border-border bg-surface px-3 py-2 text-sm text-secondary shadow-card focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
      >
        <option value="">All libraries</option>
        {(libraries ?? []).map((library) => (
          <option key={library.id} value={library.id}>
            {library.name}
          </option>
        ))}
      </select>
      {failed && (
        <span className="ml-3 text-xs text-muted">Library filter unavailable offline.</span>
      )}
    </div>
  );
}
