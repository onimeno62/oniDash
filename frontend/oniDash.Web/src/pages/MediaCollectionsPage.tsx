import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { fetchLibraries, type Library } from '../api/libraries';
import { fetchMusicAlbums, fetchMusicArtists, albumCoverUrl, type AlbumSummary, type ArtistSummary } from '../api/music';
import { fetchMovies, moviePosterUrl, type MovieSummary } from '../api/movies';
import { EmptyState } from '../components/states/EmptyState';
import { LoadingState } from '../components/states/LoadingState';

type Kind = 'albums' | 'artists' | 'movies';
export function MediaCollectionsPage({ kind }: { kind: Kind }) {
  const navigate = useNavigate();
  const [library, setLibrary] = useState<Library | null>(null);
  const [items, setItems] = useState<Array<AlbumSummary | ArtistSummary | MovieSummary>>([]);
  const [query, setQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  useEffect(() => { const c = new AbortController(); fetchLibraries(c.signal).then(async ls => { const lib = ls[0]; setLibrary(lib ?? null); if (!lib) return; if (kind === 'albums') setItems(await fetchMusicAlbums(lib.id, { limit: 2000 })); else if (kind === 'artists') setItems(await fetchMusicArtists(lib.id, { limit: 2000 })); else setItems(await fetchMovies(lib.id, { limit: 2000 })); }).catch(e => { if (!c.signal.aborted) setError(e instanceof Error ? e.message : 'Could not load collection.'); }).finally(() => { if (!c.signal.aborted) setLoading(false); }); return () => c.abort(); }, [kind]);
  const filtered = useMemo(() => { const q = query.trim().toLowerCase(); return q ? items.filter(x => ('title' in x ? x.title : 'name' in x ? x.name : x.title).toLowerCase().includes(q)) : items; }, [items, query]);
  if (loading) return <LoadingState label="Loading collection…" />;
  if (error || !library) return <EmptyState title="Collection unavailable" description={error ?? 'No media library is configured.'} />;
  const title = kind === 'albums' ? 'Albums' : kind === 'artists' ? 'Artists' : 'Movies';
  return <div className="space-y-6 pb-10"><header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between"><div><p className="text-xs font-semibold uppercase tracking-[.2em] text-accent">Library</p><h1 className="mt-1 text-3xl font-bold tracking-tight">{title}</h1><p className="mt-1 text-sm text-secondary">{filtered.length} items</p></div><input value={query} onChange={e => setQuery(e.target.value)} placeholder={`Search ${title.toLowerCase()}…`} className="rounded-xl border border-border bg-surface px-3 py-2.5 text-sm sm:w-72" /></header>{filtered.length === 0 ? <EmptyState title="Nothing found" description="Try a different search." /> : <div className={kind === 'artists' ? 'grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4' : 'grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5 xl:grid-cols-6'}>{filtered.map(item => { const itemTitle = 'title' in item ? item.title : item.name; const href = kind === 'albums' ? `/media/album/${item.id}` : kind === 'artists' ? `/media/artist/${item.id}` : `/media/movie/${item.id}`; const image = kind === 'albums' && (item as AlbumSummary).hasCover ? albumCoverUrl(item.id) : kind === 'movies' && (item as MovieSummary).hasPoster ? moviePosterUrl(item.id) : undefined; return <button key={item.id} type="button" onClick={() => navigate(href)} className="group overflow-hidden rounded-2xl border border-border bg-surface text-left shadow-card transition hover:-translate-y-0.5 hover:border-accent/40">{image ? <img src={image} alt="" loading="lazy" className="aspect-square w-full object-cover transition group-hover:scale-[1.02]" /> : <div className="grid aspect-square place-items-center bg-surface-elevated text-3xl text-accent">{kind === 'artists' ? '◉' : kind === 'movies' ? '🎬' : '♫'}</div>}<div className="p-3"><p className="truncate text-sm font-semibold">{itemTitle}</p><p className="mt-1 truncate text-xs text-secondary">{'artistName' in item ? item.artistName ?? 'Unknown artist' : 'year' in item ? String(item.year ?? 'Year unknown') : 'Artist collection'}</p></div></button>; })}</div>}</div>;
}
