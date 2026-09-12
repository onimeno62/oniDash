import type { ComponentType, SVGProps } from 'react';
import { ActivityIcon, DashboardIcon, FilmIcon, LibraryIcon, MusicIcon, SearchIcon, SlidersIcon } from './icons';
export interface NavItem { to: string; label: string; description: string; icon: ComponentType<SVGProps<SVGSVGElement>>; end?: boolean; }
export const NAV_ITEMS: NavItem[] = [
  { to: '/', label: 'Dashboard', description: 'Overview of your oniDash library.', icon: DashboardIcon, end: true },
  { to: '/library', label: 'Library', description: 'Browse and manage your media collections.', icon: LibraryIcon },
  { to: '/music', label: 'Music', description: 'Unified music dashboard: library, categories, playlists, favorites, history and insights.', icon: MusicIcon },
  { to: '/movies', label: 'Movies', description: 'Your film collection with posters and resume playback.', icon: FilmIcon },
  { to: '/manga', label: 'Manga', description: 'Manga library, reading progress, updates, categories and sources.', icon: LibraryIcon },
  { to: '/books', label: 'Books', description: 'Your bookshelf with reading progress, ratings, series and favorites.', icon: LibraryIcon },
  { to: '/unified', label: 'Unified', description: 'Cross-catalogue dashboard: continue anywhere, recent additions, favorites and activity.', icon: ActivityIcon },
  { to: '/search', label: 'Search', description: 'Global search across your entire library.', icon: SearchIcon },
  { to: '/health', label: 'API Health', description: 'Connection status of the local oniDash API.', icon: ActivityIcon },
  { to: '/settings', label: 'Settings', description: 'Appearance, libraries, plugins, and playback.', icon: SlidersIcon },
];
const TITLES = new Map(NAV_ITEMS.map((item) => [item.to, item.label]));
export function pageTitleFor(pathname: string): string { return TITLES.get(pathname) ?? 'oniDash'; }
