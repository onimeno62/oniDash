import { apiFetch } from './client';

export interface DashboardMediaItem {
  id: string;
  title: string;
  mediaType: string;
  extension: string | null;
  hasArtwork: boolean;
  timestamp: string;
  isFavorite: boolean;
}

export interface DashboardActivity {
  id: string;
  mediaItemId: string;
  title: string;
  mediaType: string;
  action: string;
  occurredAtUtc: string;
}

export function fetchContinueMedia(limit?: number, signal?: AbortSignal): Promise<DashboardMediaItem[]> {
  const p = limit ? `?limit=${limit}` : '';
  return apiFetch<DashboardMediaItem[]>(`/dashboard/continue${p}`, { signal });
}

export function fetchRecentlyAdded(limit?: number, signal?: AbortSignal): Promise<DashboardMediaItem[]> {
  const p = limit ? `?limit=${limit}` : '';
  return apiFetch<DashboardMediaItem[]>(`/dashboard/recently-added${p}`, { signal });
}

export function fetchRecentlyPlayed(limit?: number, signal?: AbortSignal): Promise<DashboardMediaItem[]> {
  const p = limit ? `?limit=${limit}` : '';
  return apiFetch<DashboardMediaItem[]>(`/dashboard/recently-played${p}`, { signal });
}

export function fetchFavorites(limit?: number, signal?: AbortSignal): Promise<DashboardMediaItem[]> {
  const p = limit ? `?limit=${limit}` : '';
  return apiFetch<DashboardMediaItem[]>(`/dashboard/favorites${p}`, { signal });
}

export function fetchActivityTimeline(limit?: number, signal?: AbortSignal): Promise<DashboardActivity[]> {
  const p = limit ? `?limit=${limit}` : '';
  return apiFetch<DashboardActivity[]>(`/dashboard/activity${p}`, { signal });
}

export function fetchLocalRecommendations(limit?: number, signal?: AbortSignal): Promise<DashboardMediaItem[]> {
  const p = limit ? `?limit=${limit}` : '';
  return apiFetch<DashboardMediaItem[]>(`/dashboard/recommendations${p}`, { signal });
}
