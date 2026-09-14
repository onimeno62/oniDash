import { useEffect, useMemo, useState } from 'react';
import { fetchLibraries, type Library } from '../api/libraries';
import { albumCoverUrl, fetchMusicAlbums, fetchMusicArtists, fetchMusicHomeCollections, fetchMusicPlaylists, formatDuration, type AlbumSummary, type ArtistSummary, type MusicHomeCollections, type PlaylistSummary, type TrackSummary } from '../api/music';
import { MusicIcon } from '../components/icons';
import { MusicPlayerBar } from '../components/MusicPlayerBar';
import { MusicTrackMenu } from '../components/MusicTrackMenu';
import { EmptyState } from '../components/states/EmptyState';
import { ErrorState } from '../components/states/ErrorState';
import { LoadingState } from '../components/states/LoadingState';
import { usePlayer } from '../hooks/usePlayer';

type Layout = 'standard' | 'bento';

const toTrack = (track: MusicHomeCollections['recentlyAdded'][number]): TrackSummary => ({ ...track });

function Cover({ track, className = '' }: { track: TrackSummary; className?: string }) {
  return track.albumId && track.hasCover ? <img src={albumCoverUrl(track.albumId)} alt="" loading="lazy" className={`object-cover ${className}`} /> : <div className={`grid place-items-center bg-surface-elevated text-primary/20 ${className}`}><MusicIcon className="size-8" /></div>;
}

function TrackCard({ track, onPlay }: { track: TrackSummary; onPlay: () => void }) {
  return <article className="group relative min-w-[190px] rounded-2xl border border-border bg-surface/70 p-3 hover:bg-surface-elevated"><button type="button" onClick={onPlay} className="relative block w-full overflow-hidden rounded-xl text-left" aria-label={`Play ${track.title}`}><Cover track={track} className="aspect-square w-full rounded-xl" /><span className="absolute bottom-2 right-2 grid size-9 place-items-center rounded-full bg-primary text-background opacity-0 shadow-lg transition group-hover:opacity-100">▶</span></button><div className="mt-3 min-w-0 pr-8"><p className="truncate text-sm font-semibold text-primary">{track.title}</p><p className="truncate text-xs text-secondary">{track.artistName ?? 'Unknown artist'}</p><p className="mt-1 truncate text-[11px] text-tertiary">{track.albumTitle} · {formatDuration(track.durationSeconds)}</p></div><MusicTrackMenu track={track} onChanged={() => undefined} /></article>;
}

function Carousel({ title, tracks, onPlay }: { title: string; tracks: TrackSummary[]; onPlay: (track: TrackSummary) => void }) {
  if (!tracks.length) return null;
  return <section><div className="mb-4 flex items-center justify-between"><h2 className="text-lg font-semibold tracking-tight text-primary">{title}</h2><span className="text-xs text-tertiary">{tracks.length}</span></div><div className="flex snap-x gap-4 overflow-x-auto pb-2 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">{tracks.map(track => <TrackCard key={track.id} track={track} onPlay={() => onPlay(track)} />)}</div></section>;
}

