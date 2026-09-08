import { Link } from 'react-router-dom';
import { PageHeader } from '../components/PageHeader';
import { EmptyState } from '../components/states/EmptyState';
import { ArrowRightIcon, InboxIcon, LibraryIcon, SearchIcon } from '../components/icons';

function greetingFor(hour: number): string {
  if (hour < 5) return 'Good night';
  if (hour < 12) return 'Good morning';
  if (hour < 18) return 'Good afternoon';
  return 'Good evening';
}

const QUICK_LINKS = [
  {
    to: '/library',
    label: 'Library',
    description: 'Browse and manage your media collections once catalogues arrive.',
    icon: LibraryIcon,
  },
  {
    to: '/search',
    label: 'Global Search',
    description: 'Search across your whole library — indexing arrives in Phase 4.',
    icon: SearchIcon,
  },
  {
    to: '/health',
    label: 'API Health',
    description: 'Live connection status, version, and database of the local API.',
    icon: ArrowRightIcon,
  },
] as const;

export function DashboardPage() {
  const now = new Date();
  const greeting = `${greetingFor(now.getHours())}`;

  return (
    <div className="space-y-8">
      <PageHeader
        title={`${greeting}`}
        subtitle={now.toLocaleDateString(undefined, {
          weekday: 'long',
          year: 'numeric',
          month: 'long',
          day: 'numeric',
        })}
      />

      <section className="relative overflow-hidden rounded-2xl border border-border bg-surface-elevated p-8 shadow-card md:p-10">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0"
          style={{
            backgroundImage:
              'radial-gradient(120% 120% at 85% 0%, color-mix(in srgb, var(--accent) 22%, transparent) 0%, transparent 55%)',
          }}
        />
        <div className="relative max-w-2xl space-y-4">
          <p className="text-xs font-medium uppercase tracking-[0.14em] text-accent">
            Local-first media library
          </p>
          <h3 className="text-2xl font-semibold tracking-tight md:text-3xl">
            Welcome to oniDash
          </h3>
          <p className="text-sm leading-relaxed text-secondary md:text-[15px]">
            Your personal dashboard for music, movies, anime, manga, and books. The
            foundation is in place — catalogue modules arrive with upcoming milestones.
          </p>
          <div className="flex flex-wrap gap-3 pt-2">
            <Link
              to="/library"
              className="inline-flex items-center gap-2 rounded-lg bg-accent px-4 py-2.5 text-sm font-medium text-white shadow-card transition-colors hover:bg-accent-hover focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
            >
              Browse library
              <ArrowRightIcon className="size-4" />
            </Link>
            <Link
              to="/health"
              className="inline-flex items-center gap-2 rounded-lg border border-border bg-surface px-4 py-2.5 text-sm font-medium text-primary transition-colors hover:bg-surface-hover focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
            >
              Check API health
            </Link>
          </div>
        </div>
      </section>

      <section aria-label="Quick links" className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {QUICK_LINKS.map(({ to, label, description, icon: Icon }) => (
          <Link
            key={to}
            to={to}
            className="group rounded-xl border border-border bg-surface p-5 transition-all hover:-translate-y-0.5 hover:border-border-strong hover:shadow-card focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
          >
            <div className="mb-3 grid size-10 place-items-center rounded-lg bg-accent-soft text-accent">
              <Icon className="size-5" />
            </div>
            <p className="flex items-center gap-1.5 font-medium">
              {label}
              <ArrowRightIcon className="size-4 text-muted transition-transform group-hover:translate-x-0.5 group-hover:text-accent" />
            </p>
            <p className="mt-1.5 text-sm leading-relaxed text-secondary">{description}</p>
          </Link>
        ))}
      </section>

      <EmptyState
        icon={<InboxIcon className="size-6" />}
        title="Your library is empty"
        description="Catalogue modules (Music, Movies, Anime, Manga, Books) and library scanning arrive in upcoming milestones. Appearance and API status are available today."
      />
    </div>
  );
}
