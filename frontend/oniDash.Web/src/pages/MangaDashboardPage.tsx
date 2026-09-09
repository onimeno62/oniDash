import { useEffect, useMemo, useState } from 'react';
import { fetchCategories, fetchContinueReading, fetchManga, fetchPopular, fetchSources, fetchUpdates, installSource, setFavorite, setStatus, type MangaCategory, type MangaSource, type MangaSummary, type MangaUpdate } from '../api/manga';
import { EmptyState } from '../components/states/EmptyState';
import { ErrorState } from '../components/states/ErrorState';
import { LoadingState } from '../components/states/LoadingState';

type View = 'home' | 'library' | 'favorites' | 'updates' | 'categories' | 'sources';
type Filter = 'all' | 'reading' | 'completed' | 'unread';

export function MangaDashboardPage() {
  const [view, setView] = useState<View>('home');
  const [filter, setFilter] = useState<Filter>('all');
  const [query, setQuery] = useState('');
  const [library, setLibrary] = useState<MangaSummary[] | null>(null);
  const [continueReading, setContinueReading] = useState<MangaSummary[]>([]);
  const [popular, setPopular] = useState<MangaSummary[]>([]);
  const [updates, setUpdates] = useState<MangaUpdate[]>([]);
  const [categories, setCategories] = useState<MangaCategory[]>([]);
  const [sources, setSources] = useState<MangaSource[]>([]);
  const [selected, setSelected] = useState<MangaSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const load = () => {
    setLoading(true); setError(null);
    const controller = new AbortController();
    Promise.all([fetchManga(controller.signal), fetchContinueReading(controller.signal), fetchUpdates(controller.signal), fetchCategories(controller.signal), fetchSources(controller.signal), fetchPopular(controller.signal)])
      .then(([items, cont, ups, cats, srcs, pop]) => { setLibrary(items); setContinueReading(cont); setUpdates(ups); setCategories(cats); setSources(srcs); setPopular(pop); })
      .catch((e: unknown) => { if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'Could not load manga.'); })
      .finally(() => setLoading(false));
    return () => controller.abort();
  };
  useEffect(load, []);

  const filtered = useMemo(() => {
    const source = library ?? [];
    const needle = query.trim().toLocaleLowerCase();
    return source.filter((m) => {
      const text = `${m.title} ${m.altTitle ?? ''} ${m.author ?? ''} ${m.artist ?? ''} ${(m.genres ?? []).join(' ')}`.toLocaleLowerCase();
      if (needle && !text.includes(needle)) return false;
      if (view === 'favorites' && !m.favorite) return false;
      if (filter === 'reading' && m.status !== 'reading') return false;
      if (filter === 'completed' && m.status !== 'completed') return false;
      if (filter === 'unread' && !(m.unreadCount ?? 0)) return false;
      return true;
    }).sort((a, b) => (b.lastReadAtUtc ?? '').localeCompare(a.lastReadAtUtc ?? '') || a.title.localeCompare(b.title));
  }, [library, query, view, filter]);

  const stats = useMemo(() => ({ total: library?.length ?? 0, reading: library?.filter(x => x.status === 'reading').length ?? 0, unread: library?.reduce((n, x) => n + (x.unreadCount ?? 0), 0) ?? 0, favorites: library?.filter(x => x.favorite).length ?? 0 }), [library]);

  const favorite = async (m: MangaSummary) => { await setFavorite(m.id, !m.favorite); setLibrary((xs) => xs?.map(x => x.id === m.id ? { ...x, favorite: !x.favorite } : x)); setSelected((x) => x?.id === m.id ? { ...x, favorite: !x.favorite } : x); };
  const markCompleted = async (m: MangaSummary) => { await setStatus(m.id, 'completed'); setLibrary((xs) => xs?.map(x => x.id === m.id ? { ...x, status: 'completed' } : x)); };
  const refresh = () => load();
  const update = async () => { setBusy(true); try { await (await import('../api/manga')).updateLibrary(); refresh(); } catch (e) { setError(e instanceof Error ? e.message : 'Update failed.'); } finally { setBusy(false); } };

  if (loading) return <LoadingState label="Loading your manga library…" />;
  if (error && !library) return <ErrorState title="Could not load Manga" message={error} onRetry={refresh} />;

  return <div className="space-y-7 pb-10">
    <header className="relative overflow-hidden rounded-3xl border border-border bg-surface-elevated p-6 shadow-card sm:p-8">
      <div className="pointer-events-none absolute -right-20 -top-24 size-80 rounded-full bg-accent/15 blur-3xl" />
      <div className="relative flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
        <div><p className="mb-2 text-xs font-semibold uppercase tracking-[0.24em] text-accent">Manga library</p><h1 className="text-3xl font-bold tracking-tight sm:text-4xl">Your manga universe.</h1><p className="mt-2 max-w-2xl text-sm leading-6 text-secondary">Discover from extensions, build your library, download chapters, and continue reading with a desktop-first experience inspired by the best manga readers.</p></div>
        <div className="flex flex-wrap gap-2"><button type="button" onClick={() => void update()} disabled={busy} className="rounded-xl bg-accent px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50">{busy ? 'Updating…' : '↻ Check for updates'}</button><button type="button" onClick={() => setView('sources')} className="rounded-xl border border-border bg-surface px-4 py-2.5 text-sm font-semibold">Extensions</button></div>
      </div>
    </header>

    {error && <ErrorState title="Manga update error" message={error} onRetry={refresh} />}
    <section className="grid grid-cols-2 gap-3 lg:grid-cols-4"><Stat label="Library" value={stats.total} hint="saved manga" /><Stat label="Reading" value={stats.reading} hint="in progress" /><Stat label="Unread" value={stats.unread} hint="chapters waiting" /><Stat label="Favorites" value={stats.favorites} hint="across all categories" /></section>

    <nav className="flex flex-wrap gap-1 rounded-2xl border border-border bg-surface p-1.5">{([['home','Home'],['library','Library'],['favorites','Favorites'],['updates',`Updates${updates.length ? ` · ${updates.length}` : ''}`],['categories','Categories'],['sources','Sources']] as [View,string][]).map(([k,l]) => <button key={k} type="button" onClick={() => setView(k)} className={`rounded-xl px-4 py-2 text-xs font-semibold ${view === k ? 'bg-surface-elevated text-primary shadow-card' : 'text-muted hover:text-primary'}`}>{l}</button>)}</nav>

    {view === 'home' && <Home continueReading={continueReading} updates={updates} popular={popular} onOpen={setSelected} onUpdates={() => setView('updates')} />}
    {view === 'library' || view === 'favorites' ? <LibraryView items={filtered} query={query} setQuery={setQuery} filter={filter} setFilter={setFilter} onOpen={setSelected} /> : null}
    {view === 'updates' && <UpdatesView updates={updates} onOpen={(id) => setSelected(library?.find(x => x.id === id) ?? null)} />}
    {view === 'categories' && <CategoriesView categories={categories} />}
    {view === 'sources' && <SourcesView sources={sources} onInstall={async (id) => { await installSource(id); setSources((xs) => xs.map(x => x.id === id ? { ...x, installed: true, enabled: true } : x)); }} />}

    {selected && <MangaDetails manga={selected} onClose={() => setSelected(null)} onFavorite={() => void favorite(selected)} onCompleted={() => void markCompleted(selected)} />}
  </div>;
}