function ContinueHero({ track, progress, onPlay }: { track: TrackSummary | null; progress: number; onPlay: () => void }) {
  if (!track) return <section className="rounded-3xl border border-border bg-surface p-8"><p className="text-xs font-semibold uppercase tracking-[.2em] text-accent">Continue listening</p><h2 className="mt-2 text-2xl font-semibold text-primary">Nothing to resume</h2><p className="mt-2 text-sm text-secondary">Start a track and oniDash will remember your position.</p></section>;
  const duration = track.durationSeconds ?? 0;
  const percent = duration ? Math.min(100, Math.max(0, progress / duration * 100)) : 0;
  return <section className="relative overflow-hidden rounded-3xl border border-border bg-surface"><div className="absolute inset-0 opacity-[.06]" style={track.albumId ? { backgroundImage: `url(${albumCoverUrl(track.albumId)})`, backgroundPosition: 'center', backgroundSize: 'cover', filter: 'blur(30px)' } : undefined} /><div className="relative grid items-center gap-7 p-5 sm:p-8 lg:grid-cols-[260px_1fr] lg:p-10"><div className="overflow-hidden rounded-2xl border border-border shadow-2xl"><Cover track={track} className="aspect-square w-full" /></div><div className="min-w-0"><p className="text-xs font-semibold uppercase tracking-[.2em] text-accent">Continue listening</p><h1 className="mt-3 truncate text-3xl font-bold tracking-tight text-primary sm:text-4xl">{track.title}</h1><p className="mt-2 truncate text-base text-secondary">{track.artistName ?? 'Unknown artist'} · {track.albumTitle}</p><div className="mt-6 h-1.5 overflow-hidden rounded-full bg-border"><div className="h-full rounded-full bg-accent" style={{ width: `${percent}%` }} /></div><div className="mt-2 flex justify-between text-[11px] tabular-nums text-tertiary"><span>{formatDuration(progress)}</span><span>{formatDuration(duration)}</span></div><button type="button" onClick={onPlay} className="mt-6 rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-background hover:opacity-90">Resume</button></div></div></section>;
}

function Discovery({ albums, artists, playlists }: { albums: AlbumSummary[]; artists: ArtistSummary[]; playlists: PlaylistSummary[] }) {
  return <div className="grid gap-8 xl:grid-cols-3"><section><h2 className="mb-4 text-lg font-semibold text-primary">Albums</h2><div className="grid grid-cols-2 gap-3">{albums.slice(0, 6).map(album => <div key={album.id} className="min-w-0"><div className="overflow-hidden rounded-xl border border-border bg-surface"><img src={albumCoverUrl(album.id)} alt="" className="aspect-square w-full object-cover" /></div><p className="mt-2 truncate text-sm font-medium text-primary">{album.title}</p><p className="truncate text-xs text-tertiary">{album.artistName ?? 'Unknown artist'}</p></div>)}</div></section><section><h2 className="mb-4 text-lg font-semibold text-primary">Artists</h2><div className="grid grid-cols-2 gap-3">{artists.slice(0, 6).map(artist => <div key={artist.id} className="rounded-xl border border-border bg-surface p-4"><div className="grid aspect-square place-items-center rounded-full bg-surface-elevated text-3xl font-semibold text-primary/50">{artist.name.slice(0, 1).toUpperCase()}</div><p className="mt-3 truncate text-sm font-medium text-primary">{artist.name}</p></div>)}</div></section><section><h2 className="mb-4 text-lg font-semibold text-primary">Playlists</h2><div className="space-y-2">{playlists.slice(0, 8).map(playlist => <div key={playlist.id} className="rounded-xl border border-border bg-surface p-4"><p className="truncate text-sm font-medium text-primary">{playlist.name}</p><p className="mt-1 text-xs text-tertiary">{playlist.isSmart ? 'Smart playlist' : 'Playlist'}</p></div>)}</div></section></div>;
}

