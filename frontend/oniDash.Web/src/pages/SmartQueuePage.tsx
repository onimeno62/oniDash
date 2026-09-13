import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { fetchContinueMedia, fetchFavorites, fetchLocalRecommendations, fetchRecentlyAdded, type DashboardMediaItem } from '../api/dashboard';
import { LoadingState } from '../components/states/LoadingState';
import { EmptyState } from '../components/states/EmptyState';

type Rule = 'continue' | 'favorites' | 'recent' | 'recommended';
const storageKey = 'onidash.smart-queue.v1';

export function SmartQueuePage() {
  const [continueMedia, setContinueMedia] = useState<DashboardMediaItem[]>([]);
  const [favorites, setFavorites] = useState<DashboardMediaItem[]>([]);
  const [recent, setRecent] = useState<DashboardMediaItem[]>([]);
  const [recommended, setRecommended] = useState<DashboardMediaItem[]>([]);
  const [queue, setQueue] = useState<DashboardMediaItem[]>([]);
  const [rule, setRule] = useState<Rule>('continue');
  const [limit, setLimit] = useState(12);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    try { const raw = localStorage.getItem(storageKey); if (raw) setQueue(JSON.parse(raw) as DashboardMediaItem[]); } catch { /* ignore malformed local state */ }
    const c = new AbortController();
    Promise.all([fetchContinueMedia(30, c.signal), fetchFavorites(30, c.signal), fetchRecentlyAdded(30, c.signal), fetchLocalRecommendations(30, c.signal)])
      .then(([cont, favs, added, recs]) => { setContinueMedia(cont); setFavorites(favs); setRecent(added); setRecommended(recs); })
      .catch((e: unknown) => { if (!c.signal.aborted) setError(e instanceof Error ? e.message : 'Could not load smart queue sources.'); })
      .finally(() => { if (!c.signal.aborted) setLoading(false); });
    return () => c.abort();
  }, []);

  const source = useMemo(() => ({ continue: continueMedia, favorites, recent, recommended }[rule]), [continueMedia, favorites, recent, recommended, rule]);
  const candidates = useMemo(() => source.slice(0, limit), [source, limit]);
  const persist = (next: DashboardMediaItem[]) => { setQueue(next); localStorage.setItem(storageKey, JSON.stringify(next)); };
  const addAll = () => { const existing = new Set(queue.map(x => x.id)); persist([...queue, ...candidates.filter(x => !existing.has(x.id))]); };
  const addOne = (item: DashboardMediaItem) => { if (!queue.some(x => x.id === item.id)) persist([...queue, item]); };
  const remove = (id: string) => persist(queue.filter(x => x.id !== id));
  const move = (index: number, direction: -1 | 1) => { const target = index + direction; if (target < 0 || target >= queue.length) return; const next = [...queue]; [next[index], next[target]] = [next[target], next[index]]; persist(next); };

  if (loading) return <LoadingState label="Building smart queue…" />;
  if (error) return <EmptyState title="Could not load smart queue" description={error} />;

  return <div className="space-y-7 pb-12">
    <header className="rounded-3xl border border-border bg-surface-elevated p-7 shadow-card sm:p-9"><p className="text-xs font-semibold uppercase tracking-[0.24em] text-accent">Playback automation</p><h1 className="mt-2 text-4xl font-bold tracking-tight">Smart queue</h1><p className="mt-3 max-w-2xl text-sm leading-6 text-secondary">Build a persistent, reorderable queue from the intelligence already in your local library. Your queue stays on this device until you clear it.</p></header>
    <section className="grid gap-6 lg:grid-cols-[1fr_1.25fr]">
      <div className="rounded-2xl border border-border bg-surface p-5 shadow-card"><div className="flex items-center justify-between"><div><h2 className="font-semibold">Queue recipe</h2><p className="mt-1 text-xs text-secondary">Choose a source, then add its candidates.</p></div><span className="rounded-full bg-accent-soft px-2 py-1 text-xs font-bold text-accent">{candidates.length} ready</span></div><div className="mt-5 grid gap-2">{([['continue','Continue'],['favorites','Favorites'],['recent','Recently added'],['recommended','Local recommendations']] as [Rule,string][]).map(([key,label]) => <button key={key} type="button" onClick={() => setRule(key)} className={`rounded-xl border p-3 text-left text-sm font-semibold transition ${rule === key ? 'border-accent bg-accent-soft text-accent' : 'border-border hover:bg-surface-elevated'}`}>{label}<span className="float-right text-xs font-normal text-muted">{{continue: continueMedia, favorites, recent, recommended}[key].length}</span></button>)}</div><div className="mt-4 flex items-center gap-2"><label htmlFor="queue-limit" className="text-xs text-muted">Limit</label><select id="queue-limit" value={limit} onChange={e => setLimit(Number(e.target.value))} className="rounded-lg border border-border bg-surface px-2 py-1.5 text-xs"><option value="6">6</option><option value="12">12</option><option value="20">20</option><option value="30">30</option></select><button type="button" onClick={addAll} disabled={!candidates.length} className="ml-auto rounded-lg bg-accent px-3 py-2 text-xs font-semibold text-white disabled:opacity-50">Add all</button></div></div>
      <div className="rounded-2xl border border-border bg-surface p-5 shadow-card"><div className="flex items-center justify-between"><div><h2 className="font-semibold">Current queue</h2><p className="mt-1 text-xs text-secondary">{queue.length} queued items</p></div><button type="button" onClick={() => persist([])} disabled={!queue.length} className="text-xs font-semibold text-danger disabled:opacity-40">Clear</button></div>{queue.length ? <div className="mt-4 divide-y divide-border">{queue.map((item, index) => <div key={`${item.id}-${index}`} className="flex items-center gap-3 py-3"><div className="grid size-8 shrink-0 place-items-center rounded-lg bg-surface-elevated text-xs font-bold text-muted">{index + 1}</div><div className="min-w-0 flex-1"><p className="truncate text-sm font-semibold">{item.title}</p><p className="text-[10px] uppercase tracking-wider text-muted">{item.mediaType}</p></div><div className="flex items-center gap-1"><button type="button" onClick={() => move(index, -1)} disabled={index === 0} className="rounded-md border border-border px-2 py-1 text-xs disabled:opacity-30" aria-label="Move up">↑</button><button type="button" onClick={() => move(index, 1)} disabled={index === queue.length - 1} className="rounded-md border border-border px-2 py-1 text-xs disabled:opacity-30" aria-label="Move down">↓</button><button type="button" onClick={() => remove(item.id)} className="rounded-md border border-border px-2 py-1 text-xs text-muted hover:text-danger" aria-label={`Remove ${item.title}`}>×</button></div></div>)}</div> : <div className="mt-5 rounded-xl border border-dashed border-border p-8 text-center"><p className="text-sm font-medium">Queue is empty</p><p className="mt-1 text-xs text-secondary">Choose a recipe and add items to build your next session.</p></div>}</div>
    </section>
    <section><div className="mb-3 flex items-center justify-between"><h2 className="text-lg font-semibold">Candidates</h2><Link to="/command" className="text-xs font-semibold text-accent">Command center →</Link></div>{candidates.length ? <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 lg:grid-cols-6">{candidates.map(item => { const queued = queue.some(x => x.id === item.id); return <article key={item.id} className="rounded-xl border border-border bg-surface p-3 shadow-card"><span className="text-[9px] font-bold uppercase tracking-wider text-accent">{item.mediaType}</span><p className="mt-2 truncate text-sm font-semibold">{item.title}</p><p className="mt-1 text-[10px] text-muted">{new Date(item.timestamp).toLocaleDateString()}</p><button type="button" onClick={() => addOne(item)} disabled={queued} className="mt-3 w-full rounded-lg border border-border px-2 py-1.5 text-xs font-semibold disabled:opacity-40">{queued ? 'Queued' : 'Add to queue'}</button></article>; })}</div> : <EmptyState title="No candidates" description="This recipe has no matching local media yet." />}</section>
  </div>;
}
