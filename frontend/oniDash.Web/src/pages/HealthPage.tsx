import type { ReactNode } from 'react';
import { PageHeader } from '../components/PageHeader';
import { StatusBadge } from '../components/StatusBadge';
import { EmptyState } from '../components/states/EmptyState';
import { LoadingState } from '../components/states/LoadingState';
import { ErrorState } from '../components/states/ErrorState';
import { useHealth } from '../hooks/useHealth';
import { ActivityIcon, CheckCircleIcon, DatabaseIcon, RefreshIcon } from '../components/icons';

function StatTile({ label, value, badge }: { label: string; value?: string; badge?: ReactNode }) {
  return (
    <div className="rounded-xl border border-border bg-surface p-4">
      <p className="text-xs font-medium uppercase tracking-wide text-muted">{label}</p>
      <div className="mt-2">{badge ?? <p className="text-sm font-medium">{value}</p>}</div>
    </div>
  );
}

export function HealthPage() {
  const { data, loading, error, refresh } = useHealth(30_000);

  return (
    <div className="space-y-8">
      <PageHeader
        title="API Health"
        subtitle="Live connection between this dashboard and the local oniDash API. Refreshes every 30 seconds."
        actions={
          <button
            type="button"
            onClick={() => void refresh()}
            className="icon-btn"
            aria-label="Refresh health status"
            title="Refresh health status"
          >
            <RefreshIcon />
          </button>
        }
      />

      {loading && <LoadingState label="Checking API health…" />}

      {!loading && error && (
        <ErrorState
          title="API unreachable"
          message={`${error} Make sure the oniDash backend is running on http://localhost:5275.`}
          onRetry={() => void refresh()}
        />
      )}

      {!loading && !error && data && (
        <>
          {data.status === 'Unhealthy' && (
            <EmptyState
              icon={<ActivityIcon className="size-6" />}
              title="The API is up, but degraded"
              description="The backend responded, but one or more critical subsystems report problems. Details below."
            />
          )}
          <section className="rounded-2xl border border-border bg-surface p-6 shadow-card">
            <div className="grid gap-3 sm:grid-cols-2">
              <StatTile
                label="Overall status"
                badge={
                  <StatusBadge tone={data.status === 'Healthy' ? 'success' : 'danger'}>
                    {data.status}
                  </StatusBadge>
                }
              />
              <StatTile
                label="Database"
                badge={
                  <StatusBadge tone={data.databaseStatus === 'Ok' ? 'success' : 'danger'}>
                    {data.databaseStatus}
                  </StatusBadge>
                }
              />
              <StatTile label="API version" value={data.version} />
              <StatTile
                label="Last checked"
                value={new Date(data.timestamp).toLocaleString()}
              />
            </div>
            <p className="mt-5 flex items-center gap-2 text-xs text-muted">
              {data.databaseStatus === 'Ok' ? <CheckCircleIcon className="size-3.5 text-success" /> : <DatabaseIcon className="size-3.5 text-danger" />}
              {data.databaseStatus === 'Ok'
                ? 'SQLite database reachable and answering queries.'
                : 'SQLite database could not be reached; core features are degraded.'}
            </p>
          </section>
        </>
      )}
    </div>
  );
}
