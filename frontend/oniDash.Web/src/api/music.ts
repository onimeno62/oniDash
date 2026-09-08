import { apiFetch } from './client';

/** A music artist (mirrors oniDash.Music.Endpoints.MusicEndpoints.ArtistSummary). */
export interface ArtistSummary {
  id: string;
  name: string;
}

/** An album (mirrors AlbumSummary). Cover art is fetched separately by album id. */
export interface AlbumSummary {
  id: string;
  title: string;
  artistName: string | null;
  year: number | null;
  hasCover: boolean;
}

/** A playable track (mirrors TrackSummary). */
export interface TrackSummary {
  id: string;
  mediaItemId: string;
  title: string;
  artistName: string | null;
  albumTitle: string;
  albumId: string | null;
  hasCover: boolean;
  trackNumber: number | null;
  discNumber: number | null;
  year: number | null;
  durationSeconds: number | null;
  genre: string | null;
}

export function fetchMusicArtists(libraryId: string, signal?: AbortSignal): Promise<ArtistSummary[]> {
  return apiFetch<ArtistSummary[]>(`/music/artists?libraryId=${libraryId}`, { signal });
}

export function fetchMusicAlbums(
  libraryId: string,
  options: { artistId?: string; signal?: AbortSignal } = {},
): Promise<AlbumSummary[]> {
  const suffix = options.artistId ? `&artistId=${options.artistId}` : '';
  return apiFetch<AlbumSummary[]>(`/music/albums?libraryId=${libraryId}${suffix}`, { signal: options.signal });
}

export function fetchMusicTracks(
  libraryId: string,
  options: { albumId?: string; artistId?: string; signal?: AbortSignal } = {},
): Promise<TrackSummary[]> {
  const params = new URLSearchParams({ libraryId });
  if (options.albumId) {
    params.set('albumId', options.albumId);
  }
  if (options.artistId) {
    params.set('artistId', options.artistId);
  }
  return apiFetch<TrackSummary[]>(`/music/tracks?${params.toString()}`, { signal: options.signal });
}

/** URL of the embedded cover art for an album (or <img src>). */
export function albumCoverUrl(albumId: string): string {
  return `/api/music/albums/${albumId}/cover`;
}

/** URL of the ranged audio stream for a track (used as <audio src>). */
export function trackStreamUrl(trackId: string): string {
  return `/api/music/tracks/${trackId}/stream`;
}

/** Rebuilds the music catalogue from indexed audio files (recovery command). */
export function reindexMusic(libraryId?: string): Promise<{ indexedTracks: number }> {
  return apiFetch<{ indexedTracks: number }>(
    libraryId ? `/music/reindex?libraryId=${libraryId}` : '/music/reindex',
    { method: 'POST' },
  );
}

export function formatDuration(seconds: number | null): string {
  if (seconds === null || !Number.isFinite(seconds) || seconds < 0) {
    return '–:––';
  }

  const total = Math.round(seconds);
  const minutes = Math.floor(total / 60);
  const rest = total % 60;
  return `${minutes}:${String(rest).padStart(2, '0')}`;
}
