import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { fetchLibraries, type Library } from '../api/libraries';
import { fetchMusicAlbums, fetchMusicArtists, fetchMusicTracks, formatDuration, albumCoverUrl, type AlbumSummary, type ArtistSummary, type TrackSummary } from '../api/music';
import { fetchMovies, moviePosterUrl, formatRuntime, type MovieSummary } from '../api/movies';
import { EmptyState } from '../components/states/EmptyState';
import { LoadingState } from '../components/states/LoadingState';
import { MusicPlayerBar } from '../components/MusicPlayerBar';
import { usePlayer } from '../hooks/usePlayer';

export function MediaDetailPage() {
  const { kind, id } = useParams<{ kind: string; id: string }>();
  const navigate = useNavigate();
  const player = usePlayer();
  const [library, setLibrary] = useState<Library | null>(null);
  const [album, setAlbum] = useState<AlbumSummary | null>(null);
  const [artist, setArtist] = useState<ArtistSummary | null>(null);
  const [tracks, setTracks] = useState<TrackSummary[]>([]);
  const [movie, setMovie] = useState<MovieSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id || !kind) return;
    const controller = new AbortController();
    setLoading(true); setError(null);
    fetchLibraries(controller.signal).then(async libraries => {
      const lib = libraries[0];
      setLibrary(lib ?? null);
      if (!lib) return;
      if (kind === 'album') {
        const albums = await fetchMusicAlbums(lib.id, { limit: 1000 });
        const found = albums.find(x => x.id === id) ?? null;
        setAlbum(found);
        setTracks(found ? await fetchMusicTracks(lib.id, { albumId: id, limit: 1000 }) : []);
      } else if (kind === 'artist') {
        const artists = await fetchMusicArtists(lib.id, { limit: 1000 });
        const found = artists.find(x => x.id === id) ?? null;
        setArtist(found);
        setTracks(found ? await fetchMusicTracks(lib.id, { artistId: id, limit: 1000 }) : []);
      } else if (kind === 'movie') {
        const movies = await fetchMovies(lib.id, { limit: 1000 });
        setMovie(movies.find(x => x.id === id) ?? null);
      }
    }).catch(e => { if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'Could not load media details.'); })
      .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [kind, id]);

  if (loading) return <LoadingState label="Loading media details…" />;
  if (error) return <EmptyState title="Could not load details" description={error} action={<button type="button" onClick={() => navigate(-1)} className="rounded-lg border border-border px-4 py-2 text-sm">Go back</button>} />;
  if (movie) return <MovieDetail movie={movie} onBack={() => navigate(-1)} />;
  if (album) return <MusicCollection title={album.title} subtitle={`${album.artistName ?? 'Unknown artist'}${album.year ? ` · ${album.year}` : ''}`} cover={album.hasCover ? albumCoverUrl(album.id) : undefined} tracks={tracks} player={player} onBack={() => navigate(-1)} />;
  if (artist) return <MusicCollection title={artist.name} subtitle={`${tracks.length} tracks`} tracks={tracks} player={player} onBack={() => navigate(-1)} />;
  return <EmptyState title="Media not found" description="This item may have been removed or reindexed." action={<button type="button" onClick={() => navigate(-1)} className="rounded-lg border border-border px-4 py-2 text-sm">Go back</button>} />;
}

function MusicCollection({ title, subtitle, cover, tracks, player, onBack }: { title: string; subtitle: string; cover?: string; tracks: TrackSummary[]; player: ReturnType<typeof usePlayer>; onBack: () => void }) {
  return <div className="space-y-6 pb-24"><button type="button" onClick={onBack} className="text-sm text-secondary hover:text-primary">← Back</button><header className="flex flex-col gap-5 rounded-3xl border border-border bg-surface-elevated p-6 sm:flex-row sm:items-end sm:p-8">{cover ? <img src={cover} alt="" className="size-40 rounded-2xl object-cover shadow-card" /> : <div className="grid size-40 shrink-0 place-items-center rounded-2xl bg-accent-soft text-4xl">♫</div>}<div><p className="text-xs font-semibold uppercase tracking-[.2em] text-accent">Music collection</p><h1 className="mt-2 text-3xl font-bold tracking-tight">{title}</h1><p className="mt-2 text-sm text-secondary">{subtitle}</p><p className="mt-3 text-xs text-tertiary">{tracks.length} tracks · {formatDuration(tracks.reduce((s,t) => s + (t.durationSeconds ?? 0), 0))} total</p></div></header><div className="overflow-hidden rounded-2xl border border-border bg-surface">{tracks.map((track, index) => <button key={track.id} type="button" onClick={() => player.toggle(track)} className="flex w-full items-center gap-4 border-b border-border px-4 py-3 text-left last:border-0 hover:bg-surface-elevated"><span className="w-7 text-right text-xs text-tertiary">{index + 1}</span><span className="min-w-0 flex-1"><span className="block truncate text-sm font-medium">{track.title}</span><span className="block truncate text-xs text-secondary">{track.artistName ?? 'Unknown artist'} · {track.albumTitle}</span></span><span className="text-xs text-tertiary">{formatDuration(track.durationSeconds)}</span></button>)}</div><MusicPlayerBar /></div>;
}

function MovieDetail({ movie, onBack }: { movie: MovieSummary; onBack: () => void }) { return <div className="space-y-6 pb-10"><button type="button" onClick={onBack} className="text-sm text-secondary hover:text-primary">← Back</button><section className="overflow-hidden rounded-3xl border border-border bg-surface"><div className="grid gap-0 lg:grid-cols-[280px_1fr]">{movie.hasPoster ? <img src={moviePosterUrl(movie.id)} alt={`Poster of ${movie.title}`} className="h-full min-h-[420px] w-full object-cover" /> : <div className="grid min-h-[420px] place-items-center bg-accent-soft text-6xl">🎬</div>}<div className="p-6 sm:p-8"><p className="text-xs font-semibold uppercase tracking-[.2em] text-accent">Movie details</p><h1 className="mt-2 text-3xl font-bold">{movie.title}</h1><p className="mt-2 text-sm text-secondary">{movie.year ?? 'Year unknown'} · {formatRuntime(movie.durationSeconds)} · {(movie.container ?? 'video').replace('.', '').toUpperCase()}</p><div className="mt-8 grid grid-cols-2 gap-3 sm:grid-cols-3"><Info label="Status" value={movie.watched ? 'Watched' : 'Unwatched'} /><Info label="Runtime" value={formatRuntime(movie.durationSeconds)} /><Info label="Year" value={String(movie.year ?? 'Unknown')} /></div></div></div></section></div> }
function Info({ label, value }: { label: string; value: string }) { return <div className="rounded-xl border border-border bg-surface-elevated p-3"><p className="text-[10px] uppercase tracking-wider text-tertiary">{label}</p><p className="mt-1 text-sm font-medium">{value}</p></div>; }
