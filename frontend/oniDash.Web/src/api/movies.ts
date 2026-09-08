import { apiFetch } from './client';

export interface MovieSummary {
  id: string;
  mediaItemId: string;
  title: string;
  year: number | null;
  durationSeconds: number | null;
  container: string | null;
  hasPoster: boolean;
  watched: boolean;
  watchProgressSeconds: number | null;
  watchedAtUtc: string | null;
}

export function fetchMovies(
  libraryId: string,
  options: { watched?: boolean; limit?: number; offset?: number; signal?: AbortSignal } = {},
): Promise<MovieSummary[]> {
  const params = new URLSearchParams({ libraryId });
  if (options.watched !== undefined) params.set('watched', String(options.watched));
  if (options.limit !== undefined) params.set('limit', String(options.limit));
  if (options.offset !== undefined) params.set('offset', String(options.offset));
  return apiFetch<MovieSummary[]>(`/movies?${params.toString()}`, { signal: options.signal });
}

export function fetchContinueWatching(libraryId: string, signal?: AbortSignal): Promise<MovieSummary[]> {
  return apiFetch<MovieSummary[]>(`/movies/continue?libraryId=${encodeURIComponent(libraryId)}`, { signal });
}

export function moviePosterUrl(movieId: string): string {
  return `/api/movies/${movieId}/poster`;
}

export function movieStreamUrl(movieId: string): string {
  return `/api/movies/${movieId}/stream`;
}

export function saveWatchProgress(movieId: string, positionSeconds: number): Promise<MovieSummary> {
  return apiFetch<MovieSummary>(`/movies/${movieId}/progress`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ positionSeconds }),
  });
}

export function setMovieWatched(movieId: string, watched: boolean): Promise<MovieSummary> {
  return apiFetch<MovieSummary>(`/movies/${movieId}/watched`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ watched }),
  });
}

export function reindexMovies(libraryId?: string): Promise<{ indexedMovies: number }> {
  return apiFetch<{ indexedMovies: number }>(
    libraryId ? `/movies/reindex?libraryId=${encodeURIComponent(libraryId)}` : '/movies/reindex',
    { method: 'POST' },
  );
}

export function formatRuntime(seconds: number | null): string {
  if (seconds === null || !Number.isFinite(seconds) || seconds <= 0) return 'Unknown runtime';
  const total = Math.round(seconds);
  const hours = Math.floor(total / 3600);
  const minutes = Math.round((total % 3600) / 60);
  return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;
}

export function formatProgressPercent(progressSeconds: number | null, durationSeconds: number | null): number {
  if (progressSeconds === null || durationSeconds === null || durationSeconds <= 0) return 0;
  return Math.min(100, Math.max(1, Math.round((progressSeconds / durationSeconds) * 100)));
}
