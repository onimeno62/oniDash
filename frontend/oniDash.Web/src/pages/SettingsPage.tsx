import { PageHeader } from '../components/PageHeader';
import { StatusBadge } from '../components/StatusBadge';
import { useHealth } from '../hooks/useHealth';
import { useTheme, type Theme } from '../theme/ThemeProvider';
import { ClockIcon } from '../components/icons';

const THEME_CHOICES: Array<{ value: Theme; label: string }> = [
  { value: 'dark', label: 'Dark' },
  { value: 'light', label: 'Light' },
  { value: 'system', label: 'System' },
];

const COMING_SECTIONS = [
  { title: 'Libraries', description: 'Manage local library folders and scanning.' },
  { title: 'Plugins', description: 'Catalogue modules: Music, Movies, Anime, Manga, Books.' },
  { title: 'Playback', description: 'Player behaviour and persistent media controls.' },
  { title: 'Metadata', description: 'Optional external metadata providers.' },
  { title: 'Advanced', description: 'Diagnostics, database location, and maintenance.' },
] as const;

function AppearanceSection() {
  const { theme, setTheme } = useTheme();

  return (
    <section className="rounded-2xl border border-border bg-surface p-6 shadow-card">
      <h3 className="text-base font-semibold tracking-tight">Appearance</h3>
      <p className="mt-1 text-sm text-secondary">
        oniDash is dark-first. System follows your Windows preference.
      </p>

      <div
        role="group"
        aria-label="Theme"
        className="mt-4 inline-flex rounded-xl border border-border bg-surface-hover/60 p-1"
      >
        {THEME_CHOICES.map(({ value, label }) => {
          const selected = theme === value;
          return (
            <button
              key={value}
              type="button"
              aria-pressed={selected}
              onClick={() => setTheme(value)}
              className={`rounded-lg px-4 py-2 text-sm font-medium transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent ${
                selected ? 'bg-accent-soft text-accent' : 'text-secondary hover:text-primary'
              }`}
            >
              {label}
            </button>
          );
        })}
      </div>
    </section>
  );
}

function AboutSection() {
  const { data } = useHealth();

  return (
    <section className="rounded-2xl border border-border bg-surface p-6 shadow-card">
      <h3 className="text-base font-semibold tracking-tight">About</h3>
      <dl className="mt-3 space-y-2 text-sm">
        <div className="flex items-center justify-between gap-4">
          <dt className="text-secondary">Application</dt>
          <dd className="font-medium">oniDash</dd>
        </div>
        <div className="flex items-center justify-between gap-4">
          <dt className="text-secondary">API version</dt>
          <dd className="font-medium">{data?.version ?? '—'}</dd>
        </div>
        <div className="flex items-center justify-between gap-4">
          <dt className="text-secondary">Delivery</dt>
          <dd className="font-medium">Local-first · Windows</dd>
        </div>
      </dl>
    </section>
  );
}

export function SettingsPage() {
  return (
    <div className="space-y-8">
      <PageHeader
        title="Settings"
        subtitle="Appearance is available today. Remaining sections arrive with their features."
      />

      <AppearanceSection />

      <section className="overflow-hidden rounded-2xl border border-border bg-surface shadow-card">
        <h3 className="sr-only">Upcoming sections</h3>
        <ul className="divide-y divide-border">
          {COMING_SECTIONS.map(({ title, description }) => (
            <li key={title} className="flex items-center justify-between gap-4 p-5">
              <div>
                <p className="text-sm font-medium">{title}</p>
                <p className="mt-0.5 text-sm text-secondary">{description}</p>
              </div>
              <StatusBadge tone="neutral">Coming soon</StatusBadge>
            </li>
          ))}
        </ul>
      </section>

      <AboutSection />

      <p className="flex items-center gap-2 text-xs text-muted">
        <ClockIcon className="size-3.5" />
        Settings persist locally in this browser; library data stays on this machine.
      </p>
    </div>
  );
}
