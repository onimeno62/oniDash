import { useState, type FormEvent } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { ApiStatusDot } from './ApiStatusDot';
import { ThemeToggle } from './ThemeToggle';
import { pageTitleFor } from './navigation';
import { MenuIcon, SearchIcon } from './icons';

interface TopBarProps {
  onMenu: () => void;
}

export function TopBar({ onMenu }: TopBarProps) {
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const [query, setQuery] = useState('');

  const onSubmit = (event: FormEvent) => {
    event.preventDefault();
    const trimmed = query.trim();
    navigate(trimmed ? `/search?q=${encodeURIComponent(trimmed)}` : '/search');
  };

  return (
    <header className="glass sticky top-0 z-40 flex h-16 shrink-0 items-center gap-2 border-b border-border px-4 md:px-6">
      <button type="button" className="icon-btn md:hidden" aria-label="Open navigation" onClick={onMenu}>
        <MenuIcon />
      </button>
      <h1 className="text-base font-semibold tracking-tight">{pageTitleFor(pathname)}</h1>

      <form role="search" onSubmit={onSubmit} className="ml-auto hidden sm:block">
        <label htmlFor="global-search" className="sr-only">
          Global search
        </label>
        <div className="relative">
          <SearchIcon className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted" />
          <input
            id="global-search"
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search oniDash…"
            className="w-40 rounded-full border border-border bg-surface-hover/60 py-2 pl-9 pr-3 text-sm text-primary transition-[width] placeholder:text-muted focus:w-56 focus:border-accent focus:outline-none md:w-48 md:focus:w-64"
          />
        </div>
      </form>

      <ApiStatusDot />
      <ThemeToggle />
    </header>
  );
}
