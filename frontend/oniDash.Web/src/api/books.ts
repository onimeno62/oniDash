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

export interface BookBookmark {
  id: string;
  pageNumber: number;
  title: string | null;
  note: string | null;
  createdAtUtc: string;
}

export interface BookAuthorGroup {
  name: string;
  bookCount: number;
}

export interface BookSeriesGroup {
  title: string;
  bookCount: number;
}

export interface BookHealthReport {
  totalBooks: number;
  missingCovers: number;
  missingFiles: number;
  missingMetadata: number;
  generatedAtUtc: string;
}

export interface BookManifest {
  format: string;
  totalPages: number;
  title: string | null;
  tableOfContents: Array<{ title: string; target: string; pageNumber: number | null }>;
}

export function fetchBooks(libraryId: string, options: { read?: boolean; author?: string; series?: string; limit?: number; signal?: AbortSignal } = {}): Promise<BookSummary[]> {
  const params = new URLSearchParams({ libraryId });
  if (options.read !== undefined) params.set('read', String(options.read));
  if (options.author) params.set('author', options.author);
  if (options.series) params.set('series', options.series);
  if (options.limit !== undefined) params.set('limit', String(options.limit));
  return apiFetch<BookSummary[]>(`/books?${params.toString()}`, { signal: options.signal });
}

export function fetchContinueReading(libraryId: string, signal?: AbortSignal): Promise<BookSummary[]> {
  return apiFetch<BookSummary[]>(`/books/continue?libraryId=${encodeURIComponent(libraryId)}`, { signal });
}

export function fetchBookAuthors(libraryId: string, signal?: AbortSignal): Promise<BookAuthorGroup[]> {
  return apiFetch<BookAuthorGroup[]>(`/books/authors?libraryId=${encodeURIComponent(libraryId)}`, { signal });
}

export function fetchBookSeries(libraryId: string, signal?: AbortSignal): Promise<BookSeriesGroup[]> {
  return apiFetch<BookSeriesGroup[]>(`/books/series?libraryId=${encodeURIComponent(libraryId)}`, { signal });
}

export function fetchBookHealth(libraryId?: string, signal?: AbortSignal): Promise<BookHealthReport> {
  const p = libraryId ? `?libraryId=${encodeURIComponent(libraryId)}` : '';
  return apiFetch<BookHealthReport>(`/books/health${p}`, { signal });
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

export function fetchBookManifest(id: string): Promise<BookManifest> {
  return apiFetch<BookManifest>(`/books/${id}/manifest`);
}

export function fetchBookBookmarks(id: string): Promise<BookBookmark[]> {
  return apiFetch<BookBookmark[]>(`/books/${id}/bookmarks`);
}

export function createBookBookmark(id: string, pageNumber: number, title?: string, note?: string): Promise<BookBookmark> {
  return apiFetch<BookBookmark>(`/books/${id}/bookmarks`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ pageNumber, title, note })
  });
}

export function deleteBookBookmark(id: string, bookmarkId: string): Promise<void> {
  return apiFetch<void>(`/books/${id}/bookmarks/${bookmarkId}`, { method: 'DELETE' });
}

export function bookCoverUrl(id: string): string { return `/api/books/${id}/cover`; }
export function bookPageUrl(id: string, pageNumber: number): string { return `/api/books/${id}/pages/${pageNumber}`; }
export function bookStreamUrl(id: string): string { return `/api/books/${id}/stream`; }
