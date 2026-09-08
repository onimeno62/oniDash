import { apiFetch } from './client';
export interface MusicOverview { tracks: number; albums: number; artists: number; playlists: number; favorites: number; listeningSeconds: number; }
export interface MusicHealth { database: boolean; tracks: number; lastUpdatedUtc: string | null; }
export function fetchMusicOverview(signal?: AbortSignal): Promise<MusicOverview> { return apiFetch<MusicOverview>('/music/statistics/overview', { signal }); }
export function fetchMusicHealth(signal?: AbortSignal): Promise<MusicHealth> { return apiFetch<MusicHealth>('/music/health', { signal }); }
export function fetchMusicTopTracks(limit = 20, signal?: AbortSignal) { return apiFetch<Array<{ trackId: string; plays: number; playedSeconds: number }>>(`/music/statistics/top-tracks?limit=${limit}`, { signal }); }
export function fetchMusicGenres(signal?: AbortSignal) { return apiFetch<Array<{ genre: string; tracks: number }>>('/music/statistics/genres', { signal }); }
