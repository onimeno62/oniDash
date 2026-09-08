import { useCallback, useEffect, useRef, useState } from 'react';
import { fetchContinueWatching, fetchMovies, formatProgressPercent, formatRuntime, moviePosterUrl, movieStreamUrl, saveWatchProgress, setMovieWatched, type MovieSummary } from '../api/movies';
import { fetchLibraries, type Library } from '../api/libraries';
import { ErrorState } from '../components/states/ErrorState';
import { EmptyState } from '../components/states/EmptyState';
import { LoadingState } from '../components/states/LoadingState';
import { FilmIcon, PlayIcon } from '../components/icons';

export function MoviesPage() {
  const [libraries, setLibraries] = useState<Library[] | null>(null);
  const [libraryId, setLibraryId] = useState('');
  const [watchedFilter, setWatchedFilter] = useState<'all' | 'unwatched' | 'watched'>('all');
  const [movies, setMovies] = useState<MovieSummary[] | null>(null);
  const [continueWatching, setContinueWatching] = useState<MovieSummary[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [playing, setPlaying] = useState<MovieSummary | null>(null);
  const [reload, setReload] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    fetchLibraries(controller.signal).then((all) => { setLibraries(all); setLibraryId((current) => current || all[0]?.id || ''); }).catch((loadError: unknown) => {
      if (!controller.signal.aborted) setError(loadError instanceof Error ? loadError.message : 'Could not load libraries.');
    });
    return () => controller.abort();
  }, []);

  useEffect(() => {
    if (!libraryId) return;
    const controller = new AbortController();
    setMovies(null); setError(null);
    fetchMovies(libraryId, { limit: 100, watched: watchedFilter === 'all' ? undefined : watchedFilter === 'watched', signal: controller.signal })
      .then(setMovies).catch((loadError: unknown) => { if (!controller.signal.aborted) setError(loadError instanceof Error ? loadError.message : 'Could not load movies.'); });
    fetchContinueWatching(libraryId, controller.signal).then(setContinueWatching).catch(() => setContinueWatching([]));
    return () => controller.abort();
  }, [libraryId, watchedFilter, reload]);

  const refresh = useCallback(() => setReload((n) => n + 1), []);
  return <div className="space-y-6">
    <div className="flex flex-wrap items-end justify-between gap-4"><div><h2 className="text-2xl font-bold tracking-tight">Movies</h2><p className="mt-1 text-sm text-secondary">Films detected in your indexed video files, with resume support.</p></div>
      <div className="flex items-center gap-2 text-sm text-secondary"><label className="flex items-center gap-2">Library<select aria-label="Movies library" value={libraryId} onChange={(event) => setLibraryId(event.target.value)} className="rounded-lg border border-border bg-surface px-3 py-2 text-sm shadow-card"><option value="" disabled>{libraries === null ? 'Loading…' : 'Choose a library'}</option>{(libraries ?? []).map((library) => <option key={library.id} value={library.id}>{library.name}</option>)}</select></label><select aria-label="Watched filter" value={watchedFilter} onChange={(event) => setWatchedFilter(event.target.value as typeof watchedFilter)} className="rounded-lg border border-border bg-surface px-3 py-2 text-sm shadow-card"><option value="all">All movies</option><option value="unwatched">Unwatched</option><option value="watched">Watched</option></select></div>
    </div>
    {error && <ErrorState title="Could not load movies" message={error} onRetry={refresh} />}
    {!error && libraryId && continueWatching.length > 0 && !playing && <section aria-label="Continue watching"><h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-muted">Continue watching</h3><ul className="flex gap-3 overflow-x-auto pb-2">{continueWatching.map((movie) => <li key={movie.id} className="w-64 shrink-0"><MovieCard movie={movie} onPlay={() => setPlaying(movie)} /></li>)}</ul></section>}
    {!error && libraryId && <section aria-label="Movie collection">{movies === null ? <LoadingState label="Loading movies…" /> : movies.length === 0 ? <EmptyState icon={<FilmIcon className="size-6" />} title="No movies here yet" description="Scan a folder containing video files (mp4, mkv, avi…); titles are parsed from the filenames." action={<button type="button" onClick={refresh} className="rounded-lg border border-border bg-surface-elevated px-4 py-2 text-sm font-medium">Check again</button>} /> : <ul className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">{movies.map((movie) => <li key={movie.id}><MovieCard movie={movie} onPlay={() => setPlaying(movie)} /></li>)}</ul>}</section>}
    {!error && !libraryId && libraries !== null && <EmptyState icon={<FilmIcon className="size-6" />} title="No libraries yet" description="Create a library and scan a movie folder to fill this page." />}
    {playing && <MoviePlayer movie={playing} onClose={() => { setPlaying(null); refresh(); }} />}
  </div>;
}

function MovieCard({ movie, onPlay }: { movie: MovieSummary; onPlay: () => void }) {
  const percent = formatProgressPercent(movie.watchProgressSeconds, movie.durationSeconds);
  return <figure className="overflow-hidden rounded-2xl border border-border bg-surface shadow-card"><div className="relative">{movie.hasPoster ? <img src={moviePosterUrl(movie.id)} alt={`Poster of ${movie.title}`} loading="lazy" className="aspect-[2/3] w-full object-cover" /> : <div aria-hidden className="grid aspect-[2/3] w-full place-items-center bg-gradient-to-br from-accent-soft to-surface"><FilmIcon className="size-10 text-accent opacity-70" /></div>}<button type="button" aria-label={`Play ${movie.title}`} onClick={onPlay} className="absolute inset-0 grid place-items-center bg-black/0 opacity-0 transition-all hover:bg-black/40 hover:opacity-100 focus-visible:bg-black/40 focus-visible:opacity-100"><span className="grid size-12 place-items-center rounded-full bg-accent text-white shadow-pop"><PlayIcon className="size-5 translate-x-0.5" /></span></button>{movie.watched && <span className="absolute right-2 top-2 rounded-full bg-success px-2 py-0.5 text-[11px] font-semibold text-white">Watched</span>}{percent > 0 && !movie.watched && <div aria-label={`Resume at ${percent}%`} className="absolute inset-x-0 bottom-0 h-1.5 bg-black/50"><div className="h-full bg-accent" style={{ width: `${percent}%` }} /></div>}</div><figcaption className="space-y-0.5 p-3"><p className="truncate text-sm font-medium">{movie.title}</p><p className="truncate text-xs text-muted">{movie.year ?? '—'} · {formatRuntime(movie.durationSeconds)}</p></figcaption></figure>;
}

function MoviePlayer({ movie, onClose }: { movie: MovieSummary; onClose: () => void }) {
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const [started, setStarted] = useState(false);
  const [mediaError, setMediaError] = useState(false);
  const lastSavedSecond = useRef(-1);
  const savePosition = useCallback(() => { const video = videoRef.current; if (video && video.currentTime > 0) { lastSavedSecond.current = Math.floor(video.currentTime); void saveWatchProgress(movie.id, video.currentTime).catch(() => undefined); } }, [movie.id]);
  useEffect(() => { const onKey = (event: KeyboardEvent) => { if (event.key === 'Escape') { savePosition(); onClose(); } }; window.addEventListener('keydown', onKey); return () => window.removeEventListener('keydown', onKey); }, [onClose, savePosition]);
  const onLoadedMetadata = useCallback(() => { const video = videoRef.current; if (video && !started && movie.watchProgressSeconds) { const maxPosition = movie.durationSeconds ? Math.max(0, movie.durationSeconds * 0.94) : movie.watchProgressSeconds; video.currentTime = Math.min(movie.watchProgressSeconds, maxPosition); } setStarted(true); }, [movie.durationSeconds, movie.watchProgressSeconds, started]);
  const onTimeUpdate = useCallback(() => { const video = videoRef.current; if (video && !video.paused && Math.floor(video.currentTime) % 10 === 0 && Math.floor(video.currentTime) !== lastSavedSecond.current) savePosition(); }, [savePosition]);
  const markWatched = useCallback(async () => { try { await setMovieWatched(movie.id, true); } finally { onClose(); } }, [movie.id, onClose]);
  return <div className="fixed inset-0 z-50 flex flex-col bg-black/95" data-testid="movie-player"><div className="flex items-center justify-between px-4 py-3"><p className="truncate text-sm font-medium text-white">{movie.title}{movie.year ? ` (${movie.year})` : ''}</p><div className="flex items-center gap-2"><button type="button" onClick={() => void markWatched()} className="rounded-lg border border-white/20 px-3 py-1.5 text-sm text-white">Mark watched</button><button type="button" aria-label="Close player" onClick={() => { savePosition(); onClose(); }} className="icon-btn text-white">×</button></div></div><div className="flex min-h-0 flex-1 items-center justify-center p-4">{mediaError ? <div role="alert" className="max-w-md space-y-3 text-center text-white"><h3 className="text-lg font-semibold">This file cannot play in the browser</h3><p className="text-sm text-white/70">The file is indexed safely, but its container or codec is not supported here.</p><button type="button" onClick={onClose} className="rounded-lg border border-white/20 px-3 py-2 text-sm text-white">Close</button></div> : <video ref={videoRef} src={movieStreamUrl(movie.id)} controls autoPlay playsInline className="max-h-full max-w-full rounded-xl shadow-pop" onLoadedMetadata={onLoadedMetadata} onTimeUpdate={onTimeUpdate} onPause={savePosition} onError={() => setMediaError(true)} onEnded={() => void markWatched()} />}</div></div>;
}
