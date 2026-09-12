import { apiFetch } from './client';
export type SeriesSummary = { id: string; libraryId: string; title: string; year?: number; seasonCount: number; episodeCount: number };
export async function fetchSeries(libraryId?: string, signal?: AbortSignal): Promise<SeriesSummary[]> { const query = libraryId ? `?libraryId=${encodeURIComponent(libraryId)}` : ''; return apiFetch<SeriesSummary[]>(`/catalogue/series${query}`, { signal }); }