export function MusicDashboardHomePage() {
  const player = usePlayer();
  const [libraries, setLibraries] = useState<Library[] | null>(null);
  const [libraryId, setLibraryId] = useState('');
  const [home, setHome] = useState<MusicHomeCollections | null>(null);
  const [albums, setAlbums] = useState<AlbumSummary[]>([]);
  const [artists, setArtists] = useState<ArtistSummary[]>([]);
  const [playlists, setPlaylists] = useState<PlaylistSummary[]>([]);
  const [layout, setLayout] = useState<Layout>(() => localStorage.getItem('onidash.music.dashboard.layout') === 'bento' ? 'bento' : 'standard');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => { const controller = new AbortController(); fetchLibraries(controller.signal).then(items => { setLibraries(items); if (items[0]) setLibraryId(v => v || items[0].id); }).catch(e => { if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'Could not load libraries.'); }); return () => controller.abort(); }, []);
  useEffect(() => { if (!libraryId) return; const controller = new AbortController(); Promise.all([fetchMusicHomeCollections(libraryId, 12, controller.signal), fetchMusicAlbums(libraryId, { limit: 12, signal: controller.signal }), fetchMusicArtists(libraryId, { limit: 12, signal: controller.signal }), fetchMusicPlaylists(controller.signal)]).then(([h, a, ar, p]) => { setHome(h); setAlbums(a); setArtists(ar); setPlaylists(p); setError(null); }).catch(e => { if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'Could not load Music Home.'); }); return () => controller.abort(); }, [libraryId]);
  useEffect(() => { localStorage.setItem('onidash.music.dashboard.layout', layout); }, [layout]);

  const continueTrack = useMemo(() => {
    const active = player.current?.track;
    if (active) return active;
    const item = home?.continueListening[0];
    if (!item) return null;
    return [...(home?.recentlyPlayed ?? []), ...(home?.recentlyAdded ?? [])].map(toTrack).find(track => track.id === item.trackId) ?? null;
  }, [home, player.current]);
  const continuePosition = continueTrack?.id === player.current?.track.id ? player.position : home?.continueListening.find(x => x.trackId === continueTrack?.id)?.positionSeconds ?? 0;
  const play = (track: TrackSummary) => player.toggle(track);

  if (error) return <ErrorState title="Music Home unavailable" message={error} onRetry={() => setLibraryId(id => id)} />;
  if (!libraries) return <LoadingState label="Loading Music…" />;
  if (!libraryId) return <EmptyState icon={<MusicIcon className="size-6" />} title="No music library" description="Create a music library and scan your audio folder to get started." />;
  if (!home) return <LoadingState label="Loading your music…" />;

  const sections = <><Carousel title="Recently played" tracks={home.recentlyPlayed.map(toTrack)} onPlay={play} /><Carousel title="Recently added" tracks={home.recentlyAdded.map(toTrack)} onPlay={play} /><Carousel title="Most played" tracks={home.mostPlayed.map(toTrack)} onPlay={play} /><Carousel title="Favorites" tracks={home.favorites.map(toTrack)} onPlay={play} /><Carousel title="Top rated" tracks={home.topRated.map(toTrack)} onPlay={play} /><Carousel title="Never played" tracks={home.neverPlayed.map(toTrack)} onPlay={play} /><Discovery albums={albums} artists={artists} playlists={playlists} /></>;
  return <div className="pb-24"><header className="mb-7 flex flex-col gap-4 border-b border-border pb-5 xl:flex-row xl:items-end xl:justify-between"><div><p className="text-xs font-semibold uppercase tracking-[.2em] text-accent">Music</p><h1 className="mt-1 text-3xl font-bold tracking-tight text-primary sm:text-4xl">Your library</h1><p className="mt-2 text-sm text-secondary">A server-backed view of what you listen to, love, and still need to hear.</p></div><div className="flex items-center gap-2"><select aria-label="Music library" value={libraryId} onChange={e => setLibraryId(e.target.value)} className="rounded-xl border border-border bg-surface px-3 py-2 text-sm text-primary">{libraries.map(l => <option key={l.id} value={l.id}>{l.name}</option>)}</select><div className="flex rounded-xl border border-border bg-surface p-1"><button type="button" onClick={() => setLayout('standard')} className={`rounded-lg px-3 py-1.5 text-xs ${layout === 'standard' ? 'bg-primary text-background' : 'text-secondary'}`}>Standard</button><button type="button" onClick={() => setLayout('bento')} className={`rounded-lg px-3 py-1.5 text-xs ${layout === 'bento' ? 'bg-primary text-background' : 'text-secondary'}`}>Bento</button></div></div></header><div className="space-y-10"><ContinueHero track={continueTrack} progress={continuePosition} onPlay={() => continueTrack && play(continueTrack)} />{sections}</div><MusicPlayerBar /></div>;
}
