export type MangaStatus = 'reading' | 'completed' | 'on_hold' | 'plan_to_read' | 'dropped';

export type MangaSummary = {
  id: string;
  title: string;
  altTitle?: string;
  author?: string;
  artist?: string;
  description?: string;
  coverUrl?: string;
  sourceId?: string;
  sourceName?: string;
  genres?: string[];
  status?: MangaStatus;
  chapterCount?: number;
  unreadCount?: number;
  readCount?: number;
  lastReadAtUtc?: string;
  latestChapter?: string;
  rating?: number;
  favorite?: boolean;
  inLibrary?: boolean;
  downloadCount?: number;
  progressPercent?: number;
  year?: number;
};

export type MangaCategory = { id: string; name: string; count: number; color?: string };
export type MangaSource = { id: string; name: string; language?: string; installed: boolean; enabled: boolean; mangaCount?: number; iconUrl?: string };
export type MangaUpdate = { id: string; mangaId: string; mangaTitle: string; coverUrl?: string; chapter: string; publishedAtUtc?: string; sourceName?: string; downloaded?: boolean; read?: boolean };

export type MangaNotification = {
  id: string;
  mangaId: string;
  mangaTitle: string;
  chapterTitle: string;
  chapterNumber: number;
  read: boolean;
  createdAtUtc: string;
};

export type MangaTracker = {
  id: string;
  mangaId: string;
  trackerName: string;
  externalTrackingId: string;
  lastSyncedChapter: number;
  status: string;
  score: number;
  lastSyncedAtUtc: string;
};

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api/manga${path}`, { ...init, headers: { Accept: 'application/json', ...(init?.headers ?? {}) } });
  if (!response.ok) throw new Error((await response.text()) || `Manga request failed (${response.status}).`);
  return response.json() as Promise<T>;
}

export function fetchManga(signal?: AbortSignal) { return request<MangaSummary[]>('/library?limit=500', { signal }); }
export function fetchContinueReading(signal?: AbortSignal) { return request<MangaSummary[]>('/continue-reading?limit=12', { signal }); }
export function fetchUpdates(signal?: AbortSignal) { return request<MangaUpdate[]>('/updates?limit=50', { signal }); }
export function fetchCategories(signal?: AbortSignal) { return request<MangaCategory[]>('/categories', { signal }); }
export function fetchSources(signal?: AbortSignal) { return request<MangaSource[]>('/sources', { signal }); }
export function fetchPopular(signal?: AbortSignal) { return request<MangaSummary[]>('/browse/popular?limit=24', { signal }); }
export function searchManga(query: string, signal?: AbortSignal) { return request<MangaSummary[]>(`/search?q=${encodeURIComponent(query)}&limit=50`, { signal }); }
export function setFavorite(id: string, favorite: boolean) { return request<void>(`/${encodeURIComponent(id)}/favorite`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ favorite }) }); }
export function setStatus(id: string, status: MangaStatus) { return request<void>(`/${encodeURIComponent(id)}/status`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ status }) }); }
export function updateProgress(id: string, chapter: string) { return request<void>(`/${encodeURIComponent(id)}/progress`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ chapter }) }); }
export function downloadChapters(id: string, chapters: string[]) { return request<void>(`/${encodeURIComponent(id)}/download`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ chapters }) }); }
export function updateLibrary() { return request<{ updated: boolean; newChaptersFound: number }>('/update', { method: 'POST' }); }
export function installSource(sourceId: string) { return request<void>('/sources/install', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ sourceId }) }); }

export function fetchNotifications(unreadOnly = false, signal?: AbortSignal) {
  return request<MangaNotification[]>(`/notifications?unreadOnly=${unreadOnly}`, { signal });
}

export function markNotificationRead(id: string) {
  return request<void>(`/notifications/${encodeURIComponent(id)}/read`, { method: 'POST' });
}

export function fetchTrackers(mangaId: string, signal?: AbortSignal) {
  return request<MangaTracker[]>(`/${encodeURIComponent(mangaId)}/trackers`, { signal });
}

export function saveTracker(mangaId: string, trackerName: string, externalTrackingId: string, lastSyncedChapter: number, status = 'reading', score = 0) {
  return request<MangaTracker>(`/${encodeURIComponent(mangaId)}/trackers`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ trackerName, externalTrackingId, lastSyncedChapter, status, score })
  });
}

export function syncAllTrackers() {
  return request<{ synced: number }>('/sync', { method: 'POST' });
}
