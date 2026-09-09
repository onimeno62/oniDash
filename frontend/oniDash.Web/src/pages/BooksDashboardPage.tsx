import { useEffect, useMemo, useState } from 'react';
import { fetchContinueReading, fetchBooks, bookCoverUrl, reindexBooks, setBookFavorite, setBookProgress, setBookRating, setBookRead, type BookSummary } from '../api/books';
import { fetchLibraries, type Library } from '../api/libraries';
import { ErrorState } from '../components/states/ErrorState';
import { EmptyState } from '../components/states/EmptyState';
import { LoadingState } from '../components/states/LoadingState';
import { LibraryIcon, PlayIcon } from '../components/icons';

type View = 'overview' | 'all' | 'unread' | 'read' | 'favorites';
type Sort = 'title' | 'author' | 'recent' | 'rating' | 'pages';

export function BooksDashboardPage() {
  const [libraries, setLibraries] = useState<Library[] | null>(null);
  const [libraryId, setLibraryId] = useState('');
  const [books, setBooks] = useState<BookSummary[] | null>(null);
  const [continueReading, setContinueReading] = useState<BookSummary[]>([]);
  const [view, setView] = useState<View>('overview');
  const [sort, setSort] = useState<Sort>('title');
  const [query, setQuery] = useState('');
  const [selected, setSelected] = useState<BookSummary | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [reload, setReload] = useState(0);

  useEffect(() => {
    const c = new AbortController();
    fetchLibraries(c.signal).then(items => { setLibraries(items); setLibraryId(current => current || items[0]?.id || ''); }).catch(e => { if (!c.signal.aborted) setError(e instanceof Error ? e.message : 'Could not load libraries.'); });
    return () => c.abort();
  }, []);

  useEffect(() => {
    if (!libraryId) return;
    const c = new AbortController();
    setBooks(null); setError(null);
    const read = view === 'read' ? true : view === 'unread' ? false : undefined;
    Promise.all([fetchBooks(libraryId, { read, limit: 500, signal: c.signal }), fetchContinueReading(libraryId, c.signal)])
      .then(([items, cont]) => { setBooks(items); setContinueReading(cont); })
      .catch(e => { if (!c.signal.aborted) setError(e instanceof Error ? e.message : 'Could not load books.'); });
    return () => c.abort();
  }, [libraryId, view, reload]);

  const visible = useMemo(() => {
    let source = books ?? [];
    if (view === 'favorites') source = source.filter(b => b.favorite);
    const q = query.trim().toLocaleLowerCase();
    if (q) source = source.filter(b => `${b.title} ${b.author ?? ''} ${b.series ?? ''} ${b.year ?? ''}`.toLocaleLowerCase().includes(q));
    return [...source].sort((a, b) => sort === 'author' ? (a.author ?? '').localeCompare(b.author ?? '') || a.title.localeCompare(b.title) : sort === 'recent' ? (b.lastReadAtUtc ?? '').localeCompare(a.lastReadAtUtc ?? '') : sort === 'rating' ? (b.rating ?? 0) - (a.rating ?? 0) : sort === 'pages' ? (b.pageCount ?? 0) - (a.pageCount ?? 0) : a.title.localeCompare(b.title));
  }, [books, query, sort, view]);

  const stats = useMemo(() => { const all = books ?? []; return { total: all.length, read: all.filter(b => b.read).length, unread: all.filter(b => !b.read).length, pages: all.reduce((n, b) => n + (b.pageCount ?? 0), 0), favorites: all.filter(b => b.favorite).length }; }, [books]);
  const refresh = () => setReload(n => n + 1);
  const update = async (fn: () => Promise<unknown>) => { setBusy(true); try { await fn(); refresh(); } catch (e) { setError(e instanceof Error ? e.message : 'Could not update book.'); } finally { setBusy(false); } };

  if (!libraries && error) return <ErrorState title="Could not load Books" message={error} onRetry={refresh} />;
  return <div className="space-y-7 pb-10">
    <header className="relative overflow-hidden rounded-3xl border border-border bg-surface-elevated p-6 shadow-card sm:p-8">
      <div className="pointer-events-none absolute -right-24 -top-28 size-80 rounded-full bg-accent/15 blur-3xl" />
      <div className="relative flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
        <div><p className="mb-2 text-xs font-semibold uppercase tracking-[0.22em] text-accent">Book library</p><h1 className="text-3xl font-bold tracking-tight sm:text-4xl">Your bookshelf, beautifully organized.</h1><p className="mt-2 max-w-2xl text-sm leading-6 text-secondary">Keep your books, reading progress, ratings, favorites, series and formats together in one focused reading space.</p></div>
        <div className="flex flex-wrap gap-2"><select aria-label="Books library" value={libraryId} onChange={e => setLibraryId(e.target.value)} className="rounded-xl border border-border bg-surface px-3 py-2.5 text-sm"><option value="" disabled>Choose library</option>{(libraries ?? []).map(l => <option key={l.id} value={l.id}>{l.name}</option>)}</select><button type="button" disabled={busy || !libraryId} onClick={() => void update(() => reindexBooks(libraryId))} className="rounded-xl bg-accent px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50">{busy ? 'Indexing…' : '↻ Reindex'}</button></div>
      </div>
    </header>
    {error && <ErrorState title="Book library error" message={error} onRetry={refresh} />}
    {!error && !libraryId && libraries && <EmptyState icon={<LibraryIcon className="size-6" />} title="Choose a library" description="Create and scan a library containing your ebook, PDF or comic book files to get started." />}
    {!error && libraryId && <>
      <section className="grid grid-cols-2 gap-3 lg:grid-cols-4"><Stat label="Books" value={stats.total} hint="in this library" /><Stat label="Read" value={stats.read} hint={`${stats.total ? Math.round(stats.read / stats.total * 100) : 0}% completed`} /><Stat label="Unread" value={stats.unread} hint="waiting on your shelf" /><Stat label="Pages" value={stats.pages.toLocaleString()} hint={`${stats.favorites} favorites`} /></section>
      {continueReading.length > 0 && view === 'overview' && <section><SectionTitle title="Continue reading" /><div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">{continueReading.slice(0, 4).map(b => <ContinueCard key={b.id} book={b} onOpen={() => setSelected(b)} />)}</div></section>}
      <section><div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between"><div className="flex flex-wrap gap-1 rounded-xl border border-border bg-surface p-1">{([['overview','Overview'],['all','All books'],['unread','Unread'],['read','Read'],['favorites','Favorites']] as [View,string][]).map(([k,l]) => <button key={k} type="button" onClick={() => setView(k)} className={`rounded-lg px-3 py-1.5 text-xs font-semibold ${view === k ? 'bg-surface-elevated text-primary shadow-card' : 'text-muted hover:text-primary'}`}>{l}</button>)}</div><div className="flex gap-2"><input aria-label="Search books" value={query} onChange={e => setQuery(e.target.value)} placeholder="Search title, author, series…" className="w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm lg:w-72" /><select aria-label="Sort books" value={sort} onChange={e => setSort(e.target.value as Sort)} className="rounded-xl border border-border bg-surface px-3 py-2 text-sm"><option value="title">Title</option><option value="author">Author</option><option value="recent">Recently read</option><option value="rating">Rating</option><option value="pages">Pages</option></select></div></div>
        {books === null ? <LoadingState label="Loading your bookshelf…" /> : visible.length === 0 ? <EmptyState icon={<LibraryIcon className="size-6" />} title={query ? 'No matching books' : view === 'favorites' ? 'No favorites yet' : 'No books found'} description={query ? 'Try another title, author or series.' : 'Scan a folder containing books to populate the collection.'} action={query ? <button type="button" onClick={() => setQuery('')} className="rounded-lg border border-border px-4 py-2 text-sm">Clear search</button> : undefined} /> : <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5 xl:grid-cols-6">{visible.map(b => <BookCard key={b.id} book={b} onOpen={() => setSelected(b)} />)}</div>}
      </section>
    </>}
    {selected && <BookDetails book={selected} busy={busy} onClose={() => setSelected(null)} onRead={read => void update(() => setBookRead(selected.id, read))} onFavorite={fav => void update(() => setBookFavorite(selected.id, fav))} onRating={rating => void update(() => setBookRating(selected.id, rating))} onProgress={page => void update(() => setBookProgress(selected.id, page))} />}
  </div>;
}

function Stat({ label, value, hint }: { label: string; value: string | number; hint: string }) { return <div className="rounded-2xl border border-border bg-surface p-4 shadow-card"><p className="text-xs font-medium text-muted">{label}</p><p className="mt-1 text-2xl font-bold tracking-tight">{value}</p><p className="mt-1 text-[11px] text-secondary">{hint}</p></div>; }
function SectionTitle({ title }: { title: string }) { return <div className="mb-3"><h2 className="text-lg font-semibold tracking-tight">{title}</h2></div>; }
function Cover({ book, className = 'h-full w-full' }: { book: BookSummary; className?: string }) { return book.hasCover ? <img src={bookCoverUrl(book.id)} alt={`Cover of ${book.title}`} loading="lazy" className={`${className} object-cover`} /> : <div className={`${className} grid place-items-center bg-gradient-to-br from-accent-soft to-surface-elevated`}><LibraryIcon className="size-10 text-accent opacity-60" /></div>; }
function BookCard({ book, onOpen }: { book: BookSummary; onOpen: () => void }) { const progress = Math.min(100, Math.max(0, book.progressPercent ?? (book.pageCount && book.progressPages ? book.progressPages / book.pageCount * 100 : 0))); return <article className="group overflow-hidden rounded-2xl border border-border bg-surface shadow-card transition hover:-translate-y-0.5 hover:shadow-pop"><button type="button" onClick={onOpen} className="block w-full text-left"><div className="relative aspect-[2/3] overflow-hidden bg-surface-elevated"><Cover book={book} /><div className="absolute inset-0 bg-gradient-to-t from-black/75 via-transparent to-transparent opacity-80" />{book.favorite && <span className="absolute left-2 top-2 rounded-full bg-black/50 px-2 py-1 text-[10px] font-bold text-white">♥</span>}{book.read && <span className="absolute right-2 top-2 rounded-full bg-success px-2 py-1 text-[10px] font-bold text-white">READ</span>}<div className="absolute bottom-3 left-3 right-3"><p className="truncate text-sm font-semibold text-white">{book.title}</p><p className="truncate text-[11px] text-white/70">{book.author ?? 'Unknown author'}</p></div>{progress > 0 && !book.read && <div className="absolute inset-x-0 bottom-0 h-1 bg-black/50"><div className="h-full bg-accent" style={{ width: `${progress}%` }} /></div>}</div></button><div className="flex items-center justify-between gap-2 p-3"><p className="truncate text-xs text-muted">{book.series ? `${book.series} · ` : ''}{book.year ?? '—'}</p><span className="text-xs text-muted">{book.format?.toUpperCase() ?? 'BOOK'}</span></div></article>; }
function ContinueCard({ book, onOpen }: { book: BookSummary; onOpen: () => void }) { const progress = Math.round(book.progressPercent ?? 0); return <article className="overflow-hidden rounded-2xl border border-border bg-surface shadow-card"><button type="button" onClick={onOpen} className="flex w-full gap-4 p-4 text-left"><div className="h-28 w-20 shrink-0 overflow-hidden rounded-lg"><Cover book={book} /></div><div className="min-w-0 py-1"><p className="truncate text-sm font-semibold">{book.title}</p><p className="mt-1 truncate text-xs text-secondary">{book.author ?? 'Unknown author'}</p><div className="mt-5 h-1.5 overflow-hidden rounded-full bg-surface-elevated"><div className="h-full bg-accent" style={{ width: `${progress}%` }} /></div><p className="mt-1 text-[11px] text-muted">{progress}% · page {book.progressPages ?? 0}{book.pageCount ? ` of ${book.pageCount}` : ''}</p></div></button></article>; }
function BookDetails({ book, busy, onClose, onRead, onFavorite, onRating, onProgress }: { book: BookSummary; busy: boolean; onClose: () => void; onRead: (v: boolean) => void; onFavorite: (v: boolean) => void; onRating: (v: number) => void; onProgress: (v: number) => void }) { const [page, setPage] = useState(String(book.progressPages ?? 0)); return <div className="fixed inset-0 z-40 flex justify-end bg-black/50 backdrop-blur-sm" onMouseDown={e => { if (e.target === e.currentTarget) onClose(); }}><aside className="h-full w-full max-w-xl overflow-y-auto border-l border-border bg-surface p-6 shadow-pop sm:p-8"><div className="flex items-center justify-between"><span className="text-xs font-semibold uppercase tracking-[0.18em] text-accent">Book information</span><button type="button" aria-label="Close details" onClick={onClose} className="text-2xl text-muted">×</button></div><div className="mt-6 flex gap-5"><div className="h-56 w-36 shrink-0 overflow-hidden rounded-xl shadow-card"><Cover book={book} /></div><div className="min-w-0"><h2 className="text-2xl font-bold">{book.title}</h2><p className="mt-2 text-sm text-secondary">{book.author ?? 'Unknown author'}</p><p className="mt-2 text-xs text-muted">{book.year ?? 'Year unknown'} · {book.pageCount ?? '—'} pages · {book.format?.toUpperCase() ?? 'BOOK'}</p><div className="mt-5 flex flex-wrap gap-2"><button disabled={busy} type="button" onClick={() => onRead(!book.read)} className="rounded-xl bg-accent px-4 py-2 text-sm font-semibold text-white">{book.read ? 'Mark unread' : 'Mark read'}</button><button disabled={busy} type="button" onClick={() => onFavorite(!book.favorite)} className="rounded-xl border border-border px-4 py-2 text-sm">{book.favorite ? '♥ Favorited' : '♡ Favorite'}</button></div></div></div><div className="mt-8 rounded-2xl border border-border bg-surface-elevated p-5"><div className="flex items-center justify-between"><div><p className="text-sm font-semibold">Reading progress</p><p className="mt-1 text-xs text-secondary">{book.progressPercent ?? 0}% complete</p></div><PlayIcon className="size-5 text-accent" /></div><div className="mt-4 flex gap-2"><input aria-label="Current page" type="number" min="0" max={book.pageCount ?? undefined} value={page} onChange={e => setPage(e.target.value)} className="w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm" /><button disabled={busy} type="button" onClick={() => onProgress(Math.max(0, Number(page) || 0))} className="rounded-xl bg-accent px-4 py-2 text-sm font-semibold text-white">Save</button></div></div><div className="mt-5 rounded-2xl border border-border p-5"><p className="text-sm font-semibold">Rating</p><div className="mt-3 flex gap-2">{[1,2,3,4,5].map(n => <button key={n} disabled={busy} type="button" aria-label={`Rate ${n} out of 5`} onClick={() => onRating(n)} className={`text-2xl ${n <= (book.rating ?? 0) ? 'text-accent' : 'text-muted'}`}>★</button>)}</div></div>{book.series && <div className="mt-5 rounded-2xl border border-border bg-surface-elevated p-5"><p className="text-xs uppercase tracking-wider text-muted">Series</p><p className="mt-1 font-semibold">{book.series}</p></div>}</aside></div>; }
