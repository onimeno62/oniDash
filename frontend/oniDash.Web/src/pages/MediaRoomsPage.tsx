import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { fetchContinueMedia, fetchFavorites, fetchRecentlyAdded, fetchLocalRecommendations, type DashboardMediaItem } from '../api/dashboard';
import { LoadingState } from '../components/states/LoadingState';
import { EmptyState } from '../components/states/EmptyState';

type Room = { kind: string; title: string; description: string; href: string; icon: string; detail: string };
const rooms: Room[] = [
  { kind: 'music', title: 'Music', description: 'Albums, artists, lyrics, queue and deep library curation.', href: '/music', icon: '♪', detail: 'Listen & curate' },
  { kind: 'movies', title: 'Movies', description: 'A focused cinema shelf with progress, posters and playback.', href: '/movies', icon: '▣', detail: 'Watch' },
  { kind: 'anime', title: 'Anime', description: 'Keep series, episodes and your watch state in one place.', href: '/search?type=anime', icon: '◇', detail: 'Explore' },
  { kind: 'manga', title: 'Manga', description: 'Continue reading, follow updates and manage sources.', href: '/manga', icon: '▤', detail: 'Read' },
  { kind: 'books', title: 'Books', description: 'Reading progress, ratings, favorites and formats.', href: '/books', icon: '▥', detail: 'Read' },
];

export function MediaRoomsPage() {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [continueMedia, setContinueMedia] = useState<DashboardMediaItem[]>([]);
  const [recent, setRecent] = useState<DashboardMediaItem[]>([]);
  const [favorites, setFavorites] = useState<DashboardMediaItem[]>([]);
  const [recommendations, setRecommendations] = useState<DashboardMediaItem[]>([]);

  useEffect(() => {
    const c = new AbortController();
    Promise.all([fetchContinueMedia(8, c.signal), fetchRecentlyAdded(8, c.signal), fetchFavorites(8, c.signal), fetchLocalRecommendations(8, c.signal)])
      .then(([cont, added, favs, recs]) => { setContinueMedia(cont); setRecent(added); setFavorites(favs); setRecommendations(recs); })
      .catch((e: unknown) => { if (!c.signal.aborted) setError(e instanceof Error ? e.message : 'Could not load media rooms.'); })
      .finally(() => { if (!c.signal.aborted) setLoading(false); });
    return () => c.abort();
  }, []);

  if (loading) return <LoadingState label="Loading your media rooms…" />;
  if (error) return <EmptyState title="Could not load media rooms" description={error} />;

  const count = new Set([...continueMedia, ...recent, ...favorites, ...recommendations].map(x => x.id)).size;
  return <div className="space-y-8 pb-12">
    <header className="relative overflow-hidden rounded-3xl border border-border bg-surface-elevated p-7 shadow-card sm:p-9">
      <div className="pointer-events-none absolute -right-24 -top-32 size-96 rounded-full bg-accent/15 blur-3xl" />
      <div className="relative max-w-3xl"><p className="text-xs font-semibold uppercase tracking-[0.24em] text-accent">oniDash media rooms</p><h1 className="mt-2 text-4xl font-bold tracking-tight sm:text-5xl">One library. Five ways to enjoy it.</h1><p className="mt-4 text-sm leading-6 text-secondary sm:text-base">Purpose-built spaces for listening, watching and reading, connected by the same local library intelligence.</p><div className="mt-6 flex flex-wrap gap-2"><Link to="/command" className="rounded-xl bg-accent px-4 py-2.5 text-sm font-semibold text-white">Open command center</Link><Link to="/intelligence" className="rounded-xl border border-border bg-surface px-4 py-2.5 text-sm font-semibold">View intelligence</Link></div></div>
    </header>

    <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">{rooms.map(room => <Link key={room.kind} to={room.href} className="group rounded-2xl border border-border bg-surface p-5 shadow-card transition hover:-translate-y-0.5 hover:shadow-pop"><div className="flex items-start justify-between"><span className="grid size-10 place-items-center rounded-xl bg-accent-soft text-xl text-accent">{room.icon}</span><span className="text-[10px] font-semibold uppercase tracking-wider text-muted">{room.detail}</span></div><h2 className="mt-5 text-lg font-semibold">{room.title}</h2><p className="mt-2 min-h-12 text-xs leading-5 text-secondary">{room.description}</p><span className="mt-4 inline-block text-xs font-semibold text-accent transition group-hover:translate-x-0.5">Enter room →</span></Link>)}</section>

    <section className="grid gap-4 md:grid-cols-4"><Metric label="In progress" value={continueMedia.length} /><Metric label="Recently added" value={recent.length} /><Metric label="Favorites" value={favorites.length} /><Metric label="Unique surfaced" value={count} /></section>
    <MediaStrip title="Continue" items={continueMedia} />
    <MediaStrip title="Recently added" items={recent} />
    <MediaStrip title="Favorites" items={favorites} />
    <MediaStrip title="Recommended locally" items={recommendations} />
  </div>;
}

function Metric({ label, value }: { label: string; value: number }) { return <div className="rounded-2xl border border-border bg-surface p-4 shadow-card"><p className="text-xs text-muted">{label}</p><p className="mt-1 text-2xl font-bold">{value}</p></div>; }
function MediaStrip({ title, items }: { title: string; items: DashboardMediaItem[] }) { return <section><div className="mb-3 flex items-center justify-between"><h2 className="text-lg font-semibold">{title}</h2><span className="text-xs text-muted">{items.length} items</span></div>{items.length ? <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 lg:grid-cols-8">{items.map(item => <Link key={item.id} to={`/media/${item.mediaType.toLowerCase()}/${item.id}`} className="rounded-xl border border-border bg-surface p-3 transition hover:border-accent"><span className="text-[9px] font-bold uppercase tracking-wider text-accent">{item.mediaType}</span><p className="mt-2 truncate text-sm font-semibold">{item.title}</p><p className="mt-1 text-[10px] text-muted">{new Date(item.timestamp).toLocaleDateString()}</p></Link>)}</div> : <p className="rounded-xl border border-dashed border-border p-5 text-sm text-secondary">Nothing here yet.</p>}</section>; }
