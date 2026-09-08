import { ApiError, apiDelete, apiFetch } from './client';

/** A library container (mirrors oniDash.Application.Libraries.LibraryDto). */
export interface Library {
  id: string;
  name: string;
  createdAtUtc: string;
}

/** A configured local folder (mirrors SourceDto). */
export interface LibrarySource {
  id: string;
  libraryId: string;
  name: string;
  rootPath: string;
  createdAtUtc: string;
}

/** Standard paged result (mirrors PagedResult<T>). */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

function jsonInit(method: 'POST' | 'PUT', body: unknown): RequestInit {
  return {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  };
}

/* ---------------------------------- Libraries --------------------------------- */

export function fetchLibraries(signal?: AbortSignal): Promise<Library[]> {
  return apiFetch<Library[]>('/libraries', { signal });
}

export function fetchLibrary(id: string, signal?: AbortSignal): Promise<Library> {
  return apiFetch<Library>(`/libraries/${id}`, { signal });
}

export function createLibrary(name: string): Promise<Library> {
  return apiFetch<Library>('/libraries', jsonInit('POST', { name }));
}

export function renameLibrary(id: string, name: string): Promise<Library> {
  return apiFetch<Library>(`/libraries/${id}`, jsonInit('PUT', { name }));
}

export function deleteLibrary(id: string): Promise<void> {
  return apiDelete(`/libraries/${id}`);
}

/* ----------------------------------- Sources ---------------------------------- */

export function fetchSources(libraryId: string, signal?: AbortSignal): Promise<LibrarySource[]> {
  return apiFetch<LibrarySource[]>(`/libraries/${libraryId}/sources`, { signal });
}

export function addSource(libraryId: string, name: string, rootPath: string): Promise<LibrarySource> {
  return apiFetch<LibrarySource>(
    `/libraries/${libraryId}/sources`,
    jsonInit('POST', { name, rootPath }),
  );
}

export function removeSource(libraryId: string, sourceId: string): Promise<void> {
  return apiDelete(`/libraries/${libraryId}/sources/${sourceId}`);
}

/* -------------------------------- Media items --------------------------------- */

/** A media item (mirrors MediaItemDto). Items are created by the scanner. */
export interface MediaItemSummary {
  id: string;
  displayName: string;
  createdAtUtc: string;
}

export function fetchLibraryItems(
  libraryId: string,
  page = 1,
  pageSize = 50,
  signal?: AbortSignal,
): Promise<PagedResult<MediaItemSummary>> {
  return apiFetch<PagedResult<MediaItemSummary>>(
    `/libraries/${libraryId}/items?page=${page}&pageSize=${pageSize}`,
    { signal },
  );
}

/** Convenience guard so callers can branch on conflict errors specifically. */
export function isConflict(error: unknown): error is ApiError {
  return error instanceof ApiError && error.status === 409;
}
