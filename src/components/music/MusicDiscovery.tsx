import { albumCoverUrl, type AlbumSummary, type ArtistSummary, type PlaylistSummary } from '../../api/music';
import { PlayIcon } from '../icons';

export function MusicDiscovery({
  albums,
  artists,
  playlists,
  onPlayAlbum,
}: {
  albums: AlbumSummary[];
  artists: ArtistSummary[];
  playlists: PlaylistSummary[];
  onPlayAlbum?: (album: AlbumSummary) => void;
}) {
  return (
    <div className="grid gap-8 lg:grid-cols-3" id="music-discovery-section">
      {/* Featured Albums */}
      <section className="flex flex-col">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-bold tracking-tight text-primary">
            Albums to rediscover
          </h2>
          <span className="text-xs font-medium text-tertiary">
            {albums.length} in collection
          </span>
        </div>
        <div className="grid grid-cols-2 gap-3.5 sm:grid-cols-3 lg:grid-cols-2">
          {albums.slice(0, 6).map((album) => (
            <div
              key={album.id}
              className="group relative flex flex-col rounded-xl border border-border/70 bg-surface/70 p-2.5 transition-all duration-200 hover:-translate-y-0.5 hover:border-accent/40 hover:bg-surface-elevated hover:shadow-md"
            >
              <div className="relative aspect-square w-full overflow-hidden rounded-lg bg-surface-elevated shadow-inner">
                <img
                  src={albumCoverUrl(album.id)}
                  alt={album.title}
                  loading="lazy"
                  className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
                />
                {onPlayAlbum && (
                  <button
                    type="button"
                    onClick={() => onPlayAlbum(album)}
                    aria-label={`Play album ${album.title}`}
                    className="absolute bottom-2 right-2 flex size-8 items-center justify-center rounded-full bg-accent text-white opacity-0 shadow-lg transition-all duration-200 group-hover:opacity-100 hover:scale-110"
                  >
                    <PlayIcon className="size-4 translate-x-0.5 fill-current" />
                  </button>
                )}
              </div>
              <div className="mt-2 min-w-0">
                <p className="truncate text-xs font-semibold text-primary group-hover:text-accent" title={album.title}>
                  {album.title}
                </p>
                <p className="truncate text-[11px] text-tertiary" title={album.artistName ?? 'Unknown'}>
                  {album.artistName ?? 'Unknown'}
                  {album.year ? ` · ${album.year}` : ''}
                </p>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* Top Artists */}
      <section className="flex flex-col">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-bold tracking-tight text-primary">
            Favorite artists
          </h2>
          <span className="text-xs font-medium text-tertiary">
            {artists.length} artists
          </span>
        </div>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-2">
          {artists.slice(0, 6).map((artist, idx) => {
            const avatarGradients = [
              'from-amber-600/30 to-rose-700/40 text-amber-300',
              'from-cyan-600/30 to-blue-700/40 text-cyan-300',
              'from-purple-600/30 to-pink-700/40 text-purple-300',
              'from-emerald-600/30 to-teal-700/40 text-emerald-300',
              'from-indigo-600/30 to-violet-700/40 text-indigo-300',
              'from-orange-600/30 to-red-700/40 text-orange-300',
            ];
            const grad = avatarGradients[idx % avatarGradients.length];

            return (
              <div
                key={artist.id}
                className="group flex flex-col items-center rounded-2xl border border-border/70 bg-surface/70 p-4 text-center transition-all duration-200 hover:-translate-y-0.5 hover:border-accent/40 hover:bg-surface-elevated hover:shadow-md"
              >
                <div
                  className={`flex size-16 items-center justify-center rounded-full bg-gradient-to-br shadow-inner ring-2 ring-border/50 transition-transform duration-300 group-hover:scale-105 ${grad}`}
                >
                  <span className="text-xl font-bold tracking-wider">
                    {artist.name.slice(0, 2).toUpperCase()}
                  </span>
                </div>
                <p className="mt-3 w-full truncate text-xs font-semibold text-primary group-hover:text-accent" title={artist.name}>
                  {artist.name}
                </p>
                <span className="mt-0.5 text-[10px] text-tertiary">
                  Artist catalog
                </span>
              </div>
            );
          })}
        </div>
      </section>

      {/* Curated Playlists */}
      <section className="flex flex-col">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-bold tracking-tight text-primary">
            Playlists & Mixes
          </h2>
          <span className="text-xs font-medium text-tertiary">
            {playlists.length} playlists
          </span>
        </div>
        <div className="space-y-2.5">
          {playlists.slice(0, 6).map((playlist) => (
            <div
              key={playlist.id}
              className="group flex items-center justify-between rounded-xl border border-border/70 bg-surface/70 px-4 py-3 transition-all duration-200 hover:border-accent/40 hover:bg-surface-elevated hover:shadow-sm"
            >
              <div className="flex items-center gap-3 min-w-0">
                <div className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-surface-elevated text-secondary group-hover:bg-accent/15 group-hover:text-accent">
                  <span className="text-sm">📻</span>
                </div>
                <div className="min-w-0">
                  <p className="truncate text-xs font-semibold text-primary group-hover:text-accent">
                    {playlist.name}
                  </p>
                  <p className="text-[11px] text-tertiary">
                    {playlist.isSmart ? '⚡ Smart Playlist' : 'Manual Curation'}
                  </p>
                </div>
              </div>
              <span className="text-xs text-tertiary opacity-0 transition-opacity group-hover:opacity-100">
                Open →
              </span>
            </div>
          ))}
          {playlists.length === 0 && (
            <div className="rounded-xl border border-dashed border-border/80 p-6 text-center text-xs text-tertiary">
              No playlists found. Create a smart playlist in library view.
            </div>
          )}
        </div>
      </section>
    </div>
  );
}
