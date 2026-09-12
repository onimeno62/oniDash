import { useEffect, useState } from 'react';
import {
  fetchContinueMedia,
  fetchRecentlyAdded,
  fetchRecentlyPlayed,
  fetchFavorites,
  fetchActivityTimeline,
  fetchLocalRecommendations,
} from '../api/dashboard';
import type { DashboardMediaItem, DashboardActivity } from '../api/dashboard';
import { LoadingState } from '../components/states/LoadingState';
import { EmptyState } from '../components/states/EmptyState';

export function UnifiedDashboardPage() {
  const [continueMedia, setContinueMedia] = useState<DashboardMediaItem[]>([]);
  const [recentAdded, setRecentAdded] = useState<DashboardMediaItem[]>([]);
  const [recentPlayed, setRecentPlayed] = useState<DashboardMediaItem[]>([]);
  const [favorites, setFavorites] = useState<DashboardMediaItem[]>([]);
  const [activity, setActivity] = useState<DashboardActivity[]>([]);
  const [recommendations, setRecommendations] = useState<DashboardMediaItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const c = new AbortController();
    Promise.all([
      fetchContinueMedia(6, c.signal),
      fetchRecentlyAdded(6, c.signal),
      fetchRecentlyPlayed(6, c.signal),
      fetchFavorites(6, c.signal),
      fetchActivityTimeline(10, c.signal),
      fetchLocalRecommendations(6, c.signal),
    ])
      .then(([cont, added, played, favs, act, recs]) => {
        setContinueMedia(cont);
        setRecentAdded(added);
        setRecentPlayed(played);
        setFavorites(favs);
        setActivity(act);
        setRecommendations(recs);
        setLoading(false);
      })
      .catch((e) => {
        if (!c.signal.aborted) {
          setError(e instanceof Error ? e.message : 'Could not load unified dashboard.');
          setLoading(false);
        }
      });

    return () => c.abort();
  }, []);

  if (loading) return <LoadingState label="Loading unified dashboard…" />;
  if (error) return <EmptyState title="Dashboard error" description={error} />;

  const renderGrid = (items: DashboardMediaItem[], emptyMsg: string) => {
    if (!items.length) return <p className="mt-2 text-sm text-secondary">{emptyMsg}</p>;
    return (
      <div className="mt-4 grid gap-3 sm:grid-cols-2 md:grid-cols-3">
        {items.map((item) => (
          <article key={item.id} className="rounded-xl border border-border bg-surface-raised p-4 transition-colors hover:border-accent">
            <span className="inline-block rounded bg-surface px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider text-accent">
              {item.mediaType}
            </span>
            <p className="mt-2 truncate font-semibold text-primary">{item.title}</p>
            <p className="mt-1 text-xs text-secondary">
              {new Date(item.timestamp).toLocaleDateString()}
            </p>
          </article>
        ))}
      </div>
    );
  };

  return (
    <div className="space-y-8 pb-12">
      <header>
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-accent">Unified Platform</p>
        <h1 className="mt-2 text-3xl font-bold">Your media dashboard</h1>
      </header>

      {/* Continue Reading/Watching/Listening */}
      <section className="rounded-2xl border border-border bg-surface p-6">
        <h2 className="font-semibold text-lg">Continue Reading / Watching / Listening</h2>
        {renderGrid(continueMedia, 'Nothing in progress right now.')}
      </section>

      {/* Recently Added */}
      <section className="rounded-2xl border border-border bg-surface p-6">
        <h2 className="font-semibold text-lg">Recently Added</h2>
        {renderGrid(recentAdded, 'No recently added media.')}
      </section>

      {/* Recently Played / Read */}
      <section className="rounded-2xl border border-border bg-surface p-6">
        <h2 className="font-semibold text-lg">Recently Played / Read</h2>
        {renderGrid(recentPlayed, 'No recent playback activity.')}
      </section>

      {/* Favorites across media */}
      <section className="rounded-2xl border border-border bg-surface p-6">
        <h2 className="font-semibold text-lg">Favorites Across Media</h2>
        {renderGrid(favorites, 'No favorites marked yet.')}
      </section>

      {/* Local Recommendations */}
      <section className="rounded-2xl border border-border bg-surface p-6">
        <h2 className="font-semibold text-lg">Recommended From Your Local Library</h2>
        {renderGrid(recommendations, 'Add more local media to generate recommendations.')}
      </section>

      {/* Activity Timeline */}
      <section className="rounded-2xl border border-border bg-surface p-6">
        <h2 className="font-semibold text-lg">Activity Timeline</h2>
        {activity.length ? (
          <div className="mt-4 divide-y divide-border">
            {activity.map((act) => (
              <div key={act.id} className="flex items-center justify-between py-2 text-sm">
                <div>
                  <span className="font-medium text-primary">{act.title}</span>
                  <span className="ml-2 text-xs text-secondary">({act.mediaType})</span>
                  <p className="text-xs text-secondary">{act.action}</p>
                </div>
                <span className="text-xs text-secondary">
                  {new Date(act.occurredAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </span>
              </div>
            ))}
          </div>
        ) : (
          <p className="mt-2 text-sm text-secondary">No activity logged yet.</p>
        )}
      </section>
    </div>
  );
}
