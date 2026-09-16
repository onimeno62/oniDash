import { formatDuration, type MusicOverview } from '../../api/music';

export function MusicStatsOverview({ overview }: { overview: MusicOverview | null }) {
  if (!overview) return null;

  const items = [
    {
      label: 'Tracks Indexed',
      value: overview.tracks.toLocaleString(),
      desc: 'Lossless audio files',
      icon: '🎼',
    },
    {
      label: 'Albums',
      value: overview.albums.toLocaleString(),
      desc: 'Complete records',
      icon: '💿',
    },
    {
      label: 'Artists',
      value: overview.artists.toLocaleString(),
      desc: 'In local catalogue',
      icon: '🎙️',
    },
    {
      label: 'Listening Time',
      value: formatDuration(overview.listeningSeconds),
      desc: 'Total hours enjoyed',
      icon: '⏱️',
    },
  ];

  return (
    <section id="library-stats-overview" className="flex flex-col">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-bold tracking-tight text-primary">
          Library telemetry & analytics
        </h2>
        <span className="text-xs font-medium text-tertiary">
          Local SQLite verified
        </span>
      </div>

      <div className="grid grid-cols-2 gap-3.5 sm:grid-cols-4">
        {items.map((item) => (
          <div
            key={item.label}
            className="group relative overflow-hidden rounded-2xl border border-border/80 bg-surface/80 p-4 transition-all duration-200 hover:border-accent/40 hover:bg-surface-elevated hover:shadow-md"
          >
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold uppercase tracking-wider text-tertiary">
                {item.label}
              </span>
              <span className="text-base opacity-70 transition-transform group-hover:scale-110">
                {item.icon}
              </span>
            </div>
            <p className="mt-3 text-2xl font-black tracking-tight tabular-nums text-primary">
              {item.value}
            </p>
            <p className="mt-1 text-[11px] text-tertiary">{item.desc}</p>
          </div>
        ))}
      </div>
    </section>
  );
}