function Stat({ label, value, hint }: { label: string; value: number; hint: string }) { return <div className="rounded-2xl border border-border bg-surface p-4 shadow-card"><p className="text-xs font-medium text-muted">{label}</p><p className="mt-1 text-2xl font-bold">{value.toLocaleString()}</p><p className="mt-1 text-[11px] text-secondary">{hint}</p></div>; }

function Home({ continueReading, updates, popular, onOpen, onUpdates }: { continueReading: MangaSummary[]; updates: MangaUpdate[]; popular: MangaSummary[]; onOpen: (m: MangaSummary) => void; onUpdates: () => void }) { return <div className="space-y-8"><Section title="Continue reading" action={continueReading.length > 4 ? 'View library' : undefined}>{continueReading.length ? <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">{continueReading.slice(0, 5).map(m => <MangaCard key={m.id} manga={m} onOpen={() => onOpen(m)} />)}</div> : <EmptyState title="Nothing in progress" description="Open a manga from your library or a source to start reading." />}</Section><Section title="Latest chapter updates" action={updates.length ? 'View all' : undefined} onAction={onUpdates}>{updates.slice(0, 6).map(u => <UpdateRow key={u.id} update={u} onOpen={() => onOpen({ id: u.mangaId, title: u.mangaTitle, coverUrl: u.coverUrl })} />)}</Section><Section title="Popular from sources"><div className="grid grid-cols-2 gap-4 sm:grid-cols-4 lg:grid-cols-6">{popular.slice(0, 12).map(m => <MangaCard key={m.id} manga={m} onOpen={() => onOpen(m)} />)}</div></Section></div>; }

function LibraryView({ items, query, setQuery, filter, setFilter, onOpen }: { items: MangaSummary[]; query: string; setQuery: (x: string) => void; filter: Filter; setFilter: (x: Filter) => void; onOpen: (m: MangaSummary) => void }) { return <section><div className="mb-4 flex flex-col gap-3 lg:flex-row lg:justify-between"><div className="flex gap-1 rounded-xl border border-border bg-surface p-1">{([['all','All'],['reading','Reading'],['unread','Unread'],['completed','Completed']] as [Filter,string][]).map(([k,l]) => <button key={k} onClick={() => setFilter(k)} className={`rounded-lg px-3 py-1.5 text-xs font-semibold ${filter === k ? 'bg-surface-elevated shadow-card' : 'text-muted'}`}>{l}</button>)}</div><input aria-label="Search manga" value={query} onChange={e => setQuery(e.target.value)} placeholder="Search title, author, genre…" className="w-full rounded-xl border border-border bg-surface px-3 py-2.5 text-sm lg:w-80" /></div>{items.length ? <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5 xl:grid-cols-6">{items.map(m => <MangaCard key={m.id} manga={m} onOpen={() => onOpen(m)} />)}</div> : <EmptyState title="No manga found" description="Try a different filter or search." />}</section>; }

function MangaCard({ manga, onOpen }: { manga: MangaSummary; onOpen: () => void }) { const progress = manga.progressPercent ?? 0; return <article className="group overflow-hidden rounded-2xl border border-border bg-surface shadow-card transition hover:-translate-y-0.5 hover:shadow-pop"><button onClick={onOpen} className="block w-full text-left"><div className="relative aspect-[2/3] overflow-hidden bg-surface-elevated">{manga.coverUrl ? <img src={manga.coverUrl} alt={`Cover of ${manga.title}`} loading="lazy" className="h-full w-full object-cover transition duration-500 group-hover:scale-105" /> : <div className="grid h-full place-items-center p-5 text-center text-sm font-semibold text-secondary">{manga.title}</div>}<div className="absolute inset-x-0 bottom-0 h-1 bg-black/30"><div className="h-full bg-accent" style={{ width: `${Math.min(100, Math.max(0, progress))}%` }} /></div>{manga.unreadCount ? <span className="absolute right-2 top-2 rounded-full bg-accent px-2 py-1 text-[10px] font-bold text-white">{manga.unreadCount} NEW</span> : null}{manga.favorite ? <span className="absolute left-2 top-2 rounded-full bg-black/60 px-2 py-1 text-xs text-white">♥</span> : null}</div></button><div className="p-3"><p className="truncate text-sm font-semibold">{manga.title}</p><p className="mt-1 truncate text-[11px] text-muted">{manga.author ?? manga.sourceName ?? 'Unknown author'}</p><div className="mt-2 flex items-center justify-between text-[10px] text-muted"><span>{manga.latestChapter ? `Ch. ${manga.latestChapter}` : 'No chapter data'}</span><span>{progress}%</span></div></div></article>; }

function Section({ title, action, onAction, children }: { title: string; action?: string; onAction?: () => void; children: React.ReactNode }) { return <section><div className="mb-3 flex items-center justify-between"><h2 className="text-lg font-semibold tracking-tight">{title}</h2>{action && <button onClick={onAction} className="text-xs font-semibold text-accent">{action}</button>}</div>{children}</section>; }
function UpdateRow({ update, onOpen }: { update: MangaUpdate; onOpen: () => void }) { return <button onClick={onOpen} className="mb-2 flex w-full items-center gap-3 rounded-xl border border-border bg-surface p-3 text-left hover:bg-surface-elevated"><div className="size-12 shrink-0 overflow-hidden rounded-lg bg-surface-elevated">{update.coverUrl && <img src={update.coverUrl} alt="" className="h-full w-full object-cover" />}</div><div className="min-w-0 flex-1"><p className="truncate text-sm font-semibold">{update.mangaTitle}</p><p className="text-xs text-secondary">Chapter {update.chapter}{update.sourceName ? ` · ${update.sourceName}` : ''}</p></div><span className={`rounded-full px-2 py-1 text-[10px] font-bold ${update.read ? 'bg-surface-elevated text-muted' : 'bg-accent-soft text-accent'}`}>{update.read ? 'READ' : 'NEW'}</span></button>; }
function UpdatesView({ updates, onOpen }: { updates: MangaUpdate[]; onOpen: (id: string) => void }) { return <section>{updates.length ? updates.map(u => <UpdateRow key={u.id} update={u} onOpen={() => onOpen(u.mangaId)} />) : <EmptyState title="No updates" description="Run a library update to check your installed sources." />}</section>; }
function CategoriesView({ categories }: { categories: MangaCategory[] }) { return <section className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">{categories.map(c => <article key={c.id} className="rounded-2xl border border-border bg-surface p-5 shadow-card"><div className="flex items-center justify-between"><h3 className="font-semibold">{c.name}</h3><span className="rounded-full bg-accent-soft px-2 py-1 text-xs font-bold text-accent">{c.count}</span></div><p className="mt-2 text-xs text-secondary">Custom library category</p></article>)}{!categories.length && <EmptyState title="No categories" description="Create categories to organize your library." />}</section>; }
function SourcesView({ sources, onInstall }: { sources: MangaSource[]; onInstall: (id: string) => Promise<void> }) { return <section><div className="mb-5 rounded-2xl border border-border bg-surface p-5"><h2 className="font-semibold">Mihon-compatible sources</h2><p className="mt-1 text-sm text-secondary">Install and manage extension-backed sources without coupling oniDash to individual websites. The backend should execute extensions through a compatible runtime such as Suwayomi.</p></div><div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">{sources.map(s => <article key={s.id} className="rounded-2xl border border-border bg-surface p-4 shadow-card"><div className="flex items-center gap-3"><div className="grid size-11 place-items-center rounded-xl bg-surface-elevated text-lg">◈</div><div className="min-w-0 flex-1"><h3 className="truncate font-semibold">{s.name}</h3><p className="text-xs text-muted">{s.language ?? 'Multi-language'}{s.mangaCount != null ? ` · ${s.mangaCount.toLocaleString()} titles` : ''}</p></div><span className={`size-2 rounded-full ${s.enabled ? 'bg-success' : 'bg-muted'}`} /></div><div className="mt-4 flex justify-end">{s.installed ? <span className="rounded-lg bg-surface-elevated px-3 py-2 text-xs font-semibold">Installed</span> : <button onClick={() => void onInstall(s.id)} className="rounded-lg bg-accent px-3 py-2 text-xs font-semibold text-white">Install extension</button>}</div></article>)}</div></section>; }
function MangaDetails({ manga, onClose, onFavorite, onCompleted }: { manga: MangaSummary; onClose: () => void; onFavorite: () => void; onCompleted: () => void }) { return <div className="fixed inset-0 z-40 flex justify-end bg-black/50 backdrop-blur-sm" onMouseDown={e => { if (e.target === e.currentTarget) onClose(); }}><aside className="h-full w-full max-w-xl overflow-y-auto border-l border-border bg-surface p-6 shadow-pop sm:p-8"><div className="flex justify-between"><p className="text-xs font-semibold uppercase tracking-[0.2em] text-accent">Manga information</p><button onClick={onClose} className="text-2xl text-muted">×</button></div><div className="mt-6 flex gap-5">{manga.coverUrl ? <img src={manga.coverUrl} alt="" className="h-56 w-36 rounded-xl object-cover shadow-card" /> : <div className="grid h-56 w-36 shrink-0 place-items-center rounded-xl bg-surface-elevated p-4 text-center text-xs">{manga.title}</div>}<div><h2 className="text-2xl font-bold">{manga.title}</h2><p className="mt-2 text-sm text-secondary">{manga.author ?? 'Unknown author'}{manga.artist ? ` · ${manga.artist}` : ''}</p><p className="mt-2 text-xs text-muted">{manga.sourceName ?? 'Local library'}{manga.year ? ` · ${manga.year}` : ''}</p><div className="mt-5 flex flex-wrap gap-2"><button onClick={onFavorite} className="rounded-xl border border-border px-3 py-2 text-sm">{manga.favorite ? '♥ Favorited' : '♡ Favorite'}</button><button onClick={onCompleted} className="rounded-xl bg-accent px-3 py-2 text-sm font-semibold text-white">Mark completed</button></div></div></div><div className="mt-7 grid grid-cols-2 gap-3"><Info label="Status" value={manga.status?.replace('_',' ') ?? 'Unknown'} /><Info label="Progress" value={`${manga.progressPercent ?? 0}%`} /><Info label="Unread" value={String(manga.unreadCount ?? 0)} /><Info label="Chapters" value={String(manga.chapterCount ?? '—')} /></div>{manga.genres?.length ? <div className="mt-6 flex flex-wrap gap-2">{manga.genres.map(g => <span key={g} className="rounded-full bg-accent-soft px-3 py-1 text-xs text-accent">{g}</span>)}</div> : null}<p className="mt-6 text-sm leading-7 text-secondary">{manga.description ?? 'No description is available yet. Metadata can be enriched from the active source and tracking providers.'}</p></aside></div>; }
function Info({ label, value }: { label: string; value: string }) { return <div className="rounded-xl border border-border bg-surface-elevated p-3"><p className="text-[10px] uppercase tracking-wider text-muted">{label}</p><p className="mt-1 text-sm font-semibold capitalize">{value}</p></div>; }
