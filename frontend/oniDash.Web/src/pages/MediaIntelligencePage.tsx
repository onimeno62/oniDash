import { useEffect, useMemo, useState } from 'react';
import { fetchActivityTimeline, fetchContinueMedia, fetchFavorites, fetchLocalRecommendations, fetchRecentlyAdded, fetchRecentlyPlayed, type DashboardActivity, type DashboardMediaItem } from '../api/dashboard';
import { LoadingState } from '../components/states/LoadingState';
import { ErrorState } from '../components/states/ErrorState';

const types = ['all', 'music', 'movie', 'anime', 'manga', 'book'];

type Dataset = { continueMedia: DashboardMediaItem[]; recentAdded: DashboardMediaItem[]; recentPlayed: DashboardMediaItem[]; favorites: DashboardMediaItem[]; recommendations: DashboardMediaItem[]; activity: DashboardActivity[] };

export function MediaIntelligencePage() {
  const [data, setData] = useState<Dataset | null>(null);
  const [type, setType] = useState('all');
  const [query, setQuery] = useState('');
  const [error, setError] = useState<string | null>(null);

  const load = () => {
    const c = new AbortController();
    Promise.all([fetchContinueMedia(12, c.signal), fetchRecentlyAdded(12, c.signal), fetchRecentlyPlayed(12, c.signal), fetchFavorites(12, c.signal), fetchLocalRecommendations(12, c.signal), fetchActivityTimeline(30, c.signal)])
      .then(([continueMedia, recentAdded, recentPlayed, favorites, recommendations, activity]) => setData({ continueMedia, recentAdded, recentPlayed, favorites, recommendations, activity }))
      .catch(e => { if (!c.signal.aborted) setError(e instanceof Error ? e.message : 'Could not load media intelligence.'); });
    return () => c.abort();
  };
  useEffect(() => load(), []);

  const filter = (items: DashboardMediaItem[]) => items.filter(item => (type === 'all' || item.mediaType.toLowerCase() === type) && (!query || item.title.toLowerCase().includes(query.toLowerCase())));
  const stats = useMemo(() => {
    if (!data) return { total: 0, active: 0, types: 0, events: 0 };
    const pool = [...data.continueMedia, ...data.recentAdded, ...data.recentPlayed, ...data.favorites];
    return { total: new Set(pool.map(x => x.id)).size, active: data.continueMedia.length, types: new Set(pool.map(x => x.mediaType)).size, events: data.activity.length };
  }, [data]);

  if (!data && !error) return <LoadingState label="Building media intelligence…" />;
  if (error && !data) return <ErrorState title="Media intelligence unavailable" message={error} onRetry={load} />;
  if (!data) return null;

  return <div className="space-y-7 pb-12">
    <header className="rounded-3xl border border-border bg-surface-elevated p-6 shadow-card sm:p-8"><div className="flex flex-col gap-5 xl:flex-row xl:items-end xl:justify-between"><div><p className="text-xs font-semibold uppercase tracking-[.22em] text-accent">Phase 6 · Media intelligence</p><h1 className="mt-2 text-3xl font-bold tracking-tight sm:text-4xl">One library. One view.</h1><p className="mt-2 max-w-2xl text-sm leading-6 text-secondary">Understand what you are consuming, what is waiting for you, and how your music, movies, anime, manga and books overlap.</p></div><div className="flex flex-wrap gap-2">{types.map(t => <button key={t} onClick={() => setType(t)} className={`rounded-full border px-3 py-1.5 text-xs font-medium capitalize ${type === t ? 'border-accent bg-accent/10 text-accent' : 'border-border text-secondary hover:text-primary'}`}>{t}</button>)}</div></div><div className="mt-5"><input value={query} onChange={e => setQuery(e.target.value)} placeholder="Filter the intelligence view…" className="w-full rounded-xl border border-border bg-surface px-3 py-2.5 text-sm outline-none focus:border-accent/50" /></div></header>
    {error && <div className="rounded-xl border border-danger/30 bg-danger/5 px-4 py-3 text-sm text-danger">{error}</div>}
    <section className="grid grid-cols-2 gap-3 lg:grid-cols-4"><Stat label="Unique media surfaced" value={stats.total} /><Stat label="In progress" value={stats.active} /><Stat label="Media types" value={stats.types} /><Stat label="Recent events" value={stats.events} /></section>
    <section className="grid gap-5 xl:grid-cols-2"><Insight title="Continue" subtitle="Pick up where you left off." items={filter(data.continueMedia)} empty="Nothing currently in progress." /><Insight title="Recently added" subtitle="The newest additions across your collection." items={filter(data.recentAdded)} empty="No recent additions." /><Insight title="Recently active" subtitle="Your latest playback and reading activity." items={filter(data.recentPlayed)} empty="No recent activity." /><Insight title="Favorites" subtitle="Things you deliberately want close at hand." items={filter(data.favorites)} empty="No favorites yet." /></section>
    <section className="rounded-2xl border border-border bg-surface p-5"><div className="flex items-end justify-between gap-3"><div><h2 className="font-semibold">Local recommendations</h2><p className="mt-1 text-xs text-secondary">Suggestions derived from the media already in your library.</p></div><span className="text-xs text-tertiary">{filter(data.recommendations).length} shown</span></div><div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">{filter(data.recommendations).map(item => <MediaCard key={item.id} item={item} />)}</div></section>
    <section className="rounded-2xl border border-border bg-surface p-5"><h2 className="font-semibold">Activity timeline</h2><div className="mt-4 divide-y divide-border">{data.activity.map(event => <div key={event.id} className="flex items-center gap-4 py-3"><span className="size-2 shrink-0 rounded-full bg-accent" /><div className="min-w-0 flex-1"><p className="truncate text-sm font-medium">{event.title}</p><p className="text-xs text-secondary">{event.action} · {event.mediaType}</p></div><time className="shrink-0 text-xs text-tertiary">{new Date(event.occurredAtUtc).toLocaleDateString()}</time></div>)}</div>{!data.activity.length && <p className="mt-3 text-sm text-secondary">No activity recorded yet.</p>}</section>
  </div>;
}
function Stat({ label, value }: { label: string; value: number }) { return <div className="rounded-2xl border border-border bg-surface p-4 shadow-card"><p className="text-xs text-muted">{label}</p><p className="mt-1 text-2xl font-bold tracking-tight">{value}</p></div>; }
function Insight({ title, subtitle, items, empty }: { title: string; subtitle: string; items: DashboardMediaItem[]; empty: string }) { return <section className="rounded-2xl border border-border bg-surface p-5"><div className="flex items-start justify-between"><div><h2 className="font-semibold">{title}</h2><p className="mt-1 text-xs text-secondary">{subtitle}</p></div><span className="rounded-full bg-surface-elevated px-2 py-1 text-[10px] font-semibold text-tertiary">{items.length}</span></div><div className="mt-4 space-y-2">{items.slice(0, 6).map(item => <MediaCard key={item.id} item={item} />)}</div>{!items.length && <p className="mt-4 text-sm text-secondary">{empty}</p>}</section>; }
function MediaCard({ item }: { item: DashboardMediaItem }) { return <article className="flex items-center gap-3 rounded-xl border border-border bg-surface-elevated p-3"><div className="grid size-10 shrink-0 place-items-center rounded-lg bg-accent/10 text-[10px] font-bold uppercase text-accent">{item.mediaType.slice(0, 2)}</div><div className="min-w-0 flex-1"><p className="truncate text-sm font-medium text-primary">{item.title}</p><p className="mt-0.5 text-[11px] capitalize text-secondary">{item.mediaType}</p></div><span className="text-[10px] text-tertiary">{new Date(item.timestamp).toLocaleDateString()}</span></article>; }
