import { apiFetch } from './client';

export interface BookSummary {
  id: string;
  mediaItemId: string;
  title: string;
  author: string | null;
  year: number | null;
  pageCount: number | null;
  format: string | null;
  hasCover: boolean;
  read: boolean;
  progressPages: number | null;
  progressPercent: number | null;
  lastReadAtUtc: string | null;
  rating: number | null;
  favorite: boolean;
  series: string | null;
}

export interface BookStats {
  total: number;
  read: number;
  unread: number;
  pages: number;
}

export function fetchBooks(libraryId: string, options: { read?: boolean; limit?: number; signal?: AbortSignal } = {}): Promise<BookSummary[]> {
  const params = new URLSearchParams({ libraryId });
  if (options.read !== undefined) params.set('read', String(options.read));
  if (options.limit !== undefined) params.set('limit', String(options.limit));
  return apiFetch<BookSummary[]>(`/books?${params.toString()}`, { signal: options.signal });
}

export function fetchContinueReading(libraryId: string, signal?: AbortSignal): Promise<BookSummary[]> {
  return apiFetch<BookSummary[]>(`/books/continue?libraryId=${encodeURIComponent(libraryId)}`, { signal });
}

export function setBookRead(id: string, read: boolean): Promise<BookSummary> {
  return apiFetch<BookSummary>(`/books/${id}/read`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ read }) });
}

export function setBookFavorite(id: string, favorite: boolean): Promise<BookSummary> {
  return apiFetch<BookSummary>(`/books/${id}/favorite`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ favorite }) });
}

export function setBookProgress(id: string, progressPages: number): Promise<BookSummary> {
  return apiFetch<BookSummary>(`/books/${id}/progress`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ progressPages }) });
}

export function setBookRating(id: string, rating: number): Promise<BookSummary> {
  return apiFetch<BookSummary>(`/books/${id}/rating`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ rating }) });
}

export function reindexBooks(libraryId?: string): Promise<{ indexedBooks: number }> {
  return apiFetch<{ indexedBooks: number }>(libraryId ? `/books/reindex?libraryId=${encodeURIComponent(libraryId)}` : '/books/reindex', { method: 'POST' });
}

export function bookCoverUrl(id: string): string { return `/api/books/${id}/cover`; }
