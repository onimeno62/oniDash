import { apiFetch } from './client';
export interface ArtistSummary { id: string; name: string; }
export interface AlbumSummary { id: string; title: string; artistName: string | null; year: number | null; hasCover: boolean; }
export interface TrackSummary { id: string; mediaItemId: string; title: string; artistName: string | null; albumTitle: string; albumId: string | null; hasCover: boolean; trackNumber: number | null; discNumber: number | null; year: number | null; durationSeconds: number | null; genre: string | null; }
export interface MusicMetadata { title: string | null; trackArtist: string | null; albumArtist: string | null; album: string | null; trackNumber: number | null; discNumber: number | null; year: number | null; durationSeconds: number | null; genre: string | null; }
export function fetchMusicMetadata(trackId: string, signal?: AbortSignal): Promise<MusicMetadata> { return apiFetch<MusicMetadata>(`/music/tracks/${encodeURIComponent(trackId)}/metadata`, { signal }); }
export function updateMusicMetadata(trackId: string, update: Partial<Omit<MusicMetadata, 'durationSeconds'>> & { confirmed: true }): Promise<void> { return apiFetch<void>(`/music/tracks/${encodeURIComponent(trackId)}/metadata`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(update) }); }
export function albumCoverUrl(albumId: string): string { return `/api/music/albums/${albumId}/cover`; }
export function trackStreamUrl(trackId: string): string { return `/api/music/tracks/${trackId}/stream`; }
