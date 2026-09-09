import { useEffect, useMemo, useState } from 'react';
import { fetchContinueWatching, fetchMovies, formatProgressPercent, formatRuntime, moviePosterUrl, movieStreamUrl, reindexMovies, saveWatchProgress, setMovieWatched, type MovieSummary } from '../api/movies';
import { fetchLibraries, type Library } from '../api/libraries';
import { ErrorState } from '../components/states/ErrorState';
import { EmptyState } from '../components/states/EmptyState';
import { LoadingState } from '../components/states/LoadingState';
import { FilmIcon, PlayIcon } from '../components/icons';

type ViewMode = 'overview' | 'library' | 'unwatched' | 'watched';
type SortMode = 'title' | 'year' | 'runtime' | 'recent';

export function MoviesDashboardPage() {
  const [libraries, setLibraries] = useState<Library[] | null>(null);
  const [libraryId, setLibraryId] = useState('');
  const [movies, setMovies] = useState<MovieSummary[] | null>(null);
  const [continueWatching, setContinueWatching] = useState<MovieSummary[]>([]);
  const [view, setView] = useState<ViewMode>('overview');
  const [sort, setSort] = useState<SortMode>('title');
  const [query, setQuery] = useState('');
  const [selected, setSelected] = useState<MovieSummary | null>(null);
  const [playing, setPlaying] = useState<MovieSummary | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [reload, setReload] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    fetchLibraries(controller.signal).then((items) => {
      setLibraries(items);
      setLibraryId((current) => current || items[0]?.id || '');
    }).catch((e: unknown) => { if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'Could not load libraries.'); });
    return () => controller.abort();
  }, []);

  useEffect(() => {
    if (!libraryId) return;
    const controller = new AbortController();
    setMovies(null); setError(null);
    const watched = view === 'watched' ? true : view === 'unwatched' ? false : undefined;
    Promise.all([
      fetchMovies(libraryId, { watched, limit: 200, signal: controller.signal }),
      fetchContinueWatching(libraryId, controller.signal),
    ]).then(([all, cont]) => { setMovies(all); setContinueWatching(cont); })
      .catch((e: unknown) => { if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'Could not load movies.'); });
    return () => controller.abort();
  }, [libraryId, view, reload]);

  const visible = useMemo(() => {
    const source = movies ?? [];
    const needle = query.trim().toLocaleLowerCase();
    const filtered = needle ? source.filter((m) => `${m.title} ${m.year ?? ''}`.toLocaleLowerCase().includes(needle)) : source;
    return [...filtered].sort((a, b) => {
      if (sort === 'year') return (b.year ?? 0) - (a.year ?? 0) || a.title.localeCompare(b.title);
      if (sort === 'runtime') return (b.durationSeconds ?? 0) - (a.durationSeconds ?? 0);
      if (sort === 'recent') return (b.watchedAtUtc ?? '').localeCompare(a.watchedAtUtc ?? '');
      return a.title.localeCompare(b.title);
    });
  }, [movies, query, sort]);

  const stats = useMemo(() => {
    const all = movies ?? [];
    const runtime = all.reduce((sum, m) => sum + (m.durationSeconds ?? 0), 0);
    return { total: all.length, watched: all.filter((m) => m.watched).length, unwatched: all.filter((m) => !m.watched).length, hours: Math.round(runtime / 3600) };
  }, [movies]);

  const refresh = () => setReload((n) => n + 1);
  const markWatched = async (movie: MovieSummary, watched: boolean) => {
    await setMovieWatched(movie.id, watched);
    setSelected(null); refresh();
  };
  const runReindex = async () => {
    setBusy(true);
    try { await reindexMovies(libraryId || undefined); refresh(); } catch (e) { setError(e instanceof Error ? e.message : 'Reindex failed.'); } finally { setBusy(false); }
  };

  if (!libraries && error) return <ErrorState title="Could not load Movies" message={error} onRetry={refresh} />;

  return <div className="space-y-7 pb-10">
    <header className="relative overflow-hidden rounded-3xl border border-border bg-surface-elevated p-6 shadow-card sm:p-8">
      <div className="pointer-events-none absolute -right-20 -top-24 size-72 rounded-full bg-accent/15 blur-3xl" />
      <div className="relative flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
        <div><p className="mb-2 text-xs font-semibold uppercase tracking-[0.22em] text-accent">Movie library</p><h1 className="text-3xl font-bold tracking-tight sm:text-4xl">Your cinema, organized.</h1><p className="mt-2 max-w-2xl text-sm leading-6 text-secondary">Browse your collection, pick up where you stopped, and keep every local movie one click away.</p></div>
        <div className="flex flex-wrap gap-2"><select aria-label="Movies library" value={libraryId} onChange={(e) => setLibraryId(e.target.value)} className="rounded-xl border border-border bg-surface px-3 py-2.5 text-sm"><option value="" disabled>Choose library</option>{(libraries ?? []).map((l) => <option key={l.id} value={l.id}>{l.name}</option>)}</select><button type="button" onClick={() => void runReindex()} disabled={busy || !libraryId} className="rounded-xl bg-accent px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50">{busy ? 'Indexing…' : '↻ Reindex'}</button></div>
      </div>
    </header>

    {error && <ErrorState title="Movie library error" message={error} onRetry={refresh} />}
    {!error && !libraryId && libraries && <EmptyState icon={<FilmIcon className="size-6" />} title="Choose a library" description="Create and scan a library containing your movie files to get started." />}
    {!error && libraryId && <>
      <section className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Stat label="Movies" value={stats.total} hint="in this library" />
        <Stat label="Watched" value={stats.watched} hint={`${stats.total ? Math.round(stats.watched / stats.total * 100) : 0}% completed`} />
        <Stat label="Unwatched" value={stats.unwatched} hint="ready to watch" />
        <Stat label="Runtime" value={`${stats.hours}h`} hint="total indexed runtime" />
      </section>

      {continueWatching.length > 0 && view === 'overview' && <section><SectionTitle title="Continue watching" action={<button type="button" onClick={() => setView('unwatched')} className="text-xs font-medium text-accent">See all</button>} /><div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">{continueWatching.slice(0, 4).map((movie) => <ContinueCard key={movie.id} movie={movie} onPlay={() => setPlaying(movie)} onOpen={() => setSelected(movie)} />)}</div></section>}

      <section>
        <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between"><div className="flex flex-wrap gap-1 rounded-xl border border-border bg-surface p-1">{([['overview','Overview'],['library','All movies'],['unwatched','Unwatched'],['watched','Watched']] as [ViewMode,string][]).map(([key,label]) => <button key={key} type="button" onClick={() => setView(key)} className={`rounded-lg px-3 py-1.5 text-xs font-semibold ${view === key ? 'bg-surface-elevated text-primary shadow-card' : 'text-muted hover:text-primary'}`}>{label}</button>)}</div><div className="flex gap-2"><input aria-label="Search movies" value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search titles…" className="w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm lg:w-64" /><select aria-label="Sort movies" value={sort} onChange={(e) => setSort(e.target.value as SortMode)} className="rounded-xl border border-border bg-surface px-3 py-2 text-sm"><option value="title">Title</option><option value="year">Newest</option><option value="runtime">Runtime</option><option value="recent">Recently watched</option></select></div></div>
        {movies === null ? <LoadingState label="Loading your cinema…" /> : visible.length === 0 ? <EmptyState icon={<FilmIcon className="size-6" />} title={query ? 'No matching movies' : 'No movies found'} description={query ? 'Try another title or clear the search.' : 'Scan a folder containing movie files to populate the collection.'} action={query ? <button type="button" onClick={() => setQuery('')} className="rounded-lg border border-border px-4 py-2 text-sm">Clear search</button> : undefined} /> : <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5 xl:grid-cols-6">{visible.map((movie) => <MovieCard key={movie.id} movie={movie} onPlay={() => setPlaying(movie)} onOpen={() => setSelected(movie)} />)}</div>}
      </section>
    </>}
    {selected && <MovieDetails movie={selected} onClose={() => setSelected(null)} onPlay={() => { setPlaying(selected); setSelected(null); }} onWatched={(watched) => void markWatched(selected, watched)} />}
    {playing && <MoviePlayer movie={playing} onClose={() => { setPlaying(null); refresh(); }} />}
  </div>;
}

function Stat({ label, value, hint }: { label: string; value: string | number; hint: string }) { return <div className="rounded-2xl border border-border bg-surface p-4 shadow-card"><p className="text-xs font-medium text-muted">{label}</p><p className="mt-1 text-2xl font-bold tracking-tight">{value}</p><p className="mt-1 text-[11px] text-secondary">{hint}</p></div>; }
function SectionTitle({ title, action }: { title: string; action?: React.ReactNode }) { return <div className="mb-3 flex items-center justify-between"><h2 className="text-lg font-semibold tracking-tight">{title}</h2>{action}</div>; }

function MovieCard({ movie, onPlay, onOpen }: { movie: MovieSummary; onPlay: () => void; onOpen: () => void }) { const progress = formatProgressPercent(movie.watchProgressSeconds, movie.durationSeconds); return <article className="group overflow-hidden rounded-2xl border border-border bg-surface shadow-card transition hover:-translate-y-0.5 hover:shadow-pop"><button type="button" onClick={onOpen} className="block w-full text-left"><div className="relative aspect-[2/3] overflow-hidden bg-surface-elevated">{movie.hasPoster ? <img src={moviePosterUrl(movie.id)} alt={`Poster of ${movie.title}`} loading="lazy" className="h-full w-full object-cover transition duration-500 group-hover:scale-105" /> : <div className="grid h-full place-items-center bg-gradient-to-br from-accent-soft to-surface"><FilmIcon className="size-10 text-accent opacity-60" /></div>}<div className="absolute inset-0 bg-gradient-to-t from-black/70 via-transparent to-transparent opacity-80" />{movie.watched && <span className="absolute right-2 top-2 rounded-full bg-success px-2 py-1 text-[10px] font-bold text-white">WATCHED</span>}{progress > 0 && !movie.watched && <div className="absolute inset-x-0 bottom-0 h-1 bg-black/50"><div className="h-full bg-accent" style={{ width: `${progress}%` }} /></div>}<span className="absolute bottom-3 left-3 right-3 truncate text-sm font-semibold text-white">{movie.title}</span></div></button><div className="flex items-center justify-between gap-2 p-3"><p className="truncate text-xs text-muted">{movie.year ?? '—'} · {formatRuntime(movie.durationSeconds)}</p><button type="button" aria-label={`Play ${movie.title}`} onClick={onPlay} className="grid size-8 shrink-0 place-items-center rounded-full bg-accent text-white"><PlayIcon className="size-3.5 translate-x-px" /></button></div></article>; }

function ContinueCard({ movie, onPlay, onOpen }: { movie: MovieSummary; onPlay: () => void; onOpen: () => void }) { const progress = formatProgressPercent(movie.watchProgressSeconds, movie.durationSeconds); return <article className="group relative overflow-hidden rounded-2xl border border-border bg-surface shadow-card"><button type="button" onClick={onOpen} className="block w-full text-left"><div className="relative aspect-video overflow-hidden bg-surface-elevated">{movie.hasPoster ? <img src={moviePosterUrl(movie.id)} alt="" className="h-full w-full object-cover opacity-90 transition group-hover:scale-105" /> : <div className="h-full bg-gradient-to-br from-accent-soft to-surface" />}<div className="absolute inset-0 bg-gradient-to-t from-black/80 to-transparent" /><div className="absolute bottom-3 left-3"><p className="font-semibold text-white">{movie.title}</p><p className="text-xs text-white/70">{formatRuntime(movie.durationSeconds)} · {progress}% watched</p></div></div></button><div className="flex gap-2 p-3"><button type="button" onClick={onPlay} className="flex-1 rounded-lg bg-accent px-3 py-2 text-xs font-semibold text-white">Resume</button><button type="button" onClick={onOpen} className="rounded-lg border border-border px-3 py-2 text-xs font-medium">Details</button></div></article>; }

function MovieDetails({ movie, onClose, onPlay, onWatched }: { movie: MovieSummary; onClose: () => void; onPlay: () => void; onWatched: (watched: boolean) => void }) { return <div className="fixed inset-0 z-40 flex justify-end bg-black/50 p-0 backdrop-blur-sm" onMouseDown={(e) => { if (e.target === e.currentTarget) onClose(); }}><aside className="h-full w-full max-w-xl overflow-y-auto border-l border-border bg-surface p-6 shadow-pop sm:p-8"><div className="flex justify-between"><span className="text-xs font-semibold uppercase tracking-[0.18em] text-accent">Movie information</span><button type="button" aria-label="Close details" onClick={onClose} className="text-2xl text-muted hover:text-primary">×</button></div><div className="mt-6 flex gap-5">{movie.hasPoster ? <img src={moviePosterUrl(movie.id)} alt="" className="h-48 w-32 rounded-xl object-cover shadow-card" /> : <div className="grid h-48 w-32 shrink-0 place-items-center rounded-xl bg-surface-elevated"><FilmIcon className="size-8 text-accent" /></div>}<div className="min-w-0"><h2 className="text-2xl font-bold">{movie.title}</h2><p className="mt-2 text-sm text-secondary">{movie.year ?? 'Year unknown'} · {formatRuntime(movie.durationSeconds)} · {(movie.container ?? 'video').replace('.', '').toUpperCase()}</p><div className="mt-4 flex gap-2"><button type="button" onClick={onPlay} className="rounded-xl bg-accent px-4 py-2 text-sm font-semibold text-white">▶ Play</button><button type="button" onClick={() => onWatched(!movie.watched)} className="rounded-xl border border-border px-4 py-2 text-sm">{movie.watched ? 'Mark unwatched' : 'Mark watched'}</button></div></div></div><div className="mt-8 grid grid-cols-2 gap-3"><Info label="Status" value={movie.watched ? 'Watched' : movie.watchProgressSeconds ? `${formatProgressPercent(movie.watchProgressSeconds, movie.durationSeconds)}% complete` : 'Not started'} /><Info label="Runtime" value={formatRuntime(movie.durationSeconds)} /><Info label="Year" value={String(movie.year ?? 'Unknown')} /><Info label="Container" value={(movie.container ?? 'Unknown').toUpperCase()} /></div><div className="mt-6 rounded-2xl border border-border bg-surface-elevated p-4"><p className="text-xs font-semibold uppercase tracking-wide text-muted">Library management</p><p className="mt-2 text-sm text-secondary">This movie is indexed from your local filesystem. Reindex after changing files or folders so the catalogue stays synchronized.</p><button type="button" onClick={() => onWatched(false)} className="mt-4 text-xs font-semibold text-accent">Reset watch status</button></div></aside></div>; }
function Info({ label, value }: { label: string; value: string }) { return <div className="rounded-xl border border-border p-3"><p className="text-[10px] font-semibold uppercase tracking-wide text-muted">{label}</p><p className="mt-1 text-sm font-medium">{value}</p></div>; }

function MoviePlayer({ movie, onClose }: { movie: MovieSummary; onClose: () => void }) { const [position, setPosition] = useState(movie.watchProgressSeconds ?? 0); const [error, setError] = useState(false); const save = () => { if (position > 0) void saveWatchProgress(movie.id, position); }; return <div className="fixed inset-0 z-50 flex flex-col bg-black/95"><div className="flex items-center justify-between px-4 py-3 text-white"><p className="truncate text-sm font-medium">{movie.title}</p><button type="button" aria-label="Close player" onClick={() => { save(); onClose(); }} className="rounded-lg px-3 py-1 text-2xl text-white/70 hover:text-white">×</button></div><div className="flex min-h-0 flex-1 items-center justify-center p-4">{error ? <div className="text-center text-white"><p className="font-semibold">This video cannot play in the browser.</p><p className="mt-2 text-sm text-white/60">The file is indexed, but its container or codec is not browser-compatible.</p></div> : <video src={movieStreamUrl(movie.id)} controls autoPlay playsInline className="max-h-full max-w-full rounded-xl" onLoadedMetadata={(e) => { if (movie.watchProgressSeconds) e.currentTarget.currentTime = movie.watchProgressSeconds; }} onTimeUpdate={(e) => setPosition(e.currentTarget.currentTime)} onPause={save} onEnded={() => { void setMovieWatched(movie.id, true); onClose(); }} onError={() => setError(true)} />}</div></div>; }
