import { apiFetch } from './client';

export interface ArtistSummary { id: string; name: string; }
export interface AlbumSummary { id: string; title: string; artistName: string | null; year: number | null; hasCover: boolean; }
export interface TrackSummary {
  id: string; mediaItemId: string; title: string; artistName: string | null; albumTitle: string;
  albumId: string | null; hasCover: boolean; trackNumber: number | null; discNumber: number | null;
  year: number | null; durationSeconds: number | null; genre: string | null;
}

type PageOptions = { limit?: number; offset?: number; signal?: AbortSignal };

function pageParams(libraryId: string, options: PageOptions): URLSearchParams {
  const params = new URLSearchParams({ libraryId });
  if (options.limit !== undefined) params.set('limit', String(options.limit));
  if (options.offset !== undefined) params.set('offset', String(options.offset));
  return params;
}

export function fetchMusicArtists(libraryId: string, options: PageOptions = {}): Promise<ArtistSummary[]> {
  return apiFetch<ArtistSummary[]>(`/music/artists?${pageParams(libraryId, options)}`, { signal: options.signal });
}

export function fetchMusicAlbums(libraryId: string, options: PageOptions & { artistId?: string } = {}): Promise<AlbumSummary[]> {
  const params = pageParams(libraryId, options);
  if (options.artistId) params.set('artistId', options.artistId);
  return apiFetch<AlbumSummary[]>(`/music/albums?${params}`, { signal: options.signal });
}

export function fetchMusicTracks(libraryId: string, options: PageOptions & { albumId?: string; artistId?: string } = {}): Promise<TrackSummary[]> {
  const params = pageParams(libraryId, options);
  if (options.albumId) params.set('albumId', options.albumId);
  if (options.artistId) params.set('artistId', options.artistId);
  return apiFetch<TrackSummary[]>(`/music/tracks?${params}`, { signal: options.signal });
}

export function albumCoverUrl(albumId: string): string { return `/api/music/albums/${albumId}/cover`; }
export function trackStreamUrl(trackId: string): string { return `/api/music/tracks/${trackId}/stream`; }

export function reindexMusic(libraryId?: string): Promise<{ indexedTracks: number }> {
  return apiFetch<{ indexedTracks: number }>(libraryId ? `/music/reindex?libraryId=${encodeURIComponent(libraryId)}` : '/music/reindex', { method: 'POST' });
}

export function formatDuration(seconds: number | null): string {
  if (seconds === null || !Number.isFinite(seconds) || seconds < 0) return '–:––';
  const total = Math.round(seconds);
  return `${Math.floor(total / 60)}:${String(total % 60).padStart(2, '0')}`;
}
