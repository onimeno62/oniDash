import { apiFetch } from './client';

/** One ranked hit (mirrors oniDash.Application.Search.SearchResult). */
export interface SearchResult {
  itemId: string;
  displayName: string;
  libraryId: string;
  libraryName: string;
}

/**
 * Global full-text search across all indexed media items. All terms must match
 * (prefix matching), ranked most-relevant-first. A blank query returns [].
 */
export function searchMedia(
  query: string,
  options: { libraryId?: string; limit?: number; signal?: AbortSignal } = {},
): Promise<SearchResult[]> {
  const params = new URLSearchParams({ q: query });
  if (options.libraryId) {
    params.set('libraryId', options.libraryId);
  }
  if (options.limit !== undefined) {
    params.set('limit', String(options.limit));
  }
  return apiFetch<SearchResult[]>(`/search?${params.toString()}`, { signal: options.signal });
}

/** Rebuilds the search index from persisted items (recovery command). */
export function reindexSearch(): Promise<{ indexedItems: number }> {
  return apiFetch<{ indexedItems: number }>('/search/reindex', { method: 'POST' });
}
